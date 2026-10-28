$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$q="$p\Verification\reef-life-v1";$stage="$p\Tools\ReefLife-Merge-Stage";$out="$p\Tools\ReefLife-Release-Final"
$launch=Get-Content "$q\normal-launch.json" -Raw|ConvertFrom-Json
$game=Get-Process -Id $launch.pid -ErrorAction Stop
$game.Refresh()
$expected="$p\Builds\ReefLifeV1\GuichaoBay.exe"
$desktop=[Environment]::GetFolderPath('Desktop')
$ws=New-Object -ComObject WScript.Shell
$link=$ws.CreateShortcut((Join-Path $desktop '深海灯塔－水面改进版.lnk'))
$errors=@(Select-String -LiteralPath "$p\Verification\reef-life-normal.log" -Pattern 'Shader error|NullReferenceException|MissingReferenceException|Unhandled Exception|error CS\d|Failed to create')
$normal=[ordered]@{passed=($game.Responding -and $game.Path -eq $expected -and $link.TargetPath -eq $expected -and $link.Arguments -eq '' -and $errors.Count -eq 0);pid=$game.Id;responding=$game.Responding;title=$game.MainWindowTitle;exe=$game.Path;shortcutTarget=$link.TargetPath;shortcutArguments=$link.Arguments;errorMatches=$errors.Count;checked=(Get-Date -Format o)}
$normal|ConvertTo-Json|Set-Content "$q\normal-startup.json" -Encoding UTF8
if(!$normal.passed){throw ($normal|ConvertTo-Json)}
if(Test-Path -LiteralPath $out){throw 'Final release staging already exists'}
New-Item -ItemType Directory -Path $out|Out-Null
$h=Get-Content "$stage\Tools\source-hashes.json" -Raw|ConvertFrom-Json
foreach($prop in $h.PSObject.Properties){
 $src=Join-Path $p $prop.Name
 if((Get-FileHash -LiteralPath $src).Hash.ToLowerInvariant() -ne $prop.Value){throw "Live source changed: $($prop.Name)"}
 $dst=Join-Path $out $prop.Name;New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force|Out-Null
 Copy-Item -LiteralPath $src -Destination $dst
 Copy-Item -LiteralPath ($src+'.meta') -Destination ($dst+'.meta')
}
New-Item -ItemType Directory -Path "$out\Tools" -Force|Out-Null
foreach($n in @('Install-ReefLife.ps1','Compare-ReefLife.ps1','source-hashes.json')){Copy-Item -LiteralPath "$stage\Tools\$n" -Destination "$out\Tools\$n"}
foreach($n in @('Install-ReefLife.ps1','Compare-ReefLife.ps1')){
 $tokens=$null;$parseErrors=$null
 [Management.Automation.Language.Parser]::ParseFile("$out\Tools\$n",[ref]$tokens,[ref]$parseErrors)|Out-Null
 if($parseErrors.Count -gt 0){throw ($parseErrors|Out-String)}
}
Copy-Item -LiteralPath "$stage\README.md" -Destination "$out\README.md"
$proof="$out\Verification\reef-life-v1"
New-Item -ItemType Directory -Path "$proof\before","$proof\after" -Force|Out-Null
foreach($mode in @('before','after')){Copy-Item -LiteralPath "$q\$mode\result.json" -Destination "$proof\$mode\result.json"}
Copy-Item -LiteralPath "$q\after\normal.png" -Destination "$proof\after\normal.png"
Add-Type -AssemblyName System.Drawing
foreach($n in @('before/normal','after/north','after/nursery')){
 $img=New-Object Drawing.Bitmap((Join-Path $q ($n+'.png')))
 try{$img.Save((Join-Path $proof ($n+'.jpg')),[Drawing.Imaging.ImageFormat]::Jpeg)}finally{$img.Dispose()}
}
foreach($n in @('source-audit.json','image-comparison.json','ecology-result.json','normal-startup.json','desktop-archive.json','video-check.json','reef-life-demo.mp4')){
 Copy-Item -LiteralPath "$q\$n" -Destination "$proof\$n"
}
$manifest=[ordered]@{}
Get-ChildItem -LiteralPath $out -Recurse -File|ForEach-Object {$manifest[$_.FullName.Substring($out.Length+1).Replace('\','/')]=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}
$manifest|ConvertTo-Json|Set-Content "$out\file-manifest.json" -Encoding UTF8
$a=Join-Path $desktop '深海灯塔－版本与视频'
$zip=Join-Path $a '03-更新包与备份\Deepsea-Reef-Life-V1.zip'
$priorBackup="$p\Tools\ReefLife-pre-refuge-review.zip"
if(Test-Path -LiteralPath $zip){
 if(Test-Path -LiteralPath $priorBackup){throw 'Prior-review backup already exists'}
 Copy-Item -LiteralPath $zip -Destination $priorBackup
 Remove-Item -LiteralPath $zip
}
Compress-Archive -Path "$out\*" -DestinationPath $zip -CompressionLevel Optimal
$sha=(Get-FileHash -LiteralPath $zip).Hash
$records=New-Object System.Collections.Generic.List[object]
function Add-Flat($v){
 if($null -eq $v){return}
 if(($v.PSObject.Properties.Name -contains 'operation') -and ($v.operation -is [string])){
  if(!($v.operation -eq 'create' -and $v.destination -eq $zip)){$records.Add($v)}
  return
 }
 if($v.PSObject.Properties.Name -contains 'value'){Add-Flat $v.value;return}
 if(($v -is [Collections.IEnumerable]) -and !($v -is [string])){foreach($x in $v){Add-Flat $x};return}
 throw 'Unknown archive record'
}
Add-Flat (Get-Content -LiteralPath (Join-Path $a '整理清单.json') -Raw|ConvertFrom-Json)
$records.Add([pscustomobject]@{operation='create';source=$out;destination=$zip;sha256=$sha;bytes=(Get-Item -LiteralPath $zip).Length})
$records.ToArray()|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $a '整理清单.json') -Encoding UTF8
$records.ToArray()|Export-Csv -LiteralPath (Join-Path $a '整理清单.csv') -NoTypeInformation -Encoding UTF8
$b=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip))
$lines=New-Object System.Collections.Generic.List[string]
for($i=0;$i -lt $b.Length;$i+=120){$lines.Add($b.Substring($i,[Math]::Min(120,$b.Length-$i)))}
[IO.File]::WriteAllLines("$q\Release.b64",$lines.ToArray(),[Text.Encoding]::ASCII)
[pscustomobject]@{bytes=(Get-Item -LiteralPath $zip).Length;sha256=$sha;chars=$b.Length;lines=$lines.Count;files=$manifest.Count+1;normal=$normal}|ConvertTo-Json -Depth 4
