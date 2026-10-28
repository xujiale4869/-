$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$editor='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
$report=Get-Content ($p+'\Verification\coast-art-v9\result.json') -Raw | ConvertFrom-Json
if(!$report.passed -or $report.appearanceVersion -ne 'CoastMaterials-Stage3-v9-ShoreAndMarine'){throw 'Current player validation not passed'}
$log=$p+'\Verification\coast-art-v9-surfaces.log'
$u=Start-Process $editor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$p+'"'),'-executeMethod','Deepsea.Editor.CoastSurfaceValidation.Verify','-logFile',('"'+$log+'"')) -PassThru
Write-Output ('SURFACE_CHECK_PID='+$u.Id)
$u.WaitForExit()
if($u.ExitCode -ne 0){throw ('Surface validation failed: '+$log)}
$route=Get-Content ($p+'\Verification\coast-art-v9\visual-surface-route.json') -Raw | ConvertFrom-Json
if(!$route.passed){throw 'Visual rock route validation failed'}
$route | ConvertTo-Json
& ($p+'\Tools\CoastArtV9\Package\Prepare-Evidence.ps1')
Write-Output 'V9_VISUAL_ROUTE_AND_EVIDENCE_OK'
