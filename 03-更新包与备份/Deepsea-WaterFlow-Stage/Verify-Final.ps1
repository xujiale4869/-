$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$u='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
$exe=Join-Path $p 'Builds\WaterFlowV1\GuichaoBay.exe'
function Editor($method,$log){$run=Start-Process -FilePath $u -ArgumentList @('-batchmode','-projectPath',('"'+$p+'"'),'-executeMethod',$method,'-logFile',('"'+$p+'\Verification\'+$log+'"')) -Wait -PassThru; if($run.ExitCode -ne 0){throw ('Editor failed '+$method)}}
$run=Start-Process -FilePath $u -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$p+'"'),'-executeMethod','Deepsea.Editor.WaterFlowBuild.Build','-logFile',('"'+$p+'\Verification\water-flow-build.log"')) -Wait -PassThru
if($run.ExitCode -ne 0){throw 'Build failed'}
foreach($mode in @('before','after','ecology')){
 $args=@('-water-flow-check');$result='Verification\water-flow-v1\'+$mode+'\result.json'
 if($mode -eq 'before'){$args+='-flow-baseline'}
 if($mode -eq 'ecology'){$args=@('-ecology-check');$result='Verification\ecology-v1\result.json'}
 $args+=@('-logFile',('"'+$p+'\Verification\water-flow-'+$mode+'.log"'))
 $started=Get-Date;$run=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList $args -Wait -PassThru
 $rp=Join-Path $p $result
 if($run.ExitCode -ne 0 -or !(Test-Path -LiteralPath $rp)){throw ('Player failed '+$mode)}
 if((Get-Item -LiteralPath $rp).LastWriteTime -lt $started){throw ('Stale result '+$mode)}
 $j=Get-Content -LiteralPath $rp -Raw|ConvertFrom-Json;if(!$j.passed){throw ('Check failed '+$mode)}
 Write-Output ('PASSED '+$mode);Get-Content -LiteralPath $rp -Raw
 if($mode -eq 'ecology'){Copy-Item -LiteralPath $rp -Destination (Join-Path $p 'Verification\water-flow-v1\ecology-result.json') -Force}
}
$started=Get-Date
Editor 'Deepsea.Editor.ExpeditionPlayCheck.RunTerrain' 'water-flow-terrain.log'
$rp=Join-Path $p 'Verification\unity-expedition-result.json'
if((Get-Item -LiteralPath $rp).LastWriteTime -lt $started -or !(Get-Content -LiteralPath $rp -Raw|ConvertFrom-Json).passed){throw 'Terrain regression failed'}
Copy-Item -LiteralPath $rp -Destination (Join-Path $p 'Verification\water-flow-v1\terrain-result.json') -Force
Write-Output 'PASSED terrain'
$started=Get-Date
$run=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList @('-water-flow-check','-water-flow-demo','-logFile',('"'+$p+'\Verification\water-flow-demo.log"')) -Wait -PassThru
if($run.ExitCode -ne 0){throw 'Demo failed'}
$rp=Join-Path $p 'Verification\water-flow-v1\after\result.json'
if((Get-Item -LiteralPath $rp).LastWriteTime -lt $started -or !(Get-Content -LiteralPath $rp -Raw|ConvertFrom-Json).passed){throw 'Demo check failed'}
$ff='C:\Users\许嘉乐\AppData\Local\Programs\nodejs\node_modules\@ffmpeg-installer\win32-x64\ffmpeg.exe'
& $ff -y -framerate 20 -i (Join-Path $p 'Verification\water-flow-v1\after\video-%04d.png') -c:v libx264 -preset fast -crf 21 -pix_fmt yuv420p -an (Join-Path $p 'Verification\water-flow-v1\water-flow-demo.mp4') 2> (Join-Path $p 'Verification\water-flow-ffmpeg.log')
if($LASTEXITCODE -ne 0){throw 'Encoding failed'}
Write-Output 'WATER_FLOW_FINAL_ALL_PASSED'
