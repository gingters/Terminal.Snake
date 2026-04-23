#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir   = Resolve-Path (Join-Path $scriptDir '..')
$artifacts = Join-Path $rootDir 'artifacts'
$coverage  = Join-Path $artifacts 'coverage'
$report    = Join-Path $artifacts 'report'

Push-Location $rootDir
try {
    Write-Host "==> Restoring local tools"
    dotnet tool restore | Out-Null

    Write-Host "==> Building solution (Release)"
    dotnet build TerminalSnake.slnx -c Release --nologo

    Write-Host "==> Running tests with coverage"
    if (Test-Path $coverage) { Remove-Item $coverage -Recurse -Force }
    if (Test-Path $report)   { Remove-Item $report   -Recurse -Force }
    dotnet test TerminalSnake.slnx `
        -c Release `
        --no-build `
        --settings (Join-Path $rootDir 'coverage.runsettings') `
        --collect:"XPlat Code Coverage" `
        --results-directory $coverage `
        --logger 'console;verbosity=minimal'

    Write-Host "==> Generating coverage report"
    dotnet tool run reportgenerator -- `
        "-reports:$coverage/**/coverage.cobertura.xml" `
        "-targetdir:$report" `
        "-reporttypes:Html;JsonSummary;TextSummary"

    Write-Host "==> Enforcing quality gate"
    dotnet run --project (Join-Path $rootDir 'scripts/QualityGateCheck/QualityGateCheck.csproj') `
        --configuration Release `
        --no-build `
        -- $coverage

    Write-Host "==> Gate passed. HTML report: $report/index.html"
}
finally {
    Pop-Location
}
