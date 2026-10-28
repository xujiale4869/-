param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3')
$ErrorActionPreference='Stop'
$dir=Join-Path $Project 'Assets\Deepsea\ThirdParty\CoastArtV4'
New-Item -ItemType Directory -Path $dir -Force | Out-Null
$records=@()
foreach($id in @('seaside_rock','worn_plaster_wall')){
 $j=Invoke-RestMethod -Uri ('https://api.polyhaven.com/files/'+$id)
 foreach($key in @('Diffuse','nor_gl','arm')){
  $entry=$j.$key.'2k'.jpg
  if(!$entry -or !$entry.url -or !$entry.md5){throw ('Missing official 2K texture: '+$id+'/'+$key)}
  $name=[IO.Path]::GetFileName(([Uri]$entry.url).AbsolutePath);$target=Join-Path $dir $name
  if(!(Test-Path $target) -or (Get-FileHash $target -Algorithm MD5).Hash -ne $entry.md5){Invoke-WebRequest -UseBasicParsing -Uri $entry.url -OutFile $target}
  if((Get-FileHash $target -Algorithm MD5).Hash -ne $entry.md5){throw ('Texture hash mismatch: '+$name)}
  $records+=[PSCustomObject]@{asset=$id;map=$key;file=$name;bytes=(Get-Item $target).Length;url=$entry.url;md5=$entry.md5;license='CC0';source=('https://polyhaven.com/a/'+$id)}
  Write-Output ('TEXTURE_OK '+$name)
 }
}
$records | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir 'texture-sources.json') -Encoding UTF8
@'
Coast Art V4 - texture sources
Seaside Rock: https://polyhaven.com/a/seaside_rock
Worn Plaster Wall: https://polyhaven.com/a/worn_plaster_wall
Author: Dimitrios Savva (as listed on the asset pages).
License: CC0. https://polyhaven.com/license
2K diffuse, OpenGL normal and AO/roughness/metal maps. Official download checksums recorded in texture-sources.json.
'@ | Set-Content (Join-Path $dir 'SOURCE-LICENSE.txt') -Encoding UTF8
