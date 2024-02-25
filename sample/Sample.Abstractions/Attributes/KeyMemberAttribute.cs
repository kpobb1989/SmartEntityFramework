namespace Sample.Abstractions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class KeyMemberAttribute : Attribute
    {
    }
}
