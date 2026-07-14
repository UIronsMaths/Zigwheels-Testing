using System;

[TestFixture("chrome")]
[TestFixture("firefox")]
[TestFixture("edge")]
[Parallelizable(ParallelScope.All)]
public class IntentionalFailure : BaseTest
{
    private HomePage _page = null!;

    public IntentionalFailure(string browser) : base(browser)
    {
    }

    [SetUp]
    public void PageSetUp()
    {
        _page = new HomePage(Driver);
    }

    [Test]
    public void IntentionalFailureTest()
    {
        LogStep("Navigating to the home page");
        _page.NavigateToHomePage();
        LogStep("Intentionally failing the test");
        Assert.Fail("This test is designed to fail intentionally.");
    }
}
