$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$exe=Join-Path $p 'Builds\ReefGardenV1\GuichaoBay.exe'
foreach($mode in @('before','after')){
 $args=@('-reef-garden-check','-logFile',('"'+$p+'\Verification\reef-garden-'+$mode+'.log"'))
 if($mode -eq 'before'){$args+='-reef-garden-baseline'}else{$args+='-reef-garden-demo'}
 $started=Get-Date
 $player=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList $args -Wait -PassThru
 $result=Join-Path $p ('Verification\reef-garden-v1\'+$mode+'\result.json')
 if((Get-Item -LiteralPath $result).LastWriteTime -lt $started){throw 'Stale result'}
 $r=Get-Content -LiteralPath $result -Raw|ConvertFrom-Json
 $r|Select-Object passed,detail,corals,branchCorals,plateCorals,tufts,blades,triangles,renderers,bakeMilliseconds,highestTip,minimumLandingDistance|ConvertTo-Json
 if($player.ExitCode -ne 0 -or !$r.passed){throw "Player check failed: $mode"}
}
Write-Output 'REEF_GARDEN_PAIRED_CHECK_OK'
