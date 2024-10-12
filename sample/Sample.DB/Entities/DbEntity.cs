using Sample.DB.Attributes;

using System.Reflection;
using System.Collections;
using System.Diagnostics;

namespace Sample.DB.Entities
{
    public class DbEntity: IEquatable<DbEntity>
    {
        [IgnoreCompare]
        public int Id { get; set; }

        [DebuggerStepThrough]
        public override bool Equals(object? obj)
        {
            return this.Equals(obj as DbEntity);
        }

        [DebuggerStepThrough]
        public bool Equals(DbEntity? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            var properties = GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IsNavigationProperty(p) && !Attribute.IsDefined(p, typeof(IgnoreCompareAttribute)));

            return properties.All(p => object.Equals(p.GetValue(this), p.GetValue(other)));
        }


        [DebuggerStepThrough]
        public static bool operator ==(DbEntity left, DbEntity right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }


        [DebuggerStepThrough]
        public static bool operator !=(DbEntity left, DbEntity right)
        {
            return !(left == right);
        }

        [DebuggerStepThrough]
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                var properties = GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && !IsNavigationProperty(p) && !Attribute.IsDefined(p, typeof(IgnoreCompareAttribute)));

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(this);
                    if (value != null)
                    {
                        hash = hash * 31 + value.GetHashCode();
                    }
                }

                return hash;
            }
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
    }
}