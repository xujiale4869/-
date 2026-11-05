param([switch]$Build,[switch]$Prepare)
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $project
try{
 if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Existing Unity session is open; finish it first.'}
 if(!$Prepare -and (Test-Path Tools/SeaSeamlessV29/accepted-manifest.json)){$m=Get-Content Tools/SeaSeamlessV29/accepted-manifest.json -Raw -Encoding UTF8|ConvertFrom-Json;foreach($f in $m.files){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash -ne $f.sha256){throw ('Accepted file changed: '+$f.path)}};Write-Output ('Verified '+$m.files.Count+' accepted inputs; no overwrite.')}
 if($Build -or $Prepare){$method=if($Prepare){'Deepsea.Editor.SeaSeamlessBuildV29.PrepareAndBuild'}else{'Deepsea.Editor.SeaSeamlessBuildV29.Build'};$args=@('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+$project+'/Temp/seamless-v29-build.log"'));$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe' -ArgumentList $args -WorkingDirectory $project -PassThru;$p.WaitForExit();$p.Refresh();if($p.ExitCode -ne 0){throw ('Unity failed: '+$p.ExitCode)}}
}finally{Pop-Location}
