using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Sample.DB.Extensions;

public static class DbContextLoggerExtensions
{
    internal static ILogger? GetDefaultLogger(this DbContext context, string categoryName = nameof(DbContextBulkExtensions))
    {
        try
        {
            var serviceProvider = context.GetService<IServiceProvider>();
            var loggerFactory = serviceProvider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            return loggerFactory?.CreateLogger(categoryName);
        }
        catch
        {
            return null;
        }
    }
}