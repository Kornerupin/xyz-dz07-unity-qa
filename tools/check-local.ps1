$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
function Find-Tool([string]$Name, [string]$BundledPath) {
    $candidate = Get-Command $Name -ErrorAction SilentlyContinue
    if ($candidate) { return $candidate.Source }
    if (Test-Path -LiteralPath $BundledPath) { return $BundledPath }
    throw "$Name is required. Install it and rerun this script."
}
$runtimeRoot = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies'
$pythonTool = Find-Tool 'python' (Join-Path $runtimeRoot 'python/python.exe')
$nodeTool = Find-Tool 'node' (Join-Path $runtimeRoot 'node/bin/node.exe')
Push-Location $projectRoot
try {
    & $pythonTool -m unittest discover -s tests -v
    if ($LASTEXITCODE -ne 0) { throw 'Python checks failed.' }
    & $nodeTool tests/test_issue.cjs
    if ($LASTEXITCODE -ne 0) { throw 'Issue contract checks failed.' }
    & $nodeTool --check tools/create_ai_issue.cjs
    if ($LASTEXITCODE -ne 0) { throw 'Issue JavaScript syntax failed.' }
    Write-Host 'Local analyzer/mutation/Issue checks passed. Unity and GitHub Actions still require a real run.'
} finally { Pop-Location }
