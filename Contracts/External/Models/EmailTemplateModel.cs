namespace Contracts.External.Models;

/// <summary>
/// The model handed to a template. Composes the three independent inputs the contract insists on not
/// collapsing: the strongly-typed <see cref="Content"/> (chosen by <c>{source}.{action}</c>),
/// <see cref="Branding"/> (chosen by <c>tenant</c>) and <see cref="Fmt"/> bound to the recipient's
/// <see cref="Locale"/> (from <c>content.locale</c>).
///
/// <see cref="Content"/> is <c>dynamic</c> only so templates can access the concrete model's members
/// late-bound (<c>@Model.Content.InvoiceTotal</c>); the value itself is always one of the strongly
/// typed content classes, never a dictionary.
/// </summary>
public class EmailTemplateModel
{
    public dynamic Content { get; init; } = default!;
    public EmailBranding Branding { get; init; } = default!;
    public LocaleFormatter Fmt { get; init; } = default!;
    public string Locale { get; init; } = default!;
}
