using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.DB.Comparers;
using Sample.DB.Entities;
using Sample.DB.SyncUp;
using System.Linq.Expressions;

namespace Sample.DB.Extensions;

/// <summary>
/// Provides extension methods for synchronizing collections of entities with the database context.
/// </summary>
public static class DbContextSyncExtensions
{
    private static readonly int BatchSize = 1000;

    /// <summary>
    /// Synchronizes a collection of new entities with the database. Inserts new entities, updates existing ones, and optionally deletes entities not present in the new collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify entities.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="newEntities">The collection of new entities to synchronize.</param>
    /// <param name="keySelector">An expression selecting the key property or properties for identifying entities.</param>
    /// <param name="fullSync">If true, entities not present in <paramref name="newEntities"/> will be deleted from the database.</param>
    /// <param name="logger">Optional logger for logging sync operations.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A <see cref="SyncUpResult"/> containing counts of inserted, updated, deleted, and total processed entities.</returns>
    public static async Task<SyncUpResult> SyncUpAsync<TEntity, TKey>(
        this DbContext context,
        ICollection<TEntity> newEntities,
        Expression<Func<TEntity, TKey>> keySelector,
        bool fullSync = true,
        ILogger? logger = null,
        CancellationToken ct = default
    ) where TEntity : DbEntity, new()
    {
        logger ??= context.GetDefaultLogger();

        var dbSet = context.Set<TEntity>();
        var keyFunc = keySelector.Compile();
        var comparer = new DbEntityComparer<TEntity, TKey>(keySelector);

        var predicate = BuildKeyPredicate(keySelector, newEntities);

        var syncUpResult = new SyncUpResult();

        foreach (var chunk in newEntities.Chunk(BatchSize))
        {
            var dbEntities = await dbSet.Where(predicate).ToListAsync(ct);

            var group = chunk.GroupJoin(
                dbEntities,
                newEntity => keyFunc(newEntity),
                dbEntity => keyFunc(dbEntity),
                (newEntity, dbGroup) => new
                {
                    NewEntity = newEntity,
                    DbEntity = dbGroup.FirstOrDefault()
                });

            var toInsert = new List<TEntity>();
            var toUpdate = new List<TEntity>();

            foreach (var pair in group)
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
                await context.BulkInsertAsync(toInsert, BatchSize, ct: ct);
                logger?.LogInformation("Bulk inserted {ToInsertCount} {Name} records.", toInsert.Count,
                    typeof(TEntity).Name);
            }

            if (toUpdate.Count != 0)
            {
                await context.BulkUpdateAsync(toUpdate, BatchSize, ct: ct);
                logger?.LogInformation("Bulk updated {ToUpdateCount} {Name} records.", toUpdate.Count,
                    typeof(TEntity).Name);
            }

            syncUpResult.Total += chunk.Count();
            logger?.LogInformation("Processed {Total} {Name}", syncUpResult.Total, typeof(TEntity).Name);
        }

        if (fullSync)
        {
            List<int> toDelete = await dbSet
                .Where(Expression.Lambda<Func<TEntity, bool>>(Expression.Not(predicate.Body), predicate.Parameters))
                .Select(s => s.Id)
                .ToListAsync(ct);

            if (toDelete.Count != 0)
            {
                syncUpResult.Deleted = await context.BulkDeleteAsync<TEntity, int>(
                    toDelete,
                    BatchSize,
                    ct: ct);

                logger?.LogInformation("Bulk deleted {Count} {Name} records.", syncUpResult.Deleted,
                    typeof(TEntity).Name);
            }
            else
            {
                logger?.LogInformation("No records to delete for {Name}.", typeof(TEntity).Name);
            }
        }

        // Build a projection for key(s) and Id
        var dbKeyIdPairs = await dbSet
            .Where(predicate)
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

        logger?.LogInformation("Sync complete: Inserted {Inserted}, Updated {Updated}, Deleted {Deleted}",
            syncUpResult.Inserted, syncUpResult.Updated, syncUpResult.Deleted);

        return syncUpResult;
    }

    /// <summary>
    /// Extracts property names from a key selector expression.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>A read-only list of property names used in the key selector.</returns>
    /// <exception cref="ArgumentException">Thrown if the expression is not a simple property or anonymous type.</exception>
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

    /// <summary>
    /// Builds a predicate expression for filtering entities by their key values.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="entities">The collection of entities to extract key values from.</param>
    /// <returns>An expression that can be used to filter entities by their key values.</returns>
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