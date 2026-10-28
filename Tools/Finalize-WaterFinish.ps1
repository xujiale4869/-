$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$d=[Environment]::GetFolderPath('Desktop')
$a=Join-Path $d '深海灯塔－版本与视频'
$q=Join-Path $p 'Verification\water-finish-v1'
$records=@(Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)
$records=@($records | Where-Object {!($_.operation -eq 'copy' -and $_.destination.EndsWith('.lnk'))})
$src=Join-Path $q 'water-finish-demo.mp4';$dst=Join-Path $a '02-演示视频\14-水面质感--water-finish-demo.mp4'
if(Test-Path -LiteralPath $dst){throw 'Latest video archive already exists'}
Copy-Item -LiteralPath $src -Destination $dst
if((Get-FileHash -LiteralPath $src).Hash -ne (Get-FileHash -LiteralPath $dst).Hash){throw 'Latest video hash mismatch'}
$records+=[pscustomobject]@{operation='copy';source=$src;destination=$dst;sha256=(Get-FileHash -LiteralPath $dst).Hash;bytes=(Get-Item -LiteralPath $dst).Length}
$ws=New-Object -ComObject WScript.Shell
$v=$ws.CreateShortcut((Join-Path $a '最新水面演示.lnk'));$v.TargetPath=$dst;$v.WorkingDirectory=Split-Path -Parent $dst;$v.Save()
$exe=Join-Path $p 'Builds\WaterFinishV1\GuichaoBay.exe'
$lnk=$ws.CreateShortcut((Join-Path $d '深海灯塔－水面改进版.lnk'));$lnk.TargetPath=$exe;$lnk.Arguments='';$lnk.WorkingDirectory=$p;$lnk.Save()
$records | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records | Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$links=@(Get-ChildItem -LiteralPath $a -Recurse -Filter *.lnk | ForEach-Object {$l=$ws.CreateShortcut($_.FullName);[pscustomobject]@{path=$_.FullName;target=$l.TargetPath;exists=Test-Path -LiteralPath $l.TargetPath;arguments=$l.Arguments}})
if(@($links|Where-Object {!$_.exists}).Count -gt 0){throw 'Archived link invalid'}
$res=[pscustomobject]@{passed=$true;date=(Get-Date).ToString('o');archive=$a;buildCount=13;videoCount=11;verifiedCopies=@($records|Where-Object {$_.operation -eq 'copy'}).Count;movedDesktopItems=@($records|Where-Object {$_.operation -eq 'move' -or $_.operation -eq 'shortcut'}).Count;links=$links;currentGame=$exe}
$res|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理结果.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $a '整理结果.json') -Destination (Join-Path $q 'desktop-archive.json')
Copy-Item -LiteralPath (Join-Path $a '请先读我.txt') -Destination (Join-Path $q 'desktop-archive-readme.txt')
@'
本轮新版演示已加入 02-演示视频，双击本目录“最新水面演示”即可观看。内容包含岸边水面、直航、转弯与停船；视频为 20fps 固定步长验证录制，无音轨，不代表实际游玩帧率。
'@ | Add-Content -LiteralPath (Join-Path $a '请先读我.txt') -Encoding UTF8
$player=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList @('-logFile',('"'+$p+'\Verification\water-finish-normal.log"')) -PassThru
[pscustomobject]@{pid=$player.Id;exe=$exe;started=(Get-Date).ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $q 'normal-launch.json') -Encoding UTF8
Write-Output ('NORMAL_GAME_PID '+$player.Id)
$res|Select-Object passed,buildCount,videoCount,movedDesktopItems,verifiedCopies|ConvertTo-Json
