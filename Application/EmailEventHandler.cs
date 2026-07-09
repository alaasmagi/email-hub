using System.Text.Json;
using Base.Contracts.Message;
using Base.Keycloak.Events;
using Contracts.Application;
using Contracts.External.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application;

/// <summary>
/// Single handler for every event carried on the <c>email</c> type (routing key). The transport
/// routes by <c>type</c>, so all email payloads arrive here; the <c>action</c> selects the concrete
/// content DTO (used as the template model) and doubles as the email type for template resolution.
/// </summary>
public class EmailEventHandler : IBaseEventHandler<JsonElement>
{
    private static readonly JsonSerializerOptions ContentSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailEventHandler> _logger;

    public EmailEventHandler(IServiceScopeFactory scopeFactory, ILogger<EmailEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(IBaseEventEnvelope<JsonElement> @event, CancellationToken cancellationToken = default)
    {
        var content = @event.Content;

        var model = DeserializeContent(@event.Action, content);
        if (model is null)
        {
            _logger.LogWarning(
                "Ignoring unsupported email event. Source: {Source}, Action: {Action}",
                @event.Source,
                @event.Action);
            return;
        }

        if (!TryGetRecipient(content, out var email, out var locale))
        {
            _logger.LogWarning(
                "Ignoring email event without a recipient address. Source: {Source}, Action: {Action}",
                @event.Source,
                @event.Action);
            return;
        }

        var request = new SendTemplatedEmailRequest
        {
            ServiceName = NormalizeServiceName(@event.Source),
            // The action doubles as the email type -> SenderIdentity.EmailType / Template lookup.
            EmailType = @event.Action,
            ToEmail = email,
            LanguageCode = NormalizeLanguageCode(locale),
            CorrelationId = BuildCorrelationId(@event),
            Content = model
        };

        using var scope = _scopeFactory.CreateScope();
        var dispatchService = scope.ServiceProvider.GetRequiredService<IEmailDispatchService>();

        var result = await dispatchService.SendTemplatedEmailAsync(request, cancellationToken);
        if (!result.Successful)
        {
            // Throwing lets the RabbitMQ consumer nack (requeue: false) the poison message.
            throw new InvalidOperationException(
                result.Error?.Message ?? $"Failed to dispatch '{@event.Action}' email.");
        }
    }

    private static object? DeserializeContent(string action, JsonElement content)
    {
        return action switch
        {
            "user.verify" => content.Deserialize<VerifyEmailContent>(ContentSerializerOptions),
            "user.password.reset" => content.Deserialize<PasswordResetContent>(ContentSerializerOptions),
            "user.2fa.otp" => content.Deserialize<OtpEmailContent>(ContentSerializerOptions),
            "monthly.invoice" => content.Deserialize<InvoiceEmailContent>(ContentSerializerOptions),
            "daily.lunch.recommendation" => content.Deserialize<LunchRecommendationEmailContent>(ContentSerializerOptions),
            _ => null
        };
    }

    private static bool TryGetRecipient(JsonElement content, out string email, out string? locale)
    {
        email = string.Empty;
        locale = null;

        if (content.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (content.TryGetProperty("email", out var emailElement) &&
            emailElement.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(emailElement.GetString()))
        {
            email = emailElement.GetString()!;
        }
        else
        {
            return false;
        }

        if (content.TryGetProperty("locale", out var localeElement) &&
            localeElement.ValueKind == JsonValueKind.String)
        {
            locale = localeElement.GetString();
        }

        return true;
    }

    private static string NormalizeServiceName(string? source)
    {
        return string.IsNullOrWhiteSpace(source)
            ? "identity"
            : source.Trim().ToLowerInvariant();
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static string BuildCorrelationId(IBaseEventEnvelope<JsonElement> @event)
    {
        return string.Join(':', @event.Source, @event.Action, @event.Timestamp.ToString("O"));
    }
}

