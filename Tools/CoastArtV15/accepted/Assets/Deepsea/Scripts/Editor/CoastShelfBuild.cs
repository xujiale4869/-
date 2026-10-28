using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
public static class CoastShelfBuild {
 const string Root="Assets/Deepsea/Resources/CoastShelf/";
 [Serializable]class Piece{public string name;public Vector3[] vertices,normals;public Color[] colors;public Vector2[] uv;public int[] triangles;}
 [Serializable]class Archive{public Piece[] meshes;public string inferred="Art-directed bathymetry from a perspective concept. Unseen shape and dimensions inferred. Original physics unchanged.";}
 public static void Prepare(){
  Directory.CreateDirectory(Root);AssetDatabase.Refresh();var records=new List<Piece>();int total=0;
  for(int chunk=0;chunk<8;chunk++){
   float z0=CoastShelfShape.Z0+chunk*11.5f,z1=Mathf.Min(CoastShelfShape.Z1,z0+11.5f);
   int nx=Mathf.CeilToInt((CoastShelfShape.X1-CoastShelfShape.X0)/CoastShelfShape.Step),nz=Mathf.CeilToInt((z1-z0)/CoastShelfShape.Step);
   var vertices=new Vector3[(nx+1)*(nz+1)];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];var colours=new Color[vertices.Length];var triangles=new List<int>();
   for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){
    int k=j*(nx+1)+i;float x=Mathf.Lerp(CoastShelfShape.X0,CoastShelfShape.X1,i/(float)nx),z=Mathf.Lerp(z0,z1,j/(float)nz),stone;
    vertices[k]=new Vector3(x,CoastShelfShape.Height(x,z,out stone),z);normals[k]=CoastShelfShape.Normal(x,z);uv[k]=new Vector2(x*.12f,z*.12f);colours[k]=new Color(stone,1-CoastShelfShape.Fracture(x,z)*stone*.32f,1,1);
   }
   for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){
    int k=j*(nx+1)+i;bool valid=true;foreach(int v in new[]{k,k+1,k+nx+1,k+nx+2})if(!CoastShelfShape.Contains(vertices[v].x,vertices[v].z))valid=false;
    if(!valid)continue;triangles.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});
   }
   var mesh=new Mesh{name="EN_ContinuousShelf"+chunk+"_LOD0",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.colors=colours;mesh.uv=uv;mesh.triangles=triangles.ToArray();CreaseNormals(mesh);mesh.RecalculateBounds();
   string path=Root+"Shelf"+chunk+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
   records.Add(new Piece{name=mesh.name,vertices=mesh.vertices,normals=mesh.normals,colors=mesh.colors,uv=mesh.uv,triangles=mesh.triangles});total+=mesh.triangles.Length/3;
  }
  if(total>60000)throw new Exception("Continuous shelf triangle budget exceeded: "+total);
  const int maskSize=1024;var mask=new Texture2D(maskSize,maskSize,TextureFormat.RGB24,false,true);var pixels=new Color32[maskSize*maskSize];
  for(int y=0;y<maskSize;y++)for(int x=0;x<maskSize;x++){
   float wx=Mathf.Lerp(CoastShelfShape.X0,CoastShelfShape.X1,(x+.5f)/maskSize),wz=Mathf.Lerp(CoastShelfShape.Z0,CoastShelfShape.Z1,(y+.5f)/maskSize);
   // Clip only a conservative interior. A narrow overlap remains above the original floor,
   // preventing point-sampled mask pixels from cutting tiny holes outside a rendered quad.
   bool covered=CoastShelfShape.CoveredCell(wx,wz)&&CoastShelfShape.CoveredCell(wx-.12f,wz)&&CoastShelfShape.CoveredCell(wx+.12f,wz)&&CoastShelfShape.CoveredCell(wx,wz-.12f)&&CoastShelfShape.CoveredCell(wx,wz+.12f);
   byte on=covered?(byte)255:(byte)0;pixels[y*maskSize+x]=new Color32(on,on,on,255);
  }
  mask.SetPixels32(pixels);mask.Apply();File.WriteAllBytes(Root+"Coverage.png",mask.EncodeToPNG());UnityEngine.Object.DestroyImmediate(mask);AssetDatabase.ImportAsset(Root+"Coverage.png");
  var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"Coverage.png");importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Shelf.mat");var shader=Shader.Find("Deepsea/CoastHabitatGround");if(shader==null)throw new Exception("Shelf shader missing");
  if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"Shelf.mat");}else material.shader=shader;
  // One shading function and the same material inputs on both sides of the coverage edge.
  material.CopyPropertiesFromMaterial(Resources.Load<Material>("CoastMaterials/Ground"));material.SetFloat("_ReplacementShelf",1);
  material.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Verification/coast-art-v15");File.WriteAllText("Verification/coast-art-v15/shelf-meshes.json",JsonUtility.ToJson(new Archive{meshes=records.ToArray()}));
  Debug.Log("COAST_CONTINUOUS_SHELF_BUILT chunks=8 triangles="+total+" noise=FastNoiseLite seed=231301");
 }
 // Preserve geological breaks. Analytic smoothed normals made the original bank look melted.
 static void CreaseNormals(Mesh mesh){
  var v=mesh.vertices;var t=mesh.triangles;var c=mesh.colors;var u=mesh.uv;var adjacent=new List<int>[v.Length];var face=new Vector3[t.Length/3];
  for(int i=0;i<v.Length;i++)adjacent[i]=new List<int>();
  for(int f=0;f<face.Length;f++){int k=f*3;face[f]=Vector3.Cross(v[t[k+1]]-v[t[k]],v[t[k+2]]-v[t[k]]).normalized;for(int j=0;j<3;j++)adjacent[t[k+j]].Add(f);}
  var points=new Vector3[t.Length];var normals=new Vector3[t.Length];var colours=new Color[t.Length];var uv=new Vector2[t.Length];var indices=new int[t.Length];
  for(int k=0;k<t.Length;k++){
   int old=t[k],f=k/3;Vector3 n=Vector3.zero;foreach(int other in adjacent[old])if(Vector3.Dot(face[f],face[other])>.743f)n+=face[other];
   points[k]=v[old];normals[k]=n.normalized;colours[k]=c[old];uv[k]=u[old];indices[k]=k;
  }
  mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.vertices=points;mesh.normals=normals;mesh.colors=colours;mesh.uv=uv;mesh.triangles=indices;
 }
}
}
