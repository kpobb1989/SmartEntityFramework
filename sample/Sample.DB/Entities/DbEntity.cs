using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Sample.DB.Entities
{
    public abstract class DbEntity
    {
        public int Id { get; set; }
        
        public void CopyScalarFieldsFrom(DbEntity source, bool copyId = false)
        {
            foreach (var prop in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (!copyId && prop.Name == nameof(Id)) continue;
                if (IsCollectionOrNavigation(prop)) continue;
                prop.SetValue(this, prop.GetValue(source));
            }
        }

        public void ClearNavigationProperties()
        {
            foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanWrite) continue;
                if (IsCollectionOrNavigation(prop))
                {
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                    {
                        // For collections, set to empty array or list if possible
                        if (prop.PropertyType.IsArray)
                        {
                            prop.SetValue(this, Array.CreateInstance(prop.PropertyType.GetElementType()!, 0));
                        }
                        else if (prop.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                        {
                            prop.SetValue(this, Activator.CreateInstance(prop.PropertyType));
                        }
                        else
                        {
                            prop.SetValue(this, null);
                        }
                    }
                    else
                    {
                        // For navigation properties, set to null
                        prop.SetValue(this, null);
                    }
                }
            }
        }

        private static bool IsCollectionOrNavigation(PropertyInfo prop)
        {
            if (prop.PropertyType == typeof(string))
                return false;
            if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                return true;
            var type = prop.PropertyType;
            
            return type.IsClass && type != typeof(string);
        }
    }
}