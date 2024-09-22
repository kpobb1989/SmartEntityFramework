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
        ""zip"": 94583,
        ""employees"": [
            {
                ""email"": ""vyasyapupkin@chevron.com"",
                ""firstName"": ""Vyasya1"",
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
        ""zip"": 94000,
        ""employees"": [
{
                ""email"": ""vlad@netflix.com"",
                ""firstName"": ""Vlad"",
                ""lastName"": ""Kushnir""
            }]
    }
]";


           // unitOfWork.Repository<CompanyEntity>().Delete();

           // await unitOfWork.SaveChangesAsync(ct);

            var dtoCompanies = JsonSerializer.Deserialize<CompanyDto[]>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            }) ?? Enumerable.Empty<CompanyDto>();

            var dbCompanies = dtoCompanies.Select(dto =>
            {
                var company = new CompanyEntity
                {
                    Name = dto.Name,
                    Address = dto.Address,
                    Zip = dto.Zip,

                    //Employees = dto.Employees?.Select(employee => new EmployeeEntity
                    //{
                    //    Email = employee.Email,
                    //    FirstName = employee.FirstName,
                    //    LastName = employee.LastName,
                    //}).ToList()
                };

                return company;
            }).ToList();

            await unitOfWork.Repository<CompanyEntity>().RefreshAsync(dbCompanies, ct: ct);

            var dbEmployees = dtoCompanies.SelectMany(c => c.Employees!, (company, employee) => new EmployeeEntity
            {
                Email = employee.Email,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                CompanyId = dbCompanies.First(c => c.Name == company.Name).Id,
            }).ToList();

            await unitOfWork.Repository<EmployeeEntity>().RefreshAsync(dbEmployees, ct: ct);
        }

        public record CompanyDto
        {
            public string? Name { get; set; }
            public string? Address { get; set; }
            public int? Zip { get; set; }
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
