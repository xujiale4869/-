$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q=Join-Path $p 'Verification\reef-garden-v1'
& "$p\Tools\Compare-ReefGarden.ps1"
$f='C:\Users\许嘉乐\AppData\Local\Programs\nodejs\node_modules\@ffmpeg-installer\win32-x64\ffmpeg.exe'
$x=Start-Process -FilePath $f -ArgumentList @('-y','-framerate','20','-i',('"'+$q+'\after\video-%04d.png"'),'-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',('"'+$q+'\reef-garden-demo.mp4"')) -RedirectStandardError "$q\encode.log" -Wait -PassThru
if($x.ExitCode -ne 0){throw 'Encode failed'}
$x=Start-Process -FilePath $f -ArgumentList @('-v','error','-i',('"'+$q+'\reef-garden-demo.mp4"'),'-f','null','NUL') -RedirectStandardError "$q\decode.log" -Wait -PassThru
if($x.ExitCode -ne 0){throw 'Decode failed'}
$d=[Environment]::GetFolderPath('Desktop');$a=Join-Path $d '深海灯塔－版本与视频'
$records=New-Object System.Collections.Generic.List[object]
foreach($record in @(Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)){$records.Add($record)}
function Copy-Checked($src,$dst){
 New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null
 if(!(Test-Path -LiteralPath $dst)){Copy-Item -LiteralPath $src -Destination $dst}
 $hash=(Get-FileHash -LiteralPath $src).Hash
 if((Get-FileHash -LiteralPath $dst).Hash -ne $hash){throw "Archive hash mismatch: $dst"}
 $records.Add([pscustomobject]@{operation='copy';source=$src;destination=$dst;sha256=$hash;bytes=(Get-Item -LiteralPath $dst).Length})
}
$old="$p\Builds\WaterFinishV1";$oldArchive=Join-Path $a '01-历史版本\14-水面质感'
Get-ChildItem -LiteralPath $old -Recurse -File|ForEach-Object {Copy-Checked $_.FullName (Join-Path $oldArchive $_.FullName.Substring($old.Length+1))}
$video=Join-Path $a '02-演示视频\15-海草与珊瑚--reef-garden-demo.mp4'
Copy-Checked "$q\reef-garden-demo.mp4" $video
$ws=New-Object -ComObject WScript.Shell
$l=$ws.CreateShortcut((Join-Path $a '04-历史入口\深海灯塔－水面质感V1.lnk'));$l.TargetPath="$oldArchive\GuichaoBay.exe";$l.Arguments='';$l.WorkingDirectory=$oldArchive;$l.Save()
$l=$ws.CreateShortcut((Join-Path $a '最新水面演示.lnk'));$l.TargetPath=$video;$l.Arguments='';$l.WorkingDirectory=Split-Path -Parent $video;$l.Save()
$exe="$p\Builds\ReefGardenV1\GuichaoBay.exe"
$l=$ws.CreateShortcut((Join-Path $d '深海灯塔－水面改进版.lnk'));$l.TargetPath=$exe;$l.Arguments='';$l.WorkingDirectory=$p;$l.Save()
$links=@(Get-ChildItem -LiteralPath $a -Recurse -Filter *.lnk|ForEach-Object {$s=$ws.CreateShortcut($_.FullName);if(!(Test-Path -LiteralPath $s.TargetPath)){throw 'Broken archive link'};[pscustomobject]@{path=$_.FullName;target=$s.TargetPath;exists=$true;arguments=$s.Arguments}})
$records.ToArray()|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records.ToArray()|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$res=[pscustomobject]@{passed=$true;date=(Get-Date).ToString('o');archive=$a;buildCount=14;videoCount=12;verifiedCopies=@($records.ToArray()|Where-Object {$_.operation -eq 'copy'}).Count;movedDesktopItems=25;links=$links;currentGame=$exe;latestVideo=$video}
$res|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理结果.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $a '整理结果.json') -Destination "$q\desktop-archive.json" -Force
$txt=Join-Path $a '请先读我.txt';$content=[IO.File]::ReadAllText($txt).Replace('13 个完整 Windows 运行包','14 个完整 Windows 运行包')
$content+="\r\n2026-10-19 海草与珊瑚版：当前桌面游戏入口已更新；最新水面演示指向 16 秒、20fps 的新群落视频（无音轨）。上一版水面质感运行包已加入 01-历史版本/14-水面质感。\r\n"
[IO.File]::WriteAllText($txt,$content,(New-Object Text.UTF8Encoding($true)))
$player=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList @('-logFile',('"'+$p+'\Verification\reef-garden-normal.log"')) -PassThru
[pscustomobject]@{pid=$player.Id;exe=$exe;started=(Get-Date).ToString('o')}|ConvertTo-Json|Set-Content "$q\normal-launch.json" -Encoding UTF8
$res|Select-Object passed,buildCount,videoCount,verifiedCopies|ConvertTo-Json
Get-FileHash "$q\reef-garden-demo.mp4"|Select-Object Hash|ConvertTo-Json
Write-Output ('REEF_NORMAL_PID '+$player.Id)
