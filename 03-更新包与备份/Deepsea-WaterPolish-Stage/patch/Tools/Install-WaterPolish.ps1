param(
 [string]$ProjectPath=(Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔-Unity海陆-v0.3'),
 [string]$UnityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe',
 [switch]$Build
)
$ErrorActionPreference='Stop'
$patch=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath (Join-Path $ProjectPath 'Assets\Deepsea\Scripts\Runtime\ShorelineAtmosphere.cs'))){throw '需要上一版岸浪与海声工程。'}
$baseline=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'baseline-hashes.json') -Raw | ConvertFrom-Json
$files=Get-ChildItem -LiteralPath (Join-Path $patch 'Assets') -File -Recurse | Where-Object {
 if($_.Extension -ne '.meta'){return $true}
 $sourceFolder=$_.FullName.Substring(0,$_.FullName.Length-5)
 $relativeFolder=$sourceFolder.Substring($patch.Length+1)
 return !((Test-Path -LiteralPath $sourceFolder -PathType Container) -and (Test-Path -LiteralPath (Join-Path $ProjectPath $relativeFolder) -PathType Container))
}
# Validate every overwrite before changing any project file.
foreach($file in $files){
 $rel=$file.FullName.Substring($patch.Length+1)
 $target=Join-Path $ProjectPath $rel
 if(!(Test-Path -LiteralPath $target)){continue}
 $current=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant()
 $new=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
 if($current -eq $new){continue}
 $key=$rel.Replace('\','/')
 $expected=$baseline.PSObject.Properties[$key]
 if($null -eq $expected -or $current -ne $expected.Value){throw "文件已有其他修改，未写入任何文件：$rel"}
}
$backup=Join-Path $ProjectPath ('Tools\WaterPolishBackup-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
foreach($file in $files){
 $rel=$file.FullName.Substring($patch.Length+1);$target=Join-Path $ProjectPath $rel
 if(Test-Path -LiteralPath $target){
  $saved=Join-Path $backup $rel;New-Item -ItemType Directory -Path (Split-Path -Parent $saved) -Force | Out-Null
  Copy-Item -LiteralPath $target -Destination $saved -Force
 }
 New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
 Copy-Item -LiteralPath $file.FullName -Destination $target -Force
}
Write-Output "已合入源码，原文件备份：$backup"
if(!$Build){Write-Output '已合入水面改进源码，尚未构建。';exit 0}
if(!(Test-Path -LiteralPath $UnityPath)){throw '没有找到 Unity'}
$buildLog=Join-Path $ProjectPath 'Verification\water-polish-build.log'
$editor=Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod','Deepsea.Editor.WaterPolishBuild.Build','-logFile',('"'+$buildLog+'"')) -Wait -PassThru
if($editor.ExitCode -ne 0){throw '水面构建失败，请查看日志'}
$exe=Join-Path $ProjectPath 'Builds\WaterPolishV1\GuichaoBay.exe'
foreach($mode in @('before','after')){
 $args=@('-water-polish-check','-logFile',('"'+$ProjectPath+'\Verification\water-polish-'+$mode+'.log"'))
 if($mode -eq 'before'){$args+= '-water-baseline'}
 $player=Start-Process -FilePath $exe -WorkingDirectory $ProjectPath -ArgumentList $args -Wait -PassThru
 if($player.ExitCode -ne 0){throw ('独立版检查失败：'+$mode)}
 $resultPath=Join-Path $ProjectPath ('Verification\water-polish-v1\'+$mode+'\result.json')
 if(!(Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json).passed){throw ('验证未通过：'+$mode)}
 if((Get-Item -LiteralPath $resultPath).LastWriteTime -lt (Get-Item -LiteralPath $exe).LastWriteTime){throw '验证结果早于构建'}
}
$terrain=Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod','Deepsea.Editor.ExpeditionPlayCheck.RunTerrain','-logFile',('"'+$ProjectPath+'\Verification\water-polish-terrain.log"')) -Wait -PassThru
if($terrain.ExitCode -ne 0){throw '登陆与行走回归失败'}
$terrainPath=Join-Path $ProjectPath 'Verification\unity-expedition-result.json'
if(!(Get-Content -LiteralPath $terrainPath -Raw|ConvertFrom-Json).passed){throw '地形回归未通过'}
Copy-Item -LiteralPath $terrainPath -Destination (Join-Path $ProjectPath 'Verification\water-polish-v1\terrain-result.json') -Force
$eco=Start-Process -FilePath $exe -WorkingDirectory $ProjectPath -ArgumentList @('-ecology-check','-logFile',('"'+$ProjectPath+'\Verification\water-polish-ecology.log"')) -Wait -PassThru
if($eco.ExitCode -ne 0){throw '鱼群与海鸟回归失败'}
$ecoPath=Join-Path $ProjectPath 'Verification\ecology-v1\result.json'
if(!(Get-Content -LiteralPath $ecoPath -Raw|ConvertFrom-Json).passed){throw '生态回归未通过'}
Copy-Item -LiteralPath $ecoPath -Destination (Join-Path $ProjectPath 'Verification\water-polish-v1\ecology-result.json') -Force
$ws=New-Object -ComObject WScript.Shell
$shortcut=$ws.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－水面改进版.lnk'))
$shortcut.TargetPath=$exe;$shortcut.Arguments='';$shortcut.WorkingDirectory=$ProjectPath;$shortcut.Save()
Start-Process -FilePath $exe -WorkingDirectory $ProjectPath
Write-Output '水面改进构建和回归验证通过，桌面入口已更新。'
