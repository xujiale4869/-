$ErrorActionPreference = 'Stop'
$p = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = Join-Path $PSScriptRoot 'Deepsea-MarineLife-V1.zip'
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
$files = @(
'Assets\Deepsea\Scripts\Runtime\Wildlife.cs','Assets\Deepsea\Scripts\Runtime\SchoolSteering.cs',
'Assets\Deepsea\Scripts\Runtime\MarineWaterEffects.cs','Assets\Deepsea\Scripts\Runtime\MarinePlayerCapture.cs',
'Assets\Deepsea\Scripts\Editor\MarineBuild.cs','Assets\Deepsea\Scripts\Editor\ProjectSetup.cs',
'Assets\Deepsea\Shaders\Ocean.shader','Assets\Deepsea\Shaders\MarineFish.shader','Assets\Deepsea\Shaders\MarineSplash.shader',
'Packages\manifest.json','Packages\packages-lock.json','ProjectSettings\GraphicsSettings.asset',
'Verification\marine-life-v1\result.json','Verification\marine-life-v1\model-import.txt','Tools\ExportMarinePatch.ps1'
)
foreach ($folder in @('Assets\Deepsea\Resources\Wildlife','Assets\Deepsea\ThirdParty\MarineLife')) {
 $files += Get-ChildItem -LiteralPath (Join-Path $p $folder) -File | ForEach-Object { $_.FullName.Substring($p.Length+1) }
 if (Test-Path -LiteralPath (Join-Path $p "$folder.meta")) { $files += "$folder.meta" }
}
$entries = @()
foreach ($rel in $files) {
 $entries += $rel
 if (($rel -notlike '*.meta') -and (Test-Path -LiteralPath (Join-Path $p "$rel.meta"))) { $entries += "$rel.meta" }
}
$zip = [IO.Compression.ZipFile]::Open($archive,[IO.Compression.ZipArchiveMode]::Create)
try {
 foreach ($rel in ($entries | Select-Object -Unique)) {
  [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $p $rel),$rel.Replace('\','/'),[IO.Compression.CompressionLevel]::Optimal) | Out-Null
 }
 [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $p 'Tools\MarineLifeV1-说明.md'),'README.md',[IO.Compression.CompressionLevel]::Optimal) | Out-Null
} finally { $zip.Dispose() }
$base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($archive))
$lines = [regex]::Matches($base64,'.{1,160}') | ForEach-Object { $_.Value }
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'marine-transfer.txt'),$lines,[Text.Encoding]::ASCII)
Get-Item -LiteralPath $archive | Select-Object Name,Length
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | Select-Object Hash
