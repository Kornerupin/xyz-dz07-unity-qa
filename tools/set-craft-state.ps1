param([Parameter(Mandatory=$true)][ValidateSet('green','broken')][string]$State)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testPath = Join-Path $projectRoot 'Assets/_Project/Tests/PlayMode/CraftTest.cs'
$source = [IO.File]::ReadAllText($testPath)
$pattern = '(const int expectedRemainingWood = )(3|4)(; // COURSE_MUTATION_POINT)'
if ([regex]::Matches($source, $pattern).Count -ne 1) { throw 'Expected exactly one known mutation point.' }
$value = if ($State -eq 'green') { '3' } else { '4' }
$replacement = '${1}' + $value + '${3}'
$result = [regex]::Replace($source, $pattern, $replacement)
if ($result -cne $source) { [IO.File]::WriteAllText($testPath, $result, [Text.UTF8Encoding]::new($false)) }
Write-Host "CraftTest: $State; expected remaining wood = $value"
git -C $projectRoot diff -- Assets/_Project/Tests/PlayMode/CraftTest.cs
