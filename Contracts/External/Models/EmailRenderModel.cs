namespace Contracts.External.Models;

public class EmailRenderModel
{
    public string ServiceName { get; set; } = default!;
    public string EmailType { get; set; } = default!;
    public string ToEmail { get; set; } = default!;
    public string LanguageCode { get; set; } = "en";

    public IReadOnlyDictionary<string, string> Variables { get; set; }
        = new Dictionary<string, string>();

    public string? Get(string key)
    {
        return Variables.TryGetValue(key, out var value)
            ? value
            : null;
    }
}