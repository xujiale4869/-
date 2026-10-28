$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class CoastAwake20261021 { [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags); }'
$end=[DateTime]::UtcNow.AddMinutes(45)
try { while([DateTime]::UtcNow -lt $end -and !(Test-Path -LiteralPath ($p+'\Tools\CoastBlockout\awake-20261021.stop'))) { [CoastAwake20261021]::SetThreadExecutionState([uint32]2147483651) | Out-Null; Start-Sleep -Seconds 15 } } finally { [CoastAwake20261021]::SetThreadExecutionState([uint32]2147483648) | Out-Null }