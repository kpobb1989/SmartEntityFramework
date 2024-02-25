using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Sample.Abstractions.DB;
using Sample.Abstractions.DB.Interfaces;

namespace Sample.Funcs
{
    public class SampleSyncUp(IUnitOfWork unitOfWork)
    {
        [Function("SampleSyncUpTimerTrigger")]
        public async Task RunTimerTrigger([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] CancellationToken ct)
        {
            var company = new CompanyEntity()
            {
                Name = "Chevron",
                Address = "6001 Bollinger Canyon Rd, Suite G, San Ramon, CA"
            };

            var company2 = new CompanyEntity()
            {
                Name = "Netflix",
                Address = "121 Albright Way, Los Gatos, CA",
            };

            var employees = new List<EmployeeEntity>()
            {
                new() { Email = "sportjoy@outlook.com", FirstName = "Vyasya1", LastName = "Pupkin" },
                new() { Email = "f0rever@i.ua", FirstName = "Alex", LastName = "Kushnir" }
            };

            await unitOfWork.Entity<CompanyEntity>().SyncUpAsync(new[] { company, company2 }, ct: ct);

            await unitOfWork.SaveChangesAsync(ct);
        }

        //[Function("SampleSyncUp")]
        //public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] CancellationToken ct)
        //{

        //}
    }
}
