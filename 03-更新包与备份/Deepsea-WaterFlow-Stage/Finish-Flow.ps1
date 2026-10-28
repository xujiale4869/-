$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$stage='C:\Users\许嘉乐\Desktop\Deepsea-WaterFlow-Stage'
$ff='C:\Users\许嘉乐\AppData\Local\Programs\nodejs\node_modules\@ffmpeg-installer\win32-x64\ffmpeg.exe'
$video=Join-Path $p 'Verification\water-flow-v1\water-flow-demo.mp4'
$encode=Start-Process -FilePath $ff -ArgumentList @('-y','-framerate','20','-i',('"'+$p+'\Verification\water-flow-v1\after\video-%04d.png"'),'-c:v','libx264','-preset','fast','-crf','21','-pix_fmt','yuv420p','-an',('"'+$video+'"')) -RedirectStandardError (Join-Path $p 'Verification\water-flow-ffmpeg.log') -Wait -PassThru
if($encode.ExitCode -ne 0){throw 'Encoding failed'}
$decode=Start-Process -FilePath $ff -ArgumentList @('-v','error','-i',('"'+$video+'"'),'-f','null','NUL') -RedirectStandardError (Join-Path $p 'Verification\water-flow-decode.log') -Wait -PassThru
if($decode.ExitCode -ne 0){throw 'Video decoding failed'}
$proof=Join-Path $stage 'Export'; New-Item -ItemType Directory -Path $proof -Force|Out-Null
$metas=@(foreach($f in (Get-Content -LiteralPath (Join-Path $stage 'source-hashes.json') -Raw|ConvertFrom-Json)){($f.path+'.meta')})
$metas+='Assets/Deepsea/ThirdParty/TiledDirectionalFlow.meta'
foreach($rel in $metas){$dest=Join-Path $proof $rel;New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $p $rel) -Destination $dest -Force}
$verify=Join-Path $proof 'Verification\water-flow-v1';New-Item -ItemType Directory -Path $verify -Force|Out-Null
foreach($mode in @('before','after')){$d=Join-Path $verify $mode;New-Item -ItemType Directory -Path $d -Force|Out-Null;foreach($name in @('normal.png','reef.png','grass.png','flow.png','result.json')){Copy-Item -LiteralPath (Join-Path $p ('Verification\water-flow-v1\'+$mode+'\'+$name)) -Destination (Join-Path $d $name) -Force}}
foreach($name in @('ecology-result.json','terrain-result.json','source-audit.json','water-flow-demo.mp4')){Copy-Item -LiteralPath (Join-Path $p ('Verification\water-flow-v1\'+$name)) -Destination (Join-Path $verify $name) -Force}
$passed=$true;foreach($name in @('before/result.json','after/result.json','ecology-result.json','terrain-result.json')){if(!(Get-Content -LiteralPath (Join-Path $verify $name) -Raw|ConvertFrom-Json).passed){$passed=$false}}
$release=[pscustomobject]@{passed=$passed;build='WaterFlowV1';built=(Get-Item -LiteralPath (Join-Path $p 'Builds\WaterFlowV1\GuichaoBay.exe')).LastWriteTime.ToString('o');videoEncodeExit=$encode.ExitCode;videoDecodeExit=$decode.ExitCode;videoBytes=(Get-Item -LiteralPath $video).Length;videoSHA256=(Get-FileHash -LiteralPath $video -Algorithm SHA256).Hash.ToLowerInvariant();note='Controlled 20 fps capture; not a frame-rate benchmark. No audio track.'}
$release|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $verify 'release-result.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $verify 'release-result.json') -Destination (Join-Path $p 'Verification\water-flow-v1\release-result.json') -Force
Get-Content -LiteralPath (Join-Path $verify 'release-result.json') -Raw
if(!$passed){throw 'Release checks failed'}
$zip=Join-Path $stage 'Evidence.zip';Compress-Archive -Path (Join-Path $proof '*') -DestinationPath $zip -Force
$base=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));$lines=for($i=0;$i -lt $base.Length;$i+=120){$base.Substring($i,[Math]::Min(120,$base.Length-$i))};[IO.File]::WriteAllLines((Join-Path $stage 'Evidence.b64'),$lines,[Text.Encoding]::ASCII)
Get-FileHash -LiteralPath $zip -Algorithm SHA256;Get-Item -LiteralPath $zip|Select-Object Name,Length
Write-Output 'WATER_FLOW_FINAL_ALL_PASSED'
