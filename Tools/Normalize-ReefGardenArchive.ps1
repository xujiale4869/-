$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$a=Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－版本与视频';$q="$p\Verification\reef-garden-v1";$out="$p\Tools\ReefGarden-Release"
$flat=New-Object System.Collections.Generic.List[object]
function Add-Flat($v){
 if($null -eq $v){return}
 if(($v.PSObject.Properties.Name -contains 'operation') -and ($v.operation -is [string])){$flat.Add($v);return}
 if($v.PSObject.Properties.Name -contains 'value'){Add-Flat $v.value;return}
 if(($v -is [Collections.IEnumerable]) -and !($v -is [string])){foreach($x in $v){Add-Flat $x};return}
 throw 'Unexpected archive record shape'
}
Add-Flat (Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)
$res=Get-Content -LiteralPath (Join-Path $a '整理结果.json') -Raw|ConvertFrom-Json
$res.verifiedCopies=@($flat.ToArray()|Where-Object {$_.operation -eq 'copy'}).Count
$res|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理结果.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $a '整理结果.json') -Destination "$q\desktop-archive.json" -Force
Copy-Item -LiteralPath "$q\desktop-archive.json" -Destination "$out\Verification\reef-garden-v1\desktop-archive.json" -Force
$manifest=[ordered]@{};Get-ChildItem -LiteralPath $out -Recurse -File|Where-Object {$_.Name -ne 'file-manifest.json'}|ForEach-Object {$manifest[$_.FullName.Substring($out.Length+1).Replace('\','/')]=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}
$manifest|ConvertTo-Json|Set-Content "$out\file-manifest.json" -Encoding UTF8
$zip=Join-Path $a '03-更新包与备份\Deepsea-Reef-Garden-V1.zip'
Copy-Item -LiteralPath $zip -Destination "$p\Tools\ReefGarden-Release-before-index.zip"
Add-Type -AssemblyName System.IO.Compression;Add-Type -AssemblyName System.IO.Compression.FileSystem
$z=[IO.Compression.ZipFile]::Open($zip,[IO.Compression.ZipArchiveMode]::Update)
try{foreach($name in @('Verification/reef-garden-v1/desktop-archive.json','file-manifest.json')){$entry=@($z.Entries|Where-Object {$_.FullName.Replace('\','/') -eq $name})[0];$entryName=$entry.FullName;$entry.Delete();[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z,(Join-Path $out $name),$entryName,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}}finally{$z.Dispose()}
foreach($record in $flat.ToArray()){if($record.destination -eq $zip){$record.sha256=(Get-FileHash -LiteralPath $zip).Hash;$record.bytes=(Get-Item -LiteralPath $zip).Length}}
$flat.ToArray()|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$flat.ToArray()|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$res|Select-Object passed,buildCount,videoCount,verifiedCopies|ConvertTo-Json
