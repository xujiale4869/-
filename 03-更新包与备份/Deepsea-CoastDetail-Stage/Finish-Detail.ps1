$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$s='C:\Users\许嘉乐\Desktop\Deepsea-CoastDetail-Stage'
$verify=Join-Path $p 'Verification\coast-detail-v1'
foreach($f in @('before/result.json','after/result.json','ecology-result.json','terrain-result.json')){if(!(Get-Content -LiteralPath (Join-Path $verify $f) -Raw|ConvertFrom-Json).passed){throw ('Failed '+$f)}}
$ff='C:\Users\许嘉乐\AppData\Local\Programs\nodejs\node_modules\@ffmpeg-installer\win32-x64\ffmpeg.exe'
$video=Join-Path $verify 'coast-detail-demo.mp4'
$r=Start-Process -FilePath $ff -ArgumentList @('-y','-framerate','20','-i',('"'+$verify+'\after\video-%04d.png"'),'-c:v','libx264','-preset','fast','-crf','21','-pix_fmt','yuv420p','-an',('"'+$video+'"')) -RedirectStandardError (Join-Path $p 'Verification\coast-detail-ffmpeg.log') -Wait -PassThru
if($r.ExitCode -ne 0){throw 'Encoding failed'}
$d=Start-Process -FilePath $ff -ArgumentList @('-v','error','-i',('"'+$video+'"'),'-f','null','NUL') -RedirectStandardError (Join-Path $p 'Verification\coast-detail-decode.log') -Wait -PassThru
if($d.ExitCode -ne 0){throw 'Decode failed'}
$release=[pscustomobject]@{passed=$true;build='CoastDetailV1';buildFinished=(Get-Item -LiteralPath (Join-Path $p 'Verification\coast-detail-build.log')).LastWriteTime.ToString('o');videoEncodeExit=$r.ExitCode;videoDecodeExit=$d.ExitCode;videoBytes=(Get-Item -LiteralPath $video).Length;videoSHA256=(Get-FileHash -LiteralPath $video -Algorithm SHA256).Hash.ToLowerInvariant();videoFrames=370;videoSeconds=18.5;note='Fixed 20 fps capture with automated normal input/physics control path. Not a frame-rate benchmark or manual keyboard playtest.'}
$release|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $verify 'release-result.json') -Encoding UTF8
$proof=Join-Path $s 'Export';New-Item -ItemType Directory -Path $proof -Force|Out-Null
foreach($f in (Get-Content -LiteralPath (Join-Path $s 'source-hashes.json') -Raw|ConvertFrom-Json)){$rel=$f.path+'.meta';$dest=Join-Path $proof $rel;New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $p $rel) -Destination $dest -Force}
$target=Join-Path $proof 'Verification\coast-detail-v1';New-Item -ItemType Directory -Path $target -Force|Out-Null
foreach($mode in @('before','after')){$out=Join-Path $target $mode;New-Item -ItemType Directory -Path $out -Force|Out-Null;foreach($name in @('normal.png','shore.png','reef.png','result.json')){Copy-Item -LiteralPath (Join-Path $verify ($mode+'\'+$name)) -Destination (Join-Path $out $name) -Force}}
foreach($name in @('grass.png','cruise.png','turn.png','stop.png')){Copy-Item -LiteralPath (Join-Path $verify ('after\'+$name)) -Destination (Join-Path $target ('after\'+$name)) -Force}
foreach($name in @('ecology-result.json','terrain-result.json','source-audit.json','release-result.json','coast-detail-demo.mp4')){Copy-Item -LiteralPath (Join-Path $verify $name) -Destination (Join-Path $target $name) -Force}
$zip=Join-Path $s 'Evidence.zip';Compress-Archive -Path (Join-Path $proof '*') -DestinationPath $zip -Force
$b=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));$lines=for($i=0;$i -lt $b.Length;$i+=120){$b.Substring($i,[Math]::Min(120,$b.Length-$i))};[IO.File]::WriteAllLines((Join-Path $s 'Evidence.b64'),$lines,[Text.Encoding]::ASCII)
$release|ConvertTo-Json
[pscustomobject]@{bytes=(Get-Item -LiteralPath $zip).Length;sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant();lines=$lines.Count}|ConvertTo-Json
Write-Output 'COAST_DETAIL_EVIDENCE_READY'
