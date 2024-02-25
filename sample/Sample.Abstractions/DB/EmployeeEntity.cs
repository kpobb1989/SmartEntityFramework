using Sample.Abstractions.Attributes;

using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Abstractions.DB
{
    [Table("Users")]
    public class EmployeeEntity : DbEntity
    {
        [KeyMember]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public int? CompanyId { get; set; }

        public CompanyEntity? Company { get; set; }
    }
}
