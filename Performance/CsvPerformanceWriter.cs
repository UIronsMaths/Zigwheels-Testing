using System.Globalization;
using System.Text;

public static class CsvPerformanceWriter
{
    private static readonly object LockObject = new();

    public static void Write(PerformanceRecord record)
    {
        var directory = Path.Combine(
            Environment.GetEnvironmentVariable("TEST_OUTPUT_ROOT")
                ?? Directory.GetCurrentDirectory(),
            "TestResults");

        Directory.CreateDirectory(directory);

        var csv = Path.Combine(directory, "PerformanceMetrics.csv");

        lock (LockObject)
        {
            var newFile = !File.Exists(csv);

            using var writer = new StreamWriter(csv, true, Encoding.UTF8);

            if (newFile)
            {
                writer.WriteLine(
                    "Test,Browser,Status,Url,PageLoad,DomReady,TTFB,Timestamp");
            }

            writer.WriteLine(string.Join(",",
                Escape(record.TestName),
                Escape(record.Browser),
                Escape(record.Status),
                Escape(record.Url),
                record.PageLoad.ToString(CultureInfo.InvariantCulture),
                record.DomReady.ToString(CultureInfo.InvariantCulture),
                record.TTFB.ToString(CultureInfo.InvariantCulture),
                record.Timestamp.ToString("O")));
        }
    }

    private static string Escape(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}