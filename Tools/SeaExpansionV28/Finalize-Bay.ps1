$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $project
$runtimePath='Verification/bay-v28-review06'
$nativePath='Verification/bay-v28-native05'
$runtime=Get-Content ($runtimePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
$native=Get-Content ($nativePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
$build=Get-Content Tools/SeaExpansionV28/build.json -Raw -Encoding UTF8|ConvertFrom-Json
if(!$runtime.passed -or !$native.passed -or !$build.passed){throw 'Final runtime, native input or build checks failed'}
$b=Get-Content Tools/SeaExpansionV28/baseline.json -Raw -Encoding UTF8|ConvertFrom-Json
$allowed=@('Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeRuntime.cs','Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/SeaLifeNavigationMotorV23.cs','Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaWaterArtUpgradeV26.cs')
$changed=@();foreach($f in $b.files){if(!(Test-Path -LiteralPath $f.path) -or (Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash -ne $f.sha256){$changed+=$f.path}}
$unexpected=@($changed|Where-Object {$_ -notin $allowed});if($unexpected.Count){throw ('Unexpected old files changed: '+($unexpected -join ', '))}
foreach($s in $b.saves){if((Test-Path -LiteralPath $s.path) -ne $s.exists){throw ('Player save existence changed: '+$s.path)};if($s.exists -and (Get-FileHash -LiteralPath $s.path -Algorithm SHA256).Hash -ne $s.sha256){throw ('Player save changed: '+$s.path)}}
if((Get-FileHash -LiteralPath $b.oldPlayer.path -Algorithm SHA256).Hash -ne $b.oldPlayer.sha256){throw 'Previous v27 player changed'}
$baseline=@{passed=$true;oldFiles=$b.files.Count;unchanged=$b.files.Count-$changed.Count;expectedChanges=$changed;normalSavesUnchanged=$true;v27PlayerUnchanged=$true}
$baseline|ConvertTo-Json -Depth 6|Set-Content Tools/SeaExpansionV28/baseline-verification.json -Encoding UTF8
foreach($dir in @('Docs/SeaExpansionV28/Runtime','Docs/SeaExpansionV28/NativeInput','Docs/SeaExpansionV28/Drafts')){New-Item -ItemType Directory -Force $dir|Out-Null}
Copy-Item ($runtimePath+'/*') Docs/SeaExpansionV28/Runtime/ -Force
Copy-Item ($nativePath+'/*') Docs/SeaExpansionV28/NativeInput/ -Force
foreach($f in @('preview.jpg','02-god-build-grid.jpg','03-crescent-atoll.jpg','04-pier-extension.jpg')){Copy-Item ($runtimePath+'/'+$f) ('Docs/SeaExpansionV28/'+$f) -Force}
if(Test-Path Verification/bay-v28-review02/result.json){Copy-Item Verification/bay-v28-review02/result.json Docs/SeaExpansionV28/Drafts/review02-material-and-boundary-failed.json -Force}
if(Test-Path Verification/bay-v28-review04/result.json){Copy-Item Verification/bay-v28-review04/result.json Docs/SeaExpansionV28/Drafts/review04-junction-walk-failed.json -Force}
if(Test-Path Verification/bay-v28-native01/result.json){Copy-Item Verification/bay-v28-native01/result.json Docs/SeaExpansionV28/Drafts/native01-driver-module-not-loaded.json -Force}
if(Test-Path Verification/bay-v28-native03/result.json){Copy-Item Verification/bay-v28-native03/result.json Docs/SeaExpansionV28/Drafts/native03-driver-dpi-mapping-failed.json -Force}
if(Test-Path Temp/bay-v28-native-driver02-error.log){Copy-Item Temp/bay-v28-native-driver02-error.log Docs/SeaExpansionV28/Drafts/native02-stopped-by-focus-guard.txt -Force}
Select-String Temp/bay-v28-review01.log -Pattern 'ArgumentNullException|Parameter name|at Deepsea'|ForEach-Object {$_.Line}|Set-Content Docs/SeaExpansionV28/Drafts/review01-initialization-failed.txt -Encoding UTF8
@{passed=$true;runtime=$runtime;nativeInput=$native;build=$build;baseline=$baseline;finalRuntime=$runtimePath;finalNativeInput=$nativePath}|ConvertTo-Json -Depth 10|Set-Content Docs/SeaExpansionV28/result.json -Encoding UTF8
$paths=@($allowed)
foreach($dir in @('Assets/Deepsea/Scripts/Runtime/SeaExpansionV28','Assets/Deepsea/Scripts/Editor/SeaExpansionV28','Assets/Deepsea/Resources/SeaExpansionV28','Assets/Deepsea/Shaders/SeaExpansionV28')){$paths+=@(Get-ChildItem -LiteralPath $dir -Recurse -File|ForEach-Object {$_.FullName.Substring($project.Length+1).Replace('\','/')});$paths+=$dir+'.meta'}
$paths+=@('Assets/Deepsea/Scenes/CoastSeaExpansionV28.unity','Assets/Deepsea/Scenes/CoastSeaExpansionV28.unity.meta','Tools/SeaExpansionV28/Rebuild-Bay.ps1','Tools/SeaExpansionV28/Test-NativeBuild.ps1','Tools/SeaExpansionV28/OriginalInputs/gpt_v28_bay_expansion_all_in_one.md')
$manifest=@($paths|Sort-Object -Unique|ForEach-Object {@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $_).Length}})
@{scope='Accepted v28 inputs; depends on the existing repository, not a complete standalone project backup';files=$manifest}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaExpansionV28/accepted-manifest.json -Encoding UTF8
& ./Tools/SeaExpansionV28/Rebuild-Bay.ps1
$desktop=[Environment]::GetFolderPath('Desktop');$shell=New-Object -ComObject WScript.Shell
$exe=Join-Path $project 'Builds/SeaExpansionV28/Guichao-SeaExpansionV28.exe'
foreach($name in @('深海灯塔－最新版','深海灯塔—最新版','深海灯塔-海域扩建与建造-v28')){$shortcut=$shell.CreateShortcut((Join-Path $desktop ($name+'.lnk')));$shortcut.TargetPath=$exe;$shortcut.Arguments='-bay-v28 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080';$shortcut.WorkingDirectory=$project;$shortcut.Description='归潮湾 v28：G海湾规划，1-4设施，R旋转，左键建造，右键拆除';$shortcut.IconLocation=$exe+',0';$shortcut.Save()}
@{passed=$true;runtimeChecks=$runtime.checks.Count;nativeChecks=$native.checks.Count;acceptedInputs=$manifest.Count;normalSavesUnchanged=$true;oldV27Unchanged=$true;playerSHA256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaExpansionV28/delivery.json -Encoding UTF8
Write-Output ('BAY_V28_DELIVERY_READY checks='+($runtime.checks.Count+$native.checks.Count)+' accepted='+$manifest.Count)
