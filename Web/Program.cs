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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

Env.LoadFile(builder.Environment.ContentRootPath);
builder.ConfigureApplicationLogging();
builder.ConfigureGlitchTip();

var connectionString = Env.GetRequired("DATABASE_CONNECTION_STRING");

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
builder.Services.AddScoped<IBaseUow, BaseUow<AppDbContext>>();

builder.Services.AddScoped<IEmailDispatchService, EmailDispatchService>();
builder.Services.AddSingleton<IEmailTemplateRenderer, RazorEmailTemplateRenderer>();

builder.Services.Configure<BrevoEmailSenderOptions>(options =>
{
    options.ApiKey = Env.GetRequired("BREVO_API_KEY");
    options.BaseUrl = Env.GetRequired("BREVO_BASE_URL");
});

builder.Services.AddHttpClient<BrevoEmailSender>();
builder.Services.AddTransient<IEmailSender>(provider =>
    provider.GetRequiredService<BrevoEmailSender>());

// RabbitMQ transport + email event consumer (alaasmagi.Base.Message.RabbitMQ).
// Queue-only: the consumer reads a pre-provisioned queue; exchange/binding topology is owned by
// the infrastructure, so this service references no exchange.
var rabbitMqOptions = BuildRabbitMqOptions();
builder.Services.AddRabbitMq(rabbitMqOptions);

builder.Services.AddSingleton(new EmailQueueOptions { QueueName = Env.GetRequired("RABBITMQ_QUEUE") });
builder.Services.AddSingleton<IBaseEventHandler<JsonElement>, EmailEventHandler>();
builder.Services.AddRabbitMqConsumer<EmailEventConsumer>();

var keycloakAuthority = Env.GetRequired("KEYCLOAK_AUTHORITY");
var keycloakClientId = Env.GetRequired("KEYCLOAK_CLIENT_ID");
var keycloakClientSecret = Env.GetRequired("KEYCLOAK_CLIENT_SECRET");
var requireHttpsMetadata = keycloakAuthority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

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

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                       | ForwardedHeaders.XForwardedHost
                       | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};

forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets()
    .AllowAnonymous();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

return;

// Builds RabbitMQ connection options from the AMQP URI in the environment. Queue-only consumer:
// no exchange is used. RabbitMqOptions.Exchange is a required member of the package type, so it is
// set to empty and never referenced (removable once the package makes Exchange optional).
static RabbitMqOptions BuildRabbitMqOptions()
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

