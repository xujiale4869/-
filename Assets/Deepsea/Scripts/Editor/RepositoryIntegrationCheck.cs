using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deepsea.Editor {
 public static class RepositoryIntegrationCheck {
  [Serializable] public class Report {
   public bool passed;
   public string unityVersion;
   public string scene="Assets/Deepsea/Scenes/CoastSeaLifeWaterV22.unity";
   public List<string> checks=new List<string>();
   public List<string> failures=new List<string>();
   public List<string> packages=new List<string>();
   public int sceneDependencies;
   public string scope="Repository dependency restoration/import check. Does not claim a new gameplay build or manual playtest.";
  }
  public static void Run() {
   var report=new Report {unityVersion=Application.unityVersion};
   Action<bool,string> check=(ok,label)=>{report.checks.Add(label+": "+ok);if(!ok)report.failures.Add(label);};
   try {
    var packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
    foreach(var name in new[]{"com.z3lx.acg-importer","com.whinarn.unitymeshsimplifier","com.cqf.urpvolumetricfog","com.unity.render-pipelines.universal"}) {
     var package=packages.FirstOrDefault(p=>p.name==name);
     check(package!=null,"Unity resolves "+name);
     if(package!=null)report.packages.Add(package.name+"@"+package.version+" ("+package.source+")");
    }
    foreach(var file in new[]{
     "Assets/Deepsea/ThirdParty/BoatAttack/GerstnerWaves.hlsl",
     "Assets/Deepsea/Shaders/Includes/TiledCurrent.hlsl",
     "Assets/Deepsea/ThirdParty/BoatAttack/LICENSE.md",
     "Assets/Deepsea/ThirdParty/NVJOB/NVJOB-LICENSE.txt",
     "Assets/Deepsea/ThirdParty/FastNoiseLite/LICENSE.txt",
     "Assets/Deepsea/Resources/UI/Lucide-LICENSE.txt",
     "Tools/AgentSkillSources/sources.lock.json",
     ".gitmodules",".gitattributes"
    })check(File.Exists(file),"Present: "+file);
    var mesh=Resources.Load<Mesh>("SeaLifeWaterV22/WaterMesh");
    check(mesh!=null&&mesh.vertexCount>10000,"Current water mesh imports with real vertex data");
    var water=Resources.Load<Material>("SeaLifeWaterV22/Water");
    check(water!=null&&water.shader!=null,"Current water material and shader resolve");
    if(water!=null&&water.shader!=null)check(!ShaderUtil.ShaderHasError(water.shader),"Current water shader has no compiler errors");
    var scene=EditorSceneManager.OpenScene(report.scene,OpenSceneMode.Single);
    check(scene.IsValid(),"Current life scene opens");
    check(UnityEngine.Object.FindObjectOfType<SeaLifeRuntime>()!=null,"Current scene retains life runtime");
    check(UnityEngine.Object.FindObjectOfType<SeaLifeWaterV22>()!=null,"Current scene retains water runtime");
    check(UnityEngine.Object.FindObjectOfType<GameDirector>()==null,"Old gameplay chain is not reintroduced");
    var dependencies=AssetDatabase.GetDependencies(report.scene,true);
    report.sceneDependencies=dependencies.Length;
    check(dependencies.Length>30,"Scene asset dependency graph resolves");
    foreach(var dependency in dependencies.Where(x=>x.StartsWith("Assets/")))
     if(!File.Exists(dependency)&&!Directory.Exists(dependency))report.failures.Add("Missing scene dependency: "+dependency);
   }catch(Exception ex){report.failures.Add(ex.ToString());}
   report.passed=report.failures.Count==0;
   Directory.CreateDirectory("Verification/repository-integration");
   File.WriteAllText("Verification/repository-integration/unity-import.json",JsonUtility.ToJson(report,true));
   if(!report.passed)throw new Exception("Repository integration validation failed: "+string.Join("; ",report.failures));
   Debug.Log("REPOSITORY_INTEGRATION_IMPORT_OK checks="+report.checks.Count+" dependencies="+report.sceneDependencies);
  }
 }
}
