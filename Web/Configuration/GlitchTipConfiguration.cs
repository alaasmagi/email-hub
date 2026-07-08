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
                options.Environment = "production";
                options.Release = Env.GetRequired("GLITCHTIP_RELEASE");
                options.SampleRate = 1.0f;

                options.MinimumEventLevel = LogLevel.Error;
                options.MinimumBreadcrumbLevel = LogLevel.Warning;
                options.SendDefaultPii = false;
            });
        });
    }
}
