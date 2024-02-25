
namespace Sample.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
    }
}
