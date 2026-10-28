$ErrorActionPreference='Stop'
$d=[Environment]::GetFolderPath('Desktop');$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$a=Join-Path $d '深海灯塔－版本与视频'
$records=New-Object System.Collections.Generic.List[object]
$versions=Join-Path $a '01-历史版本'
$mapping=[ordered]@{'01-Unity浅海-v0.1'=(Join-Path $d '深海灯塔-Unity浅海-v0.1\Builds\Windows');'02-海陆远征-v0.2'=(Join-Path $d '深海灯塔-Unity海陆-v0.2\Builds\Windows');'03-居民与界面-v0.3'="$p\Builds\Windows";'04-初版水面'="$p\Builds\WaterPreview";'05-浅海-v2'="$p\Builds\ShallowWaterV2";'06-浅海-v3'="$p\Builds\ShallowWaterV3";'07-飞鱼与海洋生物'="$p\Builds\MarineLifeV1";'08-浅滩'="$p\Builds\ShoreV1";'09-海草与海鸟'="$p\Builds\EcologyV1";'10-岸浪与海声'="$p\Builds\CoastV1";'11-浅滩光影'="$p\Builds\WaterPolishV1";'12-水流'="$p\Builds\WaterFlowV1";'13-岸边细节'="$p\Builds\CoastDetailV1"}
function Record-Copy($src,$dst){
 $h=(Get-FileHash -LiteralPath $dst).Hash
 if((Get-FileHash -LiteralPath $src).Hash -ne $h){throw "Source/archive mismatch: $dst"}
 $records.Add([pscustomobject]@{operation='copy';source=$src;destination=$dst;sha256=$h;bytes=(Get-Item -LiteralPath $dst).Length})
}
foreach($label in $mapping.Keys){$root=Join-Path $versions $label;Get-ChildItem -LiteralPath $root -File -Recurse|ForEach-Object {Record-Copy (Join-Path $mapping[$label] $_.FullName.Substring($root.Length+1)) $_.FullName}}
$videoDir=Join-Path $a '02-演示视频'
Get-ChildItem -LiteralPath $videoDir -File -Filter *.mp4|ForEach-Object {
 $parts=$_.Name -split '--',2
 $src=if($parts[0] -eq '14-水面质感'){Join-Path $p 'Verification\water-finish-v1\water-finish-demo.mp4'}else{Join-Path $p ('Verification\'+$parts[0]+'\'+$parts[1])}
 Record-Copy $src $_.FullName
}
Get-ChildItem -LiteralPath (Join-Path $a '03-更新包与备份') | ForEach-Object {$records.Add([pscustomobject]@{operation='move';source=(Join-Path $d $_.Name);destination=$_.FullName;sha256=if($_.PSIsContainer){''}else{(Get-FileHash -LiteralPath $_.FullName).Hash};bytes=if($_.PSIsContainer){0}else{$_.Length}})}
$ws=New-Object -ComObject WScript.Shell
$links=@(Get-ChildItem -LiteralPath $a -Recurse -Filter *.lnk | ForEach-Object {
 $l=$ws.CreateShortcut($_.FullName)
 if(!(Test-Path -LiteralPath $l.TargetPath)){throw "Broken link: $($_.FullName)"}
 if($_.Directory.Name -eq '04-历史入口'){$records.Add([pscustomobject]@{operation='shortcut';source=(Join-Path $d $_.Name);destination=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash;bytes=$_.Length;target=$l.TargetPath;arguments=$l.Arguments})}
 [pscustomobject]@{path=$_.FullName;target=$l.TargetPath;exists=$true;arguments=$l.Arguments}
})
$records.ToArray()|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records.ToArray()|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$res=[pscustomobject]@{passed=$true;date=(Get-Date).ToString('o');archive=$a;buildCount=$mapping.Count;videoCount=@(Get-ChildItem -LiteralPath $videoDir -Filter *.mp4).Count;verifiedCopies=@($records.ToArray()|Where-Object {$_.operation -eq 'copy'}).Count;movedDesktopItems=@($records.ToArray()|Where-Object {$_.operation -eq 'move' -or $_.operation -eq 'shortcut'}).Count;links=$links;currentGame="$p\Builds\WaterFinishV1\GuichaoBay.exe"}
$res|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理结果.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $a '整理结果.json') -Destination "$p\Verification\water-finish-v1\desktop-archive.json" -Force
Copy-Item -LiteralPath (Join-Path $a '请先读我.txt') -Destination "$p\Verification\water-finish-v1\desktop-archive-readme.txt" -Force
$res|Select-Object passed,buildCount,videoCount,verifiedCopies,movedDesktopItems|ConvertTo-Json
