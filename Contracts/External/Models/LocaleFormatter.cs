using System.Globalization;

namespace Contracts.External.Models;

/// <summary>
/// All locale-dependent formatting lives here, per the contract's rule that localisation belongs to
/// email-hub and publishers send machine-readable values. Templates call these helpers; they never
/// format wire values themselves.
///
/// Wire formats (contract §7): dates <c>2026-06-06</c>, months <c>2026-06</c>, times <c>11:00</c>,
/// amounts as invariant decimal strings <c>"97.87"</c>. Opaque fields (paymentDescription,
/// invoiceIdentifier, paymentIban, actionLink) are NOT passed through here — they are copied verbatim.
/// </summary>
public sealed class LocaleFormatter
{
    private readonly CultureInfo _culture;

    public LocaleFormatter(string? locale)
    {
        _culture = ResolveCulture(locale);
    }

    public string LocaleName => _culture.Name;

    /// <summary>ISO date (<c>2026-06-06</c>) → the recipient's locale date format.</summary>
    public string Date(string? isoDate)
    {
        if (DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date.ToString("d", _culture);
        }

        return isoDate ?? string.Empty;
    }

    /// <summary>ISO year-month (<c>2026-06</c>) → localised "month year".</summary>
    public string Month(string? isoMonth)
    {
        if (DateOnly.TryParseExact(isoMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date.ToString("MMMM yyyy", _culture);
        }

        return isoMonth ?? string.Empty;
    }

    /// <summary>Wall-clock time (<c>11:00</c>) → localised time of day. No timezone, never converted.</summary>
    public string Time(string? wallClock)
    {
        if (TimeOnly.TryParseExact(wallClock, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            return time.ToString("t", _culture);
        }

        return wallClock ?? string.Empty;
    }

    /// <summary>
    /// Invariant decimal string (<c>"97.87"</c>) → parsed as <see cref="decimal"/> (no binary float
    /// step) and formatted with the locale's grouping and decimal separators. The currency symbol is
    /// NOT added here — it comes from the template, since the message only carries the ISO 4217 code.
    /// </summary>
    public string Amount(string? invariantDecimal)
    {
        if (decimal.TryParse(invariantDecimal, NumberStyles.Number, CultureInfo.InvariantCulture,
                out var value))
        {
            return value.ToString("N2", _culture);
        }

        return invariantDecimal ?? string.Empty;
    }

    private static CultureInfo ResolveCulture(string? locale)
    {
        if (!string.IsNullOrWhiteSpace(locale))
        {
            try
            {
                return CultureInfo.GetCultureInfo(locale.Trim());
            }
            catch (CultureNotFoundException)
            {
                // Fall through to the default below.
            }
        }

        return CultureInfo.GetCultureInfo("en");
    }
}
