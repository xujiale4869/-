param([string]$ProjectPath=(Join-Path ([Environment]::GetFolderPath('Desktop')) '深海灯塔-Unity海陆-v0.3'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System; using System.Drawing;
public static class ReefLifeImageCheck {
 public static double[] Compare(string aa,string bb,int x0,int y0,int x1,int y1){
  using(var a=new Bitmap(aa))using(var b=new Bitmap(bb)){
   double diff=0,changed=0,la=0,lb=0,l2a=0,l2b=0,n=0;
   for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){
    var p=a.GetPixel(x,y);var q=b.GetPixel(x,y);double d=Math.Abs(p.R-q.R)+Math.Abs(p.G-q.G)+Math.Abs(p.B-q.B);
    diff+=d/3;if(d>0)changed++;double ap=p.R*.2126+p.G*.7152+p.B*.0722,bp=q.R*.2126+q.G*.7152+q.B*.0722;
    la+=ap;lb+=bp;l2a+=ap*ap;l2b+=bp*bp;n++;
   }
   return new[]{diff/n,changed/n,la/n,lb/n,Math.Sqrt(Math.Max(0,l2a/n-la*la/n/n)),Math.Sqrt(Math.Max(0,l2b/n-lb*lb/n/n))};
  }
 }
}
'@
$q=Join-Path $ProjectPath 'Verification\reef-life-v1'
$before=Join-Path $q 'before\normal.png';$after=Join-Path $q 'after\normal.png'
$regions=@{dryIsland=@(120,200,250,400);passiveBoat=@(746,212,753,222);mainBoat=@(635,339,644,353);openWater=@(1080,40,1250,180)}
$results=@{}
foreach($name in $regions.Keys){$r=$regions[$name];$v=[ReefLifeImageCheck]::Compare($before,$after,$r[0],$r[1],$r[2],$r[3]);$results[$name]=@{meanRGBDifference=$v[0];changedFraction=$v[1];beforeLuminance=$v[2];afterLuminance=$v[3];beforeContrast=$v[4];afterContrast=$v[5]}}
foreach($name in @('dryIsland','passiveBoat','mainBoat')){if($results[$name].meanRGBDifference -ne 0){throw "Opaque material sample changed: $name"}}
$motion=[ReefLifeImageCheck]::Compare((Join-Path $q 'after\video-0000.png'),(Join-Path $q 'after\video-0030.png'),1050,30,1260,180)
if($motion[0] -lt .25){throw 'Fixed-camera open water appears static'}
$result=@{passed=$true;regions=$results;fixedCameraWaterMeanRGBChange=$motion[0];notes='Paired stills at game time 2.5, matching camera and settled boat pose. Small opaque interior regions only; motion crop avoids boats and birds. Not a whole-scene object mask or a performance benchmark.'}
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $q 'image-comparison.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 6
