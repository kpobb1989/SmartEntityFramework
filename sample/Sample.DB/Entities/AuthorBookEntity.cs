namespace Sample.DB.Entities;

public class AuthorBookEntity : DbEntity
{
    public int AuthorId { get; set; }
    public int BookId { get; set; }
    
    public AuthorEntity? Author { get; set; }  
    public BookEntity? Book { get; set; }
}