namespace Contracts.External.Models;

/// <summary>
/// Composes the three independent inputs the contract insists on not collapsing: the strongly-typed
/// <see cref="Content"/> (chosen by <c>{source}.{action}</c>), <see cref="Branding"/> (chosen by
/// <c>tenant</c>) and <see cref="Fmt"/> bound to the recipient's <see cref="Locale"/> (from
/// <c>content.locale</c>).
///
/// The renderer unpacks this before handing it to a template: <see cref="Content"/> becomes the
/// template's <c>Model</c> (a template declares <c>@model InvoiceEmailContent</c> and reads members
/// straight off it, e.g. <c>@Model.InvoiceTotal</c>), while <see cref="Fmt"/>, <see cref="Branding"/>
/// and <see cref="Locale"/> are exposed on the ViewBag (<c>@ViewBag.Fmt.Amount(...)</c>,
/// <c>@ViewBag.Branding.DisplayName</c>). See <c>RazorEmailTemplateRenderer</c>.
///
/// <see cref="Content"/> is <c>dynamic</c> only so the renderer can pass the concrete content object
/// through without knowing its type; the value itself is always one of the strongly typed content
/// classes, never a dictionary.
/// </summary>
public class EmailTemplateModel
{
    public dynamic Content { get; init; } = default!;
    public EmailBranding Branding { get; init; } = default!;
    public LocaleFormatter Fmt { get; init; } = default!;
    public string Locale { get; init; } = default!;
}
