$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q="$p\Verification\reef-life-v1";$h=Get-Content "$p\Tools\ReefLife-Merge-Stage\Tools\source-hashes.json" -Raw|ConvertFrom-Json
$audit=@(foreach($prop in $h.PSObject.Properties){$v=(Get-FileHash -LiteralPath (Join-Path $p $prop.Name)).Hash.ToLowerInvariant();[pscustomobject]@{path=$prop.Name;expected=$prop.Value;actual=$v;match=$v -eq $prop.Value}})
if(@($audit|Where-Object {!$_.match}).Count -gt 0){throw 'Source changed'}
$audit|ConvertTo-Json|Set-Content "$q\source-audit.json" -Encoding UTF8
& "$p\Tools\Compare-ReefLife.ps1"
$ff='C:\Users\许嘉乐\AppData\Local\Programs\nodejs\node_modules\@ffmpeg-installer\win32-x64\ffmpeg.exe'
$x=Start-Process -FilePath $ff -ArgumentList @('-y','-framerate','20','-i',('"'+$q+'\after\video-%04d.png"'),'-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',('"'+$q+'\reef-life-demo.mp4"')) -RedirectStandardError "$q\encode.log" -Wait -PassThru
if($x.ExitCode -ne 0){throw 'Encode failed'}
$x=Start-Process -FilePath $ff -ArgumentList @('-v','error','-i',('"'+$q+'\reef-life-demo.mp4"'),'-f','null','NUL') -RedirectStandardError "$q\decode.log" -Wait -PassThru
if($x.ExitCode -ne 0){throw 'Decode failed'}
[pscustomobject]@{passed=$true;fps=20;frames=400;seconds=20;audio=$false;encodeExit=0;decodeExit=0;sha256=(Get-FileHash "$q\reef-life-demo.mp4").Hash;bytes=(Get-Item "$q\reef-life-demo.mp4").Length}|ConvertTo-Json|Set-Content "$q\video-check.json" -Encoding UTF8
$a=Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－版本与视频';$dst="$a\02-演示视频\17-生态活动V1.mp4"
Copy-Item -LiteralPath "$q\reef-life-demo.mp4" -Destination $dst -Force
$records=Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json
foreach($record in $records){if($record.destination -eq $dst){$record.sha256=(Get-FileHash $dst).Hash;$record.bytes=(Get-Item $dst).Length}}
$records|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$exe="$p\Builds\ReefLifeV1\GuichaoBay.exe";$g=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList @('-logFile',('"'+$p+'\Verification\reef-life-normal.log"')) -PassThru
[pscustomobject]@{pid=$g.Id;exe=$exe;started=(Get-Date).ToString('o')}|ConvertTo-Json|Set-Content "$q\normal-launch.json" -Encoding UTF8
'FINAL_NORMAL_PID '+$g.Id
