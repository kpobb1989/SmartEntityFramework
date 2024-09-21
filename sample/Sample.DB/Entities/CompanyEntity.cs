using Sample.DB.Attributes;

using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.DB.Entities
{
    [Table("Companies")]
    public class CompanyEntity : DbEntity
    {
        [CompositeKey]
        public string? Name { get; set; }

        public string? Address { get; set; }
        public int? Zip { get; set; }

        public ICollection<EmployeeEntity>? Employees { get; set; }
    }
}
