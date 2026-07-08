namespace Contracts.External.Models;

public class RenderedEmailTemplate
{
    public string Subject { get; set; } = default!;
    public string HtmlBody { get; set; } = default!;
}