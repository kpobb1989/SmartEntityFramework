using Sample.DB.Entities;

namespace Sample.DB.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<TEntity> Entity<TEntity>() where TEntity : DbEntity;

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
