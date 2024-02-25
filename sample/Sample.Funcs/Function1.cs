using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Sample.Abstractions.DB;
using Sample.Abstractions.DB.Interfaces;

namespace Sample.Funcs
{
    public class Function1(IUnitOfWork unitOfWork)
    {

        [Function("Function1")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            var user = new UserEntity() { Email = "sportjoy@outlook.com", FirstName = "Vyasya", LastName = "Pupkin" };
            var user2 = new UserEntity() { Email = "f0rever@i.ua",  FirstName = "Alex", LastName = "Kushnir" };

            await unitOfWork.Entity<UserEntity>().SyncUpAsync(new[] { user, user2 });

            await unitOfWork.SaveChangesAsync();

            return new OkResult();
        }
    }
}
