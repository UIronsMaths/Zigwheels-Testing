using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
public static class DriverFactory
{
    private static readonly Uri GridHubUri = new Uri("http://localhost:4444/wd/hub");

    public static IWebDriver Create(string browser, bool headless)
    {
        IWebDriver driver = browser.ToLowerInvariant() switch
        {
            "firefox" => new RemoteWebDriver(GridHubUri, CreateFirefoxOptions(headless)),
            "edge" => new RemoteWebDriver(GridHubUri, CreateEdgeOptions(headless)),
            _ => new RemoteWebDriver(GridHubUri, CreateChromeOptions(headless))
        };
        driver.Manage().Window.Maximize();
        return driver;
    }
    private static ChromeOptions CreateChromeOptions(bool headless)
    {
        var options = new ChromeOptions();
        if (headless) options.AddArgument("--headless=new");

        // Don't wait for full onload (ads/analytics can keep the page "loading" indefinitely)
        options.PageLoadStrategy = PageLoadStrategy.Eager;

        // --- COOKIE POPUP SUPPRESSION ---
        options.AddArgument("--w3c-cookie-consent");
        options.AddArgument("--disable-cookie-consent");
        options.AddArgument("--disable-cookies");

        // Performance optimizations
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-first-run");
        options.AddArgument("--no-default-browser-check");
        options.AddArgument("--disable-default-apps");
        options.AddArgument("--disable-sync");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-web-resources");

        // Disable notifications and password features
        options.AddArgument("--disable-notifications");
        options.AddArgument("--disable-save-password-bubble");
        options.AddArgument("--disable-password-manager-reauthentication");
        options.AddArgument("--disable-autofill-keyboard-accessory-view");
        options.AddArgument("--disable-component-extensions-with-background-pages");
        options.AddArgument("--disable-features=PasswordLeakDetection");
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);

        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("credentials_enable_service", false);

        // Suppress Chrome's "Enhanced ad privacy" / Privacy Sandbox onboarding dialog.
        // Without this, Chrome blocks waiting for a user to accept/dismiss it, which
        // manifests as the renderer-timeout you were seeing.
        options.AddArgument("--disable-features=PrivacySandboxAdsAPIsOverride,PrivacySandboxSettings4");
        options.AddUserProfilePreference("privacy_sandbox.consent_decision_made", true);
        options.AddUserProfilePreference("privacy_sandbox.consent_decision_made_v2", true);
        options.AddUserProfilePreference("privacy_sandbox.topics_data_accessible_since_v2", true);

        return options;
    }
    private static EdgeOptions CreateEdgeOptions(bool headless)
    {
        var options = new EdgeOptions();
        if (headless) options.AddArgument("--headless=new");

        // Don't wait for full onload (ads/analytics can keep the page "loading" indefinitely)
        options.PageLoadStrategy = PageLoadStrategy.Eager;


        // --- COOKIE POPUP SUPPRESSION ---
        options.AddArgument("--w3c-cookie-consent");
        options.AddArgument("--disable-cookie-consent");
        options.AddUserProfilePreference("profile.default_content_setting_values.cookies", 1);

        // Performance optimizations
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-sync");

        return options;
    }
    private static FirefoxOptions CreateFirefoxOptions(bool headless)
    {
        var options = new FirefoxOptions();
        if (headless) options.AddArgument("-headless");

        // Don't wait for full onload (ads/analytics can keep the page "loading" indefinitely)
        options.PageLoadStrategy = PageLoadStrategy.Eager;

        // --- COOKIE POPUP SUPPRESSION (Firefox Built-in Feature) ---
        // 1 = Reject all cookies, 2 = Accept all cookies (when a banner attempts to force a choice)
        options.SetPreference("cookiebanners.service.mode", 2);
        options.SetPreference("cookiebanners.service.mode.privateBrowsing", 2);

        // Performance optimizations
        options.SetPreference("dom.max_script_run_time", 30);
        options.SetPreference("browser.sessionstore.max_tabs_undo", 0);
        options.SetPreference("privacy.trackingprotection.enabled", false);

        return options;
    }
}