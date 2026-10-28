param([string]$Project='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3')
$ErrorActionPreference='Stop'
$candidates=Join-Path $Project 'Tools\CoastArtV7\Candidates'
New-Item -ItemType Directory -Path $candidates -Force | Out-Null
$boat=Join-Path $candidates 'rowing-boat.glb'
$hash='DADA73F8190968B45E945588B006CFA41D2EA3D40B7F6A59D6917D45C1192B36'
if(!(Test-Path $boat) -or (Get-FileHash $boat).Hash -ne $hash){Invoke-WebRequest -UseBasicParsing 'https://www.innerscene.com/api/library/rowing-boat-d14b7296/download' -OutFile $boat}
if((Get-FileHash $boat).Hash -ne $hash){throw 'Boat source changed; review before importing'}
$dir=Join-Path $Project 'Assets\Deepsea\ThirdParty\CoastArtV7'
python (Join-Path $PSScriptRoot 'Convert-Boat.py') $boat $dir
if($LASTEXITCODE -ne 0){throw 'GLB source conversion failed'}
$j=Invoke-RestMethod 'https://api.polyhaven.com/files/rough_linen'
$records=@()
foreach($key in @('Diffuse','nor_gl')){
 $e=$j.$key.'1k'.jpg
 if(!$e.url -or !$e.md5){throw 'Cloth texture metadata missing'}
 $name=[IO.Path]::GetFileName(([Uri]$e.url).AbsolutePath);$path=Join-Path $dir $name
 if(!(Test-Path $path) -or (Get-FileHash $path -Algorithm MD5).Hash -ne $e.md5){Invoke-WebRequest -UseBasicParsing $e.url -OutFile $path}
 if((Get-FileHash $path -Algorithm MD5).Hash -ne $e.md5){throw 'Cloth texture hash mismatch'}
 $records+=[PSCustomObject]@{map=$key;file=$name;url=$e.url;md5=$e.md5;license='CC0';source='https://polyhaven.com/a/rough_linen'}
}
$records | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir 'cloth-sources.json') -Encoding UTF8
Add-Content (Join-Path $dir 'SOURCE-LICENSE.txt') "`nRough Linen by Rico Cilliers / colormass, Poly Haven. CC0. https://polyhaven.com/a/rough_linen`n1K diffuse and OpenGL normal maps. Original character mesh authored for this project."
Write-Output 'COAST_HERO_SOURCES_READY verified boat GLB + 1K cloth diffuse/normal'
