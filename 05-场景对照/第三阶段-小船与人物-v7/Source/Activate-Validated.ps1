$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$package=$p+'\Tools\CoastArtV7\Package'
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
$evidence=$p+'\Verification\coast-art-v7'
$r=Get-Content ($evidence+'\result.json') -Raw | ConvertFrom-Json
$route=Get-Content ($evidence+'\visual-surface-route.json') -Raw | ConvertFrom-Json
$preserved=Get-Content ($evidence+'\preserved-files.json') -Raw | ConvertFrom-Json
if(!$r.passed -or !$r.heroMaterialsIntact -or !$r.heroEnvelopeSafe -or !$r.captainHiddenWhileWalking -or !$r.heroGreyComparison -or $r.heroAddedColliders -ne 0 -or !$route.passed -or $preserved.unchanged -ne 334 -or $preserved.changed.Count -ne 0){throw 'Validation incomplete'}
$exe=$p+'\Builds\CoastArtV7\GuichaoBay-ArtV7.exe'
if(!(Test-Path $exe)){throw 'Player missing'}
$link=[Environment]::GetFolderPath('Desktop')+'\深海灯塔－材质光照制作版.lnk'
if(Test-Path $link){Copy-Item -LiteralPath $link -Destination ($p+'\Tools\CoastArtV7\Before-20261022-214025\desktop-before-v7.lnk') -Force}
$ws=New-Object -ComObject WScript.Shell
$shortcut=$ws.CreateShortcut($link)
$shortcut.TargetPath=$exe
$shortcut.WorkingDirectory=$p
$shortcut.Arguments='-coast-materials -coast-ecology -screen-fullscreen 0 -screen-width 1920 -screen-height 1080'
$shortcut.Description='深海灯塔 第三阶段 v7：小船与人物外观'
$shortcut.Save()
$existing=Get-Process -Name GuichaoBay-ArtV7 -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe}
if($existing){$g=$existing | Select-Object -First 1}else{$g=Start-Process $exe -ArgumentList @('-coast-materials','-coast-ecology','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$p+'\Verification\coast-art-v7-interactive.log"')) -WorkingDirectory $p -PassThru}
$status=[PSCustomObject]@{version=$r.appearanceVersion;status='implemented-built-and-verified';remoteBackup='Tools/CoastArtV7/Before-20261022-214025';desktopShortcut=$link;executable=$exe;interactivePid=$g.Id;sourceFilesVerified=$manifest.Count;benchmark=@{window=$r.benchmarkWindow;renderResolution=$r.benchmarkRenderResolution;renderScale=$r.benchmarkRenderScale;fps=$r.fps;p95Ms=$r.p95Ms;p99Ms=$r.p99Ms};protectedFilesUnchanged=$preserved.unchanged;heroBoatTriangles=$r.heroBoatTriangles;heroCharacterTriangles=$r.heroCharacterTriangles;heroMaterialsIntact=$r.heroMaterialsIntact;heroEnvelopeSafe=$r.heroEnvelopeSafe;heroAddedColliders=$r.heroAddedColliders;visualRockSurfaces=$route.visualSurfaces;caveVisualBodySamples=$route.caveBodySamples;visualObstructions=$route.visualObstructions;referenceArtMatched=$false}
$status | ConvertTo-Json -Depth 5 | Set-Content ($package+'\status.json') -Encoding UTF8
$archive=[Environment]::GetFolderPath('Desktop')+'\深海灯塔－版本与视频\05-场景对照\第三阶段-小船与人物-v7'
New-Item -ItemType Directory -Path $archive -Force | Out-Null
Copy-Item -LiteralPath $package -Destination ($archive+'\Source') -Recurse -Force
Copy-Item -LiteralPath $evidence -Destination ($archive+'\Evidence') -Recurse -Force
$status | ConvertTo-Json -Depth 5
Write-Output 'V7_ACTIVATED_AND_ARCHIVED'
