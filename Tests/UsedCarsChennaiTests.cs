using System.Linq;
using NUnit.Framework;

[TestFixture("chrome")]
[TestFixture("firefox")]
[TestFixture("edge")]
[Parallelizable(ParallelScope.All)]
public class UsedCarsChennaiTests : BaseTest
{
    private UsedCarsPage _page = null!;

    public UsedCarsChennaiTests(string browser) : base(browser) { }

    [SetUp]
    public void PageSetUp()
    {
        _page = new UsedCarsPage(Driver);
    }

    [Test]
    public void UsedCars_In_Chennai_AreReturned()
    {
        LogStep("Navigating to used cars for Chennai");
        _page.NavigateToUsedCarsByCity("chennai");

        LogStep("Getting the page listing");
        var cars = _page.GetListedCars();

        Assert.That(cars, Is.Not.Empty, "Expected at least one used car listing for Chennai.");

        LogStep("Logging first few results");
        foreach (var c in cars.Take(5))
            TestContext.WriteLine(c.ToString());
    }
}
