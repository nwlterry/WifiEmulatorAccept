$ErrorActionPreference = "Stop"

$dest = Join-Path $env:LOCALAPPDATA "Programs\WifiEmulatorAccept"
$shortcutNames = @(
    "Wi-Fi Emulator Accept.lnk",
    "Wi-Fi Emulator Accept (.NET 8).lnk",
    "Wi-Fi Emulator Accept (.NET 10).lnk"
)

Get-Process WifiEmulatorAccept -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

foreach ($folder in @(
    [Environment]::GetFolderPath("Desktop"),
    [Environment]::GetFolderPath("Programs")
)) {
    foreach ($shortcutName in $shortcutNames) {
        $linkPath = Join-Path $folder $shortcutName
        if (Test-Path $linkPath) {
            Remove-Item $linkPath -Force
        }
    }
}

if (Test-Path $dest) {
    Remove-Item $dest -Recurse -Force
}

Write-Host "Wi-Fi Emulator Accept, a pure .NET program for Windows only, was removed."
