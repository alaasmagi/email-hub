namespace Contracts.External.Models;

public class KeycloakEmailEventPayload
{
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string ExpiresAt { get; set; } = default!;
    public int ExpiresInMinutes { get; set; }
    public string? Locale { get; set; }
    public string? ResetLink { get; set; }
    public string? VerifyLink { get; set; }
    public string? OtpCode { get; set; }
}
