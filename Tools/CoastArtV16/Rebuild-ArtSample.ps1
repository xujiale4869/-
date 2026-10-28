param([switch]$Build,[switch]$GenerateGeometry)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$projectPath=[IO.Path]::GetFullPath((Join-Path $toolRoot '..\..'))
if(-not(Test-Path -LiteralPath (Join-Path $projectPath 'Assets\Deepsea'))){throw 'Run the copy in the Unity project Tools/CoastArtV16 folder.'}
if($GenerateGeometry){
 $previousProjectRoot=$env:COAST_PROJECT_ROOT
 try{
  $env:COAST_PROJECT_ROOT=$projectPath
  $blenderPath='C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe'
  foreach($recipe in @('01_scene.py','04_prepare_delivery.py')){
   $recipePath=Join-Path $toolRoot ('build\'+$recipe)
   $recipeLog=Join-Path $toolRoot ($recipe+'.log')
   $taskProcess=Start-Process -FilePath $blenderPath -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',('"'+$recipePath+'"')) -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $recipeLog -RedirectStandardError ($recipeLog+'.errors')
   if($taskProcess.ExitCode -ne 0){throw ('Generation failed: '+$recipeLog)}
  }
 }finally{$env:COAST_PROJECT_ROOT=$previousProjectRoot}
 Write-Output 'New geometry generated. This is a fresh draft and requires new visual, mesh, route and standalone validation. Frozen inputs were not replaced.'
}else{
 $acceptedRoot=Join-Path $toolRoot 'accepted'
 $manifest=Get-Content -LiteralPath (Join-Path $toolRoot 'accepted-manifest.json') -Raw | ConvertFrom-Json
 foreach($entry in $manifest.files){
  $source=[IO.Path]::GetFullPath((Join-Path $acceptedRoot $entry.frozenPath))
  $target=[IO.Path]::GetFullPath((Join-Path $projectPath $entry.path))
  if(-not $source.StartsWith($acceptedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Frozen path escaped its folder.'}
  if(-not $target.StartsWith($projectPath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Output escaped project.'}
  if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $entry.sourceSha256){throw ('Frozen source changed: '+$entry.path)}
  New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
  if($entry.encoding -eq 'gzip'){
   $inputStream=[IO.File]::OpenRead($source);$outputStream=$null;$gzipStream=$null
   try{$gzipStream=[IO.Compression.GZipStream]::new($inputStream,[IO.Compression.CompressionMode]::Decompress);$outputStream=[IO.File]::Create($target);$gzipStream.CopyTo($outputStream)}finally{if($outputStream){$outputStream.Dispose()};if($gzipStream){$gzipStream.Dispose()};$inputStream.Dispose()}
  }else{Copy-Item -LiteralPath $source -Destination $target -Force}
  if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.sha256){throw ('Output hash mismatch: '+$entry.path)}
 }
 Write-Output ('Frozen independent v16 sample synced: '+$manifest.files.Count+' inputs. Original gameplay is not owned by this tool.')
}
if($Build){
 $unityPath='C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
 $logPath=Join-Path $toolRoot 'rebuild.log'
 $taskBuild=Start-Process -FilePath $unityPath -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$projectPath+'"'),'-executeMethod','Deepsea.Editor.CoastShowcaseBuild.Build','-logFile',('"'+$logPath+'"')) -WindowStyle Hidden -Wait -PassThru
 if($taskBuild.ExitCode -ne 0 -or -not(Select-String -LiteralPath $logPath -SimpleMatch 'COAST_SHOWCASE_BUILD_OK' -Quiet)){throw ('Build failed: '+$logPath)}
 Write-Output 'Independent v16 sample built. Launch visibly for inspection; a new build is not automatically a verified or visually approved build.'
}
