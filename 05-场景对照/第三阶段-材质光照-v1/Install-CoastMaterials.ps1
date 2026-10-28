param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3')
$ErrorActionPreference='Stop'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before installing sources'}
if(!(Test-Path (Join-Path $Project 'Assets\Deepsea\Scenes\CoastBlockout.unity'))){throw 'The accepted whitebox scene is required'}
$backup=Join-Path $Project ('Tools\CoastMaterials\Before-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item $backup -ItemType Directory -Force | Out-Null
foreach($name in @('CoastBlockoutSession.cs','CoastBlockoutCheck.cs')){Copy-Item (Join-Path $Project ('Assets\Deepsea\Scripts\Runtime\'+$name)) $backup}
foreach($part in @('Runtime','Editor','Shaders')){
 $dest=if($part -eq 'Shaders'){Join-Path $Project 'Assets\Deepsea\Shaders'}else{Join-Path $Project ('Assets\Deepsea\Scripts\'+$part)}
 Copy-Item (Join-Path $PSScriptRoot ('Files\'+$part+'\*')) $dest -Force
}
Write-Output ('COAST_MATERIALS_SOURCE_INSTALLED backup='+$backup)
