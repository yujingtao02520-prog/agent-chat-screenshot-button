param([switch]$Test)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Requires Windows x64 with .NET Framework 4.8 (Windows 10/11).' }
$framework = Split-Path $compiler
$out = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$argsList = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+',
    "/out:$out\AgentChatScreenshotButton.exe", "/win32manifest:$PSScriptRoot\app.manifest",
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
    "/reference:$framework\WPF\UIAutomationClient.dll", "/reference:$framework\WPF\UIAutomationTypes.dll", "/reference:$framework\WPF\WindowsBase.dll",
    "$PSScriptRoot\src\AgentSnip.cs")
& $compiler @argsList
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath "$PSScriptRoot\install.ps1", "$PSScriptRoot\uninstall.ps1", "$PSScriptRoot\Install.cmd", "$PSScriptRoot\README.md", "$PSScriptRoot\LICENSE" -Destination $out
if ($Test) {
    $process = Start-Process -FilePath "$out\AgentChatScreenshotButton.exe" -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Self-test failed: $($process.ExitCode)" }
    Get-Content -LiteralPath "$out\self-test-result.txt"
    Remove-Item -LiteralPath "$out\self-test-result.txt"
}
Get-FileHash -LiteralPath "$out\AgentChatScreenshotButton.exe" -Algorithm SHA256
