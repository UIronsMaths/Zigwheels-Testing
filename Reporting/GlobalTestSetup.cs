using NUnit.Framework;

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
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        ExtentReportManager.Flush();
    }
}
