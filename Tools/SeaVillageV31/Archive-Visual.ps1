$ErrorActionPreference='Stop'
Import-Module 'C:/Windows/System32/WindowsPowerShell/v1.0/Modules/Microsoft.PowerShell.Utility/Microsoft.PowerShell.Utility.psd1'
$r=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$root=$r+'/Docs/SeaVillageV31/VisualReview'
$mapping=@{B01='11-village-panorama';B02='12-south-boatwright';B03='12b-waterfront-slipway';B04='13-tavern-and-ecotone';B05='02-village-and-flora';B06='12c-hull-rib-detail';B07='15-workshop-terrace-side';B08='15b-tavern-terrace-side';B09='15c-observatory-workdeck';B10='15d-tavern-fish-rack';F01='14-flora-0';F02='14-flora-1';F03='14-flora-2';F04='14-flora-3';F05='14-flora-4';F06='14-flora-5';U01='02-nautical-build-ui';U02='09-building-720'}
$proof=@()
foreach($packet in @('p032','p033','p034')){
 $path=$root+'/'+$packet
 $manifest=Get-Content -LiteralPath ($path+'/current/manifest.json') -Raw -Encoding UTF8|ConvertFrom-Json
 [IO.Directory]::CreateDirectory($path+'/current/images')|Out-Null
 foreach($cell in $manifest.cells){
  $review=if($packet -eq 'p032'){'village-v31-review03'}elseif($packet -eq 'p033'){'village-v31-review05'}elseif(@('B01','B03','B06','U01','U02') -contains $cell.cell){'village-v31-review06'}else{'village-v31-review05'}
  $src=$r+'/Verification/'+$review+'/'+$mapping[$cell.cell]+'.jpg';$dest=$path+'/current/images/'+$cell.cell+'.jpg'
  Copy-Item -LiteralPath $src -Destination $dest -Force
  $hash=(Get-FileHash -LiteralPath $dest).Hash
  if($cell.sha256 -and $hash -ne $cell.sha256){throw ('Native evidence hash mismatch '+$packet+'/'+$cell.cell)}
  $proof+=@{packet=$packet;cell=$cell.cell;capture=$review;sha256=$hash}
 }
}
[IO.Directory]::CreateDirectory($root+'/p032/previous/images')|Out-Null
Copy-Item -LiteralPath ($r+'/Verification/village-v31-review01/11-village-panorama.jpg') -Destination ($root+'/p032/previous/images/B01.jpg') -Force
Copy-Item -LiteralPath ($r+'/Verification/village-v31-review01/12-south-boatwright.jpg') -Destination ($root+'/p032/previous/images/B02.jpg') -Force
function Copy-EvidenceTree([string]$source,[string]$destination){
 foreach($f in Get-ChildItem -LiteralPath $source -File -Recurse){$relative=$f.FullName.Substring($source.Length+1);$target=$destination+'/'+$relative;[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))|Out-Null;Copy-Item -LiteralPath $f.FullName -Destination $target -Force}
}
Copy-EvidenceTree ($root+'/p032') ($root+'/p033/previous/p032')
Copy-EvidenceTree ($root+'/p033') ($root+'/p034/previous/p033')
Copy-Item -LiteralPath ($root+'/p033/verdict.md') -Destination ($root+'/verdict.md')
Copy-Item -LiteralPath ($root+'/p034/verdict.md') -Destination ($root+'/final-amendment.md') -Force
@{verified=$true;cells=$proof;note='Unchanged cells are reused only with disclosed unchanged construction/camera rules; raw independent verdicts retained.'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath ($root+'/evidence-integrity.json') -Encoding UTF8
$backup=$r+'/Before/SeaVillageV31/round02/BlendBackups';[IO.Directory]::CreateDirectory($backup)|Out-Null
Get-ChildItem -LiteralPath ($r+'/Tools/SeaVillageV31/Masters') -Filter '*.blend1' -File|Move-Item -Destination $backup -Force
Write-Output ('VISUAL_ARCHIVED '+$proof.Count+' native cells plus full previous context')
