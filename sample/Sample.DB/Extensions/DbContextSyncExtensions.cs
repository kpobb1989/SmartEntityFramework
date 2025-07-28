using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.DB.Comparers;
using Sample.DB.Entities;
using Sample.DB.Options;
using Sample.DB.SyncUp;
using System.Linq.Expressions;

namespace Sample.DB.Extensions;

public static class DbContextSyncExtensions
{
    /// <summary>
    /// Syncs external data to the DB. Uses custom bulk methods and logs everything.
    /// </summary>
    public static async Task<SyncUpResult> SyncUpAsync<TEntity, TKey>(
        this DbContext context,
        ICollection<TEntity> newEntities,
        Expression<Func<TEntity, TKey>> keySelector,
        SyncUpOptions? options = null,
        CancellationToken ct = default
    ) where TEntity : DbEntity, new()
    {
        options ??= new SyncUpOptions();
        options.Logger ??= context.GetDefaultLogger();

        var dbSet = context.Set<TEntity>();
        var keyFunc = keySelector.Compile();
        var comparer = new DbEntityComparer<TEntity, TKey>(keySelector);

        var syncUpResult = new SyncUpResult();

        foreach (var chunk in newEntities.Chunk(options.BatchSize))
        {
            var predicate = BuildKeyPredicate(keySelector, chunk);

            var dbEntities = await dbSet.Where(predicate).ToListAsync(ct);

            var joinResults = chunk.GroupJoin(
                dbEntities,
                newEntity => keyFunc(newEntity),
                dbEntity => keyFunc(dbEntity),
                (newEntity, dbGroup) => new
                {
                    NewEntity = newEntity,
                    DbEntity = dbGroup
                        .DefaultIfEmpty()
                        .FirstOrDefault(dbEntity => dbEntity != null && keyFunc(dbEntity) != null)
                });

            var toInsert = new List<TEntity>();
            var toUpdate = new List<TEntity>();

            foreach (var pair in joinResults)
            {
                if (pair.DbEntity == null)
                {
                    var entity = new TEntity();
                    entity.CopyScalarFieldsFrom(pair.NewEntity);
                    toInsert.Add(entity);
                    syncUpResult.Inserted++;
                }
                else if (!comparer.Equals(pair.NewEntity, pair.DbEntity))
                {
                    pair.DbEntity.CopyScalarFieldsFrom(pair.NewEntity);
                    toUpdate.Add(pair.DbEntity);
                    syncUpResult.Updated++;
                }
            }

            if (toInsert.Count != 0)
            {
                await context.BulkInsertAsync(toInsert, options.BatchSize, ct: ct);
                options.Logger?.LogInformation("Bulk inserted {ToInsertCount} {Name} records.", toInsert.Count,
                    typeof(TEntity).Name);
            }

            if (toUpdate.Count != 0)
            {
                await context.BulkUpdateAsync(toUpdate, options.BatchSize, ct: ct);
                options.Logger?.LogInformation("Bulk updated {ToUpdateCount} {Name} records.", toUpdate.Count,
                    typeof(TEntity).Name);
            }

            syncUpResult.Total += chunk.Count();
            options.Logger?.LogInformation("Processed {Total} {Name}", syncUpResult.Total, typeof(TEntity).Name);
        }

        // Deletes after all batches
        if (options.FullSync)
        {
            var predicate = BuildKeyPredicate(keySelector, newEntities);

            List<int> toDelete = await dbSet
                .Where(Expression.Lambda<Func<TEntity, bool>>(Expression.Not(predicate.Body), predicate.Parameters))
                .Select(s => s.Id)
                .ToListAsync(ct);

            if (toDelete.Count != 0)
            {
                syncUpResult.Deleted = await context.BulkDeleteAsync<TEntity, int>(
                    toDelete,
                    options.BatchSize,
                    ct: ct);

                options.Logger?.LogInformation("Bulk deleted {Count} {Name} records.", syncUpResult.Deleted,
                    typeof(TEntity).Name);
            }
            else
            {
                options.Logger?.LogInformation("No records to delete for {Name}.", typeof(TEntity).Name);
            }
        }

        var keyPropertyNames = GetPropertyNamesFromExpression(keySelector);
        var predicate2 = BuildKeyPredicate(keySelector, newEntities);

        // Build a projection for key(s) and Id
        var dbKeyIdPairs = await dbSet
            .Where(predicate2)
            .Select(e => new { Key = keyFunc(e), e.Id })
            .ToListAsync(ct);

        // Map IDs back to newEntities
        foreach (var newEntity in newEntities)
        {
            var key = keyFunc(newEntity);
            var match = dbKeyIdPairs.FirstOrDefault(x => x.Key?.Equals(key) == true);
            if (match != null)
            {
                newEntity.Id = match.Id;
            }
            context.Entry(newEntity).State = EntityState.Detached;
        }

        options.Logger?.LogInformation("Sync complete: Inserted {Inserted}, Updated {Updated}, Deleted {Deleted}",
            syncUpResult.Inserted, syncUpResult.Updated, syncUpResult.Deleted);

        return syncUpResult;
    }

    private static IReadOnlyList<string> GetPropertyNamesFromExpression<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector.Body is MemberExpression memberExpr)
            return [memberExpr.Member.Name];
        if (keySelector.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression member)
            return [member.Member.Name];
        if (keySelector.Body is NewExpression newExpr)
            return newExpr.Members.Select(m => m.Name).ToArray();
        throw new ArgumentException("Only simple property or anonymous type expressions are supported");
    }

    private static Expression<Func<TEntity, bool>> BuildKeyPredicate<TEntity, TKey>(
        Expression<Func<TEntity, TKey>> keySelector,
        IEnumerable<TEntity> entities)
    {
        var keyPropertyNames = GetPropertyNamesFromExpression(keySelector);
        var keyFunc = keySelector.Compile();
        var chunkKeys = entities
            .Select(keyFunc)
            .Where(key => key != null)
            .ToList();

        var param = Expression.Parameter(typeof(TEntity), "s");

        if (keyPropertyNames.Count == 1)
        {
            var propName = keyPropertyNames[0];
            var property = Expression.Property(param, propName);

            var keyValues = chunkKeys.Select(k => k).ToList();
            var containsMethod = typeof(List<TKey>).GetMethod("Contains", [typeof(TKey)])!;
            var keysExpr = Expression.Constant(keyValues);

            var body = Expression.Call(keysExpr, containsMethod, property);
            return Expression.Lambda<Func<TEntity, bool>>(body, param);
        }
        else
        {
            Expression? body = null;
            foreach (var chunkKey in chunkKeys)
            {
                Expression? andExpr = null;
                foreach (var propName in keyPropertyNames)
                {
                    var entityProp = Expression.Property(param, propName);
                    var keyValue = typeof(TKey).GetProperty(propName)!.GetValue(chunkKey);
                    var keyProp = Expression.Constant(keyValue, entityProp.Type);
                    var equals = Expression.Equal(entityProp, keyProp);

                    if (andExpr == null)
                        andExpr = equals;
                    else
                        andExpr = Expression.AndAlso(andExpr, equals);
                }

                if (andExpr != null)
                {
                    if (body == null)
                        body = andExpr;
                    else
                        body = Expression.OrElse(body, andExpr);
                }
            }
            return Expression.Lambda<Func<TEntity, bool>>(body ?? Expression.Constant(false), param);
        }
    }

}