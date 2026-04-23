using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TerminalSnake.QualityGate;

// Developer-facing CLI that parses ReportGenerator's Summary.json and fails the
// build when coverage, file reach, or risk-hotspot thresholds are violated.
// Excluded from coverage because it is a build-time script, not product code.
[ExcludeFromCodeCoverage]
internal static class Program
{
    private const double MinLineCoverage = 85.0;
    private const double MinBranchCoverage = 75.0;
    private const double MaxComplexity = 10.0;
    private const double MaxCrapScore = 15.0;

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: QualityGateCheck <path-to-Summary.json>");
            return 2;
        }

        if (!File.Exists(args[0]))
        {
            Console.Error.WriteLine($"Summary file not found: {args[0]}");
            return 2;
        }

        using var stream = File.OpenRead(args[0]);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var failures = new List<string>();
        CheckOverallCoverage(root, failures);
        CheckEveryFileCovered(root, failures);
        CheckRiskHotspots(root, failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("Quality gate: PASSED");
            return 0;
        }

        Console.Error.WriteLine("Quality gate: FAILED");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"  - {failure}");
        }
        return 1;
    }

    private static void CheckOverallCoverage(JsonElement root, List<string> failures)
    {
        if (!root.TryGetProperty("summary", out var summary))
        {
            failures.Add("summary section missing from coverage report");
            return;
        }

        var line = ReadDouble(summary, "linecoverage");
        var branch = ReadDouble(summary, "branchcoverage");

        if (line < MinLineCoverage)
        {
            failures.Add($"line coverage {line:F2}% < required {MinLineCoverage:F2}%");
        }
        if (branch < MinBranchCoverage)
        {
            failures.Add($"branch coverage {branch:F2}% < required {MinBranchCoverage:F2}%");
        }
    }

    private static void CheckEveryFileCovered(JsonElement root, List<string> failures)
    {
        foreach (var cls in EnumerateClasses(root))
        {
            var className = cls.GetProperty("name").GetString() ?? "<unknown>";
            var coverage = ReadDouble(cls, "coverage");
            if (coverage <= 0.0)
            {
                failures.Add($"class {className} has 0% line coverage");
            }
        }
    }

    private static void CheckRiskHotspots(JsonElement root, List<string> failures)
    {
        if (!root.TryGetProperty("riskHotspots", out var hotspots) ||
            hotspots.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var hotspot in hotspots.EnumerateArray())
        {
            var location = $"{hotspot.GetProperty("assembly").GetString()}::{hotspot.GetProperty("class").GetString()}::{hotspot.GetProperty("methodName").GetString()}";
            foreach (var metric in hotspot.GetProperty("statusMetrics").EnumerateArray())
            {
                var name = metric.GetProperty("name").GetString() ?? string.Empty;
                var value = ReadDouble(metric, "value");
                ReportHotspotMetric(failures, location, name, value);
            }
        }
    }

    private static void ReportHotspotMetric(List<string> failures, string location, string name, double value)
    {
        if (name.Equals("Cyclomatic complexity", StringComparison.OrdinalIgnoreCase) && value > MaxComplexity)
        {
            failures.Add($"{location} cyclomatic complexity {value} > {MaxComplexity}");
        }
        else if (name.Equals("CrapScore", StringComparison.OrdinalIgnoreCase) && value > MaxCrapScore)
        {
            failures.Add($"{location} CRAP score {value} > {MaxCrapScore}");
        }
    }

    private static IEnumerable<JsonElement> EnumerateClasses(JsonElement root)
    {
        if (!root.TryGetProperty("coverage", out var coverage) ||
            !coverage.TryGetProperty("assemblies", out var assemblies))
        {
            yield break;
        }

        foreach (var assembly in assemblies.EnumerateArray())
        {
            if (!assembly.TryGetProperty("classes", out var classes))
            {
                continue;
            }
            foreach (var cls in classes.EnumerateArray())
            {
                yield return cls;
            }
        }
    }

    private static double ReadDouble(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return 0.0;
        }
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String => double.TryParse(value.GetString(), out var parsed) ? parsed : 0.0,
            _ => 0.0,
        };
    }
}
