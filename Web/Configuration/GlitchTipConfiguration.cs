using Sentry.AspNetCore;

namespace Web.Configuration;

public static class GlitchTipConfiguration
{
    public static void ConfigureGlitchTip(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseSentry(sentryBuilder =>
        {
            sentryBuilder.AddSentryOptions(options =>
            {
                options.Dsn = Env.GetRequired("GLITCHTIP_DSN");
                options.Environment = Env.Get("GLITCHTIP_ENVIRONMENT") ?? "production";
                options.Release = Env.Get("GLITCHTIP_RELEASE");
                options.SampleRate = (float)Env.GetDouble("GLITCHTIP_SAMPLE_RATE", 1.0);

                options.MinimumEventLevel = LogLevel.Error;
                options.MinimumBreadcrumbLevel = LogLevel.Warning;
                options.SendDefaultPii = false;
            });
        });
    }
}
