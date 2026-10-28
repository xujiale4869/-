$ErrorActionPreference='Stop'
$p='C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'
$dir=$p+'\Verification\coast-art-v9'
Add-Type -AssemblyName System.Drawing
foreach($name in @('shore-close','reef-close','shore-finish-off','reference','habitat-close','habitat-off','pier-close','cave-water','grey-comparison','boat-close','walker-close','sailing')){
 $source=Join-Path $dir ($name+'.png')
 if(!(Test-Path $source)){throw ('Missing screenshot: '+$name)}
 $src=[Drawing.Image]::FromFile($source)
 try{
  $bmp=New-Object Drawing.Bitmap 1280,720
  $g=[Drawing.Graphics]::FromImage($bmp)
  try{$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.DrawImage($src,0,0,1280,720)}finally{$g.Dispose()}
  $jpg=Join-Path $dir ($name+'-final.jpg')
  try{$bmp.Save($jpg,[Drawing.Imaging.ImageFormat]::Jpeg)}finally{$bmp.Dispose()}
  $encoded=[Convert]::ToBase64String([IO.File]::ReadAllBytes($jpg))
  $lines=New-Object 'System.Collections.Generic.List[string]'
  for($i=0;$i -lt $encoded.Length;$i+=4096){$lines.Add($encoded.Substring($i,[Math]::Min(4096,$encoded.Length-$i)))}
  [IO.File]::WriteAllLines((Join-Path $dir ($name+'-final.b64')),$lines)
  Write-Output ($name+' '+$src.Width+'x'+$src.Height+' jpg='+((Get-Item $jpg).Length))
 }finally{$src.Dispose()}
}
$base=Get-Content ($p+'\Tools\CoastMaterials\Before-20261021-144821\accepted-geometry-hashes.json') -Raw | ConvertFrom-Json
$changed=@();foreach($row in $base){$target=if([IO.Path]::IsPathRooted($row.Path)){$row.Path}else{Join-Path $p $row.Path};if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $row.Hash){$changed+=$row.Path}}
$result=[PSCustomObject]@{compared=$base.Count;unchanged=$base.Count-$changed.Count;changed=$changed;checked=(Get-Date -Format o)}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath ($dir+'\preserved-files.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 4
