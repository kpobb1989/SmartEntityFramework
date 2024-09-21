namespace Sample.DB.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IgnoreCompareAttribute : Attribute
    {
    }
}
