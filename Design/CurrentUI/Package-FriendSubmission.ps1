$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$output = Join-Path $PSScriptRoot 'CatchLabFriend-Submission-Source.zip'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [System.IO.File]::Open($output, [System.IO.FileMode]::Create)
$archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)

try {
    $unityRoots = @('Assets', 'Packages', 'ProjectSettings')
    foreach ($folder in $unityRoots) {
        $directory = Join-Path $workspace "CatchLabFriend\$folder"
        foreach ($file in (Get-ChildItem -LiteralPath $directory -File -Recurse)) {
            $relative = [System.IO.Path]::GetRelativePath($workspace, $file.FullName).Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal)
        }
    }

    foreach ($relative in @('CatchLabFriend\README_TH.md', 'CatchLabFriend\SCORING_STATUS_TH.md',
        'ControllerBridge\bridge.cjs', 'ControllerBridge\rounds.cjs', 'ControllerBridge\rounds.test.cjs',
        'ControllerBridge\game-status.cjs', 'ControllerBridge\game-status.test.cjs',
        'ControllerBridge\package.json', 'ControllerBridge\package-lock.json',
        'ControllerBridge\.env.example',
        'Firmware\CatchController\CatchController.ino',
        'Firmware\CatchController\GameLcd.h',
        'Firmware\CatchController\LcdConfig.h',
        'Firmware\CatchController\Settings.example.h')) {
        $file = Join-Path $workspace $relative
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $relative.Replace('\', '/'), [System.IO.Compression.CompressionLevel]::Optimal)
    }
}
finally {
    $archive.Dispose()
    $stream.Dispose()
}

Write-Output "Created $output"
