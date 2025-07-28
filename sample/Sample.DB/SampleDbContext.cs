using Microsoft.EntityFrameworkCore;
using Sample.DB.Entities;
using System.Diagnostics;

namespace Sample.DB
{
    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
        {
        }


        [DebuggerStepThrough]
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Unique indx for Title
            builder.Entity<BookEntity>()
                .HasIndex(s => s.Title)
                .IsUnique();

            // Unique indx for Email
            builder.Entity<AuthorEntity>()
                .HasIndex(s => s.Email)
                .IsUnique();

            builder.Entity<AuthorBookEntity>()
                .ToTable("AuthorBooks")
                .HasKey(e => new { e.AuthorId, e.BookId });
            builder.Entity<AuthorBookEntity>().Ignore(e => e.Id);

            // Unique index for AuthorId and BookId
            builder.Entity<AuthorBookEntity>()
                .HasIndex(e => new { e.AuthorId, e.BookId })
                .IsUnique();

            builder.Entity<AuthorEntity>()
                .HasMany(s => s.Books)
                .WithMany(s => s.Authors)
                .UsingEntity<AuthorBookEntity>(
                    leftEntity => leftEntity.HasOne(s => s.Book).WithMany(),
                    rightEntity => rightEntity.HasOne(s => s.Author).WithMany()
                );
        }
    }
}