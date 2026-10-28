param([switch]$Build)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$projectPath=[IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
if(-not (Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Deepsea'))){throw 'Use the copy inside the Unity project Tools/CoastArtV15 folder.'}
$manifest=Get-Content -LiteralPath (Join-Path $toolRoot 'accepted-manifest.json') -Raw | ConvertFrom-Json
foreach($entry in $manifest.files){
 $source=[IO.Path]::GetFullPath((Join-Path (Join-Path $toolRoot 'accepted') $entry.path))
 $target=[IO.Path]::GetFullPath((Join-Path $projectPath $entry.path))
 if(-not $source.StartsWith((Join-Path $toolRoot 'accepted')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Source escaped accepted folder.'}
 if(-not $target.StartsWith($projectPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Target escaped project.'}
 if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Accepted source changed: '+$entry.path)}
 New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
 Copy-Item -LiteralPath $source -Destination $target -Force
 if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Sync mismatch: '+$entry.path)}
}
Write-Output ('Frozen v15 art synced: '+$manifest.files.Count+' files. No collider or gameplay source is owned by this sync.')
if($Build){
 $unityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
 $logPath=Join-Path $toolRoot 'rebuild.log'
 $process=Start-Process -FilePath $unityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','Deepsea.Editor.CoastMaterialsBuild.Build','-logFile',('"'+$logPath+'"')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $logPath -SimpleMatch 'COAST_MATERIALS_BUILD_OK' -Quiet)){throw ('Build failed; inspect '+$logPath)}
 Write-Output 'v15 player built. Run the visible standalone acceptance before treating a new build as verified.'
}
