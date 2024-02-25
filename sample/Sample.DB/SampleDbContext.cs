using Microsoft.EntityFrameworkCore;
using Sample.Abstractions;
using Sample.Abstractions.DB;

namespace Sample.DB
{
    public class SampleDbContext : DbContext
    {
        // Empty constructor is required for running EF CLI
        public SampleDbContext()
        {
        }

        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(AppSettings.DbConnectionString);

            // optionsBuilder.UseInMemoryDatabase("SampleDatabase");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<CompanyEntity>()
                .HasMany(s => s.Employees)
                .WithOne(s => s.Company)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            //builder.Entity<Package>()
            //    .HasKey(s => new { s.Source, s.Name, s.Version });

            //builder.Entity<License>()
            //    .HasKey(s => s.Name);

            //builder.Entity<Package>()
            //    .HasMany(s => s.Licenses)
            //    .WithMany(s => s.Packages)
            //    .UsingEntity<PackageToLicense>(
            //        rightEntity => rightEntity.HasOne(s => s.License)
            //                                  .WithMany()
            //                                  .HasForeignKey(s => s.LicenseName),
            //        leftEntity => leftEntity.HasOne(s => s.Package)
            //                                .WithMany()
            //                                .HasForeignKey(s => new { s.PackageSource, s.PackageName, s.PackageVersion })
            //        );
        }
    }
}