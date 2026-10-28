from pathlib import Path
import os
P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
R=P/'Assets/Deepsea/Scripts/Runtime'

path=R/'CoastShelfShape.cs'
s=path.read_text(encoding='utf-8')
s=s.replace('float u=x-centre-bend;if(far)u=-u;', 'float raw=far?centre-x:x-centre;float u=raw+(far?bend:-bend);')
s=s.replace('h+=detail.GetNoise(x*.58f,z*.58f)*(.22f+.32f*S(0,8,u));', '''// Inclined rock plates: metre-scale masses survive the fixed gameplay camera.
  h+=detail.GetNoise(x*.58f,z*.58f)*(.35f+.45f*S(0,8,u));
  float plates=Plate(x,z);
  h+=plates*(.7f+1.15f*S(-2,7,u))*S(-9,-4,raw)*(1-S(12,16,u));''')
s=s.replace('weight=S(-12,-8,u)*(1-S(12,16,u));', 'weight=S(-9.0f,-5.5f,raw)*(1-S(12,16,u));')
anchor=' // Distance to two neighbouring geological cells'
idx=s.index(anchor)
s=s[:idx]+''' // Blended planar blocks. Each cell has its own height and dip, with rounded fractured edges.
 static float Plate(float x,float z){
  float px=(x+.32f*z)/3.7f,pz=z/4.8f;int ix=Mathf.FloorToInt(px),iz=Mathf.FloorToInt(pz);
  float sum=0,weights=0;
  for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++){
   int a=ix+i,b=iz+j;float cx=a+.5f+broad.GetNoise(a*31+71,b*23+17)*.40f;
   float cz=b+.5f+broad.GetNoise(a*19+131,b*29+53)*.40f;
   float dx=px-cx,dz=pz-cz,d=dx*dx+dz*dz;
   float w=Mathf.Exp(-d*5.5f);float height=broad.GetNoise(a*73+419,b*67+197)*1.9f+dx*.22f-dz*.17f;
   sum+=height*w;weights+=w;
  }
  return sum/Mathf.Max(.00001f,weights);
 }
''' +s[idx:]
path.write_text(s,encoding='utf-8')

# Stable arch-shaped terrain grid; archive the actual runtime visual meshes for roundtrip.
path=R/'CoastGroundSmoothing.cs'
s=path.read_text(encoding='utf-8')
s=s.replace(' public static int VisualTriangles', ''' [System.Serializable]class Piece{public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
  [System.Serializable]class Archive{public Piece[] meshes;}
  static readonly Dictionary<MeshFilter,Mesh[]> surfaces=new Dictionary<MeshFilter,Mesh[]>();
  public static void SetVisible(bool visible){foreach(var pair in surfaces)if(pair.Key!=null)pair.Key.sharedMesh=pair.Value[visible?1:0];}
  public static int VisualTriangles''')
s=s.replace('VisualTriangles=0;MaxDryDeviation=0;', 'VisualTriangles=0;MaxDryDeviation=0;surfaces.Clear();var archive=new List<Piece>();')
s=s.replace('if(h>.12f&&CoastBlockoutLayout.PathDistance(points[k])<1.5f)', 'if(x<=35.5f&&h>.12f&&CoastBlockoutLayout.PathDistance(points[k])<1.4f)')
s=s.replace('var collider=f.GetComponent<MeshCollider>();', '''surfaces.Add(f,new[]{original,visual});
    archive.Add(new Piece{name=visual.name,vertices=points,normals=normals,uv=uv,triangles=triangles});
    var collider=f.GetComponent<MeshCollider>();''')
s=s.replace('Debug.Log("COAST_RENDER_GROUND', '''if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-whitebox-check")>=0){System.IO.Directory.CreateDirectory("Verification/coast-art-v15");System.IO.File.WriteAllText("Verification/coast-art-v15/ground-meshes.json",JsonUtility.ToJson(new Archive{meshes=archive.ToArray()}));}
   Debug.Log("COAST_RENDER_GROUND''')
path.write_text(s,encoding='utf-8')
path=R/'CoastMaterialsAppearance.cs'
s=path.read_text(encoding='utf-8').replace('Grey=value;','Grey=value;CoastGroundSmoothing.SetVisible(!value);')
path.write_text(s,encoding='utf-8')

path=P/'Assets/Deepsea/Shaders/CoastHabitatGround.shader'
s=path.read_text(encoding='utf-8')
s=s.replace('half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,rockUV).rgb;half lum=', '''half3 tex=lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,rockUV).rgb,SampleRock(i.mapping,w),_ReplacementShelf);half lum=''')
s=s.replace('n=normalize(lerp(n,bump,_BumpStrength*(1-sand*.85)));', '''bump=lerp(bump,SampleBump(i.mapping,n,w),_ReplacementShelf);
 n=normalize(lerp(n,bump,_BumpStrength*(1-sand*.85)));''')
path.write_text(s,encoding='utf-8')

path=R/'CoastBlockoutCheck.cs'
s=path.read_text(encoding='utf-8')
s=s.replace('public int foundationTriangles,foundationMaterials,foundationColliders;', 'public int foundationTriangles,foundationMaterials,foundationColliders;public int visualGroundTriangles;public float visualGroundPathHeightChange;')
s=s.replace('var surface=GetComponent<CoastRockSurfaces>();', '''if(MaterialMode){report.visualGroundTriangles=CoastGroundSmoothing.VisualTriangles;report.visualGroundPathHeightChange=CoastGroundSmoothing.MaxDryDeviation;
             Require(report.visualGroundTriangles>74992&&report.visualGroundTriangles<=310000,"Visual ground tessellation invalid");
             Require(report.visualGroundPathHeightChange<.12f,"Visual shore intrudes land walking course");
             foreach(var f in region.GetComponentsInChildren<MeshFilter>())if(f.name.StartsWith("地形块")){var c=f.GetComponent<MeshCollider>();Require(c!=null&&c.sharedMesh!=f.sharedMesh,"Original ground collision separation lost");}}
            var surface=GetComponent<CoastRockSurfaces>();''')
path.write_text(s,encoding='utf-8')
print('Refined plates, continuous mask boundary, baseline grey geometry, actual render-mesh archive and acceptance checks')
