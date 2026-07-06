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
        _page.NavigateToUpcomingBikesByBrand("honda");
        //_page.ClickViewMoreIfPresent();

        var allHondaBikes = _page.GetListedBikes();
        Assert.That(allHondaBikes, Is.Not.Empty, "Expected at least one upcoming Honda bike to be listed.");

        var maxPriceLakhs = 2.0m;
        var affordable = _page.FilterByMaxPriceLakhs(allHondaBikes, maxPriceLakhs);

        Assert.That(affordable, Is.Not.Empty, "Expected at least one Honda bike under the price threshold.");
        Assert.That(affordable.All(b => b.PriceInLakhs <= maxPriceLakhs), Is.True);

        foreach (var bike in affordable)
            TestContext.WriteLine(bike.ToString());
    }

    [Test]
    public void UpcomingHondaBikes_PriceRange_1To3Lakh()
    {
        _page.NavigateToUpcomingBikesByBrand("honda");

        var allHondaBikes = _page.GetListedBikes();
        var midRange = _page.FilterByPriceRangeLakhs(allHondaBikes, minLakhs: 1.0m, maxLakhs: 3.0m);

        Assert.That(midRange, Is.Not.Empty);
        Assert.That(midRange.All(b => b.PriceInLakhs is >= 1.0m and <= 3.0m), Is.True);
    }
}