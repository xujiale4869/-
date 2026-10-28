from pathlib import Path
import re,os,shutil

P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
R=P/'Assets/Deepsea/Scripts/Runtime'
E=P/'Assets/Deepsea/Scripts/Editor'
# Restart this recipe from the preserved v14 foundation. Run 01,02,03 in order.
owned=[R/n for n in ['CoastShelfShape.cs','CoastGroundSmoothing.cs','CoastArtDetails.cs','CoastMaterialsAppearance.cs','CoastBlockoutCheck.cs']]+[E/'CoastMaterialsBuild.cs',E/'CoastShelfBuild.cs',P/'Assets/Deepsea/Shaders/CoastHabitatGround.shader']
for target in owned:
    source=P/'Tools/CoastArtV15/Before-20261024'/target.relative_to(P)
    if not source.is_file(): raise RuntimeError(f'v14 baseline missing: {source}')
    target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)

def replace(path,old,new):
    text=path.read_text(encoding='utf-8-sig')
    if old not in text: raise RuntimeError(f'anchor missing: {path}: {old[:60]}')
    path.write_text(text.replace(old,new),encoding='utf-8')

# Broad shoulders and descending ramps replace four almost vertical curtain courses.
path=R/'CoastShelfShape.cs'
text=path.read_text(encoding='utf-8-sig')
start=text.index(' static float Ridge(')
end=text.index(' // Distance to two',start)
text=text[:start]+''' static float Ridge(float x,float z,bool far,out float weight){
  float centre=far?CoastBlockoutLayout.FarSpine(z):CoastBlockoutLayout.Spine(z);
  // Inferred metre-scale promontories, with lobes rather than a repeated vertical extrusion.
  float bend=broad.GetNoise(z+37,far?314:71)*2.0f;
  float u=x-centre-bend;if(far)u=-u;
  float headland=.5f+.5f*Mathf.Sin(z*.31f+Mathf.Sin(z*.11f)*1.7f);
  float top=(far?-4.2f:-2.9f)+broad.GetNoise(z,far?73:113)*.85f;
  float shoulder=1-S(-9.0f,-3.0f,u);
  float ramp=2.0f*S(.2f,3.7f,u)+5.0f*S(2.7f,7.1f,u)
   +6.6f*S(6.2f,11.0f,u)+6.3f*S(10.0f,14.4f,u);
  float h=top-shoulder*5.4f-ramp;
  // Low bedding lips interrupt the ramp without creating tall, narrow vertical sheets.
  float bedding=.55f*S(1.5f+headland,2.4f+headland,u)
   +.70f*S(5.3f-headland,6.5f-headland,u);
  h-=bedding;
  h+=detail.GetNoise(x*.58f,z*.58f)*(.22f+.32f*S(0,8,u));
  h-=Fracture(x,z)*(.18f+.45f*S(-1,8,u))*S(-10,-5,u)*(1-S(12,15,u));
  weight=S(-12,-8,u)*(1-S(12,16,u));
  if(far)weight*=1-S(86,92,z)*(1-S(116,122,z));
  return h;
 }
''' +text[end:]
path.write_text(text,encoding='utf-8')

# Supersample only the visual terrain. Collider retains original, accepted coarse triangles.
(R/'CoastGroundSmoothing.cs').write_text('''using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
 public static class CoastGroundSmoothing {
  public static int VisualTriangles {get;private set;}
  public static float MaxDryDeviation {get;private set;}
  public static float Height(float x,float z){
   float shore=CoastBlockoutLayout.Shore(z),d=shore-x;
   if(Mathf.Abs(d)>=.85f)return CoastBlockoutLayout.TerrainHeight(x,z);
   float a=CoastBlockoutLayout.TerrainHeight(shore+.85f,z);
   float b=CoastBlockoutLayout.TerrainHeight(shore-.85f,z);
   float t=Mathf.InverseLerp(-.85f,.85f,d);
   return Mathf.Lerp(a,b,Mathf.SmoothStep(0,1,t));
  }
  public static void Apply(CoastBlockoutRegion region){
   VisualTriangles=0;MaxDryDeviation=0;
   foreach(var f in region.GetComponentsInChildren<MeshFilter>()){
    if(!f.name.StartsWith("地形块"))continue;
    var original=f.sharedMesh;var bounds=original.bounds;
    int nx=Mathf.RoundToInt(bounds.size.x/.325f),nz=Mathf.RoundToInt(bounds.size.z/.325f);
    var points=new Vector3[(nx+1)*(nz+1)];var normals=new Vector3[points.Length];var uv=new Vector2[points.Length];
    var triangles=new int[nx*nz*6];
    for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){
     int k=j*(nx+1)+i;float x=Mathf.Lerp(bounds.min.x,bounds.max.x,i/(float)nx),z=Mathf.Lerp(bounds.min.z,bounds.max.z,j/(float)nz);
     float h=Height(x,z);points[k]=new Vector3(x,h,z);uv[k]=new Vector2(x*.2f,z*.2f);
     normals[k]=new Vector3(Height(x-.09f,z)-Height(x+.09f,z),.18f,Height(x,z-.09f)-Height(x,z+.09f)).normalized;
     if(h>.12f&&CoastBlockoutLayout.PathDistance(points[k])<1.5f)MaxDryDeviation=Mathf.Max(MaxDryDeviation,Mathf.Abs(h-CoastBlockoutLayout.TerrainHeight(x,z)));
    }
    for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){
     int k=j*(nx+1)+i,t=(j*nx+i)*6;
     triangles[t]=k;triangles[t+1]=k+nx+1;triangles[t+2]=k+1;
     triangles[t+3]=k+1;triangles[t+4]=k+nx+1;triangles[t+5]=k+nx+2;
    }
    var visual=new Mesh{name="EN_ContinuousShore_"+f.name,indexFormat=IndexFormat.UInt32};
    visual.vertices=points;visual.normals=normals;visual.uv=uv;visual.triangles=triangles;visual.RecalculateBounds();f.sharedMesh=visual;VisualTriangles+=triangles.Length/3;
    var collider=f.GetComponent<MeshCollider>();if(collider!=null&&collider.sharedMesh!=original)throw new System.InvalidOperationException("Ground collision mesh changed");
   }
   Debug.Log("COAST_RENDER_GROUND triangles="+VisualTriangles+" maxPathHeightChange="+MaxDryDeviation+" originalColliders=true");
  }
 }
}
''',encoding='utf-8')

