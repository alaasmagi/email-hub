using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using Contracts.External.Models;

namespace Application;

public class KeycloakEmailEventMapper : IKeycloakEmailEventMapper
{
    private const string PasswordResetEventType = "email.password-reset";
    private const string VerifyEmailEventType = "email.verify";
    private const string OtpEventType = "email.2fa-otp";

    public IMethodResponse<SendTemplatedEmailRequest> Map(KeycloakEmailEvent emailEvent)
    {
        var validationError = Validate(emailEvent);
        if (validationError is not null)
        {
            return Failure(validationError);
        }

        var emailType = MapEmailType(emailEvent.EventType);
        if (emailType is null)
        {
            return Failure($"Unsupported Keycloak email event type '{emailEvent.EventType}'.");
        }

        var variables = BuildVariables(emailEvent);

        var request = new SendTemplatedEmailRequest
        {
            ServiceName = NormalizeServiceName(emailEvent.EventSource),
            EmailType = emailType,
            ToEmail = emailEvent.Payload.Email,
            LanguageCode = NormalizeLanguageCode(emailEvent.Payload.Locale),
            CorrelationId = BuildCorrelationId(emailEvent),
            Variables = variables
        };

        return MethodResponse<SendTemplatedEmailRequest>.Success(request);
    }

    private static string? Validate(KeycloakEmailEvent emailEvent)
    {
        if (string.IsNullOrWhiteSpace(emailEvent.EventType))
        {
            return "Keycloak email event type is required.";
        }

        if (emailEvent.Payload is null)
        {
            return "Keycloak email event payload is required.";
        }

        if (string.IsNullOrWhiteSpace(emailEvent.Payload.UserId))
        {
            return "Keycloak email event payload user id is required.";
        }

        if (string.IsNullOrWhiteSpace(emailEvent.Payload.Email))
        {
            return "Keycloak email event payload email is required.";
        }

        return emailEvent.EventType switch
        {
            PasswordResetEventType when string.IsNullOrWhiteSpace(emailEvent.Payload.ResetLink) =>
                "Password reset email event reset link is required.",
            VerifyEmailEventType when string.IsNullOrWhiteSpace(emailEvent.Payload.VerifyLink) =>
                "Verify email event verify link is required.",
            OtpEventType when string.IsNullOrWhiteSpace(emailEvent.Payload.OtpCode) =>
                "OTP email event code is required.",
            _ => null
        };
    }

    private static string? MapEmailType(string eventType)
    {
        return eventType switch
        {
            PasswordResetEventType => "password-reset",
            VerifyEmailEventType => "verify",
            OtpEventType => "2fa-otp",
            _ => null
        };
    }

    private static Dictionary<string, string> BuildVariables(KeycloakEmailEvent emailEvent)
    {
        var payload = emailEvent.Payload;

        var variables = new Dictionary<string, string>
        {
            ["eventType"] = emailEvent.EventType,
            ["eventSource"] = NormalizeServiceName(emailEvent.EventSource),
            ["realmName"] = emailEvent.RealmName,
            ["timestamp"] = emailEvent.Timestamp,
            ["userId"] = payload.UserId,
            ["email"] = payload.Email,
            ["expiresAt"] = payload.ExpiresAt,
            ["expiresInMinutes"] = payload.ExpiresInMinutes.ToString()
        };

        AddIfPresent(variables, "locale", payload.Locale);
        AddIfPresent(variables, "resetLink", payload.ResetLink);
        AddIfPresent(variables, "verifyLink", payload.VerifyLink);
        AddIfPresent(variables, "otpCode", payload.OtpCode);

        return variables;
    }

    private static void AddIfPresent(
        Dictionary<string, string> variables,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            variables[key] = value;
        }
    }

    private static string NormalizeServiceName(string? eventSource)
    {
        return string.IsNullOrWhiteSpace(eventSource)
            ? "identity"
            : eventSource.Trim().ToLowerInvariant();
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode)
            ? "en"
            : languageCode.Trim().ToLowerInvariant();
    }

    private static string BuildCorrelationId(KeycloakEmailEvent emailEvent)
    {
        return string.Join(
            ':',
            NormalizeServiceName(emailEvent.EventSource),
            emailEvent.RealmName,
            emailEvent.EventType,
            emailEvent.Payload.UserId,
            emailEvent.Timestamp);
    }

    private static IMethodResponse<SendTemplatedEmailRequest> Failure(string message)
    {
        return MethodResponse<SendTemplatedEmailRequest>.Failure(
            new Error("keycloak.email_event.map_failed", message));
    }
}
