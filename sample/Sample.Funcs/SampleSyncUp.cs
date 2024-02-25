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
            var user = new UserEntity() { Email = "sportjoy@outlook.com", FirstName = "Vyasya", LastName = "Pupkin" };
            var user2 = new UserEntity() { Email = "f0rever@i.ua",  FirstName = "Alex", LastName = "Kushnir" };

            await unitOfWork.Entity<UserEntity>().SyncUpAsync(new[] { user, user2 }, ct: ct);

            await unitOfWork.SaveChangesAsync(ct);

            return new OkResult();
        }
    }
}
