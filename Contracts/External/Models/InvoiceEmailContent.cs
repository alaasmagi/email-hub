using System.Text.Json.Serialization;

namespace Contracts.External.Models;

/// <summary>
/// Content payload for the <c>monthly.invoice</c> email event (<c>source: "invoice"</c>).
/// </summary>
public class InvoiceEmailContent
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = default!;

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("invoiceIdentifier")]
    public string InvoiceIdentifier { get; set; } = default!;

    [JsonPropertyName("invoicePeriod")]
    public string InvoicePeriod { get; set; } = default!;

    [JsonPropertyName("invoiceDate")]
    public string InvoiceDate { get; set; } = default!;

    [JsonPropertyName("invoiceDueDate")]
    public string InvoiceDueDate { get; set; } = default!;

    [JsonPropertyName("invoiceTotal")]
    public string InvoiceTotal { get; set; } = default!;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;

    [JsonPropertyName("invoiceRows")]
    public List<InvoiceRowContent> InvoiceRows { get; set; } = [];

    [JsonPropertyName("paymentFullName")]
    public string PaymentFullName { get; set; } = default!;

    [JsonPropertyName("paymentIban")]
    public string PaymentIban { get; set; } = default!;

    [JsonPropertyName("paymentDescription")]
    public string PaymentDescription { get; set; } = default!;
}

public class InvoiceRowContent
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = default!;

    [JsonPropertyName("service")]
    public string Service { get; set; } = default!;

    [JsonPropertyName("periodStart")]
    public string PeriodStart { get; set; } = default!;

    [JsonPropertyName("periodEnd")]
    public string PeriodEnd { get; set; } = default!;

    [JsonPropertyName("invoiceAmount")]
    public string InvoiceAmount { get; set; } = default!;

    [JsonPropertyName("fractions")]
    public int Fractions { get; set; }

    [JsonPropertyName("invoiceFraction")]
    public string InvoiceFraction { get; set; } = default!;
}

