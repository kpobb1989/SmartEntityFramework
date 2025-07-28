using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.DB;
using Microsoft.Extensions.Configuration;
using Sample.Funcs;
using Microsoft.EntityFrameworkCore;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // DB
        services.AddDbContext<SampleDbContext>(options => options.UseSqlServer(AppSettings.DbConnectionString));
        //    services.AddScoped<IUnitOfWork, UnitOfWork>();
    })
    .Build();

var configuration = host.Services.GetRequiredService<IConfiguration>();

AppSettings.Setup(configuration);

// Ensure DB is created and migrations are applied
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    dbContext.Database.Migrate();
}

host.Run();
