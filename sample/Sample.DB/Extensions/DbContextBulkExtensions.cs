using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.DB.Entities;

namespace Sample.DB.Extensions
{
    internal static class DbContextBulkExtensions
    {
        public static async Task BulkInsertAsync<TEntity>(
            this DbContext context,
            ICollection<TEntity> entities,
            int batchSize = 1000,
            ILogger? logger = null,
            CancellationToken ct = default
        ) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            logger ??= context.GetDefaultLogger();

            try
            {
                foreach (var chunk in entities.Chunk(batchSize))
                {
                    await context.Set<TEntity>().AddRangeAsync(chunk, ct);
                    await context.SaveChangesAsync(ct);

                    //foreach (var entity in chunk)
                    //    context.Entry(entity).State = EntityState.Detached;
                }
                
                logger?.LogInformation("BulkInsert: Successfully inserted {Count} entities", entities.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "BulkInsert failed");
                
                throw;
            }
        }

        public static async Task BulkUpdateAsync<TEntity>(
            this DbContext context,
            ICollection<TEntity> entities,
            int batchSize = 1000,
            ILogger? logger = null,
            CancellationToken ct = default
        ) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            logger ??= context.GetDefaultLogger();

            try
            {
                foreach (var chunk in entities.Chunk(batchSize))
                {
                    foreach (var entity in chunk)
                        context.Set<TEntity>().Update(entity);

                    await context.SaveChangesAsync(ct);

                    //foreach (var entity in chunk)
                    //    context.Entry(entity).State = EntityState.Detached;
                }

                logger?.LogInformation("BulkUpdate: Successfully updated {Count} entities", entities.Count);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "BulkUpdate failed");
                throw;
            }
        }

        public static async Task<int> BulkDeleteAsync<TEntity, TKey>(
            this DbContext context,
            IEnumerable<TKey> idsToDelete,
            int batchSize = 1000,
            ILogger? logger = null,
            CancellationToken ct = default
        )
            where TEntity : class
            where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(idsToDelete);

            var dbSet = context.Set<TEntity>();
            int totalDeleted = 0;

            logger ??= context.GetDefaultLogger();

            try
            {
                foreach (var chunk in idsToDelete.Chunk(batchSize))
                {
                    totalDeleted += await dbSet
                        .Where(e => chunk.Contains(EF.Property<TKey>(e, nameof(DbEntity.Id))))
                        .ExecuteDeleteAsync(ct);
                }

                logger?.LogInformation("BulkDelete: Successfully deleted {Count} entities", totalDeleted);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "BulkDelete failed");
                throw;
            }

            return totalDeleted;
        }
    }
}
