$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$exe=Join-Path $p 'Builds\WaterFinishV1\GuichaoBay.exe'
foreach($mode in @('before','after')){
 $args=@('-water-finish-check','-logFile',('"'+$p+'\Verification\water-finish-'+$mode+'.log"'))
 if($mode -eq 'before'){$args+='-water-finish-baseline'}else{$args+='-water-finish-demo'}
 $started=Get-Date
 $player=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList $args -Wait -PassThru
 if($player.ExitCode -ne 0){throw "Player check failed: $mode"}
 $r=Join-Path $p ('Verification\water-finish-v1\'+$mode+'\result.json')
 if((Get-Item -LiteralPath $r).LastWriteTime -lt $started -or !(Get-Content -LiteralPath $r -Raw|ConvertFrom-Json).passed){throw "Fresh check failed: $mode"}
 Write-Output "WATER_FINISH_QA_OK $mode"
}
$started=Get-Date
$eco=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList @('-ecology-check','-logFile',('"'+$p+'\Verification\water-finish-ecology.log"')) -Wait -PassThru
$ecoPath=Join-Path $p 'Verification\ecology-v1\result.json'
if($eco.ExitCode -ne 0 -or (Get-Item -LiteralPath $ecoPath).LastWriteTime -lt $started -or !(Get-Content -LiteralPath $ecoPath -Raw|ConvertFrom-Json).passed){throw 'Ecology regression failed'}
Copy-Item -LiteralPath $ecoPath -Destination (Join-Path $p 'Verification\water-finish-v1\ecology-result.json')
Write-Output 'WATER_FINISH_ALL_QA_OK'
