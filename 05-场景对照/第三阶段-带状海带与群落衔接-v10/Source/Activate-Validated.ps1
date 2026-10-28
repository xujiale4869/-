$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$package=$p+'\Tools\CoastArtV10\Package'
function TextHash([string]$Path){
 $s=[IO.File]::ReadAllText($Path).Replace("`r`n","`n").TrimStart([char]0xFEFF)
 $sha=[Security.Cryptography.SHA256]::Create()
 try{return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($s)))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
}
$manifest=Get-Content ($package+'\source-manifest.json') -Raw | ConvertFrom-Json
foreach($entry in $manifest){
 if((TextHash (Join-Path $p $entry.target)) -ne $entry.sha256){throw ('Source mismatch: '+$entry.target)}
 if((TextHash (Join-Path ($package+'\Files') $entry.source)) -ne $entry.sha256){throw ('Package mismatch: '+$entry.source)}
}
$assets=Get-Content ($package+'\asset-manifest.json') -Raw | ConvertFrom-Json
foreach($entry in $assets){if((Get-FileHash -LiteralPath (Join-Path $p $entry.target) -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Asset mismatch: '+$entry.target)}}
$evidence=$p+'\Verification\coast-art-v10'
$r=Get-Content ($evidence+'\result.json') -Raw | ConvertFrom-Json
$route=Get-Content ($evidence+'\visual-surface-route.json') -Raw | ConvertFrom-Json
$preserved=Get-Content ($evidence+'\preserved-files.json') -Raw | ConvertFrom-Json
if($r.kelpModelTriangles -le 0 -or $r.kelpModelTriangles -gt 1000 -or $r.marineRootsChecked -lt 15 -or $r.kelpRootGap -ge .12 -or $r.kelpRootSink -ge .20 -or $r.highestMarineTip -ge -.40 -or !$r.marineMaterialsSupported -or $r.marineUvVertices -lt 1000 -or $r.shoreFinishRocks -lt 12 -or $r.shoreFinishClearance -lt 2.6 -or $r.fps -lt 60 -or $r.habitatGrass -lt 120 -or $r.habitatShrubs -lt 12 -or $r.habitatPathClearance -le 2.05 -or $r.fernTrianglesPerPlant -gt 1500 -or !$r.passed -or !$r.heroMaterialsIntact -or !$r.heroEnvelopeSafe -or !$r.captainHiddenWhileWalking -or !$r.heroGreyComparison -or $r.heroAddedColliders -ne 0 -or !$route.passed -or $preserved.unchanged -ne 334 -or $preserved.changed.Count -ne 0){throw 'Validation incomplete'}
$exe=$p+'\Builds\CoastArtV10\GuichaoBay-ArtV10.exe'
if(!(Test-Path $exe)){throw 'Player missing'}
$link=[Environment]::GetFolderPath('Desktop')+'\深海灯塔－材质光照制作版.lnk'
if(Test-Path $link){Copy-Item -LiteralPath $link -Destination ($p+'\Tools\CoastArtV10\Before-20261023-130023\desktop-before-v10.lnk') -Force}
$ws=New-Object -ComObject WScript.Shell
$shortcut=$ws.CreateShortcut($link)
$shortcut.TargetPath=$exe
$shortcut.WorkingDirectory=$p
$shortcut.Arguments='-coast-materials -coast-ecology -screen-fullscreen 0 -screen-width 1920 -screen-height 1080'
$shortcut.Description='深海灯塔 第三阶段 v10：带状海带与群落衔接'
$shortcut.Save()
$existing=Get-Process -Name GuichaoBay-ArtV10 -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe}
if($existing){$g=$existing | Select-Object -First 1}else{$g=Start-Process $exe -ArgumentList @('-coast-materials','-coast-ecology','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$p+'\Verification\coast-art-v10-interactive.log"')) -WorkingDirectory $p -PassThru}
$status=[PSCustomObject]@{version=$r.appearanceVersion;status='implemented-built-and-verified';remoteBackup='Tools/CoastArtV10/Before-20261023-130023';desktopShortcut=$link;executable=$exe;interactivePid=$g.Id;sourceFilesVerified=$manifest.Count;benchmark=@{window=$r.benchmarkWindow;renderResolution=$r.benchmarkRenderResolution;renderScale=$r.benchmarkRenderScale;fps=$r.fps;p95Ms=$r.p95Ms;p99Ms=$r.p99Ms};protectedFilesUnchanged=$preserved.unchanged;heroBoatTriangles=$r.heroBoatTriangles;heroCharacterTriangles=$r.heroCharacterTriangles;heroMaterialsIntact=$r.heroMaterialsIntact;heroEnvelopeSafe=$r.heroEnvelopeSafe;heroAddedColliders=$r.heroAddedColliders;visualRockSurfaces=$route.visualSurfaces;caveVisualBodySamples=$route.caveBodySamples;visualObstructions=$route.visualObstructions;habitatGrass=$r.habitatGrass;habitatShrubs=$r.habitatShrubs;habitatTriangles=$r.habitatTriangles;fernTrianglesPerPlant=$r.fernTrianglesPerPlant;habitatPathClearance=$r.habitatPathClearance;habitatPerformance=$r.habitatPerformance;shoreFinishRocks=$r.shoreFinishRocks;shoreFinishTriangles=$r.shoreFinishTriangles;shoreFinishClearance=$r.shoreFinishClearance;marineTriangles=$r.marineTriangles;marineUvVertices=$r.marineUvVertices;marineMaterialsSupported=$r.marineMaterialsSupported;kelpModelTriangles=$r.kelpModelTriangles;marineRootsChecked=$r.marineRootsChecked;kelpRootGap=$r.kelpRootGap;kelpRootSink=$r.kelpRootSink;highestMarineTip=$r.highestMarineTip;kelpAnchorsRejected=$r.kelpAnchorsRejected;referenceArtMatched=$false}
$status | ConvertTo-Json -Depth 5 | Set-Content ($package+'\status.json') -Encoding UTF8
$archive=[Environment]::GetFolderPath('Desktop')+'\深海灯塔－版本与视频\05-场景对照\第三阶段-带状海带与群落衔接-v10'
New-Item -ItemType Directory -Path $archive -Force | Out-Null
Copy-Item -LiteralPath $package -Destination ($archive+'\Source') -Recurse -Force
Copy-Item -LiteralPath $evidence -Destination ($archive+'\Evidence') -Recurse -Force
$status | ConvertTo-Json -Depth 5
Write-Output 'V10_ACTIVATED_AND_ARCHIVED'
