$ErrorActionPreference = "Stop"

$dest = Join-Path $env:LOCALAPPDATA "Programs\WifiEmulatorAccept"
$shortcutName = "Wi-Fi Emulator Accept.lnk"

Get-Process WifiEmulatorAccept -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

foreach ($folder in @(
    [Environment]::GetFolderPath("Desktop"),
    [Environment]::GetFolderPath("Programs")
)) {
    $linkPath = Join-Path $folder $shortcutName
    if (Test-Path $linkPath) {
        Remove-Item $linkPath -Force
    }
}

if (Test-Path $dest) {
    Remove-Item $dest -Recurse -Force
}

Write-Host "Wi-Fi Emulator Accept was removed."
