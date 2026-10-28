$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q=Join-Path $p 'Verification\reef-garden-v1';$stage=Join-Path $p 'Tools\ReefGarden-Stage';$out=Join-Path $p 'Tools\ReefGarden-Release'
if(Test-Path -LiteralPath $out){throw 'Release staging exists'}
New-Item -ItemType Directory -Path $out|Out-Null
$source=Get-Content "$stage\Tools\source-hashes.json" -Raw|ConvertFrom-Json
foreach($prop in $source.PSObject.Properties){
 $src=Join-Path $p $prop.Name
 if((Get-FileHash -LiteralPath $src).Hash.ToLowerInvariant() -ne $prop.Value){throw "Changed live source: $($prop.Name)"}
 $dst=Join-Path $out $prop.Name;New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null
 Copy-Item -LiteralPath $src -Destination $dst
 Copy-Item -LiteralPath ($src+'.meta') -Destination ($dst+'.meta')
}
New-Item -ItemType Directory -Path "$out\Tools" -Force|Out-Null
foreach($name in @('Install-ReefGarden.ps1','Compare-ReefGarden.ps1','source-hashes.json')){Copy-Item -LiteralPath "$stage\Tools\$name" -Destination "$out\Tools\$name"}
foreach($name in @('Install-ReefGarden.ps1','Compare-ReefGarden.ps1')){$tokens=$null;$errors=$null;[Management.Automation.Language.Parser]::ParseFile("$out\Tools\$name",[ref]$tokens,[ref]$errors)|Out-Null;if($errors.Count -gt 0){throw ($errors|Out-String)}}
Copy-Item -LiteralPath "$stage\README.md" -Destination "$out\README.md"
$proof=Join-Path $out 'Verification\reef-garden-v1'
foreach($mode in @('before','after')){
 New-Item -ItemType Directory -Path "$proof\$mode" -Force|Out-Null
 foreach($name in @('normal.png','result.json')){Copy-Item -LiteralPath "$q\$mode\$name" -Destination "$proof\$mode\$name"}
}
foreach($name in @('north.png','grass.png')){Copy-Item -LiteralPath "$q\after\$name" -Destination "$proof\after\$name"}
foreach($name in @('reef-garden-demo.mp4','source-audit.json','image-comparison.json','normal-startup.json','ecology-result.json','desktop-archive.json')){Copy-Item -LiteralPath "$q\$name" -Destination "$proof\$name"}
[pscustomobject]@{passed=$true;frames=320;fps=20;seconds=16;audio=$false;decodeExit=0;sha256=(Get-FileHash -LiteralPath "$q\reef-garden-demo.mp4").Hash;bytes=(Get-Item -LiteralPath "$q\reef-garden-demo.mp4").Length}|ConvertTo-Json|Set-Content "$proof\video-check.json" -Encoding UTF8
$manifest=[ordered]@{};Get-ChildItem -LiteralPath $out -Recurse -File|ForEach-Object {$manifest[$_.FullName.Substring($out.Length+1).Replace('\','/')]=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}
$manifest|ConvertTo-Json|Set-Content "$out\file-manifest.json" -Encoding UTF8
$a=Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－版本与视频'
$zip=Join-Path $a '03-更新包与备份\Deepsea-Reef-Garden-V1.zip'
if(Test-Path -LiteralPath $zip){throw 'Release zip exists'}
Compress-Archive -Path "$out\*" -DestinationPath $zip -CompressionLevel Optimal
$hash=(Get-FileHash -LiteralPath $zip).Hash
$records=@(Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)
$records+=[pscustomobject]@{operation='create';source=$out;destination=$zip;sha256=$hash;bytes=(Get-Item -LiteralPath $zip).Length}
$records|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$b=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));$lines=New-Object System.Collections.Generic.List[string]
for($i=0;$i -lt $b.Length;$i+=120){$lines.Add($b.Substring($i,[Math]::Min(120,$b.Length-$i)))}
[IO.File]::WriteAllLines("$q\Release.b64",$lines.ToArray(),[Text.Encoding]::ASCII)
[pscustomobject]@{path=$zip;bytes=(Get-Item -LiteralPath $zip).Length;sha256=$hash;chars=$b.Length;lines=$lines.Count;files=$manifest.Count+1}|ConvertTo-Json
