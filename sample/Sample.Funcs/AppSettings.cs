using Microsoft.Extensions.Configuration;

namespace Sample.Funcs
{
    public static class AppSettings
    {
        private static IConfiguration? Configuration;

        public static void Setup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static string DbConnectionString => Configuration?.GetConnectionString("DbConnectionString") ?? "Server=(LocalDb)\\MSSQLLocalDB;Database=sample-db;Trusted_Connection=True;Encrypt=false";
    }
}
