using Sample.Abstractions.Attributes;

using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Abstractions.DB
{
    [Table("Companies")]
    public record CompanyEntity : DbEntity
    {
        [KeyMember]
        public string? Name { get; set; }

        public string? Address { get; set; }

        public ICollection<EmployeeEntity>? Employees { get; set; }
    }
}
