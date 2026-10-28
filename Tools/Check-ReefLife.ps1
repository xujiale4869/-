$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3';$exe="$p\Builds\ReefLifeV1\GuichaoBay.exe"
foreach($mode in @('before','after')){
 $args=@('-reef-life-check','-logFile',('"'+$p+'\Verification\reef-life-'+$mode+'.log"'))
 if($mode -eq 'before'){$args+='-reef-life-baseline'}else{$args+='-reef-life-demo'}
 $started=Get-Date;$x=Start-Process -FilePath $exe -WorkingDirectory $p -ArgumentList $args -Wait -PassThru
 $file="$p\Verification\reef-life-v1\$mode\result.json"
 $result=Get-Content -LiteralPath $file -Raw|ConvertFrom-Json;$result|ConvertTo-Json -Depth 4
 if($x.ExitCode -ne 0 -or (Get-Item -LiteralPath $file).LastWriteTime -lt $started -or !$result.passed){throw "Player check failed: $mode"}
}
'REEF_LIFE_PAIRED_OK'
