param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3')
$ErrorActionPreference='Stop'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before installation'}
if(!(Test-Path (Join-Path $Project 'Assets\Deepsea\Scenes\CoastBlockout.unity'))){throw 'Accepted whitebox project required'}
function TextHash([string]$Path){
 $s=[IO.File]::ReadAllText($Path).Replace("`r`n","`n").TrimStart([char]0xFEFF)
 $sha=[Security.Cryptography.SHA256]::Create()
 try{return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($s)))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}
$manifest=Get-Content (Join-Path $PSScriptRoot 'source-manifest.json') -Raw | ConvertFrom-Json
# Complete preflight before making any project changes.
foreach($entry in $manifest){
 $source=Join-Path $PSScriptRoot ('Files\'+$entry.source)
 $target=Join-Path $Project $entry.target
 if((TextHash $source) -ne $entry.sha256){throw ('Package integrity mismatch: '+$entry.source)}
 if(Test-Path -LiteralPath $target){
  $actual=TextHash $target
  if($actual -ne $entry.beforeSha256 -and $actual -ne $entry.sha256){throw ('Project file has other edits; review before installing: '+$target)}
 }elseif($entry.beforeSha256){throw ('Required existing file missing: '+$target)}
}
$backup=Join-Path $Project ('Tools\CoastArtV7\Before-'+(Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($entry in $manifest){
 $target=Join-Path $Project $entry.target
 if(Test-Path -LiteralPath $target){$copy=Join-Path $backup $entry.target;New-Item -ItemType Directory -Path (Split-Path $copy) -Force | Out-Null;Copy-Item -LiteralPath $target -Destination $copy}
}
foreach($rel in @('Assets\Deepsea\Resources\CoastMaterials','ProjectSettings\GraphicsSettings.asset','Assets\Deepsea\Config\BayPipeline.asset')){
 $from=Join-Path $Project $rel
 if(Test-Path -LiteralPath $from){$to=Join-Path $backup $rel;New-Item -ItemType Directory -Path (Split-Path $to) -Force | Out-Null;Copy-Item -LiteralPath $from -Destination $to -Recurse}
}
foreach($entry in $manifest){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Files\'+$entry.source)) -Destination (Join-Path $Project $entry.target) -Force}
Write-Output ('ART_V7_SOURCE_INSTALLED backup='+$backup)
Write-Output 'Source installation only. Build and inspect runtime before changing the desktop shortcut.'
