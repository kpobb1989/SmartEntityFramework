using Sample.Abstractions.Attributes;

using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Abstractions.DB
{
    [Table("Users")]
    public class UserEntity : DbEntity
    {
        [KeyMember]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
