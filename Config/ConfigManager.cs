using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

public static class ConfigurationManager
{
    private static readonly Lazy<TestSettings> _settings = new Lazy<TestSettings>(Load);

    public static TestSettings Settings => _settings.Value;

    private static TestSettings Load()
    {
        var envPath = FindEnvFile();
        if (string.IsNullOrEmpty(envPath))
            throw new FileNotFoundException("Required .env file not found. Place a .env file in the repository root or a parent folder.");

        var envDict = ParseDotEnv(envPath);

        // Map .env keys into configuration under TestSettings. Keys use the
        // TestSettings__Property double-underscore convention exclusively.
        var mapped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in envDict)
        {
            var key = kv.Key?.Trim() ?? string.Empty;
            var value = kv.Value ?? string.Empty;
            if (string.IsNullOrEmpty(key))
                continue;

            var configKey = key.Replace("__", ":");
            mapped[configKey] = value;
        }

        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(mapped)
            .AddEnvironmentVariables();

        IConfiguration config = builder.Build();

        var settings = new TestSettings();
        config.GetSection("TestSettings").Bind(settings);

        // Also apply TestSettings__Property environment variable overrides if present
        ApplyOverrides(settings);

        return settings;
    }

    private static string? FindEnvFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }

    private static IDictionary<string, string> ParseDotEnv(string path)
    {
        var dict = new Dictionary<string, string>();
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;
            if (line.StartsWith("#") || line.StartsWith("//"))
                continue;

            var content = line;
            // support `export KEY=VALUE`
            if (content.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                content = content.Substring("export ".Length);

            var idx = content.IndexOf('=');
            if (idx <= 0)
                continue;

            var key = content.Substring(0, idx).Trim();
            var val = content.Substring(idx + 1).Trim();

            // strip surrounding quotes
            if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                val = val.Substring(1, val.Length - 2);

            dict[key] = val;
        }

        return dict;
    }

    private static void ApplyOverrides(TestSettings settings)
    {
        var baseUrl = Environment.GetEnvironmentVariable("TestSettings__BaseUrl");
        if (!string.IsNullOrWhiteSpace(baseUrl))
            settings.BaseUrl = baseUrl;

        var headlessMode = Environment.GetEnvironmentVariable("TestSettings__HeadlessMode");
        if (bool.TryParse(headlessMode, out var headlessModeBool))
            settings.HeadlessMode = headlessModeBool;

        var explicitWait = Environment.GetEnvironmentVariable("TestSettings__ExplicitWaitSeconds");
        if (int.TryParse(explicitWait, out var explicitWaitInt))
            settings.ExplicitWaitSeconds = explicitWaitInt;

        var pageLoadTimeout = Environment.GetEnvironmentVariable("TestSettings__PageLoadTimeoutSeconds");
        if (int.TryParse(pageLoadTimeout, out var pageLoadTimeoutInt))
            settings.PageLoadTimeoutSeconds = pageLoadTimeoutInt;

        var reportType = Environment.GetEnvironmentVariable("TestSettings__ReportType");
        if (!string.IsNullOrWhiteSpace(reportType))
            settings.ReportType = reportType;

        var targetBrand = Environment.GetEnvironmentVariable("TestSettings__TargetBrand");
        if (!string.IsNullOrWhiteSpace(targetBrand))
            settings.TargetBrand = targetBrand;

        var minPrice = Environment.GetEnvironmentVariable("TestSettings__MinPriceLakhs");
        if (decimal.TryParse(minPrice, out var minPriceDecimal))
            settings.MinPriceLakhs = minPriceDecimal;

        var maxPrice = Environment.GetEnvironmentVariable("TestSettings__MaxPriceLakhs");
        if (decimal.TryParse(maxPrice, out var maxPriceDecimal))
            settings.MaxPriceLakhs = maxPriceDecimal;

        var screenshotDir = Environment.GetEnvironmentVariable("TestSettings__ScreenshotDirectory");
        if (!string.IsNullOrWhiteSpace(screenshotDir))
            settings.ScreenshotDirectory = screenshotDir;

        var extentReportDir = Environment.GetEnvironmentVariable("TestSettings__ExtentReportDirectory");
        if (!string.IsNullOrWhiteSpace(extentReportDir))
            settings.ExtentReportDirectory = extentReportDir;

        var logDir = Environment.GetEnvironmentVariable("TestSettings__LogDirectory");
        if (!string.IsNullOrWhiteSpace(logDir))
            settings.LogDirectory = logDir;

        var allureDir = Environment.GetEnvironmentVariable("TestSettings__AllureDirectory");
        if (!string.IsNullOrWhiteSpace(allureDir))
            settings.AllureDirectory = allureDir;

        var baseDir = Environment.GetEnvironmentVariable("TestSettings__BaseDirectory");
        if (!string.IsNullOrWhiteSpace(baseDir))
            settings.BaseDirectory = baseDir;
    }
}