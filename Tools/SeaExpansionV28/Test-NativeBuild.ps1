param([int]$TargetPid,[string]$Evidence)
$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class BayNativeV28 {
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref Point p);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
 [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a,uint b,bool attach);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
 [DllImport("user32.dll")] public static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
}
'@
$target=Get-Process -Id $TargetPid
$until=[DateTime]::UtcNow.AddSeconds(150)
function AwaitPhase([string]$phase){
 while([DateTime]::UtcNow -lt $until){
  $state=Join-Path $Evidence 'native-state.json'
  if(Test-Path -LiteralPath $state){try{$s=Get-Content -LiteralPath $state -Raw -Encoding UTF8|ConvertFrom-Json;if($s.phase -eq $phase){Write-Output ('NATIVE_PHASE '+$phase);return}}catch{}}
  $target.Refresh();if($target.HasExited){throw 'Target exited before native sequence completed'}
  Start-Sleep -Milliseconds 100
 }
 throw ('Timed out awaiting '+$phase)
}
function GuardFocus {$target.Refresh();[uint32]$foregroundPid=0;[BayNativeV28]::GetWindowThreadProcessId([BayNativeV28]::GetForegroundWindow(),[ref]$foregroundPid)|Out-Null;if($foregroundPid -ne $TargetPid){throw ('Focus changed; stop native input. Expected PID='+$TargetPid+' actual='+$foregroundPid)}}
function Key([byte]$vk,[int]$hold=90){GuardFocus;[BayNativeV28]::keybd_event($vk,0,0,[UIntPtr]::Zero);try{Start-Sleep -Milliseconds $hold}finally{[BayNativeV28]::keybd_event($vk,0,2,[UIntPtr]::Zero)}}
function Click([bool]$right){GuardFocus;$btn=if($right){8}else{2};[BayNativeV28]::mouse_event($btn,0,0,0,[UIntPtr]::Zero);try{Start-Sleep -Milliseconds 80}finally{[BayNativeV28]::mouse_event($btn*2,0,0,0,[UIntPtr]::Zero)}}
try{
 AwaitPhase 'await-enter'
 $activation=New-Object -ComObject WScript.Shell;if(!$activation.AppActivate($TargetPid)){throw 'Could not activate the targeted game window'}
 Start-Sleep -Milliseconds 250;$target.Refresh();$handle=$target.MainWindowHandle
 [uint32]$foregroundPid=0;$foregroundThread=[BayNativeV28]::GetWindowThreadProcessId([BayNativeV28]::GetForegroundWindow(),[ref]$foregroundPid);$ownThread=[BayNativeV28]::GetCurrentThreadId()
 [BayNativeV28]::AttachThreadInput($ownThread,$foregroundThread,$true)|Out-Null
 try{[BayNativeV28]::ShowWindow($handle,9)|Out-Null;[BayNativeV28]::BringWindowToTop($handle)|Out-Null;[BayNativeV28]::SetForegroundWindow($handle)|Out-Null}finally{[BayNativeV28]::AttachThreadInput($ownThread,$foregroundThread,$false)|Out-Null}
 Start-Sleep -Milliseconds 300
 Key 0x47
 AwaitPhase 'await-pan';Key 0x57 600
 AwaitPhase 'await-select';Key 0x33
 AwaitPhase 'await-rotate';Key 0x52
 AwaitPhase 'await-place'
 $m=Get-Content (Join-Path $Evidence 'mouse-target.json') -Raw -Encoding UTF8|ConvertFrom-Json
 [BayNativeV28]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))|Out-Null
 $rect=New-Object BayNativeV28+Rect;[BayNativeV28]::GetClientRect($target.MainWindowHandle,[ref]$rect)|Out-Null
 $point=New-Object BayNativeV28+Point;$point.X=[int]($m.x*$rect.Right/1920);$point.Y=$rect.Bottom-[int]($m.y*$rect.Bottom/1080)
 [BayNativeV28]::ClientToScreen($target.MainWindowHandle,[ref]$point)|Out-Null;GuardFocus;[BayNativeV28]::SetCursorPos($point.X,$point.Y)|Out-Null;Start-Sleep -Milliseconds 300;Click $false
 AwaitPhase 'await-remove';Click $true
 AwaitPhase 'await-exit';Key 0x47
 AwaitPhase 'done';Write-Output 'BAY_NATIVE_SEQUENCE_OK'
}finally{foreach($key in @(0x47,0x57,0x33,0x52)){[BayNativeV28]::keybd_event($key,0,2,[UIntPtr]::Zero)}}
