param(
 [string]$BlenderPath='C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe',
 [string]$PythonPath='C:\Users\许嘉乐\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe',
 [switch]$Build,
 [switch]$Regenerate
)
$ErrorActionPreference='Stop'
$projectPath=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if(-not (Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Deepsea'))){throw 'Run the copy in the Unity project Tools/CoastArtV12 folder.'}
$sourceDirectory=Join-Path $PSScriptRoot 'accepted'
if($Regenerate){
 if(-not (Test-Path -LiteralPath $BlenderPath)){throw 'Set -BlenderPath to the installed Blender executable.'}
 if(-not (Test-Path -LiteralPath $PythonPath)){throw 'Set -PythonPath to Python with NumPy and Pillow.'}
 & $BlenderPath --background --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'build\01_reef_library.py')
 if($LASTEXITCODE -ne 0){throw 'Reef mesh generation failed.'}
 & $PythonPath (Join-Path $PSScriptRoot 'build\03_caustic_texture.py')
 if($LASTEXITCODE -ne 0){throw 'Refracted caustic generation failed.'}
 $sourceDirectory=Join-Path $PSScriptRoot 'exports'
 Write-Output 'New variants generated. Run mesh and standalone acceptance again before treating these as accepted art.'
}
$destination=Join-Path $projectPath 'Assets\Deepsea\ThirdParty\CoastArtV12'
foreach($name in @('ReefLibrary.json','mesh-audit.json','CausticRefracted.png','caustic-audit.json')){
 Copy-Item -LiteralPath (Join-Path $sourceDirectory $name) -Destination (Join-Path $destination $name) -Force
}
if($Build){
 $unityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
 $logPath=Join-Path $PSScriptRoot 'build\unity-rebuild.log'
 $process=Start-Process -FilePath $unityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','Deepsea.Editor.CoastMaterialsBuild.Build','-logFile',('"'+$logPath+'"')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0){throw "Unity build failed. See $logPath"}
}
Write-Output 'Reef models and refracted caustics synced to Unity.'
