using Microsoft.Extensions.Configuration;

namespace Sample.Abstractions
{
    public static class AppSettings
    {
        private static IConfiguration? Configuration;

        public static void Setup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        //;
        public static string DbConnectionString => Configuration?.GetConnectionString("DbConnectionString") ?? "Server=localhost\\SQLEXPRESS;Database=sample-db;Trusted_Connection=True;Encrypt=false";
    }
}
