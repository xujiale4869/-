param([switch]$Build,[switch]$GenerateLighthouse)
$ErrorActionPreference='Stop'
$toolRoot=$PSScriptRoot
$projectPath=[IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
if(-not (Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Deepsea'))){throw 'Run the copy inside the Unity project Tools/CoastArtV14 folder.'}
$blenderPath='C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe'
if($GenerateLighthouse){
 $previousProject=$env:COAST_PROJECT_ROOT
 try {
  $env:COAST_PROJECT_ROOT=$projectPath
  & $blenderPath --background --factory-startup --python-exit-code 1 --python (Join-Path $toolRoot 'build\01_lighthouse.py')
  if($LASTEXITCODE -ne 0){throw 'Lighthouse draft generation failed.'}
 } finally { [Environment]::SetEnvironmentVariable('COAST_PROJECT_ROOT',$previousProject,'Process') }
 Write-Output 'Draft generated in exports. The accepted asset has not been replaced.'
 return
}
$accepted=Join-Path $toolRoot 'accepted\LighthouseMeshes.json'
$target=Join-Path $projectPath 'Assets\Deepsea\ThirdParty\CoastArtV14\LighthouseMeshes.json'
if(-not (Test-Path -LiteralPath $accepted)){throw 'Accepted lighthouse source is missing.'}
Copy-Item -LiteralPath $accepted -Destination $target -Force
$acceptedHash=(Get-FileHash -LiteralPath $accepted -Algorithm SHA256).Hash
if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $acceptedHash){throw 'Accepted asset sync hash mismatch.'}
Write-Output ('Accepted lighthouse synced: '+$acceptedHash)
if($Build){
 $unityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
 $logPath=Join-Path $toolRoot 'rebuild.log'
 $process=Start-Process -FilePath $unityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','Deepsea.Editor.CoastMaterialsBuild.Build','-logFile',('"'+$logPath+'"')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $logPath -SimpleMatch 'COAST_MATERIALS_BUILD_OK' -Quiet)){throw ('Build failed; inspect '+$logPath)}
 Write-Output 'v14 player built. Run the visible standalone acceptance before treating it as verified.'
}
