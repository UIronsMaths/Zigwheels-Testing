using OpenQA.Selenium;

public static class DriverContext
{
    private static readonly ThreadLocal<IWebDriver> _driver = new ThreadLocal<IWebDriver>();

    public static IWebDriver Driver => _driver.Value ?? throw new InvalidOperationException(
        "WebDriver has not been initialised for this thread. Call DriverContext.SetDriver() before accessing Driver.");

    public static void SetDriver(IWebDriver driver)
    {
        _driver.Value = driver ?? throw new ArgumentNullException(nameof(driver), "Driver cannot be null.");
    }

    public static bool IsInitialised => _driver.Value != null;

    public static void QuitDriver()
    {
        if (_driver.Value == null) return;

        _driver.Value.Quit();
        _driver.Value.Dispose();
        _driver.Value = null;
    }
}