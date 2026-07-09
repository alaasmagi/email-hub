namespace Contracts.External.Models;

public class SendTemplatedEmailRequest
{
    public string ServiceName { get; set; } = default!;
    public string EmailType { get; set; } = default!;
    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Strongly-typed event content payload used as the model when rendering the template.
    /// </summary>
    public object Content { get; set; } = default!;
}