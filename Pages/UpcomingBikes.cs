using System.Text.RegularExpressions;
using OpenQA.Selenium;

/// <summary>
/// Page object for https://www.zigwheels.com/upcoming-bikes and its brand-scoped
/// variants (https://www.zigwheels.com/upcoming-{brand}-bikes).
///
/// NOTE ON LOCATORS: ZigWheels' bike cards are located by their detail-page
/// href pattern ("/{brand}-bikes/{model}/") rather than CSS classes. This is
/// more resilient to CSS refactors.
/// </summary>
/// 
public class UpcomingBikesPage : BasePage
{
    private const string BaseUrl = "https://www.zigwheels.com";

    // Matches card links like /honda-bikes/shine-electric/ but NOT
    // sidebar links like /upcoming-honda-bikes (which have no trailing model segment).
    private static readonly Regex BikeDetailHrefPattern =
        new(@"^/([a-z0-9-]+-bikes)/([a-z0-9-]+)/?$", RegexOptions.IgnoreCase);

    private By AllPageLinks => By.CssSelector("a[href]");

    // Text-based match (case-insensitive) rather than a class/id, since the
    // real markup wasn't inspectable. Covers "View More Bikes", "View More",
    // "Load More" phrasing variants — trim this down once you confirm the
    // exact button text/tag in DevTools.
    private By ViewMoreButton => By.XPath(
        "//*[self::button or self::a or self::div]" +
        "[contains(translate(normalize-space(text()),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'view more') " +
        "or contains(translate(normalize-space(text()),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'load more')]");

    public UpcomingBikesPage(IWebDriver driver) : base(driver) { }

    public void NavigateToAllUpcomingBikes()
    {
        NavigateTo($"{BaseUrl}/upcoming-bikes");
        WaitForBikeCardsToLoad();
    }

    /// <summary>
    /// Navigates straight to the brand-scoped upcoming bikes page.
    /// e.g. brand: "honda" -> https://www.zigwheels.com/upcoming-honda-bikes
    /// </summary>
    public void NavigateToUpcomingBikesByBrand(string brand)
    {
        var slug = brand.Trim().ToLowerInvariant().Replace(" ", "-");
        NavigateTo($"{BaseUrl}/upcoming-{slug}-bikes");

        WaitForBikeCardsToLoad();
    }

    private void WaitForBikeCardsToLoad()
    {
        Wait.Until(d => CountBikeCardLinks(d) > 0);
    }

    private int CountBikeCardLinks(IWebDriver d) =>
        d.FindElements(AllPageLinks)
            .Select(a => a.GetAttribute("href"))
            .Where(href => href != null && BikeDetailHrefPattern.IsMatch(GetPath(href)))
            .Distinct()
            .Count();

    /// <summary>
    /// Repeatedly clicks "View More Bikes" until it disappears, stops adding
    /// new cards, or maxClicks is hit (safety net against an infinite loop if
    /// the button is misdetected). Call this before GetListedBikes() when you
    /// need the full catalogue rather than just the first page of cards.
    /// </summary>
    public void ClickViewMoreIfPresent(int maxClicks = 25)
    {
        for (int i = 0; i < maxClicks; i++)
        {
            var buttons = Driver.FindElements(ViewMoreButton);
            var button = buttons.FirstOrDefault(b => b.Displayed && b.Enabled);
            if (button == null) break;

            var countBeforeClick = CountBikeCardLinks(Driver);

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", button);
            try
            {
                button.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", button);
            }

            try
            {
                Wait.Until(d => CountBikeCardLinks(d) > countBeforeClick);
            }
            catch (WebDriverTimeoutException)
            {
                // Button clicked but no new cards appeared within the wait window
                // treat as "fully loaded" and stop rather than looping forever.
                break;
            }
        }
    }

