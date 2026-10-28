$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q="$p\Verification\reef-life-v1";$stage="$p\Tools\ReefLife-Merge-Stage"
$h=Get-Content "$stage\Tools\source-hashes.json" -Raw|ConvertFrom-Json
$audit=@(foreach($prop in $h.PSObject.Properties){$actual=(Get-FileHash -LiteralPath (Join-Path $p $prop.Name)).Hash.ToLowerInvariant();[pscustomobject]@{path=$prop.Name;expected=$prop.Value;actual=$actual;match=$actual -eq $prop.Value}})
if(@($audit|Where-Object {!$_.match}).Count -gt 0){$audit|Where-Object {!$_.match}|ConvertTo-Json;throw 'Live source changed'}
$audit|ConvertTo-Json|Set-Content "$q\source-audit.json" -Encoding UTF8
$a=Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－版本与视频'
$records=New-Object System.Collections.Generic.List[object]
function Add-Flat($v){if($null -eq $v){return};if(($v.PSObject.Properties.Name -contains 'operation') -and ($v.operation -is [string])){$records.Add($v);return};if($v.PSObject.Properties.Name -contains 'value'){Add-Flat $v.value;return};if(($v -is [Collections.IEnumerable]) -and !($v -is [string])){foreach($x in $v){Add-Flat $x};return};throw 'Unknown record structure'}
Add-Flat (Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)
function Copy-Checked($src,$dst){New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null;if(!(Test-Path -LiteralPath $dst)){Copy-Item -LiteralPath $src -Destination $dst};$sha=(Get-FileHash -LiteralPath $src).Hash;if((Get-FileHash -LiteralPath $dst).Hash -ne $sha){throw "Copy mismatch: $dst"};$records.Add([pscustomobject]@{operation='copy';source=$src;destination=$dst;sha256=$sha;bytes=(Get-Item -LiteralPath $dst).Length})}
$old="$p\Builds\ReefAbundanceV2";$saved=Join-Path $a '01-历史版本\16-生态群落V2'
Get-ChildItem -LiteralPath $old -Recurse -File|ForEach-Object {Copy-Checked $_.FullName (Join-Path $saved $_.FullName.Substring($old.Length+1))}
$video=Join-Path $a '02-演示视频\17-生态活动V1.mp4';Copy-Checked "$q\reef-life-demo.mp4" $video
$ws=New-Object -ComObject WScript.Shell
$link=$ws.CreateShortcut((Join-Path $a '04-历史入口\深海灯塔－生态群落V2.lnk'));$link.TargetPath="$saved\GuichaoBay.exe";$link.Arguments='';$link.WorkingDirectory=$saved;$link.Save()
$link=$ws.CreateShortcut((Join-Path $a '最新水面演示.lnk'));$link.TargetPath=$video;$link.Arguments='';$link.WorkingDirectory=Split-Path -Parent $video;$link.Save()
$exe="$p\Builds\ReefLifeV1\GuichaoBay.exe";$link=$ws.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－水面改进版.lnk'));$link.TargetPath=$exe;$link.Arguments='';$link.WorkingDirectory=$p;$link.Save()
$links=@(Get-ChildItem -LiteralPath $a -Recurse -Filter *.lnk|ForEach-Object {$l=$ws.CreateShortcut($_.FullName);if(!(Test-Path -LiteralPath $l.TargetPath)){throw 'Broken link'};[pscustomobject]@{path=$_.FullName;target=$l.TargetPath;exists=$true}})
$records.ToArray()|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records.ToArray()|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$res=[pscustomobject]@{passed=$true;date=(Get-Date).ToString('o');archive=$a;buildCount=@(Get-ChildItem -LiteralPath "$a\01-历史版本" -Directory).Count;videoCount=@(Get-ChildItem -LiteralPath "$a\02-演示视频" -Filter *.mp4 -File).Count;verifiedCopies=@($records.ToArray()|Where-Object {$_.operation -eq 'copy'}).Count;links=$links;currentGame=$exe;latestVideo=$video}
$res|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理结果.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $a '整理结果.json') -Destination "$q\desktop-archive.json" -Force
foreach($g in @(Get-Process GuichaoBay -ErrorAction SilentlyContinue)){if($g.Path -eq "$old\GuichaoBay.exe"){$g.CloseMainWindow()|Out-Null;$g.WaitForExit(2500)|Out-Null}}
$normal=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList @('-logFile',('"'+$p+'\Verification\reef-life-normal.log"')) -PassThru
[pscustomobject]@{pid=$normal.Id;exe=$exe;started=(Get-Date).ToString('o')}|ConvertTo-Json|Set-Content "$q\normal-launch.json" -Encoding UTF8
$res|Select-Object passed,buildCount,videoCount,verifiedCopies|ConvertTo-Json
Write-Output ('REEF_LIFE_NORMAL_PID '+$normal.Id)
