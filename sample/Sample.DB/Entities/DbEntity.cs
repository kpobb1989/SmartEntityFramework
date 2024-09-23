using Sample.DB.Attributes;

namespace Sample.DB.Entities
{
    public class DbEntity
    {
        [IgnoreCompare]
        public int Id { get; set; }

        public string? Hash { get; set; }
    }
}