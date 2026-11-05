param([switch]$Build,[switch]$Prepare)
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $project
try {
 if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'An existing Unity session is open; finish it first.'}
 if(!$Prepare -and (Test-Path Tools/SeaExpansionV28/accepted-manifest.json)){
  $accepted=Get-Content Tools/SeaExpansionV28/accepted-manifest.json -Raw -Encoding UTF8|ConvertFrom-Json
  foreach($f in $accepted.files){if(!(Test-Path -LiteralPath $f.path)){throw ('Missing '+$f.path)};if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash -ne $f.sha256){throw ('Changed accepted input: '+$f.path)}}
  Write-Output ('Verified '+$accepted.files.Count+' accepted v28 inputs; no files overwritten.')
 }
 if($Build -or $Prepare){
  $method=if($Prepare){'Deepsea.Editor.SeaBayExpansionBuildV28.PrepareAndBuild'}else{'Deepsea.Editor.SeaBayExpansionBuildV28.Build'}
  $args=@('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+$project+'/Temp/bay-v28-build.log"'))
  $p=Start-Process 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $args -WorkingDirectory $project -PassThru -Wait
  if($p.ExitCode -ne 0){throw ('Unity failed: '+$p.ExitCode)}
 }
}finally{Pop-Location}
