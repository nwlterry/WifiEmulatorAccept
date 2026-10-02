$ErrorActionPreference = "Stop"

$payload = Split-Path -Parent $MyInvocation.MyCommand.Path
$dest = Join-Path $env:LOCALAPPDATA "Programs\WifiEmulatorAccept"
$desktop = [Environment]::GetFolderPath("Desktop")
$programs = [Environment]::GetFolderPath("Programs")
$shortcutName = "Wi-Fi Emulator Accept.lnk"

$runtimes = & dotnet --list-runtimes 2>$null
if (-not ($runtimes -match "Microsoft\.WindowsDesktop\.App 10\.")) {
    Write-Host "Install the .NET 10 Windows Desktop Runtime, then run this installer again."
    exit 1
}

Get-Process WifiEmulatorAccept -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item -Path (Join-Path $payload "*") -Destination $dest -Force
$icon = Join-Path $dest "App.ico"
$exe = Join-Path $dest "WifiEmulatorAccept.exe"
if (-not (Test-Path $icon) -or -not (Test-Path $exe)) {
    Write-Host "The installer folder is missing App.ico or WifiEmulatorAccept.exe."
    exit 1
}

$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @($desktop, $programs)) {
    $linkPath = Join-Path $folder $shortcutName
    if (Test-Path $linkPath) {
        Remove-Item $linkPath -Force
    }
    $link = $shell.CreateShortcut($linkPath)
    $link.TargetPath = $exe
    $link.WorkingDirectory = $dest
    $link.IconLocation = "$icon,0"
    $link.Description = "Accept the emulator prompt when Wi-Fi reconnects"
    $link.Save()
}

Add-Type -Namespace WifiInstall -Name Native -MemberDefinition @"
[System.Runtime.InteropServices.DllImport("shell32.dll")]
public static extern void SHChangeNotify(int wEventId, uint uFlags, System.IntPtr dwItem1, System.IntPtr dwItem2);
"@
[WifiInstall.Native]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)

Start-Process -FilePath $exe
Write-Host "Installed to $dest"
Write-Host "Shortcuts use $icon"
