$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q=Join-Path $p 'Verification\water-finish-v1';$stage=Join-Path $p 'Tools\WaterFinish-Stage';$out=Join-Path $p 'Tools\WaterFinish-Release'
if(Test-Path -LiteralPath $out){throw 'Release staging exists'}
New-Item -ItemType Directory -Path $out|Out-Null
Copy-Item -LiteralPath "$stage\Assets" -Destination "$out\Assets" -Recurse
Copy-Item -LiteralPath "$stage\Tools" -Destination "$out\Tools" -Recurse
[IO.File]::WriteAllText("$out\README.md",[IO.File]::ReadAllText("$stage\README.md"),(New-Object Text.UTF8Encoding($false)))
Get-ChildItem -LiteralPath "$q\Export" -File -Recurse | Where-Object {$_.Extension -ne '.png' -or $_.Name -in @('normal.png','turn.png')} | ForEach-Object {
 $src=$_.FullName;$dst=Join-Path $out $src.Substring(("$q\Export").Length+1)
 New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null
 Copy-Item -LiteralPath $src -Destination $dst -Force
}
Copy-Item -LiteralPath "$stage\file-manifest.json" -Destination "$out\file-manifest.json"
$manifest=Get-Content "$out\file-manifest.json" -Raw|ConvertFrom-Json
foreach($prop in $manifest.PSObject.Properties){$hash=(Get-FileHash -LiteralPath (Join-Path $out $prop.Name)).Hash.ToLowerInvariant();if($hash -ne $prop.Value){throw "Release file hash mismatch: $($prop.Name)"}}
$archive=Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－版本与视频'
$zip=Join-Path $archive '03-更新包与备份\Deepsea-Water-Finish-V1.zip'
if(Test-Path -LiteralPath $zip){throw 'Final archive exists'}
Compress-Archive -Path "$out\*" -DestinationPath $zip -CompressionLevel Optimal
$records=@(Get-Content -LiteralPath (Join-Path $archive '整理清单.json') -Raw|ConvertFrom-Json)
$record=[pscustomobject]@{operation='create';source=$out;destination=$zip;sha256=(Get-FileHash -LiteralPath $zip).Hash;bytes=(Get-Item -LiteralPath $zip).Length}
$records+=$record
$records|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $archive '整理清单.json') -Encoding UTF8
$records|Export-Csv -LiteralPath (Join-Path $archive '整理清单.csv') -NoTypeInformation -Encoding UTF8
$record|ConvertTo-Json
'FINAL_ARCHIVE_SOURCE_HASHES_OK'
