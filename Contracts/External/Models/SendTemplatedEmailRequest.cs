namespace Contracts.External.Models;

public class SendTemplatedEmailRequest
{
    public string ServiceName { get; set; } = default!;
    public string EmailType { get; set; } = default!;
    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";
    public string? CorrelationId { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
}