using Domain;

namespace Web.MVC.Models;

public class TemplateIndexRow
{
    public Template Template { get; set; } = default!;
    public string ClientServiceName { get; set; } = default!;
    public string SenderEmailType { get; set; } = default!;
    public string SenderFromAddress { get; set; } = default!;
}
