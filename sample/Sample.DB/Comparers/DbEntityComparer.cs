using System.Linq.Expressions;
using System.Reflection;
using Sample.DB.Entities;
using Sample.DB.Extensions;

namespace Sample.DB.Comparers
{
    public class DbEntityComparer<TEntity, TKey> : IEqualityComparer<TEntity> where TEntity : class
    {
        private readonly PropertyInfo[] _props;

        /// <summary>
        /// Comparer for entity objects that allows custom key selection and excludes specific properties.
        /// </summary>
        public DbEntityComparer(Expression<Func<TEntity, TKey>>? keySelector = null, bool excludeId = true)
        {
            var excludeList = new HashSet<string>();
            
            if (excludeId)
                excludeList.Add(nameof(DbEntity.Id));
            
            if (keySelector != null)
            {
                foreach (var name in GetPropertyNames(keySelector))
                    excludeList.Add(name);
            }

            _props = typeof(TEntity)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p =>
                    p is { CanRead: true, CanWrite: true } &&
                    !excludeList.Contains(p.Name) &&
                    !DbEntityExtensions.IsCollectionOrNavigation(p))
                .ToArray();
        }

        public bool Equals(TEntity? x, TEntity? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            foreach (var prop in _props)
            {
                var left = prop.GetValue(x);
                var right = prop.GetValue(y);
                if (!(left?.Equals(right) ?? right == null))
                    return false;
            }

            return true;
        }

        public int GetHashCode(TEntity? obj)
        {
            if (obj == null) return 0;
            var hash = 17;
            foreach (var prop in _props)
            {
                var value = prop.GetValue(obj);
                hash = hash * 23 + (value?.GetHashCode() ?? 0);
            }
            return hash;
        }
        
        private static string[] GetPropertyNames(Expression<Func<TEntity, TKey>> keySelector)
        {
            if (keySelector.Body is MemberExpression memberExpr)
                return [memberExpr.Member.Name];
            if (keySelector.Body is NewExpression newExpr)
                return newExpr.Members?.Select(m => m.Name).ToArray() ?? [];
            return [];
        }
    }
}
