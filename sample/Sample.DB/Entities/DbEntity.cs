using Sample.DB.Attributes;

using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sample.DB.Entities
{
    public class DbEntity
    {
        [IgnoreCompare]
        public int Id { get; set; }

        public string? Hash { get; set; }

        public override string ToString()
        {
            var result = GetType()
               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(s => s.CanRead && !IsNavigationProperty(s) && !Attribute.IsDefined(s, typeof(IgnoreCompareAttribute)))
               .ToDictionary(kvp => kvp.Name, kvp => kvp.GetValue(this));

            return JsonSerializer.Serialize(result);
        }

        public override bool Equals(object? obj)
        {
            if (obj is DbEntity other)
            {
                return ToString().Equals(other.ToString());
            }

            return false;
        }

        public override int GetHashCode()
            => ToString().GetHashCode();

        public static bool operator ==(DbEntity x, DbEntity y)
            => x.ToString().Equals(y.ToString());

        public static bool operator !=(DbEntity x, DbEntity y)
            => !(x == y);

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
