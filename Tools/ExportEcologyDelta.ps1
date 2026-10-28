$ErrorActionPreference='Stop'
$p=Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$check=Get-Content -LiteralPath "$p\Verification\ecology-v1\result.json" -Raw | ConvertFrom-Json
if(!$check.passed){throw 'Ecology standalone check has not passed'}
if((Get-Item -LiteralPath "$p\Verification\ecology-v1\result.json").LastWriteTime -lt (Get-Item -LiteralPath "$p\Builds\EcologyV1\GuichaoBay.exe").LastWriteTime){throw 'Ecology check is older than build'}
Copy-Item -LiteralPath "$p\Verification\unity-expedition-result.json" -Destination "$p\Verification\ecology-v1\terrain-result.json" -Force
$stage=Join-Path $PSScriptRoot 'EcologyDelta-stage'
if(Test-Path -LiteralPath $stage){Remove-Item -LiteralPath $stage -Recurse -Force}
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$files=@('Assets\Deepsea\Resources\Wildlife\Seabird.fbx','Assets\Deepsea\Resources\Wildlife\CoastalBird.asset','ProjectSettings\GraphicsSettings.asset','Verification\ecology-v1\result.json','Verification\ecology-v1\model-import.txt','Verification\ecology-v1\terrain-result.json')
$meta=@('Scripts\Runtime\CoastalEcology.cs','Scripts\Runtime\EcologyPlayerCapture.cs','Scripts\Editor\EcologyBuild.cs','Shaders\Seabird.shader','Shaders\SeabirdShadow.shader','ThirdParty\NVJOB','ThirdParty\NVJOB\NVJOB-LICENSE.txt','ThirdParty\NVJOB\Unity-LICENSE.txt','ThirdParty\NVJOB\SOURCE.md','ThirdParty\NVJOB\Bird-original.shader.txt')
foreach($relative in $meta){$files+='Assets\Deepsea\'+$relative+'.meta'}
foreach($relative in @('Assets\Deepsea\Resources\Wildlife\Seabird.fbx','Assets\Deepsea\Resources\Wildlife\CoastalBird.asset')){$files+=$relative+'.meta'}
foreach($relative in $files){
 $source=Join-Path $p $relative
 if(!(Test-Path -LiteralPath $source)){throw "Missing $source"}
 $dest=Join-Path $stage $relative
 New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
 Copy-Item -LiteralPath $source -Destination $dest -Force
}
$zip=Join-Path $PSScriptRoot 'EcologyDelta.zip'
if(Test-Path -LiteralPath $zip){Remove-Item -LiteralPath $zip}
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$b64=[Convert]::ToBase64String([IO.File]::ReadAllBytes($zip))
$lines=[regex]::Matches($b64,'.{1,160}') | ForEach-Object {$_.Value}
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'ecology-delta-base64.txt'),$lines,[Text.Encoding]::ASCII)
[PSCustomObject]@{bytes=(Get-Item -LiteralPath $zip).Length;sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash;characters=$b64.Length;lines=$lines.Count} | ConvertTo-Json -Compress