# Keep underwater/water shader terms fixed, adjust only dry land cover's material response.
replace(P/'Assets/Deepsea/Shaders/CoastHabitatGround.shader',
 'cover=dot(cover,half3(.299,.587,.114))*half3(.30,.48,.14);',
 'cover=dot(cover,half3(.299,.587,.114))*half3(.29,.365,.17);')
replace(P/'Assets/Deepsea/Shaders/CoastHabitatGround.shader',
 'albedo=lerp(soil,cover,growth)*variation;',
 '''// Broad dry stone exposure breaks the flat grass carpet along the cliff shoulder.
 float rockPatch=smoothstep(.48,.72,.5+.26*sin(i.world.x*.29+sin(i.world.z*.15)*2.1)+.18*sin(i.world.z*.39-i.world.x*.22));
 float exposed=rockPatch*smoothstep(.12,1.8,i.world.y)*(1-smoothstep(.72,.97,geom.y))*.65;
 half3 dryRock=SampleRock(i.mapping,w)*_BaseColor.rgb;
 albedo=lerp(lerp(soil,cover,growth)*variation,dryRock,exposed);''')

# Neutral warm stone treads replacing boxes, exactly following original collision tops.
replace(R/'CoastArtDetails.cs',
 'Pier(solid);Deck(timber);DockLantern(solid,lamp);',
 'Pier(solid);Deck(timber);Stairs(masonry);DockLantern(solid,lamp);')
replace(R/'CoastArtDetails.cs',
 'Batch("石灰窗套与塔身勒脚",masonry,Material("石灰收边",0));',
 '''var paving=new Material(Shader.Find("Deepsea/CoastArchitecture")){name="岸道磨损石阶 · 米制UV"};materials.Add(paving);
  var stone=Resources.Load<Material>("CoastMaterials/Rock");paving.SetTexture("_BaseMap",stone.GetTexture("_BaseMap"));paving.SetTexture("_BumpMap",stone.GetTexture("_BumpMap"));
  paving.SetColor("_BaseColor",new Color(.83f,.81f,.72f));paving.SetFloat("_TextureWeight",.92f);paving.SetFloat("_UseVertexColor",.18f);paving.SetFloat("_Smoothness",.10f);paving.SetFloat("_NormalStrength",.35f);
  Batch("连续岸道石阶",masonry,paving);''')
replace(R/'CoastArtDetails.cs',
 ' void DockLantern(B b,B glow){',
 ''' void Stairs(B b){
  int index=0;foreach(var r in region.GetComponentsInChildren<MeshRenderer>()){
   if(r.name!="石阶")continue;replaced.Add(r);
   var t=r.transform;int columns=3;float width=t.localScale.x/columns;
   for(int i=0;i<columns;i++){
    var slab=new B();var size=new Vector3(width-.018f,t.localScale.y,t.localScale.z-.008f);
    Vector3 local=new Vector3((i-1)*width,0,0);int seed=++index;
    WornBoard(slab,local,size,new Color(.72f,.70f,.64f)*(1+(seed%4)*.035f),seed);
    int start=b.v.Count;
    for(int k=0;k<slab.v.Count;k++)b.Vertex(t.position+t.rotation*(slab.v[k]+Vector3.up*.008f),slab.c[k],slab.uv[k]);
    foreach(int tri in slab.tr)b.tr.Add(start+tri);
   }
  }
 }
 void DockLantern(B b,B glow){''')

replace(R/'CoastMaterialsAppearance.cs','Quaternion.Euler(value?48:39,-35,0);light.intensity=value?1.10f:2.25f;',
 'Quaternion.Euler(value?48:32,value?-35:-52,0);light.intensity=value?1.10f:2.15f;')
replace(R/'CoastMaterialsAppearance.cs','new Color(.22f,.32f,.39f)','new Color(.16f,.25f,.34f)')
replace(R/'CoastMaterialsAppearance.cs','new Color(.14f,.20f,.21f)','new Color(.105f,.16f,.19f)')
replace(R/'CoastMaterialsAppearance.cs','new Color(.11f,.16f,.18f)','new Color(.085f,.12f,.145f)')

for path in [E/'CoastMaterialsBuild.cs',E/'CoastShelfBuild.cs',R/'CoastBlockoutCheck.cs']:
    text=path.read_text(encoding='utf-8-sig').replace('CoastArtV14','CoastArtV15').replace('ArtV14','ArtV15').replace('coast-art-v14','coast-art-v15').replace('v14-FoundationRebuild','v15-CoastalLandform')
    path.write_text(text,encoding='utf-8')
print('v15 source changes complete; v12 water shader untouched')
