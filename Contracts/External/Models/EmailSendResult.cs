namespace Contracts.External.Models;

public class EmailSendResult
{
    public bool Successful { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// True when the failure is transient (network, Brevo 5xx, rate limit) and a retry may succeed;
    /// false for permanent failures (Brevo 4xx, invalid recipient). Only meaningful when
    /// <see cref="Successful"/> is false.
    /// </summary>
    public bool Transient { get; set; }

    public static EmailSendResult Success(string? providerMessageId = null)
    {
        return new EmailSendResult
        {
            Successful = true,
            ProviderMessageId = providerMessageId
        };
    }

    public static EmailSendResult Failure(string errorMessage, bool transient = false)
    {
        return new EmailSendResult
        {
            Successful = false,
            ErrorMessage = errorMessage,
            Transient = transient
        };
    }
}