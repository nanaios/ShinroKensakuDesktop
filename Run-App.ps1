param(
    [switch]$Verify,
    [switch]$Database,
    [switch]$Render
)

$ErrorActionPreference = 'Stop'
if (($Database -or $Render) -and -not $Verify) {
    throw '-Database と -Render は -Verify と組み合わせて指定してください。'
}

$systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$candidates = @(
    (Join-Path $env:LOCALAPPDATA 'kaken-dotnet\dotnet.exe'),
    (Join-Path $env:USERPROFILE '.dotnet\dotnet.exe')
)
if ($systemDotnet) { $candidates = @($systemDotnet.Source) + $candidates }
$runtime = $null
foreach ($candidate in $candidates | Select-Object -Unique) {
    if (-not (Test-Path -LiteralPath $candidate)) { continue }
    $sdks = & $candidate --list-sdks
    $runtimes = & $candidate --list-runtimes
    if (($sdks -match '^10\.') -and ($runtimes -match '^Microsoft.WindowsDesktop.App 10\.')) {
        $runtime = $candidate
        break
    }
}
if (-not $runtime) { throw '.NET 10 SDKとWindows Desktopランタイムを導入してください。' }

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
