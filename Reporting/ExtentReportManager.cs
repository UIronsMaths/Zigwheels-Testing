using System;
using System.IO;
using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;

/// <summary>
/// Wraps a single, shared ExtentReports instance for the entire test run.
///
/// IMPORTANT: this is deliberately a lazy singleton rather than something
/// initialised in each fixture's OneTimeSetUp. With 3 parallel [TestFixture]
/// browsers (chrome/firefox/edge), per-fixture setup/teardown runs once PER
/// FIXTURE, not once for the whole assembly -- calling Flush() from each
/// fixture's OneTimeTearDown would mean up to 3 concurrent writes to the same
/// report file, which ExtentSparkReporter isn't designed to handle safely.
///
/// Init/flush lifecycle instead lives in GlobalTestSetup ([SetUpFixture]),
/// which NUnit guarantees runs exactly once before/after ALL fixtures in the
/// assembly, regardless of how many run in parallel.
/// </summary>
public static class ExtentReportManager
{
    private static readonly object InitLock = new();
    private static ExtentReports? _extent;

    /// <summary>
    /// The directory reports/screenshots should be written under.
    ///
    /// Directory.GetCurrentDirectory() during `dotnet test` resolves to the
    /// build output folder (bin/Debug/net10.0/...), NOT the project root --
    /// that's a runtime/process-launch detail, unrelated to where you
    /// actually ran the command from. So instead:
    ///
    /// 1. If TEST_OUTPUT_ROOT is set (run-tests.ps1 and CI both set this),
    ///    use it directly -- this guarantees the C# side and the PowerShell
    ///    side agree on the exact same path, since one explicitly tells the
    ///    other rather than each independently guessing.
    /// 2. Otherwise (e.g. running via an IDE's test runner, or a plain
    ///    `dotnet test` without the wrapper script), fall back to walking up
    ///    from the build output directory. bin/<Config>/<TFM> is always 3
    ///    levels below the project directory regardless of which config or
    ///    target framework you're building, so this is a stable fallback
    ///    even though the exact folder names vary.
    /// </summary>
    public static string OutputRoot { get; } =
        Environment.GetEnvironmentVariable("TEST_OUTPUT_ROOT")
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    public static string ReportPath { get; } =
        Path.Combine(OutputRoot, "TestResults", "ExtentReport", "index.html");

    public static ExtentReports Extent
    {
        get
        {
            if (_extent == null)
            {
                lock (InitLock)
                {
                    _extent ??= Create();
                }
            }
            return _extent;
        }
    }

    private static ExtentReports Create()
    {
        var reportDir = Path.GetDirectoryName(ReportPath)!;
        if (Directory.Exists(reportDir)) Directory.Delete(reportDir, true);
        Directory.CreateDirectory(reportDir);

        var screenshotDir = Path.Combine(OutputRoot, "screenshots");
        if (Directory.Exists(screenshotDir)) Directory.Delete(screenshotDir, true);

        var spark = new ExtentSparkReporter(ReportPath);
        spark.Config.DocumentTitle = "Zigwheels Test Report";
        spark.Config.ReportName = "Zigwheels Automated Test Run";

        var extent = new ExtentReports();
        extent.AttachReporter(spark);
        extent.AddSystemInfo("Environment", Environment.GetEnvironmentVariable("CI") == null ? "Local" : "CI");

        return extent;
    }

    /// <summary>
    /// Writes the report to disk. Safe to call even if Extent was never
    /// accessed (e.g. an empty/filtered test run) -- it's a no-op in that case.
    /// </summary>
    public static void Flush() => _extent?.Flush();
}