$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
Get-Date
Get-ChildItem -LiteralPath $p -Filter AGENTS.md -Recurse -ErrorAction SilentlyContinue|Select-Object -ExpandProperty FullName
$paths=@('Assets/Deepsea/Scripts/Runtime/OceanSurface.cs','Assets/Deepsea/Shaders/Ocean.shader','Assets/Deepsea/Shaders/CoastalGround.shader','Assets/Deepsea/Shaders/ShoreSwash.shader','Assets/Deepsea/Shaders/Includes/SurfWash.hlsl','Assets/Deepsea/Scripts/Runtime/ShoreCycle.cs','Assets/Deepsea/Scripts/Runtime/ShorelineAtmosphere.cs','Assets/Deepsea/Scripts/Runtime/BoatWake.cs','Assets/Deepsea/Shaders/WakeFoam.shader','Assets/Deepsea/Scripts/Runtime/IslandTerrain.cs','Assets/Deepsea/Scripts/Runtime/VisualFactory.cs','Assets/Deepsea/Scripts/Runtime/SeabedRelief.cs','Assets/Deepsea/Scripts/Runtime/GameDirector.cs','Assets/Deepsea/Scripts/Runtime/BoatController.cs')
$files=@(foreach($rel in $paths){
 $file=Join-Path $p $rel
 if(Test-Path -LiteralPath $file){
  [pscustomobject]@{path=$rel;sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant();content=[IO.File]::ReadAllText($file)}
 }
})
$files|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $p 'Verification\coast-detail-baseline.json') -Encoding UTF8
Write-Output ('EXPORTED '+$files.Count)
Get-Process Unity,GuichaoBay -ErrorAction SilentlyContinue|Select-Object Id,ProcessName,Path
