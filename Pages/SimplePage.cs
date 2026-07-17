using OpenQA.Selenium;

public class SimplePage : BasePage
{
    public SimplePage(IWebDriver driver) : base(driver) { }

    public void GoToRelative(string path)
    {
        var baseUrl = ConfigurationManager.Settings.BaseUrl?.TrimEnd('/') ?? string.Empty;
        if (string.IsNullOrEmpty(path))
            NavigateTo(baseUrl);

        var rel = path.StartsWith("/") ? path : "/" + path;
        NavigateTo(baseUrl + rel);
    }

    public string Title => GetPageTitle();
    public string Url => GetCurrentUrl();

    public bool BodyContains(string text, bool caseInsensitive = true)
    {
        var body = Driver.FindElement(By.TagName("body")).Text ?? string.Empty;
        if (caseInsensitive)
            return body.IndexOf(text ?? string.Empty, System.StringComparison.OrdinalIgnoreCase) >= 0;

        return body.Contains(text ?? string.Empty);
    }

    public bool HasCssSelector(string cssSelector)
    {
        return Driver.FindElements(By.CssSelector(cssSelector)).Count > 0;
    }
}
