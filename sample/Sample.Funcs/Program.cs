using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Abstractions.DB.Interfaces;

using Sample.Abstractions;
using Sample.DB;
using Microsoft.EntityFrameworkCore;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(config =>
    {
        AppSettings.Setup(config.Build());
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // DB
        services.AddDbContext<SampleDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    })
    .Build();

host.Run();
