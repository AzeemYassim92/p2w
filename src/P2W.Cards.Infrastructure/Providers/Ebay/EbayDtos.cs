using System.Text.Json.Serialization;

namespace P2W.Cards.Infrastructure.Providers.Ebay;

public sealed class EbayOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";
}

public sealed class EbayBrowseSearchResponse
{
    [JsonPropertyName("itemSummaries")]
    public List<EbayItemSummaryDto> ItemSummaries { get; set; } = new();
}

public sealed class EbayItemSummaryDto
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    [JsonPropertyName("itemWebUrl")]
    public string ItemWebUrl { get; set; } = "";
    [JsonPropertyName("image")]
    public EbayImageDto? Image { get; set; }
    [JsonPropertyName("price")]
    public EbayMoneyDto? Price { get; set; }
    [JsonPropertyName("shippingOptions")]
    public List<EbayShippingOptionDto> ShippingOptions { get; set; } = new();
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
    [JsonPropertyName("seller")]
    public EbaySellerDto? Seller { get; set; }
    [JsonPropertyName("itemLocation")]
    public EbayItemLocationDto? ItemLocation { get; set; }
    [JsonPropertyName("buyingOptions")]
    public List<string> BuyingOptions { get; set; } = new();
    [JsonPropertyName("itemCreationDate")]
    public DateTime? ItemCreationDate { get; set; }
    [JsonPropertyName("itemEndDate")]
    public DateTime? ItemEndDate { get; set; }
}

public sealed class EbayMoneyDto
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
}

public sealed class EbayImageDto
{
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}

public sealed class EbayShippingOptionDto
{
    [JsonPropertyName("shippingCost")]
    public EbayMoneyDto? ShippingCost { get; set; }
}

public sealed class EbaySellerDto
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
    [JsonPropertyName("feedbackScore")]
    public decimal? FeedbackScore { get; set; }
    [JsonPropertyName("feedbackPercentage")]
    public string? FeedbackPercentage { get; set; }
}

public sealed class EbayItemLocationDto
{
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
}
