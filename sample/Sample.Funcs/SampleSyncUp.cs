using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Sample.Abstractions.DB;
using Sample.Abstractions.DB.Interfaces;

namespace Sample.Funcs
{
    public class SampleSyncUp(IUnitOfWork unitOfWork)
    {

        [Function("SampleSyncUp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] CancellationToken ct)
        {
            var company = new CompanyEntity()
            {
                Name = "Netflix",
                Address = "121 Albright Way, Los Gatos, CA",
            };

            var user = new EmployeeEntity() { Email = "sportjoy@outlook.com", FirstName = "Vyasya1", LastName = "Pupkin" };
            var user2 = new EmployeeEntity() { Email = "f0rever@i.ua", FirstName = "Alex", LastName = "Kushnir" };

            company.Employees.Add(user);
            company.Employees.Add(user2);

            await unitOfWork.Entity<CompanyEntity>().SyncUpAsync(new[] { company }, include: s => new[] { s.Employees }, ct: ct);

            await unitOfWork.SaveChangesAsync(ct);

            return new OkResult();
        }
    }
}
