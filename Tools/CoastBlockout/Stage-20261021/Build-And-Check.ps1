param(
    [string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3',
    [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $UnityEditor)){throw 'Expected Unity Editor was not found'}
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Unity Editor is already running; no changes were installed'}
if(Get-Process GuichaoBay,GuichaoBay-Blockout -ErrorAction SilentlyContinue){throw 'A game is already running; close it before an uncontended acceptance run'}
& (Join-Path $PSScriptRoot 'Install-CoastBlockout.ps1') -Project $Project
if(!('BlockoutAwake' -as [type])){
    Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class BlockoutAwake { [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags); }'
}
[BlockoutAwake]::SetThreadExecutionState([uint32]2147483651) | Out-Null
try {
    $verify=Join-Path $Project 'Verification'
    New-Item -ItemType Directory -Path $verify -Force | Out-Null
    $buildLog=Join-Path $verify 'coast-blockout-build.log'
    $u=Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$Project+'"'),'-executeMethod','Deepsea.Editor.CoastBlockoutBuild.Build','-logFile',('"'+$buildLog+'"')) -PassThru
    Write-Output ('BUILD_PID='+$u.Id)
    $u.WaitForExit()
    if($u.ExitCode -ne 0 -or !(Select-String -LiteralPath $buildLog -Pattern 'COAST_BLOCKOUT_BUILD_OK' -Quiet)){throw ('Unity build failed: '+$buildLog)}
    Write-Output 'COAST_BLOCKOUT_BUILD_VERIFIED'
    $evidence=Join-Path $Project 'Verification\coast-blockout-natural-v3'
    if(Test-Path -LiteralPath (Join-Path $evidence 'result.json')){
        Move-Item -LiteralPath $evidence -Destination ($evidence+'-previous-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
    }
    $exe=Join-Path $Project 'Builds\CoastBlockoutV1\GuichaoBay-Blockout.exe'
    $checkLog=Join-Path $verify 'coast-blockout-check.log'
    $g=Start-Process -FilePath $exe -ArgumentList @('-whitebox-check','-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-logFile',('"'+$checkLog+'"')) -WorkingDirectory $Project -PassThru
    Write-Output ('CHECK_PID='+$g.Id)
    $g.WaitForExit()
    $result=Get-Content -LiteralPath (Join-Path $evidence 'result.json') -Raw | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 5
    if($g.ExitCode -ne 0 -or !$result.passed){throw ('Whitebox acceptance failed: '+$checkLog)}
    Write-Output 'COAST_BLOCKOUT_ACCEPTANCE_VERIFIED'
} finally {
    [BlockoutAwake]::SetThreadExecutionState([uint32]2147483648) | Out-Null
}
