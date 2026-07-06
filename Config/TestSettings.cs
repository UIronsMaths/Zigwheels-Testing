public class TestSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool HeadlessMode { get; set; } = false;
    public int ExplicitWaitSeconds { get; set; } = 2;
    public int PageLoadTimeoutSeconds { get; set; } = 30;
    public string ReportType { get; set; } = string.Empty;
    public string TargetBrand { get; set; } = string.Empty;
    public decimal MinPriceLakhs { get; set; } = 0m;
    public decimal MaxPriceLakhs { get; set; } = decimal.MaxValue;
    public string ScreenshotDirectory { get; set; } = string.Empty;
    public string ExtentReportDirectory { get; set; } = string.Empty;
    public string LogDirectory { get; set; } = string.Empty;
    public string AllureDirectory { get; set; } = string.Empty;
    public string BaseDirectory { get; set; } = string.Empty;
}