    /// <summary>
    /// Scrapes every bike card currently rendered on the page. Call
    /// ClickViewMoreIfPresent() first if you need the full catalogue rather
    /// than just the first page of cards (ZigWheels paginates behind a
    /// "View More Bikes" button).
    /// </summary>
    public List<BikeInfo> GetListedBikes()
    {
        var results = new List<BikeInfo>();
        var seenUrls = new HashSet<string>();

        foreach (var link in Driver.FindElements(AllPageLinks))
        {
            try
            {
                var href = link.GetAttribute("href");

                if (string.IsNullOrEmpty(href))
                    continue;

                if (!BikeDetailHrefPattern.IsMatch(GetPath(href)))
                    continue;

                if (!seenUrls.Add(href))
                    continue;

                var name = link.Text.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    name = link.GetAttribute("title") ?? "";

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var cardText = GetAncestorCardText(link);

                results.Add(new BikeInfo
                {
                    Name = name,
                    DetailUrl = href,
                    PriceText = ExtractPriceText(cardText),
                    LaunchText = ExtractLaunchText(cardText),
                    PriceInLakhs = ParsePriceToLakhs(ExtractPriceText(cardText))
                });
            }
            catch (StaleElementReferenceException)
            {
                // DOM changed while reading this card.
                // Skip it.
            }
        }

        return results;
    }

    /// <summary>
    /// Filters an already-scraped list by max price in Lakhs.
    /// Bikes with unannounced/unparseable prices are excluded.
    /// </summary>
    public List<BikeInfo> FilterByMaxPriceLakhs(IEnumerable<BikeInfo> bikes, decimal maxLakhs) =>
        bikes.Where(b => b.PriceInLakhs.HasValue && b.PriceInLakhs.Value <= maxLakhs).ToList();

    public List<BikeInfo> FilterByPriceRangeLakhs(IEnumerable<BikeInfo> bikes, decimal minLakhs, decimal maxLakhs) =>
        bikes.Where(b => b.PriceInLakhs.HasValue && b.PriceInLakhs.Value >= minLakhs && b.PriceInLakhs.Value <= maxLakhs).ToList();

    // ---------- helpers ----------

    private static string GetPath(string? url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
    }

    private static string GetAncestorCardText(IWebElement link)
    {
        // Try progressively wider ancestors until we find text containing "Rs." or "Expected Launch"
        var ancestorXPaths = new[] { "./ancestor::li[1]", "./ancestor::div[1]", "./ancestor::article[1]" };
        foreach (var xp in ancestorXPaths)
        {
            try
            {
                var ancestor = link.FindElement(By.XPath(xp));
                var text = ancestor.Text;
                if (text.Contains("Rs.") || text.Contains("Expected Launch") || text.Contains("Price To Be Announced"))
                    return text;
            }
            catch (NoSuchElementException) { /* try next ancestor level */ }
        }
        return string.Empty;
    }

    private static string ExtractPriceText(string cardText)
    {
        var match = Regex.Match(cardText, @"(Rs\.\s*[\d,\.]+\s*(Lakh|Crore)?|Price To Be Announced)", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Trim() : string.Empty;
    }

    private static string ExtractLaunchText(string cardText)
    {
        var match = Regex.Match(cardText, @"Expected Launch\s*:\s*([^\n]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static decimal? ParsePriceToLakhs(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText)) return null;
        if (priceText.Contains("To Be Announced", StringComparison.OrdinalIgnoreCase)) return null;

        var numMatch = Regex.Match(priceText, @"[\d,]+(\.\d+)?");
        if (!numMatch.Success) return null;

        var value = decimal.Parse(numMatch.Value.Replace(",", ""));

        if (priceText.Contains("Crore", StringComparison.OrdinalIgnoreCase)) return value * 100m; // 1 Crore = 100 Lakh
        if (priceText.Contains("Lakh", StringComparison.OrdinalIgnoreCase)) return value;

        // Plain rupee figure e.g. "Rs. 95,000" -> convert to Lakhs
        return value / 100000m;
    }
}