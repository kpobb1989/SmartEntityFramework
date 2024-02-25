using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sample.Abstractions.Services.Interfaces;
using Sample.Function.Extensions;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Sample.Abstractions.DB.Interfaces;
using Sample.Abstractions.DB;

namespace Sample.Function
{
    public class SyncUpAsync
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPackageService _packageService;

        public SyncUpAsync(
            IUnitOfWork unitOfWork,
            IPackageService packageService)
        {
            _unitOfWork = unitOfWork;
            _packageService = packageService;
        }

        [FunctionName("SyncUpAsync")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log, CancellationToken cancellationToken)
        {
            _unitOfWork.Entity<UserEntity>().GetQueryable(include: entity => new object[] { entity.FirstName });


            //int pageNumber = 1;
            //int pageSize = 1;
            //int totalPages = 0;

            //log.LogInformation("Started packages sync up");

            //do
            //{
            //    var response = _packageService.GetPackages(pageNumber, pageSize);

            //    log.LogInformation($"Retrieved {response.Data.Count()} package(s) from the API ({pageNumber} from {response.TotalPages})");

            //    pageNumber++;

            //    if (totalPages == 0)
            //    {
            //        totalPages = response.TotalPages;
            //    }

            //    var licenses = response.Data
            //          .SelectMany(s => s.Licenses)
            //          .DistinctBy(s => s)
            //          .Select(s => new DAL.Entities.License()
            //          {
            //              Name = s,
            //          });

            //    log.LogInformation($"Found {licenses.Count()} unique license(s) from the API");

            //    await _unitOfWork
            //        .Licenses()
            //        .RefreshAsync(licenses, s => s.Name, cancellationToken: cancellationToken);

            //    licenses = await _unitOfWork.Licenses().Get(dbLicense => licenses.Select(s => s.Name).Contains(dbLicense.Name)).ToListAsync(cancellationToken);

            //    log.LogInformation($"Refreshed {await _unitOfWork.Licenses().CountAsync()} license(s) in the DB");

            //    var packages = response.Data
            //        .GroupBy(s => (s.Source, s.Name, s.Version), (key, values) => (Key: key, Licenses: values))
            //        .Select(s => new DAL.Entities.Package()
            //        {
            //            Name = s.Key.Name,
            //            Source = s.Key.Source,
            //            Version = s.Key.Version,
            //            Licenses = s.Licenses
            //                        .SelectMany(s => s.Licenses)
            //                        .Distinct()
            //                        .Join(licenses, license => license, dbLicense => dbLicense.Name, (_, dbLicense) => dbLicense)
            //                        .ToList(),
            //        });

            //    log.LogInformation($"Found {packages.Count()} unique package(s) from the API");

            //    await _unitOfWork
            //        .Packages()
            //        .RefreshAsync(packages, s => (s.Source, s.Name, s.Version), "Licenses", cancellationToken: cancellationToken);

            //    log.LogInformation($"Refreshed {await _unitOfWork.Packages().CountAsync()} package(s) in the DB");

            //} while (pageNumber <= totalPages);

            //log.LogInformation($"Completed packages sync up");
        }
    }
}
