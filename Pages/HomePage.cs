using System;
using OpenQA.Selenium;

public class HomePage : BasePage
{
    private const string BaseUrl = "https://www.zigwheels.com";

    public HomePage(IWebDriver driver) : base(driver) { }

    public void NavigateToHomePage()
    {
        NavigateTo($"{BaseUrl}/");
    }
}
