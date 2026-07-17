using NUnit.Framework;

[TestFixture("chrome")]
[TestFixture("firefox")]
[TestFixture("edge")]
[Parallelizable(ParallelScope.All)]
public class SimpleNavigationTests : BaseTest
{
    private SimplePage _page = null!;

    public SimpleNavigationTests(string browser) : base(browser) { }

    [SetUp]
    public void PageSetUp() => _page = new SimplePage(Driver);

    [TestCase("/news")]
    [TestCase("/tata-cars/")]
    [TestCase("/newcars/best-suv-cars")]
    [TestCase("/launches")]
    [TestCase("/newcars/electric-cars")]
    [TestCase("/tata-cars/sierra-ev")]
    [TestCase("/bikes/offers-events")]
    [TestCase("/yamaha-bike/xsr155")]
    public void NavigateToPath_ShouldLoadAndHaveTitle(string path)
    {
        LogStep($"Navigating to {path}");
        _page.GoToRelative(path);

        var title = _page.Title ?? string.Empty;
        var url = _page.Url ?? string.Empty;

        Assert.That(title, Is.Not.Null.And.Not.Empty, "Expected a non-empty page title after navigation.");

        var segment = path.Trim('/').ToLowerInvariant();
        Assert.That(url.ToLowerInvariant(), Does.Contain(segment), $"Expected current URL to contain '{segment}' but was '{url}'");
    }

    [TestCase("/news", "news")]
    [TestCase("/tata-cars/", "tata")]
    [TestCase("/newcars/best-suv-cars", "suv")]
    [TestCase("/launches", "launch")]
    [TestCase("/newcars/electric-cars", "electric")]
    [TestCase("/tata-cars/sierra-ev", "sierra")]
    [TestCase("/bikes/offers-events", "offers")]
    [TestCase("/yamaha-bike/xsr155", "yamaha")]
    public void NavigateToPath_ShouldContainExpectedText(string path, string expected)
    {
        LogStep($"Navigating to {path} and checking for text '{expected}'");
        _page.GoToRelative(path);

        Assert.That(_page.BodyContains(expected), Is.True, $"Expected page body to contain '{expected}'");
    }
}
