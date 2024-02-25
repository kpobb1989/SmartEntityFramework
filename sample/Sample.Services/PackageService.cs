using Sample.Abstractions.Services;
using Sample.Abstractions.Services.Interfaces;

namespace Sample.Services
{
    public class PackageService : IPackageService
    {
        // Mock
        public PagedList<Package> GetPackages(int pageNumber, int pageSize)
        {
            var data = new List<Package>()
                {
                    new Package()
                    {
                        Name = "Microsoft.Extensions.Primitives",
                        Source = "nuget",
                        Version = "7.0.0",
                        Licenses = new[] { "MIT" }
                    },
                    new Package()
                    {
                        Name = "Microsoft.Extensions.Primitives", // Duplicate
                        Source = "nuget",
                        Version = "7.0.0",
                        Licenses = new[] { "MIT", "PUBLIC" } // New License
                    }
                };

            return new PagedList<Package>
            {
                Data = data.Skip((pageNumber - 1) * pageSize).Take(pageSize),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = data.Count() / pageSize
            };
        }
    }
}