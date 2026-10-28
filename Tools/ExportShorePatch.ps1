$ErrorActionPreference = 'Stop'
$p = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$resultPath = Join-Path $p 'Verification\shore-v1\result.json'
$check = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
if (!$check.passed) { throw 'Shore visual check has not passed' }
if ((Get-Item -LiteralPath $resultPath).LastWriteTime -lt (Get-Item -LiteralPath (Join-Path $p 'Builds\ShoreV1\GuichaoBay.exe')).LastWriteTime) { throw 'Visual result is older than the build' }
Copy-Item -LiteralPath (Join-Path $p 'Verification\unity-expedition-result.json') -Destination (Join-Path $p 'Verification\shore-v1\terrain-result.json') -Force
$stage = Join-Path $PSScriptRoot 'ShorePatch-stage'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
[IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $PSScriptRoot 'Deepsea-MarineLife-V1.zip'),$stage)
$files = @(
'Assets\Deepsea\Scripts\Runtime\OceanSurface.cs','Assets\Deepsea\Scripts\Runtime\GameDirector.cs',
'Assets\Deepsea\Scripts\Runtime\ShoreHabitat.cs','Assets\Deepsea\Scripts\Runtime\ShorePlayerCapture.cs',
'Assets\Deepsea\Scripts\Editor\ShoreBuild.cs','Assets\Deepsea\Scripts\Editor\ProjectSetup.cs',
'Assets\Deepsea\Shaders\Ocean.shader','Assets\Deepsea\Shaders\CoastalGround.shader','Assets\Deepsea\Shaders\Seagrass.shader',
'Assets\Deepsea\Resources\Water\WaterCaustics.tif',
'Assets\Deepsea\ThirdParty\BoatAttack\BayCaustics.hlsl','Assets\Deepsea\ThirdParty\BoatAttack\Caustics.shader.txt',
'Assets\Deepsea\ThirdParty\BoatAttack\LICENSE.md','Assets\Deepsea\ThirdParty\BoatAttack\SHORE_SOURCE.md',
'ProjectSettings\GraphicsSettings.asset','Verification\shore-v1\result.json','Verification\shore-v1\terrain-result.json',
'Tools\ExportShorePatch.ps1'
)
$entries = @()
foreach ($rel in $files) {
 $entries += $rel
 if (($rel -notlike '*.meta') -and (Test-Path -LiteralPath (Join-Path $p "$rel.meta"))) { $entries += "$rel.meta" }
}
foreach ($rel in ($entries | Select-Object -Unique)) {
 $dest = Join-Path $stage $rel
 New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
 Copy-Item -LiteralPath (Join-Path $p $rel) -Destination $dest -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ShoreV1-说明.md') -Destination (Join-Path $stage 'README.md') -Force
$archive = Join-Path $PSScriptRoot 'Deepsea-Shore-V1.zip'
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$archive,[IO.Compression.CompressionLevel]::Optimal,$false)
$base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($archive))
$lines = [regex]::Matches($base64,'.{1,160}') | ForEach-Object { $_.Value }
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'shore-transfer.txt'),$lines,[Text.Encoding]::ASCII)
[PSCustomObject]@{bytes=(Get-Item -LiteralPath $archive).Length;lines=$lines.Count;sha256=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash} | ConvertTo-Json -Compress
