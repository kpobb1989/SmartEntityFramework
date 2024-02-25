namespace Sample.Abstractions.DB.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<TEntity> Entity<TEntity>() where TEntity : DbEntity;

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
