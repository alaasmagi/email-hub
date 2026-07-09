using System.Text.Json;
using Application;
using Base.Contracts.DataAccess;
using Base.Contracts.Message;
using Base.DataAccess.EF;
using Base.Message.RabbitMQ;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using DataAccess;
using DataAccess.Context;
using DTO.DataAccess.Mappers;
using External.Brevo;
using External.RabbitMQ;
using External.RazorLight;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Web.Configuration;

public static class ServiceConfiguration
{
    public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
    {
        builder.ConfigureApplicationLogging();
        builder.ConfigureGlitchTip();

        builder.Services
            .AddDataAccess()
            .AddEmailDelivery()
            .AddEmailQueue()
            .AddApplicationAuthentication()
            .AddApplicationMvc()
            .AddHealthChecks();

        builder.Services.ConfigureForwardedHeaders();
    }

    private static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(Env.GetRequired("DATABASE_CONNECTION_STRING"));
        });

        services.AddScoped<ClientEntityMapper>();
        services.AddScoped<SenderIdentityEntityMapper>();
        services.AddScoped<TemplateEntityMapper>();
        services.AddScoped<EmailEntityMapper>();

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ISenderIdentityRepository, SenderIdentityRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IBaseUow, BaseUow<AppDbContext>>();

        services.AddScoped<IEmailDispatchService, EmailDispatchService>();
        services.AddSingleton<IEmailTemplateRenderer, RazorEmailTemplateRenderer>();

        return services;
    }

    private static IServiceCollection AddEmailDelivery(this IServiceCollection services)
    {
        services.Configure<BrevoEmailSenderOptions>(options =>
        {
            options.ApiKey = Env.GetRequired("BREVO_API_KEY");
            options.BaseUrl = Env.GetRequired("BREVO_BASE_URL");
        });

        services.AddHttpClient<BrevoEmailSender>();
        services.AddTransient<IEmailSender>(provider =>
            provider.GetRequiredService<BrevoEmailSender>());

        return services;
    }

    private static IServiceCollection AddEmailQueue(this IServiceCollection services)
    {
        services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        services.AddRabbitMq(BuildRabbitMqOptions());
        services.AddSingleton(new EmailQueueOptions { QueueName = Env.GetRequired("RABBITMQ_QUEUE") });
        services.AddSingleton<IBaseEventHandler<JsonElement>, EmailEventHandler>();
        services.AddRabbitMqConsumer<EmailEventConsumer>();

        return services;
    }

    private static IServiceCollection AddApplicationAuthentication(this IServiceCollection services)
    {
        var keycloakAuthority = Env.GetRequired("KEYCLOAK_AUTHORITY");
        var keycloakClientId = Env.GetRequired("KEYCLOAK_CLIENT_ID");
        var requireHttpsMetadata = keycloakAuthority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = keycloakAuthority;
                options.ClientId = keycloakClientId;
                options.ClientSecret = Env.GetRequired("KEYCLOAK_CLIENT_SECRET");
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = keycloakAuthority;
                options.Audience = keycloakClientId;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    private static IServiceCollection AddApplicationMvc(this IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddRazorOptions(options =>
            {
                options.ViewLocationFormats.Clear();
                options.ViewLocationFormats.Add("/MVC/Views/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/MVC/Views/Shared/{0}.cshtml");
            });

        return services;
    }

    private static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                       | ForwardedHeaders.XForwardedHost
                                       | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    private static RabbitMqOptions BuildRabbitMqOptions()
    {
        var amqpUri = new Uri(Env.GetRequired("RABBITMQ_URI"));
        var userInfo = amqpUri.UserInfo.Split(':', 2);
        var virtualHost = amqpUri.AbsolutePath.Trim('/');

        return new RabbitMqOptions
        {
            Host = amqpUri.Host,
            Port = amqpUri.IsDefaultPort ? 5672 : amqpUri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            VirtualHost = string.IsNullOrEmpty(virtualHost) ? "/" : Uri.UnescapeDataString(virtualHost),
            Exchange = string.Empty
        };
    }
}
