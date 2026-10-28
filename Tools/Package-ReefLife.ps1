$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q="$p\Verification\reef-life-v1";$stage="$p\Tools\ReefLife-Merge-Stage";$out="$p\Tools\ReefLife-Release"
if(Test-Path -LiteralPath $out){throw 'Release staging exists'}
New-Item -ItemType Directory -Path $out|Out-Null
$h=Get-Content "$stage\Tools\source-hashes.json" -Raw|ConvertFrom-Json
foreach($prop in $h.PSObject.Properties){
 $src=Join-Path $p $prop.Name;if((Get-FileHash -LiteralPath $src).Hash.ToLowerInvariant() -ne $prop.Value){throw "Live source changed: $($prop.Name)"}
 $dst=Join-Path $out $prop.Name;New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null;Copy-Item -LiteralPath $src -Destination $dst;Copy-Item -LiteralPath ($src+'.meta') -Destination ($dst+'.meta')
}
New-Item -ItemType Directory -Path "$out\Tools" -Force|Out-Null
foreach($n in @('Install-ReefLife.ps1','Compare-ReefLife.ps1','source-hashes.json')){Copy-Item -LiteralPath "$stage\Tools\$n" -Destination "$out\Tools\$n"}
foreach($n in @('Install-ReefLife.ps1','Compare-ReefLife.ps1')){$tokens=$null;$errors=$null;[Management.Automation.Language.Parser]::ParseFile("$out\Tools\$n",[ref]$tokens,[ref]$errors)|Out-Null;if($errors.Count -gt 0){throw ($errors|Out-String)}}
Copy-Item -LiteralPath "$stage\README.md" -Destination "$out\README.md"
$proof="$out\Verification\reef-life-v1";New-Item -ItemType Directory -Path "$proof\before","$proof\after" -Force|Out-Null
foreach($mode in @('before','after')){Copy-Item -LiteralPath "$q\$mode\result.json" -Destination "$proof\$mode\result.json"}
Copy-Item -LiteralPath "$q\after\normal.png" -Destination "$proof\after\normal.png"
Add-Type -AssemblyName System.Drawing
foreach($n in @('before/normal','after/north','after/nursery')){
 $img=New-Object Drawing.Bitmap((Join-Path $q ($n+'.png')));try{$img.Save((Join-Path $proof ($n+'.jpg')),[Drawing.Imaging.ImageFormat]::Jpeg)}finally{$img.Dispose()}
}
foreach($n in @('source-audit.json','image-comparison.json','ecology-result.json','normal-startup.json','desktop-archive.json','video-check.json','reef-life-demo.mp4')){Copy-Item -LiteralPath "$q\$n" -Destination "$proof\$n"}
Add-Content "$out\README.md" -Encoding UTF8 -Value '\n预览说明：总览新版保留 PNG，其余附图为压缩 JPEG；材质像素检查使用的原始 PNG 保留在电脑 Verification/reef-life-v1。桌面归档现含 16 个历史运行包、14 段视频。'
$manifest=[ordered]@{};Get-ChildItem -LiteralPath $out -Recurse -File|ForEach-Object {$manifest[$_.FullName.Substring($out.Length+1).Replace('\','/')]=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}
$manifest|ConvertTo-Json|Set-Content "$out\file-manifest.json" -Encoding UTF8
$a=Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－版本与视频';$zip=Join-Path $a '03-更新包与备份\Deepsea-Reef-Life-V1.zip'
if(Test-Path -LiteralPath $zip){throw 'Release already exists'}
Compress-Archive -Path "$out\*" -DestinationPath $zip -CompressionLevel Optimal
$sha=(Get-FileHash -LiteralPath $zip).Hash
$records=New-Object System.Collections.Generic.List[object]
function Add-Flat($v){if($null -eq $v){return};if(($v.PSObject.Properties.Name -contains 'operation') -and ($v.operation -is [string])){$records.Add($v);return};if($v.PSObject.Properties.Name -contains 'value'){Add-Flat $v.value;return};if(($v -is [Collections.IEnumerable]) -and !($v -is [string])){foreach($x in $v){Add-Flat $x};return};throw 'Unknown archive record'}
Add-Flat (Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)
$records.Add([pscustomobject]@{operation='create';source=$out;destination=$zip;sha256=$sha;bytes=(Get-Item -LiteralPath $zip).Length})
$records.ToArray()|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records.ToArray()|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
@'
深海灯塔：版本与视频
01-历史版本：16 个完整历史运行包。每个 GuichaoBay.exe 需与同目录 Data 和 DLL 一起保留。
02-演示视频：14 段视频。当前最新为 17-生态活动V1.mp4，20 秒、20fps、无音轨。
03-更新包与备份：历次部署目录、传输包和正式增量源码包；Stage 为历史部署过程文件，不一定是该轮最终源码。
04-历史入口：旧试玩及演示入口。
最新水面演示：指向当前生态活动演示。
桌面“深海灯塔－水面改进版”是当前可玩版本，已更新至生态活动版。
原 Unity 开发工程保留原位置。归档运行包逐文件校验，整理清单记录来源和去向。
'@|Set-Content -LiteralPath (Join-Path $a '请先读我.txt') -Encoding UTF8
$b=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));$lines=New-Object System.Collections.Generic.List[string]
for($i=0;$i -lt $b.Length;$i+=120){$lines.Add($b.Substring($i,[Math]::Min(120,$b.Length-$i)))}
[IO.File]::WriteAllLines("$q\Release.b64",$lines.ToArray(),[Text.Encoding]::ASCII)
[pscustomobject]@{bytes=(Get-Item -LiteralPath $zip).Length;sha256=$sha;chars=$b.Length;lines=$lines.Count;files=$manifest.Count+1}|ConvertTo-Json
