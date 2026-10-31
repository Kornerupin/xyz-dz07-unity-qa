param([string]$Destination)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $Destination) { $Destination = Join-Path $projectRoot 'course-submissions/dz07/dz07-repository.zip' }
$absolutePath = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $absolutePath) { throw "Archive already exists: $absolutePath. Choose a new filename." }
$null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $absolutePath)
& git -C $projectRoot archive --format=zip --output=$absolutePath HEAD
if ($LASTEXITCODE -ne 0) { throw 'Git archive failed. Commit the intended files first.' }
Write-Host "Exported committed files: $absolutePath"
