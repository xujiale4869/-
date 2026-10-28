using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea.Editor {
public static class MarineBuild {
 public static void Build(){
  PrepareMesh();ProjectSetup.EnsureProject();
  foreach(string name in new[]{"Deepsea/MarineFish","Deepsea/MarineSplash","Deepsea/Ocean"}){
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Missing shader "+name);
   foreach(var m in ShaderUtil.GetShaderMessages(shader))if(m.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+m.message);
  }
  Directory.CreateDirectory("Builds/MarineLifeV1");
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
   scenes=new[]{ProjectSetup.MainScene},locationPathName="Builds/MarineLifeV1/GuichaoBay.exe",
   target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
  });
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Marine build failed");
  Directory.CreateDirectory("Builds/MarineLifeV1/ThirdParty/MarineLife");
  foreach(string f in Directory.GetFiles("Assets/Deepsea/ThirdParty/MarineLife","*LICENSE*.txt"))
   File.Copy(f,"Builds/MarineLifeV1/ThirdParty/MarineLife/"+Path.GetFileName(f),true);
  File.Copy("Assets/Deepsea/ThirdParty/MarineLife/SOURCE.md","Builds/MarineLifeV1/ThirdParty/MarineLife/SOURCE.md",true);
  Directory.CreateDirectory("Builds/MarineLifeV1/ThirdParty/BoatAttack");
  File.Copy("Assets/Deepsea/ThirdParty/BoatAttack/LICENSE.md","Builds/MarineLifeV1/ThirdParty/BoatAttack/LICENSE.md",true);
  Debug.Log("MARINE_BUILD_OK "+report.summary.totalSize);
 }
 static void PrepareMesh(){
  const string input="Assets/Deepsea/Resources/Wildlife/Fish.fbx";
  var importer=(ModelImporter)AssetImporter.GetAtPath(input);
  if(importer==null)throw new Exception("Missing upstream fish.fbx");
  importer.isReadable=true;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(input);var instance=UnityEngine.Object.Instantiate(source);
  var combine=new List<CombineInstance>();
  foreach(var f in instance.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)combine.Add(new CombineInstance{mesh=f.sharedMesh,transform=instance.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
  foreach(var f in instance.GetComponentsInChildren<SkinnedMeshRenderer>())if(f.sharedMesh!=null)combine.Add(new CombineInstance{mesh=f.sharedMesh,transform=instance.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
  if(combine.Count==0)throw new Exception("Imported fish contains no geometry");
  var mesh=new Mesh{name="Vivian Menard fish · normalized",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);
  UnityEngine.Object.DestroyImmediate(instance);
  Vector3 size=mesh.bounds.size,centre=mesh.bounds.center;
  Vector3 axis=size.x>size.y&&size.x>size.z?Vector3.right:size.y>size.z?Vector3.up:Vector3.forward;
  Quaternion rot=Quaternion.FromToRotation(axis,Vector3.forward);
  Vector3[] vertices=mesh.vertices;float length=Mathf.Max(size.x,Mathf.Max(size.y,size.z));
  for(int i=0;i<vertices.Length;i++)vertices[i]=rot*(vertices[i]-centre)/length;
  mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();
  const string output="Assets/Deepsea/Resources/Wildlife/SwimmingFish.asset";
  var existing=AssetDatabase.LoadAssetAtPath<Mesh>(output);
  if(existing==null)AssetDatabase.CreateAsset(mesh,output);
  else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Verification/marine-life-v1");
  File.WriteAllText("Verification/marine-life-v1/model-import.txt","source="+input+"\noriginal bounds="+size+"\nsource axis="+axis+"\nvertices="+vertices.Length);
  Debug.Log("MARINE_MESH_OK bounds="+size+" axis="+axis+" vertices="+vertices.Length);
 }
}
}
