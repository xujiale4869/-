param([switch]$Build)
$ErrorActionPreference='Stop'
$taskProject=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$taskManifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'accepted-manifest.json') -Raw | ConvertFrom-Json
$taskChanged=@()
foreach($taskItem in $taskManifest){$taskPath=[IO.Path]::GetFullPath((Join-Path $taskProject $taskItem.path));if(!$taskPath.StartsWith($taskProject+[IO.Path]::DirectorySeparatorChar)){throw 'Manifest path is outside project'};if(!(Test-Path -LiteralPath $taskPath) -or (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash -ne $taskItem.sha256){$taskChanged+=$taskItem.path}}
if($taskChanged.Count){$taskChanged | ForEach-Object {Write-Output ('Changed: '+$_)};throw 'Inputs differ from accepted version; review changes before building. No automatic restore was performed.'}
Write-Output ('Verified '+$taskManifest.Count+' accepted inputs. No files restored.')
if($Build){$taskLog=Join-Path $taskProject ('Verification\sea-water-v22-build\rebuild-'+(Get-Date -Format yyyyMMdd-HHmmss)+'.log');$taskArgs=@('-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod','Deepsea.Editor.SeaLifeWaterBuildV22.Build','-logFile',('"'+$taskLog+'"'),'-quit');$taskProcess=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru;[pscustomobject]@{processId=$taskProcess.Id;log=$taskLog}|ConvertTo-Json}
