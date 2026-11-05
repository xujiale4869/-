param([string]$RuntimePath='Verification/unified-v30-review06',[string]$NativePath='Verification/unified-v30-native03')
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $project
$runtime=Get-Content ($RuntimePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
$native=Get-Content ($NativePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
$build=Get-Content Tools/SeaUnifiedV30/build.json -Raw -Encoding UTF8|ConvertFrom-Json
if(!$runtime.passed -or !$native.passed -or !$build.passed){throw 'Final runtime/native/build not passed'}
if(!(Test-Path Docs/SeaUnifiedV30/VisualReview/p031-verdict.md)){throw 'Final independent visual critique not archived'}
$b=Get-Content Tools/SeaUnifiedV30/baseline.json -Raw -Encoding UTF8|ConvertFrom-Json
$allowed=@('Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessTerrainV29.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalSpritesV29.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalHUDV29.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaSeamlessWorldAndNauticalUIV29.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessReviewV29.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayGeometryV28.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayExpansionAndBuildSpaceV28.cs')
$changed=@();foreach($f in $b.files){if(!(Test-Path -LiteralPath $f.path) -or (Get-FileHash -LiteralPath $f.path).Hash -ne $f.sha256){$changed+=$f.path}}
$unexpected=@($changed|Where-Object {$_ -notin $allowed});if($unexpected.Count){throw ('Unexpected original input changed: '+($unexpected -join ', '))}
foreach($f in $b.saves){if((Test-Path -LiteralPath $f.path) -ne $f.exists){throw ('Save existence changed during tests: '+$f.path)};if($f.exists -and (Get-FileHash -LiteralPath $f.path).Hash -ne $f.sha256){throw ('Save changed during tests: '+$f.path)}}
foreach($f in $b.oldBuild){if((Get-FileHash -LiteralPath $f.path).Hash -ne $f.sha256){throw ('Old v29 binary changed: '+$f.path)}}
$baseline=@{passed=$true;oldFiles=$b.files.Count;unchanged=$b.files.Count-$changed.Count;expectedChanges=$changed;normalSavesUnchangedDuringTests=$true;oldV29ExeAndAssemblyUnchanged=$true}
$baseline|ConvertTo-Json -Depth 6|Set-Content Tools/SeaUnifiedV30/baseline-verification.json -Encoding UTF8
foreach($d in @('Docs/SeaUnifiedV30/Runtime','Docs/SeaUnifiedV30/NativeInput','Docs/SeaUnifiedV30/Drafts')){New-Item -ItemType Directory -Force $d|Out-Null}
Copy-Item ($RuntimePath+'/*') Docs/SeaUnifiedV30/Runtime/ -Force
Copy-Item ($NativePath+'/*') Docs/SeaUnifiedV30/NativeInput/ -Force
Copy-Item Temp/unified-v30-native-driver03.log Docs/SeaUnifiedV30/NativeInput/driver.txt -Force
foreach($f in @('preview.jpg','02-nautical-build-ui.jpg')){Copy-Item ($RuntimePath+'/'+$f) ('Docs/SeaUnifiedV30/'+$f) -Force}
$anchors='Docs/SeaUnifiedV30/StyleKit-Provisional/anchors';New-Item -ItemType Directory -Force $anchors|Out-Null
$af=@('preview.jpg','02-nautical-build-ui.jpg','04-fishing-ui.jpg','10-gallery-slot-A.jpg');$an=@('sailing.jpg','building.jpg','fishing.jpg','gallery-A.jpg');for($ai=0;$ai -lt 4;$ai++){Copy-Item ($RuntimePath+'/'+$af[$ai]) ($anchors+'/'+$an[$ai]) -Force}
foreach($n in @('01','02','03','04','05')){if(Test-Path ('Verification/unified-v30-review'+$n+'/result.json')){Copy-Item ('Verification/unified-v30-review'+$n+'/result.json') ('Docs/SeaUnifiedV30/Drafts/review'+$n+'-superseded.json') -Force}}
@{passed=$true;visualStatus='provisional runtime UI; no complete benchmark or independent content transfer';runtime=$runtime;nativeInput=$native;build=$build;baseline=$baseline;finalRuntime=$RuntimePath;finalNative=$NativePath}|ConvertTo-Json -Depth 12|Set-Content Docs/SeaUnifiedV30/result.json -Encoding UTF8
$prev='Docs/SeaSeamlessV29/PreviousV29';if(!(Test-Path $prev)){New-Item -ItemType Directory -Force $prev|Out-Null;foreach($n in @('README.md','result.json','preview.jpg','02-nautical-build-ui.jpg')){Copy-Item ('Docs/SeaSeamlessV29/'+$n) ($prev+'/'+$n)};Copy-Item Docs/SeaSeamlessV29/Runtime ($prev+'/Runtime') -Recurse}
$notice='> 当前四张预览已按 v30 交接更新；v29 原图与报告保存在 PreviousV29，最新说明见 [SeaUnifiedV30](../SeaUnifiedV30/README.md)。旧 v29 程序保留；核心共享源码已升级，精确重建 v29 请检出 58525e1。'
$old=[IO.File]::ReadAllText($project+'/Docs/SeaSeamlessV29/README.md');if(!$old.Contains('当前四张预览已按 v30')){[IO.File]::WriteAllText($project+'/Docs/SeaSeamlessV29/README.md',$notice+"`n`n"+$old,[Text.UTF8Encoding]::new($false))}
foreach($f in @('preview.jpg','02-nautical-build-ui.jpg')){Copy-Item ($RuntimePath+'/'+$f) ('Docs/SeaSeamlessV29/'+$f) -Force};foreach($f in @('03-north-coast.jpg','03b-south-coast.jpg')){Copy-Item ($RuntimePath+'/'+$f) ('Docs/SeaSeamlessV29/Runtime/'+$f) -Force}
$paths=@($allowed);foreach($dir in @('Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29','Assets/Deepsea/Scripts/Editor/SeaUnifiedV30','Assets/Deepsea/Resources/SeaSeamlessV29')){$paths+=@(Get-ChildItem -LiteralPath $dir -Recurse -File|ForEach-Object {$_.FullName.Substring($project.Length+1).Replace('\','/')});$paths+=$dir+'.meta'}
$paths+=@('Assets/Deepsea/Scenes/CoastSeaUnifiedV30.unity','Assets/Deepsea/Scenes/CoastSeaUnifiedV30.unity.meta','Tools/SeaUnifiedV30/Rebuild-Unified.ps1','Tools/SeaUnifiedV30/Finalize-Unified.ps1','Tools/SeaUnifiedV30/Test-NativeUI.ps1','Tools/SeaUnifiedV30/OriginalInputs/gpt_v30_unified_island_and_persona_ui_all_in_one.md')
$manifest=@($paths|Sort-Object -Unique|ForEach-Object {@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash;bytes=(Get-Item -LiteralPath $_).Length}})
@{scope='v30 accepted inputs, depends on existing repository; not standalone project backup';files=$manifest}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaUnifiedV30/accepted-manifest.json -Encoding UTF8
& ./Tools/SeaUnifiedV30/Rebuild-Unified.ps1
$exe=Join-Path $project 'Builds/SeaUnifiedV30/Guichao-SeaUnifiedV30.exe';$desktop=[Environment]::GetFolderPath('Desktop');$shell=New-Object -ComObject WScript.Shell
foreach($name in @('深海灯塔－最新版','深海灯塔—最新版','深海灯塔-一体半岛与斜切界面-v30')){$s=$shell.CreateShortcut((Join-Path $desktop ($name+'.lnk')));$s.TargetPath=$exe;$s.Arguments='-unified-v30 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080';$s.WorkingDirectory=$project;$s.IconLocation=$exe+',0';$s.Description='归潮湾 v30：一体半岛/生态过渡/斜切海洋UI，B/G营造，滚轮远眺';$s.Save()}
@{passed=$true;runtimeChecks=$runtime.checks.Count;nativeChecks=$native.checks.Count;acceptedInputs=$manifest.Count;oldV29Unchanged=$true;normalSavesUnchangedDuringTests=$true;visualStatus='provisional';assemblySHA256=(Get-FileHash -LiteralPath 'Builds/SeaUnifiedV30/Guichao-SeaUnifiedV30_Data/Managed/Assembly-CSharp.dll').Hash}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaUnifiedV30/delivery.json -Encoding UTF8
Write-Output ('UNIFIED_V30_DELIVERY_READY checks='+($runtime.checks.Count+$native.checks.Count)+' accepted='+$manifest.Count)
