param([switch]$Build)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not (Test-Path -LiteralPath (Join-Path $projectRoot 'Assets/Deepsea'))){$projectRoot='C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'}
$manifest=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') | ConvertFrom-Json
foreach($entry in $manifest){
 $source=Join-Path $PSScriptRoot ('accepted/'+$entry.file)
 $hash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
 if($hash -ne $entry.sha256){throw ('Frozen input hash mismatch: '+$entry.file)}
}
foreach($entry in $manifest){
 $target=Join-Path $projectRoot $entry.destination
 New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('accepted/'+$entry.file)) -Destination $target -Force
 if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Copy mismatch: '+$entry.file)}
}
Write-Output ('Restored and verified '+$manifest.Count+' accepted inputs.')
if($Build){
 $editor='C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe'
 $args=@('-batchmode','-quit','-projectPath',$projectRoot,'-executeMethod','Deepsea.Editor.CoastFineShoreBuild.Build','-logFile',(Join-Path $PSScriptRoot 'build.log'))
 $process=Start-Process -FilePath $editor -ArgumentList ($args | ForEach-Object {'"'+$_+'"'}) -WindowStyle Hidden -PassThru
 $process.WaitForExit()
 if($process.ExitCode -ne 0){throw ('Unity build failed: '+$process.ExitCode)}
}
