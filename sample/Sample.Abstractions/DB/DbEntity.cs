using Sample.Abstractions.Attributes;

using System.Reflection;

namespace Sample.Abstractions.DB
{
    public class DbEntity : IEquatable<DbEntity>
    {
        public record ValueObject(object? Value = null);

        [IgnoreMember]
        public int Id { get; set; }

        public bool Equals(DbEntity? other)
            => GetCompositeKey(this).SequenceEqual(GetCompositeKey(other));

        public override bool Equals(object? obj) => Equals(obj as DbEntity);

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var item in GetCompositeKey(this))
            {
                hash = hash * 23 + (item.Value?.GetHashCode() ?? 0);
            }
            return hash;
        }

        public static bool operator ==(DbEntity obj1, DbEntity obj2) => obj1.Equals(obj2);

        public static bool operator !=(DbEntity obj1, DbEntity obj2) => !(obj1 == obj2);

        private static IEnumerable<ValueObject> GetCompositeKey(object? obj)
        {
            return obj?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(s => !Attribute.IsDefined(s, typeof(IgnoreMemberAttribute)))
                                 .Select(s => new ValueObject(s.GetValue(obj)))
                                 .ToList() ?? Enumerable.Empty<ValueObject>();
        }
    }
}
