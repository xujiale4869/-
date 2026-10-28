using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
namespace Deepsea.Editor {
public static class CoastHeroBuild {
 const string Root="Assets/Deepsea/Resources/CoastHero/",Source="Assets/Deepsea/ThirdParty/CoastArtV7/";
 [Serializable]class BoatData{public Part[] batches;public Surface[] materials;}
 [Serializable]class Part{public float[] positions,normals,uv;public int[] indices;public int material;}
 [Serializable]class Surface{public string name,image;public float metallic,roughness;}
 public static void Prepare(){Directory.CreateDirectory(Root);AssetDatabase.Refresh();Boat();Character();AssetDatabase.SaveAssets();}
 static T Save<T>(T asset,string name)where T:UnityEngine.Object{string path=Root+name;var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(asset,path);return asset;}if(typeof(T)==typeof(Mesh)){AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(asset,path);return asset;}EditorUtility.CopySerialized(asset,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(asset);return old;}
 static Texture2D Texture(string name,bool normal=false){string path=Source+name;var importer=AssetImporter.GetAtPath(path)as TextureImporter;if(importer==null)throw new Exception("Missing hero texture "+path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.anisoLevel=4;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
 static void Boat(){
  var data=JsonUtility.FromJson<BoatData>(File.ReadAllText(Source+"BoatSource.json"));var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int[]>();
  foreach(var part in data.batches){int start=v.Count;for(int i=0;i<part.positions.Length;i+=3){v.Add(new Vector3(part.positions[i],part.positions[i+1],part.positions[i+2]));n.Add(new Vector3(part.normals[i],part.normals[i+1],part.normals[i+2]));}for(int i=0;i<part.uv.Length;i+=2)uv.Add(new Vector2(part.uv[i],part.uv[i+1]));var tr=new int[part.indices.Length];for(int i=0;i<tr.Length;i++)tr[i]=start+part.indices[i];indices.Add(tr);}
  var mesh=new Mesh{name="UV-preserving detailed rowboat",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.subMeshCount=indices.Count;for(int i=0;i<indices.Count;i++)mesh.SetTriangles(indices[i],i);mesh.RecalculateBounds();
  var reducer=new UnityMeshSimplifier.MeshSimplifier();reducer.Initialize(mesh);reducer.SimplifyMesh(Mathf.Min(1,18000f/(mesh.triangles.Length/3f)));var reduced=reducer.ToMesh();UnityEngine.Object.DestroyImmediate(mesh);reduced.name="CC0 wooden boat with original UVs";reduced.RecalculateBounds();reduced.RecalculateTangents();mesh=Save(reduced,"Rowboat.asset");
  if(mesh.subMeshCount!=data.materials.Length||mesh.uv.Length!=mesh.vertexCount)throw new Exception("Boat material/UV preservation failed");
  var mats=new Material[data.materials.Length];for(int i=0;i<mats.Length;i++){var s=data.materials[i];var mat=new Material(Shader.Find("Deepsea/CoastHeroCloth")){name=s.name};mat.SetTexture("_BaseMap",Texture(s.image));mat.SetColor("_BaseColor",i==5?new Color(.16f,.29f,.30f):i==0||i==2?new Color(.72f,.76f,.75f):Color.white);mat.SetFloat("_Metallic",s.metallic);mat.SetFloat("_Smoothness",Mathf.Clamp(1-s.roughness,.12f,.5f));mat.SetFloat("_Fabric",0);mat.SetFloat("_ColourTexture",1);mats[i]=Save(mat,"BoatMaterial-"+i+".mat");}
  var root=new GameObject("Textured CC0 rowing boat");try{root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=mats;PrefabUtility.SaveAsPrefabAsset(root,Root+"Rowboat.prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("COAST_HERO_BOAT triangles="+mesh.triangles.Length/3+" vertices="+mesh.vertexCount+" materials="+mats.Length+" bounds="+mesh.bounds);
 }
 static void Character(){
  var root=new GameObject("Fisherman with canvas jacket and brimmed hat");try{
   var rig=root.AddComponent<CoastHeroRig>();var b=new Body(root.transform);int hips=b.Bone("Hips",-1,new Vector3(0,.80f,0));
   int head=b.Bone("Head",hips,new Vector3(0,1.35f,0));int ll=b.Bone("LeftLeg",hips,new Vector3(-.115f,.80f,0)),rl=b.Bone("RightLeg",hips,new Vector3(.115f,.80f,0));
   int lk=b.Bone("LeftKnee",ll,new Vector3(-.115f,.45f,.025f)),rk=b.Bone("RightKnee",rl,new Vector3(.115f,.45f,.025f));
   int la=b.Bone("LeftArm",hips,new Vector3(-.235f,1.25f,0)),ra=b.Bone("RightArm",hips,new Vector3(.235f,1.25f,0));
   int le=b.Bone("LeftElbow",la,new Vector3(-.30f,1.02f,.025f)),re=b.Bone("RightElbow",ra,new Vector3(.30f,1.02f,.025f));
   rig.leftLeg=b.bones[ll];rig.rightLeg=b.bones[rl];rig.leftKnee=b.bones[lk];rig.rightKnee=b.bones[rk];rig.leftArm=b.bones[la];rig.rightArm=b.bones[ra];rig.leftElbow=b.bones[le];rig.rightElbow=b.bones[re];
   b.Lathe(hips,0,new[]{.68f,.73f,.88f,1.06f,1.20f,1.27f,1.31f},new[]{.20f,.215f,.18f,.18f,.235f,.22f,.14f},new[]{.15f,.16f,.15f,.155f,.16f,.13f,.10f},true);
   b.Ellipsoid(head,1,new Vector3(0,1.49f,.013f),new Vector3(.115f,.15f,.108f),20,12);
   b.Ellipsoid(head,1,new Vector3(0,1.47f,.119f),new Vector3(.031f,.044f,.039f),12,8);
   foreach(int side in new[]{-1,1}){
    int leg=side<0?ll:rl,knee=side<0?lk:rk,arm=side<0?la:ra,elbow=side<0?le:re;
    b.Ellipsoid(leg,2,new Vector3(side*.115f,.62f,0),new Vector3(.096f,.205f,.104f));
    b.Ellipsoid(knee,2,new Vector3(side*.115f,.295f,.016f),new Vector3(.081f,.19f,.085f));
    b.Ellipsoid(knee,2,new Vector3(side*.115f,.09f,.075f),new Vector3(.088f,.09f,.16f));
    b.Ellipsoid(arm,0,new Vector3(side*.258f,1.12f,.012f),new Vector3(.075f,.185f,.085f));
    b.Ellipsoid(elbow,0,new Vector3(side*.30f,.92f,.025f),new Vector3(.06f,.14f,.065f));
    b.Ellipsoid(elbow,1,new Vector3(side*.30f,.79f,.038f),new Vector3(.057f,.072f,.049f));
    b.Ellipsoid(head,1,new Vector3(side*.113f,1.485f,.005f),new Vector3(.025f,.044f,.028f),12,8);
    b.Ellipsoid(head,2,new Vector3(side*.044f,1.525f,.112f),new Vector3(.012f,.009f,.006f),10,6);
    b.Ellipsoid(hips,0,new Vector3(side*.125f,.88f,.152f),new Vector3(.074f,.086f,.018f));
    b.Ellipsoid(hips,0,new Vector3(side*.075f,1.255f,.12f),new Vector3(.052f,.065f,.018f));
   }
   b.Ellipsoid(head,1,new Vector3(0,1.35f,0),new Vector3(.065f,.09f,.065f));
   // Broad, softly curved brim and a low crown read at the fixed gameplay camera.
   b.Lathe(head,3,new[]{1.61f,1.625f,1.64f,1.655f},new[]{.13f,.23f,.23f,.132f},new[]{.115f,.19f,.19f,.115f},false);
   b.Lathe(head,3,new[]{1.645f,1.67f,1.715f,1.73f},new[]{.136f,.143f,.133f,.105f},new[]{.12f,.123f,.116f,.10f},false);
   b.Lathe(head,2,new[]{1.65f,1.67f},new[]{.145f,.147f},new[]{.125f,.127f},false);
   b.Ellipsoid(hips,2,new Vector3(0,1.03f,-.175f),new Vector3(.185f,.22f,.10f));
   for(int i=0;i<4;i++)b.Ellipsoid(hips,3,new Vector3(0,.83f+i*.10f,.162f),new Vector3(.013f,.013f,.008f),8,6);
   Mesh mesh=Save(b.Mesh(),"Fisherman.asset");var renderer=root.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.bones=b.bones.ToArray();renderer.rootBone=b.bones[hips];renderer.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(1.5f,2.6f,2.2f));renderer.quality=SkinQuality.Bone1;
   var cloth=Texture("rough_linen_diff_1k.jpg");var normal=Texture("rough_linen_nor_gl_1k.jpg",true);
   Color[] colors={new Color(.105f,.24f,.29f),new Color(.60f,.40f,.27f),new Color(.095f,.11f,.115f),new Color(.46f,.37f,.225f)};
   var mats=new Material[4];for(int i=0;i<4;i++){var m=new Material(Shader.Find("Deepsea/CoastHeroCloth")){name="Fisherman surface "+i};m.SetColor("_BaseColor",colors[i]);m.SetTexture("_BaseMap",cloth);m.SetTexture("_BumpMap",normal);m.SetFloat("_Fabric",i==0||i==3?1:0);mats[i]=Save(m,"FishermanMaterial-"+i+".mat");}renderer.sharedMaterials=mats;
   PrefabUtility.SaveAsPrefabAsset(root,Root+"Fisherman.prefab");Debug.Log("COAST_HERO_CHARACTER triangles="+mesh.triangles.Length/3+" bones="+b.bones.Count+" materials="+mats.Length);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 sealed class Body {
  Transform root;public readonly List<Transform>bones=new List<Transform>();List<Vector3>v=new List<Vector3>(),n=new List<Vector3>();List<Vector2>uv=new List<Vector2>();List<BoneWeight>w=new List<BoneWeight>();List<int>[]tr={new List<int>(),new List<int>(),new List<int>(),new List<int>()};
  public Body(Transform r){root=r;}
  public int Bone(string name,int parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent<0?root:bones[parent],false);t.position=p;bones.Add(t);return bones.Count-1;}
  void Vertex(Vector3 p,Vector3 normal,Vector2 tex,int bone){v.Add(p);n.Add(normal);uv.Add(tex);w.Add(new BoneWeight{boneIndex0=bone,weight0=1});}
  void Grid(int start,int sides,int rows,int material){for(int j=0;j<rows-1;j++)for(int i=0;i<sides;i++){int a=start+j*(sides+1)+i,c=a+sides+1;tr[material].AddRange(new[]{a,c,a+1,a+1,c,c+1});}}
  public void Ellipsoid(int bone,int mat,Vector3 p,Vector3 size,int sides=12,int rings=8){int start=v.Count;for(int j=0;j<=rings;j++){float q=-Mathf.PI*.5f+j*Mathf.PI/rings;for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;Vector3 unit=new Vector3(Mathf.Cos(q)*Mathf.Cos(a),Mathf.Sin(q),Mathf.Cos(q)*Mathf.Sin(a));Vertex(p+Vector3.Scale(unit,size),new Vector3(unit.x/size.x,unit.y/size.y,unit.z/size.z).normalized,new Vector2(i/(float)sides,j/(float)rings),bone);}}Grid(start,sides,rings+1,mat);}
  public void Lathe(int bone,int mat,float[]y,float[]rx,float[]rz,bool folds){const int sides=28;int start=v.Count;for(int j=0;j<y.Length;j++)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;float f=folds?1+.018f*Mathf.Sin(a*7+y[j]*16)+.012f*Mathf.Sin(a*11-y[j]*10):1;int lo=Mathf.Max(0,j-1),hi=Mathf.Min(y.Length-1,j+1);float slope=(rx[hi]-rx[lo])/Mathf.Max(.001f,y[hi]-y[lo]);Vector3 normal=new Vector3(Mathf.Cos(a)/rx[j],-slope/rx[j],Mathf.Sin(a)/rz[j]).normalized;Vertex(new Vector3(Mathf.Cos(a)*rx[j]*f,y[j],Mathf.Sin(a)*rz[j]*f),normal,new Vector2(i/(float)sides,y[j]*1.8f),bone);}Grid(start,sides,y.Length,mat);
   // The small top and bottom caps avoid seeing through the crown and coat hem.
   for(int end=0;end<2;end++){int row=end==0?0:y.Length-1,center=v.Count;Vertex(new Vector3(0,y[row],0),end==0?Vector3.down:Vector3.up,Vector2.one*.5f,bone);for(int i=0;i<sides;i++){int a=start+row*(sides+1)+i;if(end==0)tr[mat].AddRange(new[]{center,a,a+1});else tr[mat].AddRange(new[]{center,a+1,a});}}
  }
  public Mesh Mesh(){var m=new Mesh{name="Authored fisherman with clothing, hat and joints"};m.SetVertices(v);m.SetNormals(n);m.SetUVs(0,uv);m.boneWeights=w.ToArray();var bind=new Matrix4x4[bones.Count];for(int i=0;i<bind.Length;i++)bind[i]=bones[i].worldToLocalMatrix*root.localToWorldMatrix;m.bindposes=bind;m.subMeshCount=tr.Length;for(int i=0;i<tr.Length;i++)m.SetTriangles(tr[i],i);m.RecalculateBounds();m.RecalculateTangents();return m;}
 }
}
}
