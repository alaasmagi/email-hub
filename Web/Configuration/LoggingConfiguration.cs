using Microsoft.Extensions.Logging.Console;

namespace Web.Configuration;

public static class LoggingConfiguration
{
    public static void ConfigureApplicationLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff ";
            options.UseUtcTimestamp = true;
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.IncludeScopes = true;
        });

        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    }
}
