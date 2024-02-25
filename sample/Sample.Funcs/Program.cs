using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Abstractions.DB.Interfaces;

using Sample.Abstractions;
using Sample.DB;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // DB
        services.AddDbContext<SampleDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    })
    .Build();

var configuration = host.Services.GetRequiredService<IConfiguration>();

AppSettings.Setup(configuration);

host.Run();
