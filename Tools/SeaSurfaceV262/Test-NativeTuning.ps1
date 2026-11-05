param([int]$TargetPid,[string]$StatePath)
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class V262Native {
 [StructLayout(LayoutKind.Sequential)] public struct Point {public int X,Y;}
 [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref Point p);
 [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
}
'@
[V262Native]::SetProcessDPIAware()|Out-Null
$target=Get-Process -Id $TargetPid
$until=[DateTime]::UtcNow.AddSeconds(55)
function AwaitPhase([string]$phase){
 while([DateTime]::UtcNow -lt $until){
  if(Test-Path -LiteralPath $StatePath){
   $s=Get-Content -LiteralPath $StatePath -Raw -Encoding UTF8|ConvertFrom-Json
   if($s.phase -eq $phase){Write-Output ('NATIVE_PHASE '+$phase);return}
   if($s.phase -eq 'done'){throw ('Unexpected completed state while awaiting '+$phase)}
  }
  if($target.HasExited){throw 'Target game exited before native test completed'}
  Start-Sleep -Milliseconds 100
 }
 throw ('Timed out awaiting '+$phase)
}
function GuardFocus {if([V262Native]::GetForegroundWindow() -ne $target.MainWindowHandle){throw 'Focus changed; stop native input'}}
function F7 {GuardFocus;[V262Native]::keybd_event(0x76,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 65;[V262Native]::keybd_event(0x76,0,2,[UIntPtr]::Zero)}
function Click([double]$x,[double]$y){
 GuardFocus
 $r=New-Object V262Native+Rect;$origin=New-Object V262Native+Point
 [V262Native]::GetClientRect($target.MainWindowHandle,[ref]$r)|Out-Null
 [V262Native]::ClientToScreen($target.MainWindowHandle,[ref]$origin)|Out-Null
 $sx=$origin.X+[int]($x*($r.Right-$r.Left)/1920);$sy=$origin.Y+[int]($y*($r.Bottom-$r.Top)/1080)
 [V262Native]::SetCursorPos($sx,$sy)|Out-Null
 Start-Sleep -Milliseconds 80
 [V262Native]::mouse_event(2,0,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 80;[V262Native]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
}
try{
 AwaitPhase 'await-open'
 $target.Refresh();[V262Native]::SetForegroundWindow($target.MainWindowHandle)|Out-Null
 Start-Sleep -Milliseconds 200
 F7
 AwaitPhase 'await-slider'
 Click 320 244
 AwaitPhase 'await-preset'
 Click 205 191
 AwaitPhase 'await-close'
 F7
 AwaitPhase 'done'
 Write-Output 'NATIVE_INPUT_SEQUENCE_OK'
}finally{
 [V262Native]::keybd_event(0x76,0,2,[UIntPtr]::Zero)
 [V262Native]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
}
