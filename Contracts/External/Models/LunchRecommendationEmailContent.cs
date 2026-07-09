using System.Text.Json.Serialization;

namespace Contracts.External.Models;

/// <summary>
/// Content payload for the <c>daily.lunch.recommendation</c> email event (<c>source: "food"</c>).
/// </summary>
public class LunchRecommendationEmailContent
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = default!;

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;

    [JsonPropertyName("recommendationRows")]
    public List<RecommendationRowContent> RecommendationRows { get; set; } = [];

    [JsonPropertyName("linkToUserWheel")]
    public string LinkToUserWheel { get; set; } = default!;
}

public class RecommendationRowContent
{
    [JsonPropertyName("restaurantName")]
    public string RestaurantName { get; set; } = default!;

    [JsonPropertyName("offers")]
    public List<OfferContent> Offers { get; set; } = [];

    [JsonPropertyName("offerTimes")]
    public string OfferTimes { get; set; } = default!;

    [JsonPropertyName("link")]
    public string Link { get; set; } = default!;
}

public class OfferContent
{
    [JsonPropertyName("offerText")]
    public string OfferText { get; set; } = default!;

    [JsonPropertyName("offerPrice")]
    public string OfferPrice { get; set; } = default!;
}

