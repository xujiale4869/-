$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$t=$p+'\Tools\CoastArtV10'
[IO.File]::WriteAllBytes($t+'\revision2.zip',[Convert]::FromBase64String([IO.File]::ReadAllText($t+'\revision2.b64')))
Expand-Archive -LiteralPath ($t+'\revision2.zip') -DestinationPath ($t+'\Revision2') -Force
function CoastSourceHash([string]$Path){
$s=[IO.File]::ReadAllText($Path).Replace("`r`n","`n").TrimStart([char]0xFEFF)
$sha=[Security.Cryptography.SHA256]::Create()
try{return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($s)))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}
$old=Get-Content ($t+'\Revision2\previous-manifest.json') -Raw | ConvertFrom-Json
foreach($e in $old){if((CoastSourceHash (Join-Path $p $e.target)) -ne $e.sha256){throw ('Concurrent source change '+$e.target)}}
$new=Get-Content ($t+'\Revision2\source-manifest.json') -Raw | ConvertFrom-Json
foreach($e in $new){$src=Join-Path ($t+'\Revision2\Files') $e.source;if(Test-Path $src){if((CoastSourceHash $src) -ne $e.sha256){throw 'Transfer mismatch'};Copy-Item -LiteralPath $src -Destination (Join-Path $p $e.target) -Force}}
Copy-Item -Path ($t+'\Revision2\*') -Destination ($t+'\Package') -Recurse -Force
Write-Output 'V10_RUNTIME_DATA_REVISION_INSTALLED'
& ($t+'\Package\Build-CoastArt.ps1')
& ($t+'\Package\Verify-Final.ps1')
