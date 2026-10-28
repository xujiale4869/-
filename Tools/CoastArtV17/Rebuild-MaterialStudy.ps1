param([switch]$Build,[switch]$GenerateDraft)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$projectPath=[IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
if(-not(Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Deepsea'))){throw 'Run the copy in Tools/CoastArtV17.'}
if($GenerateDraft){
 $oldProjectRoot=$env:COAST_PROJECT_ROOT;$oldWorkspaceRoot=$env:COAST_WORKSPACE_ROOT
 try{
  $env:COAST_PROJECT_ROOT=$projectPath
  if(-not $env:COAST_WORKSPACE_ROOT){$env:COAST_WORKSPACE_ROOT='C:\Users\许嘉乐\Documents\ChatGPT\深海灯塔'}
  $blenderPath='C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe'
  $pythonPath='C:\Users\许嘉乐\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
  foreach($recipe in @('01_shore_patch.py','02_unity_materials.py','03_comparison.py','04_lighthouse_source.py')){
   $recipePath=Join-Path $toolRoot ('build\'+$recipe)
   if($recipe -in @('02_unity_materials.py','03_comparison.py')){& $pythonPath $recipePath;if($LASTEXITCODE -ne 0){throw ('Recipe failed: '+$recipe)}}else{
    $log=Join-Path $toolRoot ($recipe+'.log');$taskProcess=Start-Process -FilePath $blenderPath -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',('"'+$recipePath+'"')) -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $log -RedirectStandardError ($log+'.errors');if($taskProcess.ExitCode -ne 0){throw ('Recipe failed: '+$log)}
   }
  }
 }finally{$env:COAST_PROJECT_ROOT=$oldProjectRoot;$env:COAST_WORKSPACE_ROOT=$oldWorkspaceRoot}
 Write-Output 'New v17 draft generated. Source v14/v16 Blender masters are required in the workspace; fresh generation requires renewed visual/mesh/standalone checks. Frozen inputs were not replaced.'
}else{
 $acceptedRoot=Join-Path $toolRoot 'accepted';$manifest=Get-Content -LiteralPath (Join-Path $toolRoot 'accepted-manifest.json') -Raw | ConvertFrom-Json
 foreach($entry in $manifest.dependencies){$dependency=Join-Path $projectPath $entry.path;if(-not(Test-Path -LiteralPath $dependency) -or (Get-FileHash -LiteralPath $dependency -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Baseline dependency changed: '+$entry.path)}}
 foreach($entry in $manifest.files){
  $source=[IO.Path]::GetFullPath((Join-Path $acceptedRoot $entry.frozenPath));$target=[IO.Path]::GetFullPath((Join-Path $projectPath $entry.path))
  if(-not $source.StartsWith($acceptedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or -not $target.StartsWith($projectPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Path escaped its intended root.'}
  if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $entry.sourceSha256){throw ('Frozen input changed: '+$entry.path)}
  New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
  if($entry.encoding -eq 'gzip'){
   $inputStream=[IO.File]::OpenRead($source);$outputStream=$null;$gzipStream=$null
   try{$gzipStream=[IO.Compression.GZipStream]::new($inputStream,[IO.Compression.CompressionMode]::Decompress);$outputStream=[IO.File]::Create($target);$gzipStream.CopyTo($outputStream)}finally{if($outputStream){$outputStream.Dispose()};if($gzipStream){$gzipStream.Dispose()};$inputStream.Dispose()}
  }else{Copy-Item -LiteralPath $source -Destination $target -Force}
  if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Restored hash mismatch: '+$entry.path)}
 }
 Write-Output ('v17 frozen inputs restored: '+$manifest.files.Count+'. v16 baseline dependencies verified.')
}
if($Build){
 $unityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe';$logPath=Join-Path $toolRoot 'rebuild.log'
 $taskBuild=Start-Process -FilePath $unityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','Deepsea.Editor.CoastRefinementBuild.Build','-logFile',('"'+$logPath+'"')) -WindowStyle Hidden -Wait -PassThru
 if($taskBuild.ExitCode -ne 0 -or -not(Select-String -LiteralPath $logPath -SimpleMatch 'COAST_SHOWCASE_BUILD_OK' -Quiet)){throw ('Build failed: '+$logPath)}
 Write-Output 'v17 sample built. New builds still require visible standalone inspection.'
}
