param(
    [string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe',
    [ValidateSet('Import','Play','Expedition','Build')][string]$Action = 'Import'
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $EditorPath)) { throw "Unity Editor not found: $EditorPath" }
if (-not $env:ProgramData) { $env:ProgramData = 'C:\ProgramData' }
if (-not $env:ALLUSERSPROFILE) { $env:ALLUSERSPROFILE = 'C:\ProgramData' }
if (-not $env:TMP) { $env:TMP = $env:TEMP }
$verificationPath = Join-Path $projectPath 'Verification'
New-Item -ItemType Directory -Force -Path $verificationPath | Out-Null
$method = switch ($Action) {
    'Import' { 'Deepsea.Editor.ProjectSetup.Verify' }
    'Play' { 'Deepsea.Editor.Verification.PlaySmoke' }
    'Expedition' { 'Deepsea.Editor.ExpeditionPlayCheck.Run' }
    'Build' { 'Deepsea.Editor.ProjectSetup.BuildWindows' }
}
$logPath = Join-Path $verificationPath ('unity-' + $Action.ToLower() + '.log')
$arguments = '-batchmode -projectPath "' + $projectPath + '" -executeMethod ' + $method + ' -logFile "' + $logPath + '"'
if ($Action -notin @('Play','Expedition')) { $arguments += ' -quit' }
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity failed ($($process.ExitCode)); see $logPath" }
Write-Output "Unity $Action passed."
