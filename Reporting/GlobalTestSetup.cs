using NUnit.Framework;
using System.Text.Json;

/// <summary>
/// Runs exactly once before any test in the assembly, and exactly once after
/// every test in the assembly has finished -- regardless of how many
/// [TestFixture] variations (chrome/firefox/edge) run, and regardless of
/// whether they run in parallel. This is the correct place for the Extent
/// report's single init/flush; each fixture's own OneTimeSetUp/OneTimeTearDown
/// runs per-fixture instead, which would risk multiple concurrent flushes.
/// </summary>
[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // Touching .Extent triggers lazy initialisation.
        _ = ExtentReportManager.Extent;
        ConfigureAllureResultsDirectory();
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        ExtentReportManager.Flush();
    }

    private static void ConfigureAllureResultsDirectory()
    {
        var resultsDir = Path.Combine(ExtentReportManager.OutputRoot, "TestResults", "allure-results");
        if (Directory.Exists(resultsDir)) Directory.Delete(resultsDir, true);
        Directory.CreateDirectory(resultsDir);

        var configPath = Path.Combine(Path.GetTempPath(), $"allureConfig_{Guid.NewGuid():N}.json");
        var json = JsonSerializer.Serialize(new { allure = new { directory = resultsDir } });
        File.WriteAllText(configPath, json);

        Environment.SetEnvironmentVariable("ALLURE_CONFIG", configPath);
    }

}
