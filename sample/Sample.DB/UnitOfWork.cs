using Sample.DB.Entities;
using Sample.DB.Interfaces;

using System.Diagnostics;

namespace Sample.DB
{
    public class UnitOfWork(SampleDbContext dbContext) : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = [];

        [DebuggerStepThrough]
        public IRepository<TEntity> Entity<TEntity>() where TEntity : DbEntity
        {
            var type = typeof(TEntity);

            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new Repository<TEntity>(dbContext);
            }

            return (IRepository<TEntity>)_repositories[type];
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await dbContext.SaveChangesAsync(cancellationToken);
    }
}
