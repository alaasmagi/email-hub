using Application;
using Contracts.Application;
using Contracts.DataAccess;
using Contracts.External;
using DataAccess;
using DataAccess.Context;
using DTO.DataAccess.Mappers;
using External.Brevo;
using External.RazorLight;
using External.RabbitMQ;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

Env.LoadFile(builder.Environment.ContentRootPath);
Env.EnsureDefault("GLITCHTIP_ENVIRONMENT", "production");
builder.ConfigureApplicationLogging();
builder.ConfigureGlitchTip();

var connectionString =
    Env.Get("DATABASE_CONNECTION_STRING") ??
    Env.Get("CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DATABASE_CONNECTION_STRING must be configured in .env.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ClientEntityMapper>();
builder.Services.AddScoped<SenderIdentityEntityMapper>();
builder.Services.AddScoped<TemplateEntityMapper>();
builder.Services.AddScoped<EmailEntityMapper>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ISenderIdentityRepository, SenderIdentityRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();

builder.Services.AddScoped<IKeycloakEmailEventMapper, KeycloakEmailEventMapper>();
builder.Services.AddScoped<IEmailDispatchService, EmailDispatchService>();
builder.Services.AddSingleton<RazorEmailTemplateRenderer>();
builder.Services.AddSingleton<IRazorEmailTemplateRenderer>(provider =>
    provider.GetRequiredService<RazorEmailTemplateRenderer>());
builder.Services.AddSingleton<IEmailTemplateRenderer>(provider =>
    provider.GetRequiredService<RazorEmailTemplateRenderer>());

builder.Services.Configure<BrevoEmailSenderOptions>(options =>
{
    options.ApiKey = Env.Get("BREVO_API_KEY") ?? string.Empty;
    options.BaseUrl = Env.Get("BREVO_BASE_URL") ?? options.BaseUrl;
});

builder.Services.Configure<RabbitMqEmailConsumerOptions>(options =>
{
    options.Enabled = Env.GetBool("RABBITMQ_ENABLED", options.Enabled);
    options.Uri = Env.GetRequired("RABBITMQ_URI");
    options.ExchangeNames = Env.GetRequiredList("RABBITMQ_EXCHANGES");
    options.QueueName = Env.GetRequired("RABBITMQ_EMAIL_QUEUE");
    options.PrefetchCount = Env.GetUShort("RABBITMQ_EMAIL_PREFETCH", options.PrefetchCount);
    options.RetryDelaySeconds = Env.GetInt("RABBITMQ_RETRY_DELAY_SECONDS", options.RetryDelaySeconds);
});

builder.Services.AddHttpClient<BrevoEmailSender>();
builder.Services.AddTransient<IBrevoEmailSender>(provider =>
    provider.GetRequiredService<BrevoEmailSender>());
builder.Services.AddTransient<IEmailSender>(provider =>
    provider.GetRequiredService<BrevoEmailSender>());

builder.Services.AddSingleton<KeycloakEmailEventConsumer>();
builder.Services.AddSingleton<IRabbitMqEmailConsumer>(provider =>
    provider.GetRequiredService<KeycloakEmailEventConsumer>());
builder.Services.AddSingleton<IKeycloakEmailEventConsumer>(provider =>
    provider.GetRequiredService<KeycloakEmailEventConsumer>());
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<KeycloakEmailEventConsumer>());

var keycloakAuthority = Env.GetRequired("KEYCLOAK_AUTHORITY");
var keycloakClientId = Env.GetRequired("KEYCLOAK_CLIENT_ID");
var keycloakClientSecret = Env.GetRequired("KEYCLOAK_CLIENT_SECRET");
var requireHttpsMetadata = Env.GetBool("KEYCLOAK_REQUIRE_HTTPS_METADATA", true);

builder.Services
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
        options.ClientSecret = keycloakClientSecret;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.CallbackPath = Env.Get("KEYCLOAK_CALLBACK_PATH") ?? "/signin-oidc";
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

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
