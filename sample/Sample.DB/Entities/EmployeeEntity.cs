using Sample.DB.Attributes;

using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.DB.Entities
{
    [Table("Users")]
    public class EmployeeEntity : DbEntity
    {
        [CompositeKey]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public int? CompanyId { get; set; }

        public CompanyEntity? Company { get; set; }
    }
}
