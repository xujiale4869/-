param(
 [string]$ProjectPath=(Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔-Unity海陆-v0.3'),
 [string]$UnityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe',
 [switch]$Build
)
$ErrorActionPreference='Stop'
$patch=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath (Join-Path $ProjectPath 'Assets\Deepsea\Scripts\Runtime\CoastalEcology.cs'))){throw '需要上一版海草与海鸟联动工程。'}
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
$backup=Join-Path $ProjectPath ('Tools\CoastBackup-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
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
if(!$Build){Write-Output '尚未构建；使用 -Build 参数完成 Unity 构建与独立版检查。';exit 0}
if(!(Test-Path -LiteralPath $UnityPath)){throw "没有找到 Unity：$UnityPath"}
New-Item -ItemType Directory -Path (Join-Path $ProjectPath 'Verification') -Force | Out-Null
$buildLog=Join-Path $ProjectPath 'Verification\coast-build.log'
$unity=Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod','Deepsea.Editor.CoastBuild.Build','-logFile',('"'+$buildLog+'"')) -PassThru -Wait
if($unity.ExitCode -ne 0){throw "Unity 构建失败，请查看 $buildLog"}
$exe=Join-Path $ProjectPath 'Builds\CoastV1\GuichaoBay.exe'
$checkLog=Join-Path $ProjectPath 'Verification\coast-player.log'
$player=Start-Process -FilePath $exe -WorkingDirectory $ProjectPath -ArgumentList @('-coast-check','-logFile',('"'+$checkLog+'"')) -PassThru -Wait
if($player.ExitCode -ne 0){throw "实机检查未通过，旧启动入口保留，请查看 $checkLog"}
$result=Get-Content -LiteralPath (Join-Path $ProjectPath 'Verification\coast-v1\result.json') -Raw | ConvertFrom-Json
if(!$result.passed){throw '实机结果未通过。'}
if((Get-Item -LiteralPath (Join-Path $ProjectPath 'Verification\coast-v1\result.json')).LastWriteTime -lt (Get-Item -LiteralPath $exe).LastWriteTime){throw '实机结果早于本次构建，未更新启动入口。'}
$terrainLog=Join-Path $ProjectPath 'Verification\coast-terrain.log'
$terrain=Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-projectPath',('"'+$ProjectPath+'"'),'-executeMethod','Deepsea.Editor.ExpeditionPlayCheck.RunTerrain','-logFile',('"'+$terrainLog+'"')) -PassThru -Wait
if($terrain.ExitCode -ne 0){throw "原有登陆/行走检查未通过，请查看 $terrainLog"}
$terrainResult=Get-Content -LiteralPath (Join-Path $ProjectPath 'Verification\unity-expedition-result.json') -Raw | ConvertFrom-Json
if(!$terrainResult.passed){throw '地形回归结果未通过。'}
Copy-Item -LiteralPath (Join-Path $ProjectPath 'Verification\unity-expedition-result.json') -Destination (Join-Path $ProjectPath 'Verification\coast-v1\terrain-result.json') -Force
$ws=New-Object -ComObject WScript.Shell
$shortcut=$ws.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔－水面改进版.lnk'))
$shortcut.TargetPath=$exe;$shortcut.Arguments='';$shortcut.WorkingDirectory=$ProjectPath;$shortcut.Save()
Start-Process -FilePath $exe -WorkingDirectory $ProjectPath
Write-Output '构建和实机检查通过，已更新启动入口。'
