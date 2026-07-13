using Allure.Net.Commons;
using Allure.NUnit;
using AventStack.ExtentReports;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using System;
using System.IO;

[TestFixture("chrome")]
[TestFixture("firefox")]
[TestFixture("edge")]
[Parallelizable(ParallelScope.Self)]
[AllureNUnit]
public abstract class BaseTest
{
    protected IWebDriver Driver => DriverContext.Driver;

    private string TestName => TestContext.CurrentContext.Test.Name;

    public static TestSettings settings = null;

    private readonly string _browser;

    // NUnit creates a new fixture instance per test case by default, so this
    // instance field is safe even with parallel fixtures -- no cross-test or
    // cross-browser bleed.
    protected ExtentTest ExtentTest { get; private set; } = null!;

    protected BaseTest(string browser)
    {
        _browser = browser;
    }

    [OneTimeSetUp]
    public static void OneTimeSetUp()
    {
        settings = ConfigurationManager.Settings;
    }

    [SetUp]
    public void SetUp()
    {
        // Report initialisation itself lives in GlobalTestSetup ([SetUpFixture]),
        // which runs once for the whole assembly -- this just creates this
        // test's node under that already-initialised report.
        ExtentTest = ExtentReportManager.Extent.CreateTest($"{TestName} [{_browser}]");

        var driver = DriverFactory.Create(_browser, settings.HeadlessMode);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(settings.PageLoadTimeoutSeconds);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(settings.ExplicitWaitSeconds);

        DriverContext.SetDriver(driver);
    }

    [TearDown]
    public void TearDown()
    {
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        var message = TestContext.CurrentContext.Result.Message;

        switch (status)
        {
            case TestStatus.Passed:
                ExtentTest.Pass("Test passed.");
                break;
            case TestStatus.Failed:
                ExtentTest.Fail(message ?? "Test failed.");
                Console.WriteLine($"[{TestName}] FAILED: {message}");
                TryAttachFailureScreenshot();
                break;
            case TestStatus.Skipped:
                ExtentTest.Skip(message ?? "Test skipped.");
                break;
            default:
                ExtentTest.Info($"Outcome: {status}");
                break;
        }

        try
        {
            if (DriverContext.IsInitialised)
                DriverContext.QuitDriver();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing browser: {ex.Message}");
            ExtentTest.Warning($"Error closing browser during teardown: {ex.Message}");
        }
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        // Intentionally empty: the report is flushed exactly once for the
        // whole assembly in GlobalTestSetup, not per fixture. Flushing here
        // too (once per browser fixture) would risk concurrent writes to the
        // same report file across parallel fixtures.
    }

    // Helper for tests to declare the expected outcome at the start of the test
    protected void Expect(string expected)
    {
        ExtentTest.Info($"Expected: {expected}");
        AllureApi.Step($"Expected: {expected}");
    }

    // Helper for subclasses to log steps
    protected void LogStep(string description)
    {
        ExtentTest.Info(description);
        AllureApi.Step(description);
    }

    private void TryAttachFailureScreenshot()
    {
        try
        {
            if (!DriverContext.IsInitialised) return;

            var screenshotDir = Path.Combine(ExtentReportManager.OutputRoot, "screenshots");
            Directory.CreateDirectory(screenshotDir);

            var fileName = $"{TestName}_{_browser}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            var path = Path.Combine(screenshotDir, fileName);

            var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
            screenshot.SaveAsFile(path);

            ExtentTest.AddScreenCaptureFromPath(path);
            AllureApi.AddAttachment("Failure screenshot", "image/png", path);
        }
        catch (Exception ex)
        {
            // Don't let a screenshot-capture failure mask the original test failure.
            ExtentTest.Warning($"Could not capture failure screenshot: {ex.Message}");
        }
    }
}