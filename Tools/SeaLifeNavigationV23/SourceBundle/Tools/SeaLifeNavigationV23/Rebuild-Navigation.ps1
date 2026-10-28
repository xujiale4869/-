param([switch]$Build)
$ErrorActionPreference='Stop'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'accepted-manifest.json') -Raw | ConvertFrom-Json
foreach($f in $manifest){
 $path=Join-Path $project $f.path
 if(-not (Test-Path -LiteralPath $path)){throw ('Missing frozen input: '+$f.path)}
 if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $f.sha256){throw ('Changed input; inspect and freeze a new revision before rebuilding: '+$f.path)}
}
Write-Output ('V23_INPUTS_VERIFIED '+$manifest.Count)
if($Build){
 New-Item -ItemType Directory -Path (Join-Path $project 'Temp') -Force | Out-Null
 $unity='C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe'
 $log=Join-Path $PSScriptRoot ('build-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.log')
 $p=Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',$project,'-executeMethod','Deepsea.Editor.SeaLifeNavigationBuildV23.Build','-logFile',$log) -WorkingDirectory $project -Wait -PassThru
 if($p.ExitCode -ne 0){throw ('Unity build failed; inspect '+$log)}
 $baseLicenses=Join-Path $project 'Builds/SeaLifeV21/Licenses'
 if(Test-Path -LiteralPath $baseLicenses){Copy-Item -LiteralPath $baseLicenses -Destination (Join-Path $project 'Builds/SeaLifeNavigationV23') -Recurse -Force}
 Write-Output 'SEA_NAVIGATION_V23_REBUILD_OK'
}
