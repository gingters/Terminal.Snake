using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Linq;

namespace TerminalSnake.QualityGate;

// Developer-facing CLI that parses Cobertura coverage XML and fails the
// build when coverage, file reach, or risk-hotspot thresholds are violated.
// Excluded from coverage because it is a build-time script, not product code.
[ExcludeFromCodeCoverage]
internal static class Program
{
    private const double MinLineCoverage = 0.85;
    private const double MinBranchCoverage = 0.75;
    private const int MaxCyclomaticComplexity = 10;
    private const double MaxCrapScore = 15.0;

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: QualityGateCheck <coverage-directory>");
            return 2;
        }
        var directory = args[0];
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Coverage directory not found: {directory}");
            return 2;
        }

        var coverageFiles = Directory.GetFiles(directory, "coverage.cobertura.xml", SearchOption.AllDirectories);
        if (coverageFiles.Length == 0)
        {
            Console.Error.WriteLine($"No coverage.cobertura.xml files under {directory}");
            return 2;
        }

        var failures = new List<string>();
        foreach (var file in coverageFiles)
        {
            InspectFile(file, failures);
        }

        return ReportResult(failures);
    }

    private static int ReportResult(List<string> failures)
    {
        if (failures.Count == 0)
        {
            Console.WriteLine("Quality gate: PASSED");
            return 0;
        }
        Console.Error.WriteLine($"Quality gate: FAILED ({failures.Count} issues)");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"  - {failure}");
        }
        return 1;
    }

    private static void InspectFile(string path, List<string> failures)
    {
        var document = XDocument.Load(path);
        var root = document.Root ?? throw new InvalidOperationException($"Empty coverage file: {path}");
        CheckOverallCoverage(root, failures);
        foreach (var cls in root.Descendants("class"))
        {
            CheckClassCoverage(cls, failures);
            foreach (var method in cls.Element("methods")?.Elements("method") ?? Enumerable.Empty<XElement>())
            {
                CheckMethodComplexity(cls, method, failures);
            }
        }
    }

    private static void CheckOverallCoverage(XElement root, List<string> failures)
    {
        var lineRate = ReadDouble(root, "line-rate");
        var branchRate = ReadDouble(root, "branch-rate");
        if (lineRate < MinLineCoverage)
        {
            failures.Add($"overall line coverage {lineRate * 100:F2}% < required {MinLineCoverage * 100:F2}%");
        }
        if (branchRate < MinBranchCoverage)
        {
            failures.Add($"overall branch coverage {branchRate * 100:F2}% < required {MinBranchCoverage * 100:F2}%");
        }
    }

    private static void CheckClassCoverage(XElement cls, List<string> failures)
    {
        var name = cls.Attribute("name")?.Value ?? "<unknown>";
        var lineRate = ReadDouble(cls, "line-rate");
        if (lineRate <= 0.0)
        {
            failures.Add($"class {name} has 0% line coverage");
        }
    }

    private static void CheckMethodComplexity(XElement cls, XElement method, List<string> failures)
    {
        var className = cls.Attribute("name")?.Value ?? "<unknown>";
        var methodName = method.Attribute("name")?.Value ?? "<unknown>";
        var complexity = (int)ReadDouble(method, "complexity");
        var lineRate = ReadDouble(method, "line-rate");
        if (complexity > MaxCyclomaticComplexity)
        {
            failures.Add($"{className}::{methodName} cyclomatic complexity {complexity} > {MaxCyclomaticComplexity}");
        }
        var crap = ComputeCrapScore(complexity, lineRate);
        if (crap > MaxCrapScore)
        {
            failures.Add($"{className}::{methodName} CRAP score {crap:F1} > {MaxCrapScore:F1} (complexity={complexity}, coverage={lineRate * 100:F0}%)");
        }
    }

    private static double ComputeCrapScore(int complexity, double lineRate)
    {
        var uncovered = 1.0 - lineRate;
        return (complexity * complexity) * (uncovered * uncovered) + complexity;
    }

    private static double ReadDouble(XElement element, string attributeName)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return 0.0;
        }
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0.0;
    }
}
