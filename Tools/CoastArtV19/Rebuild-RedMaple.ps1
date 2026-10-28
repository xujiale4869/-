param([switch]$Build)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'accepted-manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest) {
    $sourceFile = Join-Path (Join-Path $PSScriptRoot 'accepted') $entry.path
    if ((Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) { throw "Frozen asset mismatch: $($entry.path)" }
    $targetFile = Join-Path $projectRoot $entry.path
    New-Item -ItemType Directory -Path (Split-Path -Parent $targetFile) -Force | Out-Null
    Copy-Item -LiteralPath $sourceFile -Destination $targetFile -Force
}
Write-Output "Restored and verified $($manifest.Count) frozen inputs."
if ($Build) {
    $editorPath = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
    $logPath = Join-Path $PSScriptRoot 'rebuild.log'
    $buildProcess = Start-Process -FilePath $editorPath -ArgumentList @('-batchmode','-quit','-projectPath',('"' + $projectRoot + '"'),'-executeMethod','Deepsea.Editor.CoastMapleBuild.Build','-logFile',('"' + $logPath + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($buildProcess.ExitCode -ne 0) { throw "Unity build failed; inspect $logPath" }
}
