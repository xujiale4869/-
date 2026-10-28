using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
public static class EcologyBuild {
 public static void Build(){
  PrepareBird();ProjectSetup.EnsureProject();
  foreach(string name in new[]{"Deepsea/Ocean","Deepsea/CoastalGround","Deepsea/Seagrass","Deepsea/MarineFish","Deepsea/MarineSplash","Deepsea/Seabird","Deepsea/SeabirdShadow"}){
   Shader shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);
   foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+message.message);
  }
  const string output="Builds/EcologyV1";Directory.CreateDirectory(output);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectSetup.MainScene},locationPathName=output+"/GuichaoBay.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Ecology build failed");
  foreach(string folder in new[]{"BoatAttack","MarineLife","NVJOB"}){
   string target=output+"/ThirdParty/"+folder;Directory.CreateDirectory(target);
   foreach(string f in Directory.GetFiles("Assets/Deepsea/ThirdParty/"+folder))
    if(!f.EndsWith(".meta")&&(f.EndsWith(".md")||f.IndexOf("LICENSE",StringComparison.OrdinalIgnoreCase)>=0))File.Copy(f,target+"/"+Path.GetFileName(f),true);
  }
  Debug.Log("ECOLOGY_BUILD_OK "+report.summary.totalSize);
 }
 static void PrepareBird(){
  const string input="Assets/Deepsea/Resources/Wildlife/Seabird.fbx";
  var importer=(ModelImporter)AssetImporter.GetAtPath(input);
  if(importer==null)throw new Exception("Missing upstream bird mesh");
  importer.isReadable=true;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(input);var instance=UnityEngine.Object.Instantiate(source);
  var combine=new List<CombineInstance>();
  foreach(var f in instance.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)combine.Add(new CombineInstance{mesh=f.sharedMesh,transform=instance.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
  foreach(var f in instance.GetComponentsInChildren<SkinnedMeshRenderer>())if(f.sharedMesh!=null)combine.Add(new CombineInstance{mesh=f.sharedMesh,transform=instance.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
  if(combine.Count==0)throw new Exception("Bird contains no geometry");
  var mesh=new Mesh{name="NVJOB bird · normalized coastal mesh",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);
  UnityEngine.Object.DestroyImmediate(instance);
  Vector3 size=mesh.bounds.size,centre=mesh.bounds.center;Quaternion rotation=Quaternion.identity;
  if(size.y>size.x&&size.y>size.z)rotation=Quaternion.Euler(0,0,-90);
  else if(size.z>size.x)rotation=Quaternion.Euler(0,90,0);
  else if(size.y>size.z)rotation=Quaternion.Euler(90,0,0);
  float length=Mathf.Max(size.x,Mathf.Max(size.y,size.z));Vector3[] vertices=mesh.vertices;
  for(int i=0;i<vertices.Length;i++)vertices[i]=rotation*(vertices[i]-centre)/length;
  mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();var bounds=mesh.bounds;bounds.Expand(.5f);mesh.bounds=bounds;
  const string output="Assets/Deepsea/Resources/Wildlife/CoastalBird.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(output);
  if(existing==null)AssetDatabase.CreateAsset(mesh,output);
  else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
  AssetDatabase.SaveAssets();Directory.CreateDirectory("Verification/ecology-v1");
  File.WriteAllText("Verification/ecology-v1/model-import.txt","upstream="+input+"\noriginal bounds="+size+"\nrotation="+rotation.eulerAngles+"\nvertices="+vertices.Length);
  Debug.Log("ECOLOGY_BIRD_OK "+size+" verts="+vertices.Length);
 }
}
}
