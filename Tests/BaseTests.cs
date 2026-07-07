using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using System;

[TestFixture("chrome")]
[TestFixture("firefox")]
[TestFixture("edge")]
[Parallelizable(ParallelScope.Self)]
public abstract class BaseTest
{
    protected IWebDriver Driver => DriverContext.Driver;

    private string TestName => TestContext.CurrentContext.Test.Name;

    public static TestSettings settings = null;

    private readonly string _browser;

    protected BaseTest(string browser)
    {
        _browser = browser;
    }

    [OneTimeSetUp]
    public static void OneTimeSetUp()
    {
        settings = ConfigurationManager.Settings;

        // TODO: initialise reporting (Extent/Allure) once that infrastructure exists
    }

    [SetUp]
    public void SetUp()
    {
        var driver = DriverFactory.Create(_browser, settings.HeadlessMode);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(settings.PageLoadTimeoutSeconds);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(settings.ExplicitWaitSeconds);

        DriverContext.SetDriver(driver);

        // 1. Navigate to the target domain briefly
        //driver.Navigate().GoToUrl("https://zigwheels.com");

        // 2. Inject the cookie that the website looks for to skip the popup
        // (You can find the exact Name/Value pairs using your browser DevTools from step 1)
        //driver.Manage().Cookies.AddCookie(new Cookie("cookie_consent_accepted", "true"));
        //driver.Manage().Cookies.AddCookie(new Cookie("euconsent-v2", "true"));

        // TODO: create per-test report entry once reporting exists
    }

    [TearDown]
    public void TearDown()
    {
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        var message = TestContext.CurrentContext.Result.Message;

        if (status == TestStatus.Failed)
        {
            // TODO: capture screenshot and log failure once that infrastructure exists
            Console.WriteLine($"[{TestName}] FAILED: {message}");
        }

        try
        {
            if (DriverContext.IsInitialised)
                DriverContext.QuitDriver();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing browser: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        // TODO: flush reports once reporting exists
    }

    // Helper for tests to declare the expected outcome at the start of the test
    protected void Expect(string expected)
    {
        // TODO: wire up once logging exists
    }

    // Helper for subclasses to log steps
    protected void LogStep(string description)
    {
        // TODO: route to Extent/Allure/Serilog once that infrastructure exists
        Console.WriteLine($"[{TestName}] STEP: {description}");
    }
}