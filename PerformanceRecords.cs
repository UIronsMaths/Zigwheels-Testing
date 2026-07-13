public class PerformanceRecord
{
    public string TestName { get; set; } = "";

    public string Browser { get; set; } = "";

    public string Status { get; set; } = "";

    public string Environment { get; set; } = "";

    public string Branch { get; set; } = "";

    public string Commit { get; set; } = "";

    public string PipelineId { get; set; } = "";

    public string Url { get; set; } = "";

    public double PageLoad { get; set; }

    public double DomReady { get; set; }

    public double TTFB { get; set; }

    public DateTime Timestamp { get; set; }
}