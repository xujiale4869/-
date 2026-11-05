param([int]$TargetPid,[string]$StatePath)
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class HarborNativeV27 {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
}
'@
$target=Get-Process -Id $TargetPid
$until=[DateTime]::UtcNow.AddSeconds(65)
function AwaitPhase([string]$phase){
 while([DateTime]::UtcNow -lt $until){
  if(Test-Path -LiteralPath $StatePath){$s=Get-Content -LiteralPath $StatePath -Raw -Encoding UTF8|ConvertFrom-Json;if($s.phase -eq $phase){Write-Output ('NATIVE_PHASE '+$phase);return}}
  if($target.HasExited){throw 'Target exited before native test completed'}
  Start-Sleep -Milliseconds 100
 }
 throw ('Timed out awaiting '+$phase)
}
function F8 {
 $target.Refresh()
 if([HarborNativeV27]::GetForegroundWindow() -ne $target.MainWindowHandle){throw 'Focus changed; stop native input'}
 [HarborNativeV27]::keybd_event(0x77,0,0,[UIntPtr]::Zero)
 Start-Sleep -Milliseconds 65
 [HarborNativeV27]::keybd_event(0x77,0,2,[UIntPtr]::Zero)
}
try {
 AwaitPhase 'await-off'
 $target.Refresh();$activation=New-Object -ComObject WScript.Shell
 if(!$activation.AppActivate($TargetPid)){throw "Could not activate the targeted test window"}
 Start-Sleep -Milliseconds 300
 [HarborNativeV27]::SetForegroundWindow($target.MainWindowHandle)|Out-Null
 Start-Sleep -Milliseconds 200
 F8
 AwaitPhase 'await-on'
 F8
 AwaitPhase 'done'
 Write-Output 'HARBOR_NATIVE_F8_SEQUENCE_OK'
} finally {[HarborNativeV27]::keybd_event(0x77,0,2,[UIntPtr]::Zero)}
