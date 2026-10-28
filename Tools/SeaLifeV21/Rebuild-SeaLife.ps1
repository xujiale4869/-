param([switch]$Build,[switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not(Test-Path -LiteralPath (Join-Path $projectRoot 'Assets/Deepsea'))){throw 'Place this script in the project Tools/SeaLifeV21 folder.'}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
foreach($entry in $manifest.files){
 if($entry.path -notmatch '^Assets/Deepsea/(Scripts/(Runtime|Editor)/SeaLifeV21(/|\.meta$)|Resources/SeaLifeV21(/|\.meta$)|Scenes/CoastSeaLife\.unity(\.meta)?$)'){throw "Unexpected manifest target: $($entry.path)"}
 $frozen=Join-Path (Join-Path $PSScriptRoot 'accepted') $entry.path
 $target=Join-Path $projectRoot $entry.path
 if((Get-FileHash -LiteralPath $frozen -Algorithm SHA256).Hash -ne $entry.sha256){throw "Frozen input changed: $($entry.path)"}
 if(-not $VerifyOnly){New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null;Copy-Item -LiteralPath $frozen -Destination $target -Force}
 if(-not(Test-Path -LiteralPath $target)){throw "Missing input: $($entry.path)"}
 if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.sha256){throw "Project input changed: $($entry.path)"}
}
Write-Output "SeaLifeV21 inputs verified: $($manifest.files.Count)"
if($Build){
 $editorPath='C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe'
 $buildLog=Join-Path $PSScriptRoot 'rebuild.log'
 $arguments=@('-batchmode','-quit','-projectPath',('"'+$projectRoot+'"'),'-executeMethod','Deepsea.Editor.SeaLifeBuild.Build','-logFile',('"'+$buildLog+'"'))
 $editorProcess=Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
 if($editorProcess.ExitCode -ne 0){throw "Build failed. Read $buildLog"}
 Write-Output 'Build complete: Builds/SeaLifeV21/Guichao-SeaLife.exe'
}
