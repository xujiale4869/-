$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$exe=Join-Path $p 'Builds\CoastDetailV1\GuichaoBay.exe'
while(Get-Process -Id 752 -ErrorAction SilentlyContinue){Start-Sleep -Milliseconds 500}
if(!(Select-String -LiteralPath (Join-Path $p 'Verification\coast-detail-build.log') -Pattern 'COAST_DETAIL_BUILD_OK')){throw 'Build did not complete'}
foreach($mode in @('before','after','ecology')){
 $a=@('-coast-detail-check');$relative='Verification\coast-detail-v1\'+$mode+'\result.json'
 if($mode -eq 'before'){$a+='-coast-detail-baseline'}
 if($mode -eq 'after'){$a+='-coast-detail-demo'}
 if($mode -eq 'ecology'){$a=@('-ecology-check');$relative='Verification\ecology-v1\result.json'}
 $a+=@('-logFile',('"'+$p+'\Verification\coast-detail-'+$mode+'.log"'))
 $started=Get-Date;$r=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList $a -Wait -PassThru
 $f=Join-Path $p $relative;if(!(Test-Path -LiteralPath $f)){throw ('No result '+$mode)}
 if((Get-Item -LiteralPath $f).LastWriteTime -lt $started){throw ('Stale result '+$mode)}
 Get-Content -LiteralPath $f -Raw
 if($r.ExitCode -ne 0 -or !(Get-Content -LiteralPath $f -Raw|ConvertFrom-Json).passed){throw ('Check failed '+$mode)}
 if($mode -eq 'ecology'){Copy-Item -LiteralPath $f -Destination (Join-Path $p 'Verification\coast-detail-v1\ecology-result.json') -Force}
 Write-Output ('PASSED '+$mode)
}
$u='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe';$started=Get-Date
$r=Start-Process -FilePath $u -ArgumentList @('-batchmode','-projectPath',('"'+$p+'"'),'-executeMethod','Deepsea.Editor.ExpeditionPlayCheck.RunTerrain','-logFile',('"'+$p+'\Verification\coast-detail-terrain.log"')) -Wait -PassThru
$f=Join-Path $p 'Verification\unity-expedition-result.json'
if($r.ExitCode -ne 0 -or (Get-Item -LiteralPath $f).LastWriteTime -lt $started -or !(Get-Content -LiteralPath $f -Raw|ConvertFrom-Json).passed){throw 'Terrain regression failed'}
Copy-Item -LiteralPath $f -Destination (Join-Path $p 'Verification\coast-detail-v1\terrain-result.json') -Force
Write-Output 'COAST_DETAIL_FINAL_CHECKS_PASSED'
