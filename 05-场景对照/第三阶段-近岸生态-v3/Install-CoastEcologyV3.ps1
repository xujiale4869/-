param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3')
$ErrorActionPreference='Stop'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before installation'}
if(!(Test-Path (Join-Path $Project 'Assets\Deepsea\Scenes\CoastBlockout.unity'))){throw 'Accepted whitebox project required'}
$backup=Join-Path $Project ('Tools\CoastEcologyV3\Before-'+(Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($part in @('Runtime','Editor','Shaders')){
 $dest=if($part -eq 'Shaders'){Join-Path $Project 'Assets\Deepsea\Shaders'}else{Join-Path $Project ('Assets\Deepsea\Scripts\'+$part)}
 foreach($source in Get-ChildItem (Join-Path $PSScriptRoot ('Files\'+$part)) -File){
  $target=Join-Path $dest $source.Name
  if(Test-Path -LiteralPath $target){Copy-Item -LiteralPath $target -Destination $backup}
  Copy-Item -LiteralPath $source.FullName -Destination $target -Force
 }
}
Write-Output ('ECOLOGY_V3_INSTALLED backup='+$backup)
