param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3')
$ErrorActionPreference='Stop'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close Unity Editor before installing the whitebox source'}
$source=Join-Path $PSScriptRoot 'Files'
$runtime=Join-Path $Project 'Assets\Deepsea\Scripts\Runtime'
$islandPath=Join-Path $runtime 'IslandTerrain.cs'
$oceanPath=Join-Path $runtime 'OceanSurface.cs'
if(!(Test-Path -LiteralPath $islandPath) -or !(Test-Path -LiteralPath $oceanPath)){throw 'Original Unity game source not found'}
foreach($part in @('Runtime','Editor','Shaders')){
    if(!(Test-Path -LiteralPath (Join-Path $source $part))){throw ('Missing package folder: '+$part)}
}
if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Reference-Stage2.png'))){throw 'Missing target reference image'}
function Replace-Required([string]$Text,[string]$Old,[string]$New,[string]$Marker){
    if($Text.Contains($Marker)){return $Text}
    if(!$Text.Contains($Old)){throw ('Source no longer matches expected integration point: '+$Marker)}
    return $Text.Replace($Old,$New)
}
$island=[IO.File]::ReadAllText($islandPath).Replace("`r`n","`n")
$ocean=[IO.File]::ReadAllText($oceanPath).Replace("`r`n","`n")
$island=Replace-Required $island "public static float Height(float x,float z)`n        {" "public static float Height(float x,float z)`n        {`n            return CoastBlockoutSession.Active&&CoastBlockoutLayout.Contains(x,z)?CoastBlockoutLayout.Height(x,z):BaseHeight(x,z);`n        }`n        public static float BaseHeight(float x,float z)`n        {" 'public static float BaseHeight('
$ocean=Replace-Required $ocean "void Start(){`n  var timer=" "void Start(){`n  if(CoastBlockoutSession.Active){ReflectionEnabled=false;BathymetryReady=true;return;}`n  var timer=" 'if(CoastBlockoutSession.Active){ReflectionEnabled=false;BathymetryReady=true;return;}'
$ocean=Replace-Required $ocean "public float BottomHeight(float x,float z){`n  float height=" "public float BottomHeight(float x,float z){`n  if(CoastBlockoutSession.Active&&CoastBlockoutLayout.Contains(x,z))return Mathf.Min(-.30f,CoastBlockoutLayout.Height(x,z));`n  float height=" 'if(CoastBlockoutSession.Active&&CoastBlockoutLayout.Contains(x,z))return'
$ocean=Replace-Required $ocean "public static float Height(Vector3 p,float time){`n  return" "public static float Height(Vector3 p,float time){`n  if(CoastBlockoutSession.Active)return SeaLevel;`n  return" 'if(CoastBlockoutSession.Active)return SeaLevel;'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backup=Join-Path $Project ('Tools\CoastBlockout\Before-'+$stamp)
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $islandPath,$oceanPath -Destination $backup
$scene=Join-Path $Project 'Assets\Deepsea\Scenes\GuichaoBay.unity'
$originalEntry='C:\Users\许嘉乐\Desktop\深海灯塔－立体海岸制作版.lnk'
Copy-Item -LiteralPath $scene -Destination $backup
if(Test-Path -LiteralPath $originalEntry){Copy-Item -LiteralPath $originalEntry -Destination $backup}
Get-FileHash -LiteralPath $scene,(Join-Path $Project 'Builds\CoastSliceV2\GuichaoBay.exe') | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'preserved-hashes.json') -Encoding UTF8
$utf8=New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($islandPath,$island,$utf8)
[IO.File]::WriteAllText($oceanPath,$ocean,$utf8)
foreach($part in @('Runtime','Editor','Shaders')){
    $target=if($part -eq 'Shaders'){Join-Path $Project 'Assets\Deepsea\Shaders'}else{Join-Path $Project ('Assets\Deepsea\Scripts\'+$part)}
    Get-ChildItem -LiteralPath (Join-Path $source $part) -File | Copy-Item -Destination $target -Force
}
$references=Join-Path $Project 'Docs\CoastBlockout-References'
New-Item -ItemType Directory -Path $references -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Reference-Stage2.png') -Destination $references -Force
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $Project 'Tools\CoastBlockout\Install-CoastBlockout.ps1') -Force
Write-Output ('COAST_BLOCKOUT_SOURCE_INSTALLED backup='+$backup)
