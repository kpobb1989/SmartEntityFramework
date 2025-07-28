using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.DB.Entities
{
    [Table("Books")]
    public class BookEntity : DbEntity
    {
        public string? Title { get; set; }

        public ICollection<AuthorEntity>? Authors { get; set; } = new List<AuthorEntity>();
    }
}
