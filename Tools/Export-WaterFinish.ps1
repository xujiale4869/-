$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q=Join-Path $p 'Verification\water-finish-v1'
$export=Join-Path $q 'Export'
New-Item -ItemType Directory -Path $export -Force|Out-Null
foreach($mode in @('before','after')){
 $dst=Join-Path $export ('Verification\water-finish-v1\'+$mode);New-Item -ItemType Directory -Path $dst -Force|Out-Null
 foreach($name in @('normal.png','shore.png','reef.png','turn.png','cruise.png','stop.png','result.json')){Copy-Item -LiteralPath (Join-Path $q ($mode+'\'+$name)) -Destination $dst -Force}
}
$dst=Join-Path $export 'Verification\water-finish-v1'
foreach($name in @('water-finish-demo.mp4','image-comparison.json','source-audit.json','normal-startup.json','ecology-result.json','desktop-archive.json','desktop-archive-readme.txt')){Copy-Item -LiteralPath (Join-Path $q $name) -Destination $dst -Force}
$h=Get-Content "$p\Tools\WaterFinish-Stage\Tools\source-hashes.json" -Raw|ConvertFrom-Json
foreach($prop in $h.PSObject.Properties){
 $relative=$prop.Name+'.meta';$file=Join-Path $p $relative;$dst=Join-Path $export $relative
 New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null
 Copy-Item -LiteralPath $file -Destination $dst -Force
}
$zip=Join-Path $q 'Evidence.zip';if(Test-Path -LiteralPath $zip){throw 'Evidence already exists'}
Compress-Archive -Path "$export\*" -DestinationPath $zip -CompressionLevel Optimal
$encoded=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip));$lines=New-Object System.Collections.Generic.List[string]
for($i=0;$i -lt $encoded.Length;$i+=120){$lines.Add($encoded.Substring($i,[Math]::Min(120,$encoded.Length-$i)))}
[IO.File]::WriteAllLines((Join-Path $q 'Evidence.b64'),$lines.ToArray(),[Text.Encoding]::ASCII)
[pscustomobject]@{bytes=(Get-Item -LiteralPath $zip).Length;sha256=(Get-FileHash -LiteralPath $zip).Hash;chars=$encoded.Length;lines=$lines.Count}|ConvertTo-Json
