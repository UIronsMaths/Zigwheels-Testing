using System.Linq;
using NUnit.Framework;

[TestFixture("chrome")]
[TestFixture("firefox")]
[TestFixture("edge")]
[Parallelizable(ParallelScope.All)]
public class UpcomingHondaBikesTests : BaseTest
{
    private UpcomingBikesPage _page = null!;

    public UpcomingHondaBikesTests(string browser) : base(browser)
    {
    }

    [SetUp]
    public void PageSetUp()
    {
        _page = new UpcomingBikesPage(Driver);
    }

    [Test]
    public void UpcomingHondaBikes_Under_2Lakh_AreReturned()
    {
        LogStep("Navigating to upcoming Honda Bikes page");
        _page.NavigateToUpcomingBikesByBrand("honda");

        LogStep("Getting the page listing");
        var allHondaBikes = _page.GetListedBikes();
        Assert.That(allHondaBikes, Is.Not.Empty, "Expected at least one upcoming Honda bike to be listed.");

        LogStep("Filtering results by max price of 2 Lakh");
        var maxPriceLakhs = 2.0m;
        var affordable = _page.FilterByMaxPriceLakhs(allHondaBikes, maxPriceLakhs);

        Assert.That(affordable, Is.Not.Empty, "Expected at least one Honda bike under the price threshold.");
        Assert.That(affordable.All(b => b.PriceInLakhs <= maxPriceLakhs), Is.True);

        LogStep("Logging all bikes meeting the criteria");
        foreach (var bike in affordable)
            TestContext.WriteLine(bike.ToString());
    }

    [Test]
    public void UpcomingHondaBikes_PriceRange_1To3Lakh()
    {
        LogStep("Navigating to upcoming Honda Bikes page");
        _page.NavigateToUpcomingBikesByBrand("honda");

        LogStep("Getting the page listing");
        var allHondaBikes = _page.GetListedBikes();

        LogStep("Filtering results by specified price range");
        var midRange = _page.FilterByPriceRangeLakhs(allHondaBikes, minLakhs: 1.0m, maxLakhs: 3.0m);

        Assert.That(midRange, Is.Not.Empty);
        Assert.That(midRange.All(b => b.PriceInLakhs is >= 1.0m and <= 3.0m), Is.True);
    }


}