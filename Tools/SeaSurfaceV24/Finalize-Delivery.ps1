$ErrorActionPreference='Stop'
$p='C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'
$t=$p+'/Tools/SeaSurfaceV24'
$ev=$p+'/Verification/sea-surface-v24-review05'
$ev4=$p+'/Verification/sea-surface-v24-4k-review05'
$n=Get-Content -LiteralPath ($ev+'/result.json') -Raw|ConvertFrom-Json
$k=Get-Content -LiteralPath ($ev4+'/result.json') -Raw|ConvertFrom-Json
if(-not $n.passed -or -not $k.passed){throw 'Final checks failed'}
$base=Get-Content -LiteralPath ($t+'/Before/hashes.json') -Raw|ConvertFrom-Json
$changes=@()
foreach($f in $base){if((Get-FileHash -LiteralPath ($p+'/'+$f.path) -Algorithm SHA256).Hash -ne $f.sha256){$changes+=$f.path}}
if($changes.Count -ne 1 -or $changes[0] -ne 'Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/SeaLifeNavigationV23.cs'){throw ('Unexpected changes: '+($changes -join ','))}
[PSCustomObject]@{checked=$base.Count;unchanged=$base.Count-1;changed=$changes}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath ($t+'/baseline-verification.json') -Encoding UTF8
$rollback=[IO.File]::ReadAllText(($t+'/Before/SeaLifeNavigationV23.cs'))
$expected=$rollback.Replace('public static Vector3 Field(Vector2 p,float t){return NavigationWaveV23.Field(p,t);}','public static Vector3 Field(Vector2 p,float t){return SeaSurfaceV24.RollingActive?OceanWaveV24.Field(p,SeaSurfaceV24.Active.Clock):NavigationWaveV23.Field(p,t);}').Replace('if(ready&&!game.Paused&&Input.GetKeyDown(KeyCode.F7))','if(SeaSurfaceV24.Active==null&&ready&&!game.Paused&&Input.GetKeyDown(KeyCode.F7))')
if([IO.File]::ReadAllText(($p+'/Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/SeaLifeNavigationV23.cs')) -cne $expected){throw 'Unexpected extra edits in v23 source'}
$doc='C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v24'
New-Item -ItemType Directory -Path ($doc+'/runtime/1080'),($doc+'/runtime/4k') -Force|Out-Null
& 'C:/Users/许嘉乐/AppData/Local/Programs/nodejs/node_modules/@ffmpeg-installer/win32-x64/ffmpeg.exe' -hide_banner -loglevel error -y -framerate 30 -i ($ev+'/motion/%04d.jpg') -vf scale=1280:720 -c:v libx264 -preset veryfast -crf 22 -pix_fmt yuv420p -movflags +faststart ($t+'/preview.mp4')
if($LASTEXITCODE -ne 0){throw 'Preview encode failed'}
Copy-Item -LiteralPath ($ev+'/motion/0060.jpg') -Destination ($t+'/preview.jpg') -Force
Copy-Item -LiteralPath ($t+'/reference-analysis.md'),($t+'/README.md'),($t+'/baseline-verification.json'),($t+'/preview.mp4'),($t+'/preview.jpg'),($t+'/build-05.log') -Destination $doc -Force
Get-ChildItem -LiteralPath $ev -File|Copy-Item -Destination ($doc+'/runtime/1080') -Force
Get-ChildItem -LiteralPath $ev4 -File|Copy-Item -Destination ($doc+'/runtime/4k') -Force
Copy-Item -LiteralPath ($t+'/review-05.log'),($t+'/review-4k-05.log') -Destination ($doc+'/runtime') -Force
$ws=New-Object -ComObject WScript.Shell
$desktop=[Environment]::GetFolderPath('Desktop')
$app=$ws.CreateShortcut(($desktop+'/深海灯塔-海面层次与性能-v24.lnk'))
$app.TargetPath=$p+'/Builds/SeaSurfaceV24/Guichao-SeaSurface.exe'
$app.Arguments='-screen-fullscreen 0 -screen-width 1920 -screen-height 1080'
$app.WorkingDirectory=$p
$app.Description='v24 海面层次与性能样板；F7水面对照，F9帧率'
$app.Save()
$clip=$ws.CreateShortcut(($desktop+'/深海灯塔-v24海浪演示.lnk'))
$clip.TargetPath=$t+'/preview.mp4'
$clip.Save()
$summary=@'

