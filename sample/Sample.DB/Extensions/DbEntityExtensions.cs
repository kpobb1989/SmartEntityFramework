using System.Collections;
using System.Reflection;
using Sample.DB.Entities;

namespace Sample.DB.Extensions;

public static class DbEntityExtensions
{
    internal static void CopyFieldsFrom(this DbEntity destination, DbEntity source, bool copyId = false)
    {
        foreach (var prop in typeof(DbEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (!copyId && prop.Name == nameof(DbEntity.Id)) continue;
            if (IsCollectionOrNavigation(prop)) continue;
            prop.SetValue(destination, prop.GetValue(source));
        }
    }

    internal static bool IsCollectionOrNavigation(PropertyInfo prop)
    {
        if (prop.PropertyType == typeof(string))
            return false;
        if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
            return true;
        var type = prop.PropertyType;

        return type.IsClass && type != typeof(string);
    }
}