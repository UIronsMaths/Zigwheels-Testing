using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

public abstract class BasePage
{
    protected IWebDriver Driver { get; }
    protected WebDriverWait Wait { get; }

    protected BasePage(IWebDriver driver, int explicitWaitSeconds = 10)
    {
        Driver = driver;
        Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWaitSeconds));
    }

    protected IWebElement WaitForElement(By locator) => Wait.Until(d => d.FindElement(locator));

    protected bool IsElementVisible(By locator)
    {
        try
        {
            return Driver.FindElement(locator).Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    protected void NavigateTo(string url)
    {
        Driver.Navigate().GoToUrl(url);

        
        // Handles cookie consent popups specific to Zigwheels, iff present. This is a best-effort approach
        // We poll briefly and move on if the popup isn't present, it won't throw an exception unless WebDriverWait.Until() throws on a null result like in prior implementations.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        IWebElement? button = null;

        while(DateTime.UtcNow < deadline)
        {
            button = Driver.FindElements(By.CssSelector("button.fc-cta-consent")).FirstOrDefault();
            if (button != null) break;
            Thread.Sleep(250); // Poll every 250ms
        }

        if (button != null)
        {
            ((IJavaScriptExecutor)Driver)
                .ExecuteScript("arguments[0].click();", button);
        }
        
    }

    public string GetPageTitle() => Driver.Title;

    public string GetCurrentUrl() => Driver.Url;

    protected IWebElement Find(By locator) =>
    Wait.Until(d => d.FindElement(locator));

    protected void Click(By locator) =>
        Find(locator).Click();

    protected void Type(By locator, string text)
    {
        var element = Find(locator);
        element.Clear();
        element.SendKeys(text);
    }

    protected string Text(By locator) =>
        Find(locator).Text;
}