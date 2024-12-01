using Microsoft.Extensions.Configuration;

namespace DMediatR.Tests
{
    public static class Configuration
    {
        public static IConfiguration Get()
        {
            return Get(null);
        }

        public static IConfiguration Get(string? environment)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables();
            return builder.Build();
        }
    }
}