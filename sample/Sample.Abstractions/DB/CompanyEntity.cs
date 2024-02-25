using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Abstractions.DB
{
    [Table("Companies")]
    public class CompanyEntity : DbEntity
    {
        public string? Name { get; set; }

        public string? Address { get; set; }

        public ICollection<EmployeeEntity> Employees { get; set; } = new List<EmployeeEntity>();
    }
}
