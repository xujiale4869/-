param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3',[string]$Destination='')
$ErrorActionPreference='Stop'
if(!$Destination){$Destination=Join-Path $Project 'Assets\Deepsea\ThirdParty\CoastArtV8'}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$records=New-Object 'System.Collections.Generic.List[object]'
function Fetch-HabitatFile($entry,[string]$relative,[string]$id,[string]$kind){
 if(!$entry.url -or !$entry.md5){throw ('Missing official asset '+$id+' '+$kind)}
 $target=Join-Path $Destination $relative
 New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
 if(!(Test-Path $target) -or (Get-FileHash $target -Algorithm MD5).Hash -ne $entry.md5){Invoke-WebRequest -UseBasicParsing -Uri $entry.url -OutFile $target}
 if((Get-FileHash $target -Algorithm MD5).Hash -ne $entry.md5){throw ('Asset checksum failed '+$relative)}
 $records.Add([PSCustomObject]@{asset=$id;map=$kind;file=$relative;bytes=(Get-Item $target).Length;url=$entry.url;md5=$entry.md5;license='CC0';source=('https://polyhaven.com/a/'+$id)})
 Write-Output ('HABITAT_ASSET_OK '+$relative)
}
foreach($id in @('grass_medium_01','shrub_02')){
 $j=Invoke-RestMethod ('https://api.polyhaven.com/files/'+$id)
 Fetch-HabitatFile $j.fbx.'1k'.fbx ($id+'\'+$id+'.fbx') $id 'FBX'
 foreach($key in @('Diffuse','Alpha')){
  $entry=if($key -eq 'Alpha'){$j.$key.'1k'.png}else{$j.$key.'1k'.jpg}
  $name=[IO.Path]::GetFileName(([Uri]$entry.url).AbsolutePath)
  Fetch-HabitatFile $entry ($id+'\'+$name) $id $key
 }
}
$id='grass_path_2';$j=Invoke-RestMethod ('https://api.polyhaven.com/files/'+$id)
foreach($key in @('Diffuse','nor_gl','arm')){
 $entry=$j.$key.'1k'.jpg;$name=[IO.Path]::GetFileName(([Uri]$entry.url).AbsolutePath)
 Fetch-HabitatFile $entry ($id+'\'+$name) $id $key
}
$records | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $Destination 'habitat-sources.json') -Encoding UTF8
@'
Coastal Habitat v8 - CC0 assets from Poly Haven.
Grass Medium 01: https://polyhaven.com/a/grass_medium_01
Shrub 02: https://polyhaven.com/a/shrub_02
Grass Path 2: https://polyhaven.com/a/grass_path_2
License: https://polyhaven.com/license (CC0)
1K FBX, diffuse/alpha for plant meshes; 1K diffuse, OpenGL normal and ARM for ground.
Source URLs, original sizes and verified MD5 hashes are recorded in habitat-sources.json.
Meshes are reduced while retaining their original texture coordinates.
'@ | Set-Content (Join-Path $Destination 'SOURCE-LICENSE.txt') -Encoding UTF8
Write-Output 'HABITAT_SOURCES_VERIFIED'
