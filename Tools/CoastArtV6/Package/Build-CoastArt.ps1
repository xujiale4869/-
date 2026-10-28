param(
 [string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3',
 [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$p=$Project
$editor=$UnityEditor
& (Join-Path $PSScriptRoot "Download-ArtTextures.ps1") -Project $p
Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class MaterialsAwake { [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags); }'
[MaterialsAwake]::SetThreadExecutionState([uint32]2147483651) | Out-Null
try {
 $log=Join-Path $p 'Verification\coast-art-v6-build.log'
 $u=Start-Process $editor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$p+'"'),'-executeMethod','Deepsea.Editor.CoastMaterialsBuild.Build','-logFile',('"'+$log+'"')) -PassThru
 Write-Output ('BUILD_PID='+$u.Id)
 $u.WaitForExit()
 if($u.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'COAST_MATERIALS_BUILD_OK' -Quiet)){throw "Stage3 material build failed; see $log"}
 Write-Output 'BUILD_OK'
 $exe=Join-Path $p 'Builds\CoastArtV6\GuichaoBay-ArtV6.exe'
 $checklog=Join-Path $p 'Verification\coast-art-v6-check.log'
 $g=Start-Process $exe -ArgumentList @('-coast-materials','-coast-ecology','-whitebox-check','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$checklog+'"')) -WorkingDirectory $p -PassThru
 Write-Output ('CHECK_PID='+$g.Id)
 if(!$g.WaitForExit(180000)){[void]$g.CloseMainWindow();throw 'Ecology check timed out; inspect the player log'}
 $report=Get-Content (Join-Path $p 'Verification\coast-art-v6\result.json') -Raw | ConvertFrom-Json
 $report | ConvertTo-Json -Depth 5
 if($g.ExitCode -ne 0 -or !$report.passed){throw 'Stage3 material acceptance failed'}
 Write-Output 'COAST_MATERIALS_ACCEPTANCE_OK'
} finally {[MaterialsAwake]::SetThreadExecutionState([uint32]2147483648) | Out-Null}
