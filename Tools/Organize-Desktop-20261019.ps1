param([string]$Desktop=[Environment]::GetFolderPath('Desktop'))
$ErrorActionPreference='Stop'
$project=Join-Path $Desktop '深海灯塔-Unity海陆-v0.3'
$archive=Join-Path $Desktop '深海灯塔－版本与视频'
$versions=Join-Path $archive '01-历史版本';$videos=Join-Path $archive '02-演示视频';$packages=Join-Path $archive '03-更新包与备份';$entries=Join-Path $archive '04-历史入口'
@($archive,$versions,$videos,$packages,$entries) | ForEach-Object {New-Item -ItemType Directory -Path $_ -Force|Out-Null}
$records=New-Object System.Collections.Generic.List[object]
$map=@{}
function Copy-VerifiedFile([string]$source,[string]$target){
 New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force|Out-Null
 $hash=(Get-FileHash -LiteralPath $source).Hash
 if(Test-Path -LiteralPath $target){if((Get-FileHash -LiteralPath $target).Hash -ne $hash){throw "归档目标已有不同内容：$target"}}
 else {Copy-Item -LiteralPath $source -Destination $target}
 if((Get-FileHash -LiteralPath $target).Hash -ne $hash){throw "归档校验失败：$target"}
 $records.Add([pscustomobject]@{operation='copy';source=$source;destination=$target;sha256=$hash;bytes=(Get-Item -LiteralPath $target).Length})
}
function Copy-Build([string]$source,[string]$label){
 if(!(Test-Path -LiteralPath (Join-Path $source 'GuichaoBay.exe')) -or !(Test-Path -LiteralPath (Join-Path $source 'GuichaoBay_Data'))){throw "运行包不完整：$source"}
 $target=Join-Path $versions $label
 Get-ChildItem -LiteralPath $source -Recurse -File | ForEach-Object {Copy-VerifiedFile $_.FullName (Join-Path $target $_.FullName.Substring($source.Length+1))}
 $map[$source]=$target
 Write-Output "已复制并校验历史运行包：$label"
}
Copy-Build (Join-Path $Desktop '深海灯塔-Unity浅海-v0.1\Builds\Windows') '01-Unity浅海-v0.1'
Copy-Build (Join-Path $Desktop '深海灯塔-Unity海陆-v0.2\Builds\Windows') '02-海陆远征-v0.2'
$labels=[ordered]@{Windows='03-居民与界面-v0.3';WaterPreview='04-初版水面';ShallowWaterV2='05-浅海-v2';ShallowWaterV3='06-浅海-v3';MarineLifeV1='07-飞鱼与海洋生物';ShoreV1='08-浅滩';EcologyV1='09-海草与海鸟';CoastV1='10-岸浪与海声';WaterPolishV1='11-浅滩光影';WaterFlowV1='12-水流';CoastDetailV1='13-岸边细节'}
foreach($name in $labels.Keys){Copy-Build (Join-Path $project ('Builds\'+$name)) $labels[$name]}
$videoMap=@{}
Get-ChildItem -LiteralPath (Join-Path $project 'Verification') -Recurse -Filter *.mp4 -File | Where-Object {$_.DirectoryName -notmatch 'water-finish-v1'} | ForEach-Object {
 $target=Join-Path $videos ($_.Directory.Name+'--'+$_.Name)
 Copy-VerifiedFile $_.FullName $target;$videoMap[$_.FullName]=$target
}
$known=@('Deepsea-Coast-Stage','Deepsea-CoastDetail-Stage','Deepsea-WaterFlow-Stage','Deepsea-WaterPolish-Stage','Deepsea-CoastDetail-Stage.b64','Deepsea-CoastDetail-Stage.zip','Deepsea-WaterFlow-Stage.b64','Deepsea-WaterFlow-Stage.zip','Export-Coast-Detail.ps1','GuichaoBay-v02-before-resume-20261015-114532.zip','GuichaoBay-Windows-v0.2-resumed.zip','GuichaoBay-Windows-v0.3.zip')
foreach($name in $known){
 $source=Join-Path $Desktop $name;$target=Join-Path $packages $name
 if(!(Test-Path -LiteralPath $source)){continue}
 if(Test-Path -LiteralPath $target){throw "归档目标已存在，未移动：$name"}
 Move-Item -LiteralPath $source -Destination $target
 $records.Add([pscustomobject]@{operation='move';source=$source;destination=$target;sha256='';bytes=0})
}
$ws=New-Object -ComObject WScript.Shell
Get-ChildItem -LiteralPath $Desktop -Filter '深海灯塔*.lnk' | Where-Object {$_.Name -ne '深海灯塔－水面改进版.lnk'} | ForEach-Object {
 $source=$_.FullName;$link=$ws.CreateShortcut($source);$oldTarget=$link.TargetPath;$newTarget=$oldTarget
 foreach($key in $map.Keys){if($oldTarget.StartsWith($key+'\',[StringComparison]::OrdinalIgnoreCase)){$newTarget=$map[$key]+$oldTarget.Substring($key.Length);break}}
 if($videoMap.ContainsKey($oldTarget)){$newTarget=$videoMap[$oldTarget]}
 if(!(Test-Path -LiteralPath $newTarget)){throw "快捷方式目标不存在：$source -> $newTarget"}
 $dest=Join-Path $entries $_.Name
 if(Test-Path -LiteralPath $dest){throw "历史入口已存在：$dest"}
 Copy-VerifiedFile $source $dest
 $archived=$ws.CreateShortcut($dest);$archived.TargetPath=$newTarget
 if($newTarget.EndsWith('.exe',[StringComparison]::OrdinalIgnoreCase) -and $newTarget.StartsWith($versions)){$archived.WorkingDirectory=Split-Path -Parent $newTarget}
 $archived.Save()
 Remove-Item -LiteralPath $source
 $records.Add([pscustomobject]@{operation='shortcut';source=$source;destination=$dest;sha256=(Get-FileHash -LiteralPath $dest).Hash;bytes=(Get-Item -LiteralPath $dest).Length;target=$newTarget;previousTarget=$oldTarget})
}
$records | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $archive '整理清单.json') -Encoding UTF8
$records | Export-Csv -LiteralPath (Join-Path $archive '整理清单.csv') -NoTypeInformation -Encoding UTF8
@'
深海灯塔：版本与视频

01-历史版本：13 个完整 Windows 运行包。进入对应文件夹运行 GuichaoBay.exe，需保留同目录的 GuichaoBay_Data、UnityPlayer.dll 等文件。
02-演示视频：历次实机演示，文件名前缀表示对应版本。
03-更新包与备份：从桌面移入的旧更新包、传输包和部署目录。Stage 为当时部署过程文件，不保证是该轮最终源码；正式工程及正式交付包优先。
04-历史入口：旧试玩及视频快捷方式，已指向本归档内相应的运行包和视频。

当前游戏入口仍在桌面：深海灯塔－水面改进版。
Unity v0.1、v0.2、v0.3 原工程保留原位置；本文件夹存放独立历史运行包，保留完整的开发工程和测试引用。
历史运行包与视频逐文件核对 SHA256。整理清单记录来源和去向；历史游戏正常启动会共享游戏原有的用户存档设置。
本轮水面质感新版验证通过后，新增当前演示视频与“最新视频”入口。
'@ | Set-Content -LiteralPath (Join-Path $archive '请先读我.txt') -Encoding UTF8
$links=@(Get-ChildItem -LiteralPath $entries -Filter *.lnk | ForEach-Object {$l=$ws.CreateShortcut($_.FullName);[pscustomobject]@{name=$_.Name;target=$l.TargetPath;exists=Test-Path -LiteralPath $l.TargetPath;arguments=$l.Arguments}})
if(@($links|Where-Object {!$_.exists}).Count -gt 0){throw '整理后存在失效入口'}
$result=[pscustomobject]@{passed=$true;date=(Get-Date).ToString('o');archive=$archive;buildCount=$map.Count;videoCount=$videoMap.Count;verifiedCopies=@($records|Where-Object {$_.operation -eq 'copy'}).Count;links=$links}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $archive '整理结果.json') -Encoding UTF8
$result | Select-Object passed,archive,buildCount,videoCount,verifiedCopies | ConvertTo-Json
