using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Sample.Abstractions;
using Sample.Abstractions.DB.Interfaces;
using Sample.Abstractions.Services.Interfaces;
using Sample.DB;
using Sample.Function;
using Sample.Services;

using System;
using System.IO;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Sample.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Configuration
            var configuration = GetConfiguration();
            AppSettings.Setup(configuration);

            // DB
            builder.Services.AddDbContext<SampleDbContext>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Services
            builder.Services.AddScoped<IPackageService, PackageService>();
        }

        private IConfiguration GetConfiguration()
        {
            var path = Environment.CurrentDirectory;

            var builder = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }
    }
}
