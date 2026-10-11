param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts\FreeSeat-Setup",
    [string]$OutputBaseFilename = "FreeSeat-Setup"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishScript = Join-Path $PSScriptRoot "Publish-OneFile.ps1"
$setupScript = Join-Path $repoRoot "src\FreeSeat.Setup\FreeSeat.iss"
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts") + [IO.Path]::DirectorySeparatorChar)
$setupOutputDirectory = [IO.Path]::GetFullPath([IO.Path]::Combine($repoRoot, $OutputDirectory))
$setupOutputPath = Join-Path $setupOutputDirectory ($OutputBaseFilename + ".exe")
$innoCompiler = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"

if (-not $setupOutputDirectory.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must remain inside $artifactsRoot"
}
if ($OutputBaseFilename -notmatch '^[A-Za-z0-9._-]+$') {
    throw "OutputBaseFilename may contain only letters, numbers, periods, underscores, and hyphens."
}

if (-not (Test-Path -LiteralPath $publishScript)) {
    throw "The one-file application publishing script was not found: $publishScript"
}
if (-not (Test-Path -LiteralPath $setupScript)) {
    throw "The Inno Setup script was not found: $setupScript"
}
if (-not (Test-Path -LiteralPath $innoCompiler)) {
    throw "Inno Setup 6 compiler was not found: $innoCompiler"
}

& $publishScript -Configuration $Configuration
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    throw "The FreeSeat one-file publish failed with exit code $LASTEXITCODE"
}

$applicationPath = Join-Path $repoRoot "artifacts\FreeSeat-OneFile\FreeSeat.exe"
if (-not (Test-Path -LiteralPath $applicationPath)) {
    throw "The published one-file app was not found: $applicationPath"
}
if ((Get-Item -LiteralPath $applicationPath).Length -lt 1MB) {
    throw "The published FreeSeat executable is unexpectedly small."
}

New-Item -ItemType Directory -Path $setupOutputDirectory -Force | Out-Null
if (Test-Path -LiteralPath $setupOutputPath) {
    Remove-Item -LiteralPath $setupOutputPath -Force
}

& $innoCompiler /Qp ("/O" + $setupOutputDirectory) ("/F" + $OutputBaseFilename) $setupScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
}
if (-not (Test-Path -LiteralPath $setupOutputPath)) {
    throw "Inno Setup completed without producing $setupOutputPath"
}

Get-Item -LiteralPath $setupOutputPath | Select-Object FullName, Length, LastWriteTime
