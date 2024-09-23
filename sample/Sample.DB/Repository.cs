using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Sample.DB.Attributes;
using Sample.DB.Entities;
using Sample.DB.Interfaces;

using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace Sample.DB
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : DbEntity
    {
        private readonly SampleDbContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;

        [DebuggerStepThrough]
        public Repository(SampleDbContext dbContext)
        {
            _dbContext = dbContext;

            _dbSet = _dbContext.Set<TEntity>();
        }

        public void Create(TEntity entity)
        {
            entity.Hash = GetHash(entity);

            _dbSet.Add(entity);
        }

        public void Create(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.Hash = GetHash(entity);
            }

            _dbSet.AddRange(entities);
        }

        public void Update(TEntity entity)
        {
            entity.Hash = GetHash(entity);

            if (_dbContext.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }

            _dbContext.Entry(entity).State = EntityState.Modified;
        }

        public void Update(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public int Delete()
            => _dbSet.ExecuteDelete();

        public int Delete(TEntity entity)
            => _dbSet.Where(s => s.Id == entity.Id).ExecuteDelete();

        public int Delete(IEnumerable<TEntity> entities)
        {
            var ids = entities.Select(s => s.Id).ToList();

            return _dbSet.Where(s => ids.Contains(s.Id)).ExecuteDelete();
        }

        public IQueryable<TEntity> GetQueryable(
            Expression<Func<TEntity, bool>>? filter = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool includeAll = false,
            Expression<Func<TEntity, object?[]>>? include = null,
            bool readOnly = true)
        {
            IQueryable<TEntity> query = _dbSet;

            if (readOnly)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = query.OrderBy(orderBy);

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

        public async Task RefreshAsync(
            IEnumerable<TEntity> newEntities,
            bool deleteUnmatch = false,
            CancellationToken ct = default)
        {
            if (newEntities.Any(HasNavigationPropertyWithValue))
            {
                throw new NotSupportedException("Navigation property can not be refreshed");
            }

            foreach (var entity in newEntities)
            {
                entity.Hash = GetHash(entity);
            }

            var dbEntitiesQuery = GetQueryable();

            var keySelector = CreateKeySelector();

            var entitiesToAdd = newEntities.ExceptBy(dbEntitiesQuery.Select(keySelector.Compile()), keySelector.Compile()).ToList();

            Create(entitiesToAdd);

            var entitiesToUpdate = newEntities.Join(dbEntitiesQuery, keySelector.Compile(), keySelector.Compile(), (newEntity, dbEntity) => (newEntity, dbEntity))
                .Where(s => s.dbEntity.Hash != s.newEntity.Hash)
                .ToList();

            foreach (var (newEntity, dbEntity) in entitiesToUpdate)
            {
                Update(dbEntity, newEntity);
            }

            if (deleteUnmatch)
            {
                var entitiesToDelete = dbEntitiesQuery.ExceptBy(newEntities.Select(keySelector.Compile()), keySelector).ToList();

                Delete(entitiesToDelete);
            }

            await _dbContext.SaveChangesAsync(ct);

            newEntities
                .Join(dbEntitiesQuery, keySelector.Compile(), keySelector.Compile(), (newEntity, dbEntity) => new { NewEntity = newEntity, DbEntity = dbEntity })
                .ToList()
               .ForEach(group => group.NewEntity.Id = group.DbEntity.Id);
        }

        private List<INavigation> GetNavigationProperties(Type type)
            => _dbContext.Model.FindEntityType(type)!.GetNavigations().ToList();

        private bool HasNavigationPropertyWithValue(TEntity dbEntity)
        {
            var nagivationProperties = GetNavigationProperties(typeof(TEntity));

            var props = dbEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var hasValue = nagivationProperties.Join(props, nav => nav.Name, prop => prop.Name, (nav, prop) => prop.GetValue(dbEntity))
                .Where(s => s != null);

            return hasValue.Any();
        }

        private static Expression<Func<TEntity, object>> CreateKeySelector()
        {
            var parameter = Expression.Parameter(typeof(TEntity), "s");

            var members = typeof(TEntity)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => Attribute.IsDefined(prop, typeof(CompositeKeyAttribute)))
                .ToArray();

            if (members.Length == 0)
            {
                throw new InvalidOperationException("No properties marked with CompositeKeyAttribute found.");
            }

            Type tupleType = Type.GetType($"System.ValueTuple`{members.Length}")!;

            if (tupleType == null)
            {
                throw new InvalidOperationException($"Tuple type for arity {members.Length} could not be found.");
            }

            var genericArgs = members.Select(m => m.PropertyType).ToArray();
            var constructedTupleType = tupleType.MakeGenericType(genericArgs);

            var propertyExpressions = members.Select(m => Expression.Property(parameter, m)).ToArray();

            var body = Expression.New(constructedTupleType.GetConstructor(genericArgs)!, propertyExpressions);

            return Expression.Lambda<Func<TEntity, object>>(Expression.Convert(body, typeof(object)), parameter);
        }

        private int Update(TEntity dbEntity, TEntity newEntity)
        {
            var dbProperties = GetPrimitiveProperties(dbEntity);
            var newProperties = GetPrimitiveProperties(newEntity);

            var propsTouUpdate = dbProperties.Join(newProperties, dbProperty => dbProperty.Name, newProperty => newProperty.Name, (dbProperty, newProperty) => (DbProperty: dbProperty, NewProperty: newProperty))
                        .Where(group => !Attribute.IsDefined(group.DbProperty, typeof(IgnoreCompareAttribute)) && !Attribute.IsDefined(group.DbProperty, typeof(CompositeKeyAttribute)))
                        .Select(group => (group.DbProperty, DbValue: group.DbProperty.GetValue(dbEntity), NewValue: group.NewProperty.GetValue(newEntity)))
                        .Where(s => !object.Equals(s.DbValue, s.NewValue))
                        .ToList();

            propsTouUpdate.ForEach(group =>
            {
                group.DbProperty.SetValue(dbEntity, group.NewValue);
            });

            if (propsTouUpdate.Count > 0)
            {
                if (_dbContext.Entry(dbEntity).State == EntityState.Detached)
                {
                    _dbSet.Attach(dbEntity);
                }

                _dbContext.Entry(dbEntity).State = EntityState.Modified;
            }

            return propsTouUpdate.Count;
        }

        private IEnumerable<PropertyInfo> GetPrimitiveProperties(TEntity dbEntity)
        {
            var nagivationProperties = GetNavigationProperties(typeof(TEntity)).Select(s => s.Name);

            var primitiveProps = dbEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ExceptBy(nagivationProperties, s => s.Name);

            return primitiveProps;
        }

        private static bool IsNavigationProperty(PropertyInfo property)
        {
            // Check if the type is a primitive or value type (not a navigation property)
            if (property.PropertyType.IsPrimitive || property.PropertyType.IsValueType || property.PropertyType == typeof(string))
            {
                return false;
            }

            // Check if it's a collection (which is likely a navigation property)
            if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
            {
                return true;
            }

            // Check if it's a class (which might be a navigation property)
            if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                return true;
            }

            return false;
        }

        private static string GetHash(object input)
        {
            var result = input.GetType()
               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(s => s.CanRead && !IsNavigationProperty(s) && !Attribute.IsDefined(s, typeof(IgnoreCompareAttribute)) && s.Name != nameof(DbEntity.Hash))
               .ToDictionary(kvp => kvp.Name, kvp => kvp.GetValue(input));

            var json = JsonSerializer.Serialize(result);

            byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(json));

            StringBuilder sb = new();

            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
