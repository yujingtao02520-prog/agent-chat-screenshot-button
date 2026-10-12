param([switch]$RemoveCaptures)
$ErrorActionPreference = 'Stop'
$destination = Join-Path $env:LOCALAPPDATA 'AgentChatScreenshotButton'
$exe = Join-Path $destination 'AgentChatScreenshotButton.exe'
Get-Process -Name AgentChatScreenshotButton -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe } | Stop-Process
foreach ($folder in @([Environment]::GetFolderPath('Programs'), [Environment]::GetFolderPath('Startup'))) {
    $shortcut = Join-Path $folder 'Agent Chat Screenshot Button.lnk'
    if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut }
}
if (Test-Path -LiteralPath $exe) { Remove-Item -LiteralPath $exe }
if ($RemoveCaptures) {
    $captures = Join-Path $destination 'Captures'
    $expected = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'AgentChatScreenshotButton\Captures'))
    if ([IO.Path]::GetFullPath($captures) -ne $expected) { throw 'Unexpected capture path.' }
    if (Test-Path -LiteralPath $captures) { Remove-Item -LiteralPath $captures -Recurse -Force }
}
Write-Host 'Uninstalled. Captures are preserved unless -RemoveCaptures is specified.'
