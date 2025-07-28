using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.DB.Entities
{
    [Table("Authors")]
    public class AuthorEntity : DbEntity
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }

        public ICollection<BookEntity> Books { get; set; } = new List<BookEntity>();
    }
}
