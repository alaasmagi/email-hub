using Microsoft.Extensions.Logging.Console;

namespace Web.Configuration;

public static class LoggingConfiguration
{
    public static void ConfigureApplicationLogging(this WebApplicationBuilder builder)
    {
        var defaultLevel = GetLogLevel("LOG_LEVEL", LogLevel.Information);
        var microsoftLevel = GetLogLevel("MICROSOFT_LOG_LEVEL", LogLevel.Warning);
        var entityFrameworkLevel = GetLogLevel("ENTITY_FRAMEWORK_LOG_LEVEL", LogLevel.Warning);

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(defaultLevel);
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff ";
            options.UseUtcTimestamp = true;
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.IncludeScopes = true;
        });

        builder.Logging.AddFilter("Microsoft", microsoftLevel);
        builder.Logging.AddFilter("Microsoft.AspNetCore", microsoftLevel);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", entityFrameworkLevel);
    }

    private static LogLevel GetLogLevel(string name, LogLevel fallback)
    {
        var value = Env.Get(name);
        return Enum.TryParse<LogLevel>(value, ignoreCase: true, out var level)
            ? level
            : fallback;
    }
}
