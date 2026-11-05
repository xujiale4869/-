param([switch]$Build,[switch]$Regenerate)
$ErrorActionPreference='Stop'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $project
try {
 if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close or finish the existing Unity session first.'}
 if($Regenerate){
  & 'C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe' -b -P Tools/SeaHarborV27/generate_harbor_and_reef_v27.py -- --out-dir Assets/Deepsea/Resources/SeaHarborV27
  if($LASTEXITCODE -ne 0){throw 'Blender failed'}
  Write-Output 'Generated a new draft. Recheck runtime images before replacing the accepted manifest.'
 } else {
  $accepted=Get-Content Tools/SeaHarborV27/accepted-manifest.json -Raw -Encoding UTF8|ConvertFrom-Json
  foreach($item in $accepted.files){if(!(Test-Path -LiteralPath $item.path)){throw ('Missing '+$item.path)};if((Get-FileHash -LiteralPath $item.path -Algorithm SHA256).Hash -ne $item.sha256){throw ('Hash changed: '+$item.path)}}
  Write-Output ('Verified '+$accepted.files.Count+' accepted inputs; no files restored or overwritten.')
 }
 if($Build){
  $method=if($Regenerate){'Deepsea.Editor.SeaHarborBuildV27.ImportAndBuild'}else{'Deepsea.Editor.SeaHarborBuildV27.Build'}
  $args=@('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+$project+'/Temp/harbor-v27-rebuild.log"'))
  $editor=Start-Process 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $args -WorkingDirectory $project -PassThru -Wait
  if($editor.ExitCode -ne 0){throw ('Unity failed: '+$editor.ExitCode)}
 }
} finally {Pop-Location}
