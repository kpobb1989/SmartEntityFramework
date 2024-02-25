using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Sample.Abstractions.Attributes;
using Sample.Abstractions.DB;
using Sample.Abstractions.DB.Interfaces;

using System.Linq.Expressions;
using System.Reflection;

namespace Sample.DB
{
    public class Repository<TEntity>(SampleDbContext dbContext) : IRepository<TEntity> where TEntity : DbEntity
    {
        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            bool includeAll = false,
            Expression<Func<TEntity, object?[]>>? include = null,
            bool readOnly = true,
            CancellationToken ct = default)
            => await GetQueryable(filter, orderBy: null, includeAll, include, readOnly).FirstOrDefaultAsync(ct);

        public async Task<IEnumerable<TEntity>> ToListAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool includeAll = false,
            Expression<Func<TEntity, object?[]>>? include = null,
            bool readOnly = true,
            CancellationToken ct = default)
            => await GetQueryable(filter, orderBy, includeAll, include, readOnly).ToListAsync(ct);

        public async Task<long> CountAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            CancellationToken ct = default)
            => await GetQueryable(filter).LongCountAsync(ct);

        public IQueryable<TEntity> GetQueryable(
            Expression<Func<TEntity, bool>>? filter = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool includeAll = false,
            Expression<Func<TEntity, object?[]>>? include = null,
            bool readOnly = true)
        {
            IQueryable<TEntity> query = dbContext.Set<TEntity>();

            if (readOnly)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = query.OrderBy(orderBy);
            }

            if (includeAll)
            {
                foreach (var entityName in GetNavigationProperties(typeof(TEntity)).Where(s => !s.IsCollection).Select(s => s.Name))
                {
                    query = query.Include(entityName);
                }
            }

            if (include != null && include.Body is NewArrayExpression array)
            {
                var includeProperties = array.Expressions
                    .Select(expression => expression as MemberExpression)
                    .Where(expression => expression != null)
                    .Select(expression => expression!.Member.Name)
                    .ToArray();

                var navigationProperties = GetNavigationProperties(typeof(TEntity))
                                            .Select(s => s.Name)
                                            .ToArray();

                var entitiesToInclude = navigationProperties.Join(includeProperties, nav => nav, input => input, (nav, _) => nav);

                foreach (var entityName in entitiesToInclude)
                {
                    query = query.Include(entityName);
                }
            }

            return query;
        }

        public async Task SyncUpAsync(
            IEnumerable<TEntity> newEntities,
            bool deleteUnmatch = true,
            CancellationToken ct = default)
        {
            foreach (var entity in newEntities.Where(s => HasNavigationPropertiesWithValue(s)))
            {
                throw new Exception("Navigation properties can not be synced up");
            }

            var dbEntities = await ToListAsync(ct: ct);

            var keySelector = CreateKeySelector();

            var entitiesToAdd = newEntities.ExceptBy(dbEntities.Select(keySelector), keySelector).ToList();

            Add(entitiesToAdd);

            var entitiesToUpdate = dbEntities
                .Join(newEntities, keySelector, keySelector, (dbEntity, newEntity) => (dbEntity, newEntity))
                .Where(group => group.dbEntity != group.newEntity)
                .Select(group => new { HasPropsToUpdate = Update(group.dbEntity, group.newEntity), DbEntity = group.dbEntity })
                .Where(group => group.HasPropsToUpdate)
                .Select(group => group.DbEntity)
                .ToList();

            Update(entitiesToUpdate);

            if (deleteUnmatch)
            {
                var entitiesToDelete = dbEntities.ExceptBy(newEntities.Select(keySelector), keySelector).ToList();

                Remove(entitiesToDelete);
            }
        }

        public void Add(TEntity entity)
            => dbContext.Set<TEntity>().Add(entity);

        public void Add(IEnumerable<TEntity> entities)
            => dbContext.Set<TEntity>().AddRange(entities);

        public void Update(TEntity entity)
        {
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.Set<TEntity>().Attach(entity);
            }

            dbContext.Entry(entity).State = EntityState.Modified;
        }

        public void Update(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public void Remove(TEntity entity)
        {
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.Set<TEntity>().Attach(entity);
            }

            dbContext.Set<TEntity>().Remove(entity);
        }

        public void Remove(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Remove(entity);
            }
        }

        private static Func<TEntity, object> CreateKeySelector()
        {
            var keyProperties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => Attribute.IsDefined(prop, typeof(KeyMemberAttribute))).ToArray();

            // Create parameter expression for the entity
            ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "s");

            // Create member bindings for each key property
            var bindings = keyProperties.Select(property =>
            {
                MemberExpression propertyExpression = Expression.Property(parameter, property);
                UnaryExpression convertExpression = Expression.Convert(propertyExpression, property.PropertyType);
                return Expression.Bind(property, convertExpression);
            });

            // Create anonymous type initializer
            var anonymousType = Expression.New(typeof(TEntity));
            var initializer = Expression.MemberInit(anonymousType, bindings);

            // Create lambda expression
            var lambda = Expression.Lambda<Func<TEntity, object>>(initializer, parameter);

            return lambda.Compile();
        }

        private bool Update(TEntity dbEntity, TEntity newEntity)
        {
            var dbProperties = GetPrimitiveProperties(dbEntity);
            var newProperties = GetPrimitiveProperties(newEntity);

            var propsTouUpdate = dbProperties.Join(newProperties, dbProperty => dbProperty.Name, newProperty => newProperty.Name, (dbProperty, newProperty) => (DbProperty: dbProperty, NewProperty: newProperty))
                        .Where(group => !Attribute.IsDefined(group.DbProperty, typeof(IgnoreMemberAttribute)) && !Attribute.IsDefined(group.DbProperty, typeof(KeyMemberAttribute)))
                        .Select(group => (group.DbProperty, dbValue: group.DbProperty.GetValue(dbEntity), newValue: group.NewProperty.GetValue(newEntity)))
                        .Where(group => group.dbValue != null && !group.dbValue.Equals(group.newValue) || group.dbValue == null && group.newValue != null)
                        .Select(group => (group.DbProperty, NewValue: group.newValue))
                        .ToList();

            propsTouUpdate.ForEach(group =>
            {
                group.DbProperty.SetValue(dbEntity, group.NewValue);
            });

            return propsTouUpdate.Count > 0;
        }

        private List<INavigation> GetNavigationProperties(Type type)
            => dbContext.Model.FindEntityType(type)!.GetNavigations().ToList();

        private bool HasNavigationPropertiesWithValue(TEntity dbEntity)
        {
            var nagivationProperties = GetNavigationProperties(typeof(TEntity));

            var props = dbEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var hasValue = nagivationProperties.Join(props, nav => nav.Name, prop => prop.Name, (nav, prop) => prop.GetValue(dbEntity))
                .Where(s => s != null);

            return hasValue.Any();
        }

        private IEnumerable<PropertyInfo> GetPrimitiveProperties(TEntity dbEntity)
        {
            var nagivationProperties = GetNavigationProperties(typeof(TEntity)).Select(s => s.Name);

            var primitiveProps = dbEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ExceptBy(nagivationProperties, s => s.Name);

            return primitiveProps;
        }
    }
}
