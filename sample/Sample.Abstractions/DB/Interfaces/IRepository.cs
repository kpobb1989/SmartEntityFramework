using System.Linq.Expressions;

namespace Sample.Abstractions.DB.Interfaces
{
    public interface IRepository<TEntity> where TEntity : DbEntity
    {
        Task<TEntity?> FirstOrDefaultAsync(
          Expression<Func<TEntity, bool>>? filter = null,
          bool includeAll = false,
          Expression<Func<TEntity, object?[]>>? include = null,
          bool asNoTracking = true,
          CancellationToken ct = default);

        Task<IEnumerable<TEntity>> ToListAsync(
          Expression<Func<TEntity, bool>>? filter = null,
          Expression<Func<TEntity, object>>? orderBy = null,
          bool includeAll = false,
          Expression<Func<TEntity, object?[]>>? include = null,
          bool asNoTracking = true,
          CancellationToken ct = default);

        Task SyncUpAsync(
            IEnumerable<TEntity> newData,
            bool deleteUnmatch = true,
            CancellationToken ct = default);

        IQueryable<TEntity> GetQueryable(
               Expression<Func<TEntity, bool>>? filter = null,
               Expression<Func<TEntity, object>>? orderBy = null,
               bool includeAll = false,
               Expression<Func<TEntity, object?[]>>? include = null,
               bool asNoTracking = true);

        void Add(TEntity entity);

        void Add(IEnumerable<TEntity> entities);

        void Update(TEntity entity);

        void Update(IEnumerable<TEntity> entities);

        void Remove(TEntity entity);

        void Remove(IEnumerable<TEntity> entities);

        Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default);
    }
}
