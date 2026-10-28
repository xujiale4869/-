param(
 [switch]$Build,
 [switch]$GenerateCorals,
 [string]$BlenderPath='C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe'
)
$ErrorActionPreference='Stop'
$projectPath=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if(-not (Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Deepsea'))){throw 'Use the copy inside Unity project Tools/CoastArtV13.'}
$sourceDirectory=Join-Path $PSScriptRoot 'accepted'
if($GenerateCorals){
 & $BlenderPath --background --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'build\02_coral_forms.py')
 if($LASTEXITCODE -ne 0){throw 'Coral generation failed.'}
 $sourceDirectory=Join-Path $PSScriptRoot 'exports'
 Write-Output 'New coral output generated; repeat model and standalone acceptance before treating it as accepted art.'
}
$destination=Join-Path $projectPath 'Assets\Deepsea\ThirdParty\CoastArtV13'
foreach($name in @('CoralLibrary.json','coral-mesh-audit.json')){Copy-Item -LiteralPath (Join-Path $sourceDirectory $name) -Destination (Join-Path $destination $name) -Force}
if($Build){
 $unityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
 $logPath=Join-Path $PSScriptRoot 'build\unity-rebuild.log'
 $process=Start-Process -FilePath $unityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','Deepsea.Editor.CoastMaterialsBuild.Build','-logFile',('"'+$logPath+'"')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0){throw "Unity build failed; see $logPath"}
}
Write-Output 'Coast art resources synced. Build output is Builds/CoastArtV13/GuichaoBay-ArtV13.exe.'
