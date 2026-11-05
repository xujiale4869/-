$ErrorActionPreference='Stop'
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $project
$runtime=Get-Content Verification/harbor-v27-review03/result.json -Raw -Encoding UTF8|ConvertFrom-Json
$native=Get-Content Verification/harbor-v27-native03/result.json -Raw -Encoding UTF8|ConvertFrom-Json
$baseline=Get-Content Tools/SeaHarborV27/baseline-verification.json -Raw -Encoding UTF8|ConvertFrom-Json
$build=Get-Content Tools/SeaHarborV27/build.json -Raw -Encoding UTF8|ConvertFrom-Json
if(!$runtime.passed -or !$native.passed -or !$baseline.passed -or !$build.passed){throw 'Delivery checks have not passed'}
$b=Get-Content Tools/SeaHarborV27/baseline.json -Raw -Encoding UTF8|ConvertFrom-Json
if((Get-FileHash -LiteralPath $b.savePath -Algorithm SHA256).Hash -ne $b.saveHash){throw 'Normal player save changed'}
$original=(Get-FileHash Tools/SeaHarborV27/OriginalInputs/gpt_v27_harbor_reef_all_in_one.md -Algorithm SHA256).Hash
if($original -ne 'CCC243E0EA6651250683391F3B509B95E343FD2E32E85D6303A2FBC33B86CC96'){throw 'Original package transfer differs'}
foreach($dir in @('Docs/SeaHarborV27/Runtime','Docs/SeaHarborV27/NativeInput','Docs/SeaHarborV27/Drafts')){New-Item -ItemType Directory -Force $dir|Out-Null}
Copy-Item Verification/harbor-v27-review03/* Docs/SeaHarborV27/Runtime/ -Force
Copy-Item Verification/harbor-v27-native03/* Docs/SeaHarborV27/NativeInput/ -Force
foreach($file in @('preview.jpg','01-baseline.jpg','02-harbor.jpg','03-stern-lantern.jpg','04-reef.jpg')){Copy-Item ('Verification/harbor-v27-review03/'+$file) ('Docs/SeaHarborV27/'+$file) -Force}
Copy-Item Verification/harbor-v27-review01/result.json Docs/SeaHarborV27/Drafts/review01-failed-bed-filter.json -Force
Copy-Item Verification/harbor-v27-review02/result.json Docs/SeaHarborV27/Drafts/review02-before-reef-polish.json -Force
Copy-Item Verification/harbor-v27-native02/result.json Docs/SeaHarborV27/Drafts/native02-stopped-by-focus-guard.json -Force
@{passed=$true;runtime=$runtime;nativeInput=$native;build=$build;baseline=$baseline;originalPackageSHA256=$original;finalRuntime='harbor-v27-review03';finalNativeInput='harbor-v27-native03'}|ConvertTo-Json -Depth 10|Set-Content Docs/SeaHarborV27/result.json -Encoding UTF8
Select-String Temp/harbor-v27-build03.log -Pattern 'HARBOR_MESH |HARBOR_V27_BUILD_OK|warning CS|Shader warning'|ForEach-Object {$_.Line}|Sort-Object -Unique|Set-Content Docs/SeaHarborV27/build-evidence.txt -Encoding UTF8
# Retain only accepted .blend sources in Git; automatic .blend1 backups stay local.
'Sources/*.blend1'|Set-Content Tools/SeaHarborV27/.gitignore -Encoding ASCII
$paths=@()
foreach($dir in @('Assets/Deepsea/Resources/SeaHarborV27','Assets/Deepsea/Scripts/Editor/SeaHarborV27','Tools/SeaHarborV27/OriginalInputs')){$paths+=@(Get-ChildItem -LiteralPath $dir -Recurse -File|ForEach-Object {$_.FullName.Substring($project.Length+1).Replace('\','/')})}
$paths+=@(Get-ChildItem Tools/SeaHarborV27/Sources -Filter '*.blend' -File|ForEach-Object {$_.FullName.Substring($project.Length+1).Replace('\','/')})
$paths+=@('Assets/Deepsea/Scenes/CoastSeaHarborV27.unity','Assets/Deepsea/Scenes/CoastSeaHarborV27.unity.meta','Assets/Deepsea/Resources/SeaHarborV27.meta','Assets/Deepsea/Scripts/Editor/SeaHarborV27.meta','Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaHarborAndReefUpgradeV27.cs','Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaHarborAndReefUpgradeV27.cs.meta','Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaHarborReviewV27.cs','Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaHarborReviewV27.cs.meta','Assets/Deepsea/Shaders/UnderwaterReefVibrantV27.shader','Assets/Deepsea/Shaders/UnderwaterReefVibrantV27.shader.meta','Assets/Deepsea/Shaders/HarborLightingV27.hlsl','Assets/Deepsea/Shaders/HarborLightingV27.hlsl.meta','Assets/Deepsea/Shaders/HarborSurfaceV27.shader','Assets/Deepsea/Shaders/HarborSurfaceV27.shader.meta','Assets/Deepsea/Shaders/SeaSurfaceV24/SeaSurfaceSparkleHarborV27.shader','Assets/Deepsea/Shaders/SeaSurfaceV24/SeaSurfaceSparkleHarborV27.shader.meta','Tools/SeaHarborV27/generate_harbor_and_reef_v27.py','Tools/SeaHarborV27/Rebuild-Harbor.ps1','Tools/SeaHarborV27/Test-NativeF8.ps1')
$manifest=@($paths|Sort-Object -Unique|ForEach-Object {@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $_).Length}})
@{scope='Accepted new v27 inputs only; depends on the existing Unity project';files=$manifest}|ConvertTo-Json -Depth 5|Set-Content Tools/SeaHarborV27/accepted-manifest.json -Encoding UTF8
& ./Tools/SeaHarborV27/Rebuild-Harbor.ps1
$desktop=[Environment]::GetFolderPath('Desktop')
$shell=New-Object -ComObject WScript.Shell
$exe=Join-Path $project 'Builds/SeaHarborV27/Guichao-SeaHarborV27.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Missing final player'}
foreach($name in @('深海灯塔－最新版','深海灯塔-港湾与礁区-v27')){
 $shortcut=$shell.CreateShortcut((Join-Path $desktop ($name+'.lnk')))
 $shortcut.TargetPath=$exe
 $shortcut.Arguments='-harbor-v27 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080'
 $shortcut.WorkingDirectory=$project
 $shortcut.Description='v27 港湾与礁区：F8对照，F7水面调参，F6水面对照，F9帧率'
 $shortcut.IconLocation=$exe+',0'
 $shortcut.Save()
}
@{passed=$true;acceptedFiles=$manifest.Count;runtimeChecks=$runtime.checks.Count;nativeChecks=$native.checks.Count;originalPackageSHA256=$original;playerSHA256=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash;normalSaveUnchanged=$true}|ConvertTo-Json -Depth 4|Set-Content Tools/SeaHarborV27/delivery.json -Encoding UTF8
Write-Output ('HARBOR_DELIVERY_READY accepted='+$manifest.Count+' checks='+($runtime.checks.Count+$native.checks.Count))
