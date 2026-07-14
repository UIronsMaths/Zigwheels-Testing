using Allure.Net.Commons;
using AventStack.ExtentReports;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using System.Text;
using System.Text.Json;

namespace Zigwheels.Utilities
{
    public static class PerformanceManager
    {
        public static void CaptureAndReport(
            IWebDriver driver,
            ExtentTest extentTest,
            string browser,
            TestStatus status)
        {
            try
            {
                if (driver == null)
                    return;

                var js = (IJavaScriptExecutor)driver;

                var metrics = (Dictionary<string, object>?)js.ExecuteScript(@"
                    const nav = performance.getEntriesByType('navigation')[0];

                    if (!nav)
                        return null;

                    return {
                        url: window.location.href,
                        title: document.title,
                        pageLoad: nav.loadEventEnd,
                        domReady: nav.domContentLoadedEventEnd,
                        ttfb: nav.responseStart,
                        timestamp: new Date().toISOString()
                    };
                ");

                if (metrics == null)
                    return;

                var url = metrics["url"]?.ToString() ?? string.Empty;
                var title = metrics["title"]?.ToString() ?? string.Empty;

                double pageLoad = Convert.ToDouble(metrics["pageLoad"]);
                double domReady = Convert.ToDouble(metrics["domReady"]);
                double ttfb = Convert.ToDouble(metrics["ttfb"]);

                var timestamp = metrics["timestamp"]?.ToString() ?? DateTime.UtcNow.ToString("O");

                //--------------------------------------------------------
                // Extent Report
                //--------------------------------------------------------

                extentTest.Info($@"
<h3>Performance Metrics</h3>

<table border='1' cellpadding='5' cellspacing='0' style='border-collapse:collapse'>
    <tr>
        <th>Metric</th>
        <th>Value</th>
    </tr>

    <tr>
        <td>Page Title</td>
        <td>{title}</td>
    </tr>

    <tr>
        <td>URL</td>
        <td>{url}</td>
    </tr>

    <tr>
        <td>Page Load</td>
        <td>{pageLoad:N0} ms</td>
    </tr>

    <tr>
        <td>DOM Ready</td>
        <td>{domReady:N0} ms</td>
    </tr>

    <tr>
        <td>Time To First Byte</td>
        <td>{ttfb:N0} ms</td>
    </tr>

    <tr>
        <td>Captured</td>
        <td>{timestamp}</td>
    </tr>
</table>");

                //--------------------------------------------------------
                // Allure JSON attachment
                //--------------------------------------------------------

                var json = JsonSerializer.Serialize(new
                {
                    Title = title,
                    Url = url,
                    PageLoad = pageLoad,
                    DomReady = domReady,
                    TimeToFirstByte = ttfb,
                    Timestamp = timestamp
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                AllureApi.AddAttachment(
                    "Performance Metrics",
                    "application/json",
                    Encoding.UTF8.GetBytes(json),
                    ".json");

                //--------------------------------------------------------
                // Allure Text Summary
                //--------------------------------------------------------

                var summary =
$"""
Performance Metrics

Page Title : {title}

URL        : {url}

Page Load  : {pageLoad:N0} ms

DOM Ready  : {domReady:N0} ms

TTFB       : {ttfb:N0} ms

Captured   : {timestamp}
""";

                AllureApi.AddAttachment(
                    "Performance Summary",
                    "text/plain",
                    Encoding.UTF8.GetBytes(summary),
                    ".txt");

                //--------------------------------------------------------
                // CSV
                //--------------------------------------------------------

                CsvPerformanceWriter.Write(new PerformanceRecord
                {
                    TestName = TestContext.CurrentContext.Test.Name,

                    Browser = browser,

                    Status = status.ToString(),

                    Environment =
                        Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
                        ?? "Local",

                    Branch =
                        Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME")
                        ?? "Local",

                    Commit =
                        Environment.GetEnvironmentVariable("CI_COMMIT_SHORT_SHA")
                        ?? "Local",

                    PipelineId =
                        Environment.GetEnvironmentVariable("CI_PIPELINE_ID")
                        ?? "Local",

                    Url = url,

                    PageLoad = pageLoad,

                    DomReady = domReady,

                    TTFB = ttfb,

                    Timestamp = DateTime.UtcNow
                });

                extentTest.Info("Performance metrics exported to PerformanceMetrics.csv");
            }
            catch (Exception ex)
            {
                extentTest.Warning($"Unable to capture performance metrics: {ex.Message}");
            }
        }
    }
}