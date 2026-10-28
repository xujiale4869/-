$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$editor='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class NaturalAwake { [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags); }'
[NaturalAwake]::SetThreadExecutionState([uint32]2147483651) | Out-Null
try {
 $log=Join-Path $p 'Verification\coast-blockout-natural-build.log'
 $u=Start-Process $editor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$p+'"'),'-executeMethod','Deepsea.Editor.CoastBlockoutBuild.Build','-logFile',('"'+$log+'"')) -PassThru
 Write-Output ('BUILD_PID='+$u.Id)
 $u.WaitForExit()
 if($u.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'COAST_BLOCKOUT_BUILD_OK' -Quiet)){throw "Natural v3 build failed; see $log"}
 Write-Output 'BUILD_OK'
 $exe=Join-Path $p 'Builds\CoastBlockoutV1\GuichaoBay-Blockout.exe'
 $checklog=Join-Path $p 'Verification\coast-blockout-natural-final-visual.log'
 $g=Start-Process $exe -ArgumentList @('-whitebox-visual','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$checklog+'"')) -WorkingDirectory $p -PassThru
 Write-Output ('CHECK_PID='+$g.Id)
 $g.WaitForExit()
 $report=Get-Content (Join-Path $p 'Verification\coast-blockout-natural-visual\visual-result.json') -Raw | ConvertFrom-Json
 $report | ConvertTo-Json -Depth 5
 if($g.ExitCode -ne 0 -or !$report.passed){throw 'Natural v3 acceptance failed'}
 Write-Output 'NATURAL_V3_FINAL_VISUAL_OK'
} finally {[NaturalAwake]::SetThreadExecutionState([uint32]2147483648) | Out-Null}
