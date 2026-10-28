$ErrorActionPreference='Stop'
$p='C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'
$t=$p+'/Tools/SeaLifeNavigationV23'
$ev=$p+'/Verification/sea-navigation-v23-review03'
$report=Get-Content -LiteralPath ($ev+'/result.json') -Raw | ConvertFrom-Json
if(-not $report.passed){throw 'Final review failed'}
$base=Get-Content -LiteralPath ($t+'/Before-20261028/baseline-hashes.json') -Raw | ConvertFrom-Json
$diff=@()
foreach($f in $base){if((Get-FileHash -LiteralPath ($p+'/'+$f.path) -Algorithm SHA256).Hash -ne $f.sha256){$diff+=$f.path}}
if($diff.Count -ne 1 -or $diff[0] -notmatch 'SeaLifeRuntime.cs$'){throw ('Unexpected baseline changes: '+($diff -join ','))}
[PSCustomObject]@{checked=$base.Count;unchanged=$base.Count-$diff.Count;changed=$diff} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath ($t+'/baseline-verification.json') -Encoding UTF8
$dirs=@('Assets/Deepsea/Scripts/Runtime/SeaLifeNavigationV23','Assets/Deepsea/Scripts/Editor/SeaLifeNavigationV23','Assets/Deepsea/Shaders/SeaLifeNavigationV23','Assets/Deepsea/Resources/SeaLifeNavigationV23','Tools/SeaLifeNavigationV23/ThirdParty')
$files=@()
foreach($d in $dirs){$files+=Get-ChildItem -LiteralPath ($p+'/'+$d) -File -Recurse; if(Test-Path -LiteralPath ($p+'/'+$d+'.meta')){$files+=Get-Item -LiteralPath ($p+'/'+$d+'.meta')}}
foreach($f in @('Assets/Deepsea/Scenes/CoastSeaLifeNavigationV23.unity','Assets/Deepsea/Scenes/CoastSeaLifeNavigationV23.unity.meta','Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeRuntime.cs','Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeRuntime.cs.meta','Tools/SeaLifeNavigationV23/Rebuild-Navigation.ps1','Tools/SeaLifeNavigationV23/README-tools.md','Tools/SeaLifeNavigationV23/reference-analysis.md')){$files+=Get-Item -LiteralPath ($p+'/'+$f)}
$files=$files|Sort-Object FullName -Unique
$manifest=@()
foreach($f in $files){$rel=$f.FullName.Substring($p.Length+1).Replace('\','/');$manifest+=[PSCustomObject]@{path=$rel;sha256=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash};$dest=$t+'/accepted/'+$rel; New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null; Copy-Item -LiteralPath $f.FullName -Destination $dest -Force}
$manifest|ConvertTo-Json -Depth 4|Set-Content -LiteralPath ($t+'/accepted-manifest.json') -Encoding UTF8
& ($t+'/Rebuild-Navigation.ps1')
$summary=@'

## 2026-10-28 v23：GitHub波浪与惯性船控
用户授权查找GitHub水面流动和船控实现并直接应用，不兼容部分自行实现。已在v22基线接入Boat Attack Gerstner公式与Habrador水阻公式，固定提交、原始源码及许可见Tools/SeaLifeNavigationV23/ThirdParty。UPM/URP版本未改；推进、制动、舵向平滑、转向惯性、随波船身与历史尾迹为适配实现。
最新报告C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v23/reference-analysis.md。桌面“深海灯塔-流动水面与船控-v23”和“深海灯塔-v23航行演示”。程序Builds/SeaLifeNavigationV23/Guichao-SeaNavigation.exe；场景CoastSeaLifeNavigationV23.unity；构建Deepsea.Editor.SeaLifeNavigationBuildV23.Build。原v22程序保留，产品名/存档沿用v21。W推进、松键滑行、S制动倒车、A/D平滑转向；F7为v22水材质/原推进算法对照，共享新碰撞/尾迹/视觉层，不是完整旧版二进制还原。
最终Verification/sea-navigation-v23-review03，46项自动检查通过，运行0错误0警告；1080p锁60固定600帧帧率和P95见最终报告。实际引擎144帧/12fps演示，不是实时性能证据。57项旧文件清单中，仅SeaLifeRuntime.cs增加一处FixedUpdate路由，其余56哈希一致；不是全工程所有资产哈希声明。build-04成功，有20项构建警告，不报全工程无警告。工具Rebuild-Navigation.ps1默认只核对冻结输入，不自动覆盖；accepted依赖旧环境，非全工程备份。显式Prepare会从v22重新生成v23场景。
已测人工测试障碍阻挡与倒退脱离，没有全海湾浅水逐段验收；UI、生态群落与钓鱼人物动作未重做，没有完整人工钓鱼/水族馆回归或用户已认可美术的声明。
'@
foreach($ag in @(($p+'/AGENTS.md'),'C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/AGENTS.md')){$content=[IO.File]::ReadAllText($ag);$pattern="(?ms)^## 2026-10-28 v23：GitHub波浪与惯性船控\r?\n.*?(?=^## |\z)";if([regex]::IsMatch($content,$pattern)){$content=[regex]::Replace($content,$pattern,[System.Text.RegularExpressions.MatchEvaluator]{param($m) $summary.TrimStart()+[Environment]::NewLine})}else{$content+=$summary};[IO.File]::WriteAllText($ag,$content,[Text.UTF8Encoding]::new($true))}
$doc='C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v23'
Copy-Item -LiteralPath ($t+'/accepted-manifest.json'),($t+'/baseline-verification.json') -Destination $doc -Force
Add-Type -AssemblyName System.Drawing
$src=[Drawing.Image]::FromFile(($ev+'/04-turning-wake.png'));$thumb=New-Object Drawing.Bitmap($src,1280,720);$thumb.Save(($t+'/preview.jpg'),[Drawing.Imaging.ImageFormat]::Jpeg);$thumb.Dispose();$src.Dispose()
$bundle=$t+'/SourceBundle'
New-Item -ItemType Directory -Path $bundle -Force|Out-Null
foreach($f in $manifest){$dest=$bundle+'/'+$f.path;New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force|Out-Null;Copy-Item -LiteralPath ($p+'/'+$f.path) -Destination $dest -Force}
Copy-Item -LiteralPath ($t+'/preview.jpg'),($t+'/accepted-manifest.json'),($t+'/baseline-verification.json'),($ev+'/result.json') -Destination $bundle -Force
Copy-Item -LiteralPath ($t+'/README-tools.md') -Destination ($bundle+'/README.md') -Force
$zip=$t+'/SeaLifeNavigation-v23-source-and-verification.zip'
Compress-Archive -Path ($bundle+'/*') -DestinationPath $zip -Force
$b64=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip))
$lines=for($i=0;$i -lt $b64.Length;$i+=1000){$b64.Substring($i,[Math]::Min(1000,$b64.Length-$i))}
[IO.File]::WriteAllLines(($t+'/source-bundle.b64'),$lines,[Text.Encoding]::ASCII)
Write-Output ('FROZEN_INPUTS '+$manifest.Count)
Write-Output ('SOURCE_BYTES '+(Get-Item -LiteralPath $zip).Length)
Write-Output ('BASE64_LINES '+$lines.Count)
Write-Output ('VIDEO_BYTES '+(Get-Item -LiteralPath ($ev+'/navigation-motion-v23.mp4')).Length)
Write-Output 'V23_DELIVERY_READY'
