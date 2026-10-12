param([switch]$Startup, [switch]$NoLaunch)
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'AgentChatScreenshotButton.exe'
if (!(Test-Path -LiteralPath $source)) { $source = Join-Path $PSScriptRoot 'dist\AgentChatScreenshotButton.exe' }
if (!(Test-Path -LiteralPath $source)) { throw 'Build first or download a release ZIP.' }
$destination = Join-Path $env:LOCALAPPDATA 'AgentChatScreenshotButton'
$exe = Join-Path $destination 'AgentChatScreenshotButton.exe'
Get-Process -Name AgentChatScreenshotButton -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe } | Stop-Process
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $exe -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'uninstall.ps1') -Destination $destination -Force
$shell = New-Object -ComObject WScript.Shell
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Agent Chat Screenshot Button.lnk'
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $destination
$shortcut.Description = 'Capture a region and paste into the focused agent chat input'
$shortcut.Save()
if ($Startup) {
    $startupLink = Join-Path ([Environment]::GetFolderPath('Startup')) 'Agent Chat Screenshot Button.lnk'
    $shortcut = $shell.CreateShortcut($startupLink); $shortcut.TargetPath = $exe; $shortcut.Save()
}
Write-Host "Installed: $exe"
if (!$NoLaunch) { Start-Process -FilePath $exe -WindowStyle Hidden }
