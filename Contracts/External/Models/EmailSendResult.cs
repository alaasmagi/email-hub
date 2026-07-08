namespace Contracts.External.Models;

public class EmailSendResult
{
    public bool Successful { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public static EmailSendResult Success(string? providerMessageId = null)
    {
        return new EmailSendResult
        {
            Successful = true,
            ProviderMessageId = providerMessageId
        };
    }

    public static EmailSendResult Failure(string errorMessage)
    {
        return new EmailSendResult
        {
            Successful = false,
            ErrorMessage = errorMessage
        };
    }
}