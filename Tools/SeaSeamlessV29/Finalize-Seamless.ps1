$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $project
$runtimePath='Verification/seamless-v29-review07'
$nativePath='Verification/seamless-v29-native02'
$runtime=Get-Content ($runtimePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
$native=Get-Content ($nativePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
$build=Get-Content Tools/SeaSeamlessV29/build.json -Raw -Encoding UTF8|ConvertFrom-Json
if(!$runtime.passed -or !$native.passed -or !$build.passed){throw 'Final runtime, native input or build checks failed'}
if(!(Test-Path Docs/SeaSeamlessV29/VisualReview/verdict.md)){throw 'Independent visual review and explicit provisional status required'}
$b=Get-Content Tools/SeaSeamlessV29/baseline.json -Raw -Encoding UTF8|ConvertFrom-Json
$allowed=@('Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeRuntime.cs','Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeUI.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayExpansionAndBuildSpaceV28.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayGeometryV28.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayBuildUIV28.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayStructuresV28.cs')
$changed=@();foreach($f in $b.files){if(!(Test-Path -LiteralPath $f.path) -or (Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash -ne $f.sha256){$changed+=$f.path}}
$unexpected=@($changed|Where-Object {$_ -notin $allowed});if($unexpected.Count){throw ('Unexpected old files changed: '+($unexpected -join ', '))}
foreach($s in $b.saves){if((Test-Path -LiteralPath $s.path) -ne $s.exists){throw ('Player save existence changed during isolated tests: '+$s.path)};if($s.exists -and (Get-FileHash -LiteralPath $s.path -Algorithm SHA256).Hash -ne $s.sha256){throw ('Player save changed during isolated tests: '+$s.path)}}
foreach($f in $b.oldBuild){if((Get-FileHash -LiteralPath $f.path -Algorithm SHA256).Hash -ne $f.sha256){throw ('Previous v28 binary changed: '+$f.path)}}
$baseline=@{passed=$true;oldFiles=$b.files.Count;unchanged=$b.files.Count-$changed.Count;expectedChanges=$changed;normalSavesUnchangedDuringTests=$true;v28ExeAndAssemblyUnchanged=$true}
$baseline|ConvertTo-Json -Depth 6|Set-Content Tools/SeaSeamlessV29/baseline-verification.json -Encoding UTF8
foreach($dir in @('Docs/SeaSeamlessV29/Runtime','Docs/SeaSeamlessV29/NativeInput','Docs/SeaSeamlessV29/Drafts')){New-Item -ItemType Directory -Force $dir|Out-Null}
Copy-Item ($runtimePath+'/*') Docs/SeaSeamlessV29/Runtime/ -Force
Copy-Item ($nativePath+'/*') Docs/SeaSeamlessV29/NativeInput/ -Force
Copy-Item Temp/seamless-v29-native-driver02.log Docs/SeaSeamlessV29/NativeInput/driver.txt -Force
foreach($f in @('preview.jpg','02-nautical-build-ui.jpg')){Copy-Item ($runtimePath+'/'+$f) ('Docs/SeaSeamlessV29/'+$f) -Force}
foreach($n in @('01','04','05','06')){Copy-Item ('Verification/seamless-v29-review'+$n+'/result.json') ('Docs/SeaSeamlessV29/Drafts/review'+$n+'-superseded.json') -Force}
@{passed=$true;visualStatus='provisional; not whole-game style validation';runtime=$runtime;nativeInput=$native;build=$build;baseline=$baseline;finalRuntime=$runtimePath;finalNativeInput=$nativePath}|ConvertTo-Json -Depth 10|Set-Content Docs/SeaSeamlessV29/result.json -Encoding UTF8
$paths=@($allowed)
foreach($dir in @('Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29','Assets/Deepsea/Scripts/Editor/SeaSeamlessWorldV29','Assets/Deepsea/Resources/SeaSeamlessV29')){$paths+=@(Get-ChildItem -LiteralPath $dir -Recurse -File|ForEach-Object {$_.FullName.Substring($project.Length+1).Replace('\','/')});$paths+=$dir+'.meta'}
$paths+=@('Assets/Deepsea/Scenes/CoastSeaSeamlessV29.unity','Assets/Deepsea/Scenes/CoastSeaSeamlessV29.unity.meta','Tools/SeaSeamlessV29/Rebuild-Seamless.ps1','Tools/SeaSeamlessV29/Test-NativeUI.ps1','Tools/SeaSeamlessV29/Finalize-Seamless.ps1','Tools/SeaSeamlessV29/OriginalInputs/gpt_v29_seamless_world_and_ui_all_in_one.md')
$manifest=@($paths|Sort-Object -Unique|ForEach-Object {@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $_).Length}})
@{scope='Accepted v29 inputs; depends on the existing repository, not a complete standalone project backup';files=$manifest}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaSeamlessV29/accepted-manifest.json -Encoding UTF8
& ./Tools/SeaSeamlessV29/Rebuild-Seamless.ps1
$desktop=[Environment]::GetFolderPath('Desktop');$shell=New-Object -ComObject WScript.Shell
$exe=Join-Path $project 'Builds/SeaSeamlessV29/Guichao-SeaSeamlessV29.exe'
foreach($name in @('深海灯塔－最新版','深海灯塔—最新版','深海灯塔-连续海域与航海界面-v29')){$shortcut=$shell.CreateShortcut((Join-Path $desktop ($name+'.lnk')));$shortcut.TargetPath=$exe;$shortcut.Arguments='-seamless-v29 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080';$shortcut.WorkingDirectory=$project;$shortcut.Description='归潮湾 v29：滚轮看远海，B/G海湾营造，L船灯；保留旧v28入口';$shortcut.IconLocation=$exe+',0';$shortcut.Save()}
@{passed=$true;runtimeChecks=$runtime.checks.Count;nativeChecks=$native.checks.Count;acceptedInputs=$manifest.Count;normalSavesUnchangedDuringTests=$true;oldV28ExeAndAssemblyUnchanged=$true;visualStatus='provisional';playerSHA256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaSeamlessV29/delivery.json -Encoding UTF8
Write-Output ('SEAMLESS_V29_DELIVERY_READY checks='+($runtime.checks.Count+$native.checks.Count)+' accepted='+$manifest.Count)
