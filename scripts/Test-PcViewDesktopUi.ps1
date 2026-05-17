Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$appPath = Join-Path $repoRoot 'artifacts\PcView\PcView.App.exe'
$testRoot = Join-Path $repoRoot 'tests\PcView.App.E2E'

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw "Published app not found: $appPath"
}

$env:PCVIEW_APP_PATH = $appPath
python -m pytest $testRoot
