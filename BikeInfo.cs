public class BikeInfo
{
    public string Name { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string LaunchText { get; set; } = string.Empty;

    /// <summary>
    /// Parsed price in Lakhs (1 Lakh = 100,000 INR). Null when price is
    /// "Price To Be Announced" or otherwise unparseable.
    /// </summary>
    public decimal? PriceInLakhs { get; set; }

    public override string ToString() => $"{Name} | {PriceText} | {LaunchText}";
}