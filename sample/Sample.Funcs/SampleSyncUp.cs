using Microsoft.Azure.Functions.Worker;

using Sample.DB.Entities;
using Sample.DB.Interfaces;

using System.Text.Json;

namespace Sample.Funcs
{
    public class SampleSyncUp(IUnitOfWork unitOfWork)
    {
        [Function("SampleSyncUpTimerTrigger")]
        public async Task RunTimerTrigger([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] CancellationToken ct)
        {
            var json = @"
[
    {
        ""name"": ""Chevron"",
        ""address"": ""6001 Bollinger Canyon Rd, Suite G, San Ramon, CA"",
        ""employees"": [
            {
                ""email"": ""vyasyapupkin@chevron.com"",
                ""firstName"": ""Vyasya"",
                ""lastName"": ""Pupkin""
            },
            {
                ""email"": ""mattereza@chevron.com"",
                ""firstName"": ""Mat"",
                ""lastName"": ""Tereza""
            }
        ]
    },
    {
        ""name"": ""Netflix"",
        ""address"": ""121 Albright Way, Los Gatos, CA"",
        ""employees"": []
    }
]";
            var companies = JsonSerializer.Deserialize<CompanyDto[]>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            }) ?? Enumerable.Empty<CompanyDto>();

            var dbCompanies = companies.Select(c => new CompanyEntity
            {
                Name = c.Name,
                Address = c.Address,
            }).ToList();

            await unitOfWork.Entity<CompanyEntity>().RefreshAsync(dbCompanies, ct: ct);

            await unitOfWork.SaveChangesAsync(ct);

            var dbEmployees = companies.SelectMany(c => c.Employees!, (company, employee) => new EmployeeEntity
            {
                Email = employee.Email,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                CompanyId = dbCompanies.First(c => c.Name == company.Name).Id,
            }).ToList();

            await unitOfWork.Entity<EmployeeEntity>().RefreshAsync(dbEmployees, ct: ct);

            await unitOfWork.SaveChangesAsync(ct);
        }

        public record CompanyDto
        {
            public string? Name { get; set; }
            public string? Address { get; set; }
            public EmployeeDto[]? Employees { get; set; } = [];
        }

        public record EmployeeDto
        {
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
        }
    }
}
