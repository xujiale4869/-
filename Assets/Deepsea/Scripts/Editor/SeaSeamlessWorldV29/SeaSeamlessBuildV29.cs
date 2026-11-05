using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea.Editor {
 public static class SeaSeamlessBuildV29 {
  const string Root="Assets/Deepsea/Resources/SeaSeamlessV29/",Scene="Assets/Deepsea/Scenes/CoastSeaSeamlessV29.unity";
  public static void PrepareAndBuild(){Prepare();Build();}
  public static void Prepare(){
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Directory.CreateDirectory(Root);EditorSceneManager.OpenScene("Assets/Deepsea/Scenes/CoastSeaExpansionV28.unity");BakeNativeField();
   for(int i=0;i<6;i++){var src=Resources.Load<Mesh>("CoastBlockout/ScannedRock");if(src==null)throw new Exception("Missing original reef mesh "+i);var simplifier=new UnityMeshSimplifier.MeshSimplifier();var options=UnityMeshSimplifier.SimplificationOptions.Default;options.PreserveUVSeamEdges=false;options.PreserveUVFoldoverEdges=false;options.PreserveSurfaceCurvature=true;options.VertexLinkDistance=1e-5;simplifier.SimplificationOptions=options;simplifier.Initialize(src);simplifier.SimplifyMesh(Mathf.Clamp01(1800f/(src.triangles.Length/3f)));var m=simplifier.ToMesh();m.name="NativeRock"+i;var b=src.bounds;var v=m.vertices;for(int k=0;k<v.Length;k++){var q=new Vector3((v[k].x-b.center.x)/b.size.x,(v[k].y-b.min.y)/b.size.y,(v[k].z-b.center.z)/b.size.z);q.x+=Mathf.Sin(q.y*5+i)*.055f;q.z+=Mathf.Cos(q.x*4+i*1.7f)*.045f;v[k]=q;}m.vertices=v;m.RecalculateNormals();m.RecalculateBounds();Debug.Log("SEAMLESS_ROCK "+i+" sourceVertices="+src.vertexCount+" lodVertices="+m.vertexCount+" lodTriangles="+m.triangles.Length/3);Save(m,Root+"Rock"+i+".asset");}
   Mesh bark=Resources.Load<Mesh>("CoastMaple/EN_RedMaple00_Bark_LOD0"),leaves=Resources.Load<Mesh>("CoastMaple/EN_RedMaple00_Leaves_LOD0");if(bark!=null&&leaves!=null){var b=bark.bounds;b.Encapsulate(leaves.bounds);float scale=5.8f/b.size.y;foreach(var pair in new[]{new KeyValuePair<string,Mesh>("MapleBark",bark),new KeyValuePair<string,Mesh>("MapleLeaves",leaves)}){var m=UnityEngine.Object.Instantiate(pair.Value);m.name=pair.Key;var v=m.vertices;for(int k=0;k<v.Length;k++)v[k]=(v[k]-new Vector3(b.center.x,b.min.y,b.center.z))*scale;m.vertices=v;m.RecalculateBounds();Save(m,Root+pair.Key+".asset");}}
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var rt=UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>();if(rt.GetComponent<SeaSeamlessWorldAndNauticalUIV29>()==null)rt.gameObject.AddComponent<SeaSeamlessWorldAndNauticalUIV29>();EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();Debug.Log("SEAMLESS_V29_PREPARED");
  }
  static void Save(UnityEngine.Object asset,string path){var old=AssetDatabase.LoadMainAssetAtPath(path);if(old==null)AssetDatabase.CreateAsset(asset,path);else{EditorUtility.CopySerialized(asset,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(asset);}}
  static void BakeNativeField(){
   var root=GameObject.Find("EN_MistCapeShowcase");if(root==null)throw new Exception("Missing native scene");var field=ScriptableObject.CreateInstance<NativeBayHeightfieldV29>();field.width=173;field.height=177;int count=field.width*field.height;field.values=Enumerable.Repeat(-100f,count).ToArray();field.distances=new float[count];field.nearest=Enumerable.Repeat(-1,count).ToArray();
   foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
    string n=f.name;var renderer=f.GetComponent<Renderer>();if(f.sharedMesh==null||renderer==null||!renderer.enabled||!f.gameObject.activeInHierarchy)continue;
    bool terrain=n.Contains("FineShore")||n.Contains("Land_Rock")||n.Contains("Reef_Rock")||n.Contains("Ground")||n.Contains("Shelf")||n.Contains("Substrate")||n.Contains("Inland")||n.Contains("Headland")||n.Contains("MapleFoot");if(!terrain||n.Contains("Leaves")||n.Contains("Shadow")||n.Contains("Water")||n.Contains("Pier")||n.Contains("LOD1"))continue;
    field.sourceMeshes++;var verts=f.sharedMesh.vertices;for(int j=0;j<verts.Length;j++)verts[j]=f.transform.TransformPoint(verts[j]);var tri=f.sharedMesh.triangles;
    for(int j=0;j<tri.Length;j+=3){Vector3 a=verts[tri[j]],b=verts[tri[j+1]],c=verts[tri[j+2]];if(Vector3.Cross(b-a,c-a).y<=.000001f)continue;float denom=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(denom)<.000001f)continue;
     int x0=Mathf.Max(0,Mathf.CeilToInt((Mathf.Min(a.x,Mathf.Min(b.x,c.x))-field.minX)/field.step)),x1=Mathf.Min(field.width-1,Mathf.FloorToInt((Mathf.Max(a.x,Mathf.Max(b.x,c.x))-field.minX)/field.step));int z0=Mathf.Max(0,Mathf.CeilToInt((Mathf.Min(a.z,Mathf.Min(b.z,c.z))-field.minZ)/field.step)),z1=Mathf.Min(field.height-1,Mathf.FloorToInt((Mathf.Max(a.z,Mathf.Max(b.z,c.z))-field.minZ)/field.step));
     for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){float xx=field.minX+x*field.step,zz=field.minZ+z*field.step,u=((b.z-c.z)*(xx-c.x)+(c.x-b.x)*(zz-c.z))/denom,v=((c.z-a.z)*(xx-c.x)+(a.x-c.x)*(zz-c.z))/denom,w=1-u-v;if(u<-.00001f||v<-.00001f||w<-.00001f)continue;float y=a.y*u+b.y*v+c.y*w;if(y>10||y<-20)continue;int k=z*field.width+x;if(y>field.values[k])field.values[k]=y;}
    }
   }
   field.safeValues=(float[])field.values.Clone();for(int z=0;z<field.height;z++)for(int x=0;x<field.width;x++){int k=z*field.width+x;if(field.values[k]<-90)continue;float safe=field.values[k];for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++){int xx=x+dx,zz=z+dz;if(xx<0||xx>=field.width||zz<0||zz>=field.height)continue;float value=field.values[zz*field.width+xx];if(value>-90)safe=Mathf.Min(safe,value);}field.safeValues[k]=safe;}
   var q=new Queue<int>();for(int i=0;i<count;i++)if(field.values[i]>-90){field.nearest[i]=i;q.Enqueue(i);field.coveredSamples++;}
   while(q.Count>0){int k=q.Dequeue(),x=k%field.width,z=k/field.width;foreach(int next in new[]{x>0?k-1:-1,x<field.width-1?k+1:-1,z>0?k-field.width:-1,z<field.height-1?k+field.width:-1}){if(next<0||field.nearest[next]>=0)continue;field.nearest[next]=field.nearest[k];int n=field.nearest[next];field.distances[next]=Vector2.Distance(new Vector2(next%field.width,next/field.width),new Vector2(n%field.width,n/field.width))*field.step;q.Enqueue(next);}}
   if(field.sourceMeshes<4||field.coveredSamples<100)throw new Exception("Insufficient native heightfield data");Save(field,Root+"NativeHeightfield.asset");Debug.Log("SEAMLESS_NATIVE_FIELD meshes="+field.sourceMeshes+" covered="+field.coveredSamples);
  }
  public static void Build(){
   EditorSceneManager.OpenScene(Scene);var files=new[]{"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorBuildSettings.asset"};var bytes=files.Select(File.ReadAllBytes).ToArray();var name=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;var gp=GraphicsSettings.renderPipelineAsset;var qp=QualitySettings.renderPipeline;BuildReport report=null;
   try{PlayerSettings.productName="深海灯塔 · 海湾生活 v21";PlayerSettings.enableFrameTimingStats=true;GraphicsSettings.renderPipelineAsset=Resources.Load<UniversalRenderPipelineAsset>("SeaSurfaceV24/Pipeline");QualitySettings.renderPipeline=GraphicsSettings.renderPipelineAsset;Directory.CreateDirectory("Builds/SeaSeamlessV29");report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/SeaSeamlessV29/Guichao-SeaSeamlessV29.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});}
   finally{PlayerSettings.productName=name;PlayerSettings.enableFrameTimingStats=timing;GraphicsSettings.renderPipelineAsset=gp;QualitySettings.renderPipeline=qp;AssetDatabase.SaveAssets();for(int i=0;i<files.Length;i++)File.WriteAllBytes(files[i],bytes[i]);}
   if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("v29 build failed");foreach(var f in Directory.GetFiles("Builds/SeaExpansionV28/Licenses","*",SearchOption.AllDirectories)){var dst=Path.Combine("Builds/SeaSeamlessV29/Licenses",f.Substring("Builds/SeaExpansionV28/Licenses".Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(f,dst,true);}File.WriteAllText("Tools/SeaSeamlessV29/build.json","{\"passed\":true,\"warnings\":"+report.summary.totalWarnings+"}");Debug.Log("SEAMLESS_V29_BUILD_OK");
  }
 }
}
