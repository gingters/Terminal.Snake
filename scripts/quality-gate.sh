#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS="$ROOT_DIR/artifacts"
COVERAGE_DIR="$ARTIFACTS/coverage"
REPORT_DIR="$ARTIFACTS/report"

cd "$ROOT_DIR"

echo "==> Restoring local tools"
dotnet tool restore

echo "==> Building solution (Release)"
dotnet build TerminalSnake.slnx -c Release --nologo

echo "==> Running tests with coverage"
rm -rf "$COVERAGE_DIR" "$REPORT_DIR"
dotnet test TerminalSnake.slnx \
    -c Release \
    --no-build \
    --settings "$ROOT_DIR/coverage.runsettings" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$COVERAGE_DIR" \
    --logger "console;verbosity=minimal"

echo "==> Generating coverage report"
dotnet tool run reportgenerator -- \
    "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
    "-targetdir:$REPORT_DIR" \
    "-reporttypes:Html;JsonSummary;TextSummary" \
    "-riskHotspotsAnalysisThresholds:metricThresholdForCyclomaticComplexity=10;metricThresholdForCognitiveComplexity=15;metricThresholdForCrapScore=15"

echo "==> Enforcing quality gate"
dotnet run --project "$ROOT_DIR/scripts/QualityGateCheck/QualityGateCheck.csproj" \
    --configuration Release \
    --no-build \
    -- "$REPORT_DIR/Summary.json"

echo "==> Gate passed. HTML report: $REPORT_DIR/index.html"