## 2026-10-28 v24：海面层次与性能样板
用户明确否定v23的波浪滚动感、海面/水下层次与性能。本轮独立v24样板以海面为主、浅水隐现为目标，增加方向性滚动波、深度/朝向/浪峰控制的可见度、青蓝反光。旧美术/干净绿色海带保留，用户尚未评价本轮效果；不将检查通过当作美术认可。
桌面“深海灯塔-海面层次与性能-v24”和“深海灯塔-v24海浪演示”。报告C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v24/reference-analysis.md；程序Builds/SeaSurfaceV24/Guichao-SeaSurface.exe；场景CoastSeaSurfaceV24.unity；默认构建Deepsea.Editor.SeaSurfaceBuildV24.Build。F7对照水面，F9帧率。正常产品名/存档沿用v21。
实际取消限帧，原v23状态同镜头1080p58.53–65.03FPS；v24最终147.55–148.92FPS，主画面renderScale=1。FXAA替4倍MSAA、半分辨率折射颜色副本、水下细节分级和单次接触阴影滤波。保留阴影与原AO采样精度/强度；画面存在抗锯齿和水下细节取舍。61项原输入清单只允许v23主类两个可选钩子变化，其余60项哈希一致；不是全工程哈希声明。
4K窗口请求被限制为2880x1800，采用3840x2160离屏场景加本机窗口呈现，B10G11R11_UFloatPack32、renderScale=1；最终约56.42FPS/P95 19.35ms，未达到稳定60或4K高刷，不称为4K显示器原生测试。最终证据Verification/sea-surface-v24-review05和sea-surface-v24-4k-review05；32项1080p检查通过，两次运行0错误0警告，build-05成功但仍有历史构建警告。
视频8秒240张1080p引擎帧按30fps固定步长录制，交付缩为720p；不是实时录屏或性能证据。测试使用唯一临时存档。未做全海湾浅水/全地图长期性能、完整人工钓鱼/水族馆回归，UI/生态/钓鱼人物动作未重做。
新增四份水下材质shader与运行时克隆，原材质/shader不改；0.25–0.85m水深渐变降低法线/遮罩/BRDF细节，岸上及水下相机保留完整路径。新增源码/Shader/资源在SeaSurfaceV24目录；v23主类只加波形采样和F7两个可选钩子，备份Tools/SeaSurfaceV24/Before/SeaLifeNavigationV23.cs。构建使用已验证Unity方法，冻结清单用于核对，不自动恢复覆盖；显式Prepare/TuneAndBuild/PrepareBudgetAndBuild属于生成步骤，会覆盖对应v24资源。accepted/源码包依赖既有v23环境，并非完整工程。
'@
foreach($ag in @(($p+'/AGENTS.md'),'C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/AGENTS.md')){if(-not(Select-String -LiteralPath $ag -Pattern 'v24：海面层次与性能样板' -Quiet)){Add-Content -LiteralPath $ag -Value $summary -Encoding UTF8}}
if(Test-Path -LiteralPath ($p+'/Builds/SeaSurfaceV24/Licenses')){Copy-Item -LiteralPath ($p+'/Builds/SeaSurfaceV24/Licenses') -Destination ($t+'/ThirdParty') -Recurse -Force}
$dirs=@('Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24','Assets/Deepsea/Scripts/Editor/SeaSurfaceV24','Assets/Deepsea/Shaders/SeaSurfaceV24','Assets/Deepsea/Resources/SeaSurfaceV24','Tools/SeaSurfaceV24/ThirdParty')
$files=@()
foreach($d in $dirs){$files+=Get-ChildItem -LiteralPath ($p+'/'+$d) -File -Recurse;if(Test-Path -LiteralPath ($p+'/'+$d+'.meta')){$files+=Get-Item -LiteralPath ($p+'/'+$d+'.meta')}}
foreach($f in @('Assets/Deepsea/Scenes/CoastSeaSurfaceV24.unity','Assets/Deepsea/Scenes/CoastSeaSurfaceV24.unity.meta','Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/SeaLifeNavigationV23.cs','Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23/SeaLifeNavigationV23.cs.meta','Tools/SeaSurfaceV24/README.md','Tools/SeaSurfaceV24/reference-analysis.md')){$files+=Get-Item -LiteralPath ($p+'/'+$f)}
$files=$files|Sort-Object FullName -Unique
$manifest=@()
$bundle=$t+'/SourceBundle'
New-Item -ItemType Directory -Path $bundle -Force|Out-Null
foreach($f in $files){$rel=$f.FullName.Substring($p.Length+1).Replace('\','/');$manifest+=[PSCustomObject]@{path=$rel;sha256=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash};foreach($baseDir in @(($t+'/accepted'),$bundle)){$dest=$baseDir+'/'+$rel;New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force|Out-Null;Copy-Item -LiteralPath $f.FullName -Destination $dest -Force}}
$manifest|ConvertTo-Json -Depth 4|Set-Content -LiteralPath ($t+'/accepted-manifest.json') -Encoding UTF8
foreach($f in $manifest){if((Get-FileHash -LiteralPath ($t+'/accepted/'+$f.path) -Algorithm SHA256).Hash -ne $f.sha256){throw ('Snapshot mismatch '+$f.path)}}; Write-Output ('V24_INPUTS_VERIFIED '+$manifest.Count)
Copy-Item -LiteralPath ($t+'/preview.mp4'),($t+'/preview.jpg'),($t+'/README.md'),($t+'/reference-analysis.md'),($t+'/accepted-manifest.json'),($t+'/baseline-verification.json') -Destination $bundle -Force
Copy-Item -LiteralPath ($ev+'/result.json') -Destination ($bundle+'/performance-1080.json') -Force
Copy-Item -LiteralPath ($ev4+'/result.json') -Destination ($bundle+'/performance-4k.json') -Force
New-Item -ItemType Directory -Path ($bundle+'/source-backup') -Force|Out-Null
Copy-Item -LiteralPath ($t+'/Before/SeaLifeNavigationV23.cs') -Destination ($bundle+'/source-backup') -Force
$zip=$t+'/SeaSurface-v24-review.zip'
Compress-Archive -Path ($bundle+'/*') -DestinationPath $zip -Force
$b64=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip))
$lines=for($i=0;$i -lt $b64.Length;$i+=1000){$b64.Substring($i,[Math]::Min(1000,$b64.Length-$i))}
[IO.File]::WriteAllLines(($t+'/source-bundle.b64'),$lines,[Text.Encoding]::ASCII)
Write-Output ('FROZEN '+$manifest.Count)
Write-Output ('ZIP_BYTES '+(Get-Item -LiteralPath $zip).Length)
Write-Output ('BASE64_LINES '+$lines.Count)
Write-Output ('VIDEO_BYTES '+(Get-Item -LiteralPath ($t+'/preview.mp4')).Length)
Write-Output 'V24_DELIVERY_READY'
