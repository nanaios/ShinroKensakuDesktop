param(
    [switch]$Verify,
    [switch]$Database,
    [switch]$Render
)

$ErrorActionPreference = 'Stop'
if (($Database -or $Render) -and -not $Verify) {
    throw '-Database と -Render は -Verify と組み合わせて指定してください。'
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw 'dotnetが見つかりません。.NET 10 SDK（x64）を導入してください。' }
$runtime = $dotnet.Source
if (-not ((& $runtime --list-sdks) -match '^10\.') -or
    -not ((& $runtime --list-runtimes) -match '^Microsoft\.WindowsDesktop\.App 10\.')) {
    throw "$runtime に.NET 10 SDKとWindows Desktopランタイムがありません。.NET 10 SDK（x64）を導入してください。"
}

if ($Verify) {
    $project = Join-Path $PSScriptRoot 'tests\SmokeTests\SmokeTests.csproj'
    $options = @()
    if ($Database) { $options += '--database' }
    if ($Render) { $options += '--render' }
    if ($options.Count -gt 0) {
        & $runtime run --project $project -c Release -- @options
    } else {
        & $runtime run --project $project -c Release
    }
} else {
    & $runtime run --project (Join-Path $PSScriptRoot 'ShinroKensakuDesktop.csproj') -c Release
}
exit $LASTEXITCODE
