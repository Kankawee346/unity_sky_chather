$ErrorActionPreference = 'Stop'
$script = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Package-Code-Lite.py'))
python $script
if ($LASTEXITCODE -ne 0) { throw "Package-Code-Lite.py failed ($LASTEXITCODE)" }
