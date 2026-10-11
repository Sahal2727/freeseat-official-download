param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts\FreeSeat-OneFile"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\FreeSeat.App\FreeSeat.App.csproj"
$payloadDirectory = Join-Path $repoRoot "artifacts\FreeSeat-OneFile-Payload"
$archivePath = Join-Path $repoRoot ("artifacts\FreeSeat-Payload-" + [Guid]::NewGuid().ToString("N") + ".zip")
$launcherSource = Join-Path $PSScriptRoot "SingleFileLauncher.cs"
$icon = Join-Path $repoRoot "assets\branding\FreeSeat-monitor-icon.ico"
$outputPath = Join-Path $repoRoot $OutputDirectory
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts") + [IO.Path]::DirectorySeparatorChar)
$outputPath = [IO.Path]::GetFullPath($outputPath)

if (-not $outputPath.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must remain inside $artifactsRoot"
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw ".NET Framework 4.x compiler was not found at $compiler"
}

if (Test-Path -LiteralPath $payloadDirectory) {
    Get-ChildItem -LiteralPath $payloadDirectory -Force | Remove-Item -Force -Recurse
} else {
    New-Item -ItemType Directory -Path $payloadDirectory | Out-Null
}

dotnet publish $project -c $Configuration -r win-x64 --self-contained true --no-restore -m:1 `
    -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false -p:DebugType=None `
    -o $payloadDirectory
if ($LASTEXITCODE -ne 0) { throw "FreeSeat publish failed with exit code $LASTEXITCODE" }

if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
Compress-Archive -Path (Join-Path $payloadDirectory "*") -DestinationPath $archivePath -CompressionLevel Optimal

if (Test-Path -LiteralPath $outputPath) {
    Get-ChildItem -LiteralPath $outputPath -Force | Remove-Item -Force -Recurse
} else {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}
$launcher = Join-Path $outputPath "FreeSeat.exe"
& $compiler /nologo /target:winexe "/out:$launcher" "/win32icon:$icon" `
    /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll `
    /reference:System.Windows.Forms.dll "/resource:$archivePath,FreeSeat.Payload.zip" $launcherSource
if ($LASTEXITCODE -ne 0) { throw "FreeSeat launcher compilation failed with exit code $LASTEXITCODE" }

Get-ChildItem -LiteralPath $outputPath -File | Select-Object Name, Length

Remove-Item -LiteralPath $payloadDirectory -Force -Recurse
Remove-Item -LiteralPath $archivePath -Force
