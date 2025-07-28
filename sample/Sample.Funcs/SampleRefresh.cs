using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Sample.DB;
using Sample.DB.Entities;
using Sample.DB.Extensions;
using Sample.DB.Options;

namespace Sample.Funcs
{
    public class SampleRefresh(SampleDbContext dbContext)
    {
        [Function(nameof(SampleRefresh_HttpTrigger))]
        public async Task SampleRefresh_HttpTrigger([HttpTrigger(AuthorizationLevel.Function, "get")] FunctionContext context, CancellationToken ct)
        {
            var json = @"
[
    {
        ""firstName"": ""Joanne1"",
        ""lastName"": ""Rowling"",
        ""email"": ""joanne.rowling@example.com"",
        ""books"": [
            {
                ""title"": ""Harry Potter and the Philosopher's Stone1""
            },
            {
                ""title"": ""Harry Potter and the Chamber of Secrets2""
            }
        ]
    },
    {
        ""firstName"": ""J.R.R."",
        ""lastName"": ""Tolkien"",
        ""email"": ""tolkien@example.com"",
        ""books"": [
            {
                ""title"": ""The Hobbit""
            },
            {
                ""title"": ""The Lord of the Rings""
            }
        ]
    }
]";

            var dtoAuthors = JsonSerializer.Deserialize<AuthorDto[]>(json,
                                 new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                             ?? Enumerable.Empty<AuthorDto>();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var syncUpOptions = new SyncUpOptions()
                {
                    Logger = context.GetLogger<SampleRefresh>()
                };

                // Sync Authors
                var authors = dtoAuthors.Select(authorDto => new AuthorEntity
                {
                    FirstName = authorDto.FirstName,
                    LastName = authorDto.LastName,
                    Email = authorDto.Email,
                    Books = authorDto.Books?.Select(bookDto => new BookEntity
                    {
                        Title = bookDto.Title
                    }).ToList() ?? []
                }).ToList();
                await dbContext.SyncUpAsync(authors, a => a.Email, syncUpOptions, ct: ct);

                // Sync Books
                var books = authors.SelectMany(a => a.Books).ToList();
                await dbContext.SyncUpAsync(books, b => b.Title, syncUpOptions, ct: ct);

                // Sync AuthorBooks
                var authorBooks = authors.SelectMany(a => a.Books, (a, b) => new AuthorBookEntity() { AuthorId = a.Id, BookId = b.Id }).ToList();
                await dbContext.SyncUpAsync(authorBooks, ab => new { ab.AuthorId, ab.BookId }, syncUpOptions, ct: ct);

                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }
        }


        // DTO definitions
        record AuthorDto
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public BookDto[]? Books { get; set; } = [];
        }

        record BookDto
        {
            public string? Title { get; set; }
        }
    }
}