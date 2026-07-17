using System.Text.RegularExpressions;
using OpenQA.Selenium;

public class UsedCarsPage : BasePage
{
    private const string BaseUrl = "https://www.zigwheels.com";

    // Matches detail links like /used-cars/chennai/honda-city-2018-.../
    private static readonly Regex CarDetailHrefPattern =
        new(@"^/used-cars/([a-z0-9-]+)/([a-z0-9-]+)/?$", RegexOptions.IgnoreCase);

    private By AllPageLinks => By.CssSelector("a[href]");

    public UsedCarsPage(IWebDriver driver) : base(driver) { }

    public void NavigateToUsedCarsByCity(string city)
    {
        var slug = city.Trim().ToLowerInvariant().Replace(" ", "-");
        NavigateTo($"{BaseUrl}/used-cars/{slug}");
        WaitForCarCardsToLoad();
    }

    private void WaitForCarCardsToLoad()
    {
        Wait.Until(d => CountCarCardLinks(d) > 0);
    }

    private int CountCarCardLinks(IWebDriver d) =>
        d.FindElements(AllPageLinks)
            .Select(a => a.GetAttribute("href"))
            .Where(href => href != null && CarDetailHrefPattern.IsMatch(GetPath(href)))
            .Distinct()
            .Count();

    public List<BikeInfo> GetListedCars()
    {
        var results = new List<BikeInfo>();
        var seenUrls = new HashSet<string>();

        foreach (var link in Driver.FindElements(AllPageLinks))
        {
            try
            {
                var href = link.GetAttribute("href");
                if (string.IsNullOrEmpty(href)) continue;
                if (!CarDetailHrefPattern.IsMatch(GetPath(href))) continue;
                if (!seenUrls.Add(href)) continue;

                var name = link.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = link.GetAttribute("title") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) continue;

                var cardText = GetAncestorCardText(link);

                results.Add(new BikeInfo
                {
                    Name = name,
                    DetailUrl = href,
                    PriceText = ExtractPriceText(cardText),
                    PriceInLakhs = ParsePriceToLakhs(ExtractPriceText(cardText))
                });
            }
            catch (StaleElementReferenceException)
            {
                // skip
            }
        }

        return results;
    }

    // Helpers borrowed from UpcomingBikesPage
    private static string GetPath(string? url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
    }

    private static string GetAncestorCardText(IWebElement link)
    {
        var ancestorXPaths = new[] { "./ancestor::li[1]", "./ancestor::div[1]", "./ancestor::article[1]" };
        foreach (var xp in ancestorXPaths)
        {
            try
            {
                var ancestor = link.FindElement(By.XPath(xp));
                var text = ancestor.Text;
                if (text.Contains("Rs.") || text.Contains("Owner") || text.Contains("Kilometre"))
                    return text;
            }
            catch (NoSuchElementException) { }
        }
        return string.Empty;
    }

    private static string ExtractPriceText(string cardText)
    {
        var match = Regex.Match(cardText, @"(Rs\.\s*[\d,\.]+\s*(Lakh|Crore)?|Price To Be Announced)", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Trim() : string.Empty;
    }

    private static decimal? ParsePriceToLakhs(string priceText)
    {
        if (string.IsNullOrWhiteSpace(priceText)) return null;
        if (priceText.Contains("To Be Announced", StringComparison.OrdinalIgnoreCase)) return null;

        var numMatch = Regex.Match(priceText, @"[\d,]+(\.\d+)?");
        if (!numMatch.Success) return null;

        var value = decimal.Parse(numMatch.Value.Replace(",", ""));

        if (priceText.Contains("Crore", StringComparison.OrdinalIgnoreCase)) return value * 100m;
        if (priceText.Contains("Lakh", StringComparison.OrdinalIgnoreCase)) return value;

        return value / 100000m;
    }
}
