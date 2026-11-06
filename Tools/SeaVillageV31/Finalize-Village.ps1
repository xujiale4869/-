param([string]$Review='village-v31-review02',[string]$Native='village-v31-native02')
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$r=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Push-Location $r
try{
 $reviewPath=$r+'/Verification/'+$Review;$nativePath=$r+'/Verification/'+$Native
 $a=Get-Content -LiteralPath ($reviewPath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
 $b=Get-Content -LiteralPath ($nativePath+'/result.json') -Raw -Encoding UTF8|ConvertFrom-Json
 $stats=Get-Content -LiteralPath ($reviewPath+'/village-stats.json') -Raw -Encoding UTF8|ConvertFrom-Json
 if(!$a.passed -or !$b.passed -or $a.errors.Count -or $b.errors.Count -or $a.checksCount -lt 108 -or $b.checksCount -ne 20){throw 'Current runtime/native acceptance is incomplete'}
 if(!(Test-Path Docs/SeaVillageV31/VisualReview/verdict.md)){throw 'Independent visual review must be archived before finalizing'}
 $baseline=Get-Content Tools/SeaVillageV31/baseline.json -Raw -Encoding UTF8|ConvertFrom-Json
 $expected=@('Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaSeamlessWorldAndNauticalUIV29.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessTerrainV29.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessReviewV29.cs','Assets/Deepsea/Scripts/Runtime/SeaExpansionV28/SeaBayExpansionAndBuildSpaceV28.cs')
 $changed=@();foreach($f in $baseline.files){if((Get-FileHash -LiteralPath ($r+'/'+$f.path) -Algorithm SHA256).Hash -ne $f.sha256){if($expected -notcontains $f.path){throw ('Unexpected old input changed: '+$f.path)};$changed+=$f.path}}
 if($changed.Count -ne 4){throw ('Expected exactly four old source changes, found '+$changed.Count)}
 $saveRoot=[Environment]::GetFolderPath('LocalApplicationData')+'Low/GuichaoStudio/深海灯塔 · 海湾生活 v21'
 foreach($s in $baseline.saves){if((Get-FileHash -LiteralPath ($saveRoot+'/'+$s.name) -Algorithm SHA256).Hash -ne $s.sha256){throw ('Normal save changed during isolated tests: '+$s.name)}}
 if(Test-Path -LiteralPath ($saveRoot+'/sea-bay-expansion-v31.json')){throw 'v31 layout already exists; inspect migration instead of repeating first-run finalization'}
 foreach($f in $baseline.v30){if((Get-FileHash -LiteralPath ($r+'/'+$f.path) -Algorithm SHA256).Hash -ne $f.sha256){throw ('Preserved v30 binary changed: '+$f.path)}}
 $docs=$r+'/Docs/SeaVillageV31';[IO.Directory]::CreateDirectory($docs)|Out-Null
 Copy-Item -LiteralPath $reviewPath -Destination ($docs+'/Runtime') -Recurse -Force
 Copy-Item -LiteralPath $nativePath -Destination ($docs+'/NativeInput') -Recurse -Force
 Copy-Item -LiteralPath ($reviewPath+'/11-village-panorama.jpg') -Destination ($docs+'/preview.jpg') -Force
 Copy-Item -LiteralPath ($reviewPath+'/02-village-and-flora.jpg') -Destination ($docs+'/02-village-and-flora.jpg') -Force
 [IO.Directory]::CreateDirectory($docs+'/Drafts')|Out-Null
 foreach($n in @('village-v31-review01','village-v31-native01')){if(Test-Path ('Verification/'+$n)){Copy-Item -LiteralPath ($r+'/Verification/'+$n) -Destination ($docs+'/Drafts/'+$n) -Recurse -Force}}
 [IO.File]::WriteAllText($docs+'/Drafts/README.md',"# Superseded evidence`nInitial review01 passed107 functional checks and native01 passed20, but visible tall boatwright foundations/tree occlusion required revised plots and a fresh build. These are historical drafts, not final art evidence.`n")
 $dll=(Get-FileHash Builds/SeaVillageV31/Guichao-SeaVillageV31_Data/Managed/Assembly-CSharp.dll -Algorithm SHA256).Hash
 $proof=@{passed=$true;oldInputs=$baseline.files.Count;expectedSourceChanges=$changed;unchanged=$baseline.files.Count-4;normalSavesUnchangedDuringTests=$true;v30ExeAndAssemblyUnchanged=$true;assemblySHA256=$dll}
 $proof|ConvertTo-Json -Depth 6|Set-Content Tools/SeaVillageV31/baseline-verification.json -Encoding UTF8
 Copy-Item ($reviewPath+'/result.json') ($docs+'/result.json') -Force
 $paths=@((Get-Content Tools/SeaUnifiedV30/accepted-manifest.json -Raw -Encoding UTF8|ConvertFrom-Json).files|ForEach-Object {$_.path})
 $paths+=@(Get-ChildItem Assets/Deepsea/Resources/SeaVillageV31 -File -Recurse|ForEach-Object {$_.FullName.Substring($r.Length+1).Replace('\','/')})
 $paths+=@('Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaVillageAndBiodiversityV31.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaVillageAndBiodiversityV31.cs.meta','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/VillageReviewV31.cs','Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/VillageReviewV31.cs.meta','Assets/Deepsea/Scripts/Editor/SeaVillageV31/SeaVillageBuildV31.cs','Assets/Deepsea/Scripts/Editor/SeaVillageV31/SeaVillageBuildV31.cs.meta','Assets/Deepsea/Scenes/CoastSeaVillageV31.unity','Assets/Deepsea/Scenes/CoastSeaVillageV31.unity.meta','Tools/SeaVillageV31/generate_village_and_biodiversity_v31.py','Tools/SeaVillageV31/Rebuild-Village.ps1','Tools/SeaVillageV31/validate_mesh_sources_v31.py')
 $inputs=@($paths|Sort-Object -Unique|ForEach-Object {$f=Get-Item -LiteralPath ($r+'/'+$_);@{path=$_;bytes=$f.Length;sha256=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash}})
 @{files=$inputs;scope='v31 frozen inputs; existing repository required; not a standalone full-project backup'}|ConvertTo-Json -Depth 6|Set-Content Tools/SeaVillageV31/accepted-manifest.json -Encoding UTF8
 $tokens=@{'{{RUNTIME_CHECKS}}'=$a.checksCount;'{{NATIVE_CHECKS}}'=$b.checksCount;'{{FLORA}}'=$stats.marinePlacements;'{{SHORE}}'=$stats.shorePlacements;'{{SPECIES}}'=($stats.species -join ', ');'{{SAIL_FPS}}'=('{0:F2}' -f $a.sailingFps);'{{SAIL_P95}}'=('{0:F2}' -f $a.sailingP95);'{{PLAN_FPS}}'=('{0:F2}' -f $a.planningFps);'{{PLAN_P95}}'=('{0:F2}' -f $a.planningP95);'{{OLD_INPUTS}}'=$baseline.files.Count;'{{UNCHANGED}}'=($baseline.files.Count-4);'{{FROZEN_INPUTS}}'=$inputs.Count;'{{DLL_SHA}}'=$dll;'{{REVIEW}}'=$Review;'{{NATIVE}}'=$Native}
 $text=[IO.File]::ReadAllText($r+'/Tools/SeaVillageV31/README.template.md');foreach($key in $tokens.Keys){$text=$text.Replace($key,[string]$tokens[$key])};[IO.File]::WriteAllText($docs+'/README.md',$text)
 $old=[IO.File]::ReadAllText($r+'/AGENTS.md');$lineEnd=$old.IndexOf("`n",$old.IndexOf("当前最新版"));if($lineEnd -gt 0){$start=$old.IndexOf("当前最新版");$old=$old.Substring(0,$start)+'当前最新版为 v31：六类水下植物、潮间带过渡与三座海岬渔村建筑，见 Docs/SeaVillageV31/README.md，程序 Builds/SeaVillageV31/Guichao-SeaVillageV31.exe。v30 与旧存档保留；整体美术仍为 provisional。'+$old.Substring($lineEnd)}
 if(!$old.Contains('## 2026-11-06 v31：')){$old+="`n`n## 2026-11-06 v31：渔村与生态多样性`n用户提供v31一键包。原文Tools/SeaVillageV31/OriginalInputs，实际适配资源烘焙、材质槽、导入坐标、姿态限高、可见地面、温和空地和重入保护。11个Blender FBX/源文件与8170三角，运行时使用同源JSON烘焙的Unity Mesh，三座生成建筑实际渲染；非原稿方块回退。留绿色海带、水光学、人物/船与旧玩法。三建筑是后续NPC据点，没有NPC行为/室内/新收益系统。布局sea-bay-expansion-v31.json首次从v30复制，正常产品名仍v21。`n最终运行$($a.checksCount)项/原生键鼠$($b.checksCount)项通过；旧$($baseline.files.Count)输入仅四旧源码变化，正常档测试时不改，v30exe/dll保留。精确数据与视觉限制见Docs/SeaVillageV31，首稿高石基/遮挡失败证据保留Drafts，功能通过不等于美术认可。短窗性能不得称全图长期稳定或4K表现。默认Rebuild-Village校验冻结输入，-Build重建，-Prepare显式重生成需重新验收。不要重跑已普通启动后的首轮Finalize基线断言；旧v30冻结清单会拒绝共享源码升级，精确v30源码检出9069585。`n"};[IO.File]::WriteAllText($r+'/AGENTS.md',$old)
 $desktop=[Environment]::GetFolderPath('Desktop');$shell=New-Object -ComObject WScript.Shell
 foreach($name in @('深海灯塔—最新版','深海灯塔－最新版','深海灯塔-渔村与多样生态-v31')){$s=$shell.CreateShortcut($desktop+'/'+$name+'.lnk');$s.TargetPath=$r+'/Builds/SeaVillageV31/Guichao-SeaVillageV31.exe';$s.WorkingDirectory=$r;$s.Description='归潮湾 v31 · 海岬渔村与多样生态';$s.IconLocation=$s.TargetPath+',0';$s.Save()}
 @{passed=$true;runtimeChecks=$a.checksCount;nativeChecks=$b.checksCount;acceptedInputs=$inputs.Count;normalSavesUnchangedDuringTests=$true;oldV30Unchanged=$true;visualStatus='provisional';assemblySHA256=$dll}|ConvertTo-Json|Set-Content Tools/SeaVillageV31/delivery.json -Encoding UTF8
 Write-Output ('V31_FINALIZED '+$a.checksCount+' runtime / '+$b.checksCount+' native / '+$inputs.Count+' inputs')
}finally{Pop-Location}
