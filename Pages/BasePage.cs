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
        PreAcceptCookieConsent();
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
        SuppressConsentOverlay();

        /*
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
        */

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

    protected void PreAcceptCookieConsent()
    {
        Driver.Navigate().GoToUrl(ConfigurationManager.Settings.BaseUrl);
        // FCCDCF marks "user has made a consent choice" and appears to persist fine,
        // but FCNEC is a nonce that Google's own script regenerates on every page load/
        // refresh -- pre-seeding it is fighting a moving target and isn't reliable.
        // Instead of faking cookie state, interact with the real consent iframe.

        // The consent dialog is NOT inside an iframe. The "googlefcLoaded" /
        // "googlefcInactive" iframes visible in DevTools are hidden (display:none,
        // 0x0, src="about:blank") internal state-tracking frames Google's script
        // toggles between -- not the popup itself. The real dialog (div.fc-consent-root
        // -> div.fc-dialog-container -> div.fc-dialog) renders directly in the main
        // document as a sibling of those frames.
        SuppressConsentOverlay();
    }



    /// <summary>
    /// Force-hides the Zigwheels consent dialog (div.fc-consent-root / .fc-dialog-overlay /
    /// .fc-dialog-container) and neutralizes its click-blocking and scroll-lock.
    ///

    /// IMPORTANT: this sets styles directly on the elements via element.style.setProperty(...,

    /// 'important'), rather than injecting an external stylesheet rule. An external stylesheet

    /// rule can always in principle lose a CSS specificity fight -- and did: the site has its

    /// own "div.fc-consent-root { display: block !important; }" rule, which is MORE specific

    /// (element + class) than a plain ".fc-consent-root { display: none !important; }" rule

    /// (class only), so it was winning. An inline style with the !important flag set via

    /// setProperty's third argument always beats any external/author stylesheet rule, no

    /// matter how specific -- that's a hard rule in the CSS cascade, not a heuristic.

    ///

    /// A MutationObserver keeps re-applying this to any matching elements added later (covers

    /// re-renders or the popup script re-inserting nodes), so this only needs to run once per

    /// page load rather than needing to be called again after every DOM change.

    ///

    /// Tradeoff: this hides the popup without ever telling the site's script that the user

    /// consented. Fine for tests that don't touch consent-gated behavior (ads, personalization),

    /// but the site's own JS still considers consent "pending" -- use AcceptConsentIfPresent()

    /// instead if a test specifically needs the real consent cookies to be set.

    /// </summary>

    protected void SuppressConsentOverlay()

    {

        const string script = @"

            (function() {

                if (window.__consentOverrideAttached) return;

                window.__consentOverrideAttached = true;



                function nuke(el) {

                    el.style.setProperty('display', 'none', 'important');

                    el.style.setProperty('pointer-events', 'none', 'important');

                }



                function scan() {

                    document.querySelectorAll(

                        '.fc-consent-root, .fc-dialog-overlay, .fc-dialog-container'

                    ).forEach(nuke);



                    document.documentElement.style.setProperty('overflow', 'auto', 'important');

                    if (document.body) {

                        document.body.style.setProperty('overflow', 'auto', 'important');

                    }

                }



                scan();

                new MutationObserver(scan).observe(document.documentElement, {

                    childList: true,

                    subtree: true

                });

            })();";



        ((IJavaScriptExecutor)Driver).ExecuteScript(script);

    }



    /// <summary>
    /// Alternative to SuppressConsentOverlay(): waits for the real "Consent" button
    /// and clicks it, causing the site's own script to set genuine consent cookies.
    /// Slower (polls up to timeoutSeconds) but produces a real consent state, which
    /// SuppressConsentOverlay() never does. Kept here in case a test needs it.
    /// </summary>
    protected void AcceptConsentIfPresent(int timeoutSeconds = 5)
    {
        var consentButton = By.CssSelector("div.fc-consent-root button.fc-cta-consent");
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var button = wait.Until(d =>
            {
                var el = d.FindElements(consentButton).FirstOrDefault();
                return (el != null && el.Displayed && el.Enabled) ? el : null;
            });

            try
            {
                button.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", button);
            }
        }
        catch (WebDriverTimeoutException)
        {
            // Popup didn't appear within the wait window -- nothing to accept.
        }
    }

}