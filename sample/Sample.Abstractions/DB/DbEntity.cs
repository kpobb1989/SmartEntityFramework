using Sample.Abstractions.Attributes;

namespace Sample.Abstractions.DB
{
    public record DbEntity
    {
        [IgnoreMember]
        public int Id { get; set; }
    }
}
