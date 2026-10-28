using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;

namespace Deepsea.Editor
{
    public static class CoastBlockoutBuild
    {
        public const string Scene="Assets/Deepsea/Scenes/CoastBlockout.unity";
        const string AssetsRoot="Assets/Deepsea/Resources/CoastBlockout";
        [MenuItem("深海灯塔/白模/生成场景与构建")]
        public static void Build()
        {
            Directory.CreateDirectory(AssetsRoot+"/Meshes");AssetDatabase.Refresh();
            PrepareScannedRock();
            Material clay=MaterialAsset("Clay","Deepsea/BlockoutClay",new Color(.65f,.66f,.67f));
            Material water=MaterialAsset("TechnicalWater","Deepsea/BlockoutWater",Color.white);
            water.SetColor("_WaterColor",new Color(.18f,.36f,.46f));
            var region=CoastBlockoutGeometry.Build(null,clay,water);PersistMeshes(region.gameObject);
            var prefab=PrefabUtility.SaveAsPrefabAsset(region.gameObject,AssetsRoot+"/EastCoast.prefab");UnityEngine.Object.DestroyImmediate(region.gameObject);
            var boat=CoastBlockoutGeometry.BuildBoat(clay);PersistMeshes(boat);
            PrefabUtility.SaveAsPrefabAsset(boat,AssetsRoot+"/BoatProxy.prefab");UnityEngine.Object.DestroyImmediate(boat);
            var original=EditorSceneManager.OpenScene(ProjectSetup.MainScene);
            EditorSceneManager.SaveScene(original,Scene,true);
            var scene=EditorSceneManager.OpenScene(Scene);
            var director=UnityEngine.Object.FindObjectOfType<GameDirector>();
            var session=director.gameObject.AddComponent<CoastBlockoutSession>();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            session.region=instance.GetComponent<CoastBlockoutRegion>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorUtility.SetDirty(clay);EditorUtility.SetDirty(water);IncludeShaders();AssetDatabase.SaveAssets();
            BuildPlayer();
        }
        static void PrepareScannedRock()
        {
            var prefab=Resources.Load<GameObject>("CoastSlice/boulder_01");
            if(prefab==null)throw new InvalidOperationException("Existing CoastSlice/boulder_01 asset missing");
            MeshFilter chosen=null;int least=int.MaxValue;
            foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
                if(f.sharedMesh!=null&&f.sharedMesh.triangles.Length<least){chosen=f;least=f.sharedMesh.triangles.Length;}
            if(chosen==null)throw new InvalidOperationException("Existing boulder has no mesh");
            Vector3[] v=chosen.sharedMesh.vertices;var matrix=prefab.transform.worldToLocalMatrix*chosen.transform.localToWorldMatrix;
            var bounds=new Bounds(matrix.MultiplyPoint3x4(v[0]),Vector3.zero);
            for(int i=0;i<v.Length;i++){v[i]=matrix.MultiplyPoint3x4(v[i]);bounds.Encapsulate(v[i]);}
            for(int i=0;i<v.Length;i++)v[i]=new Vector3((v[i].x-bounds.center.x)/bounds.extents.x,(v[i].y-bounds.min.y)/bounds.size.y,(v[i].z-bounds.center.z)/bounds.extents.z);
            var prepared=new Mesh{name="白模复用扫描岩石 · 归一化最低LOD",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            prepared.vertices=v;prepared.triangles=chosen.sharedMesh.triangles;prepared.RecalculateNormals();prepared.RecalculateBounds();
            var reducer=new UnityMeshSimplifier.MeshSimplifier();reducer.Initialize(prepared);reducer.SimplifyMesh(.085f);
            var reduced=reducer.ToMesh();reduced.name=prepared.name;UnityEngine.Object.DestroyImmediate(prepared);prepared=reduced;least=prepared.triangles.Length;SmoothSharedNormals(prepared);
            string path=AssetsRoot+"/ScannedRock.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(prepared,path);else{EditorUtility.CopySerialized(prepared,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(prepared);}
            AssetDatabase.SaveAssets();Debug.Log("BLOCKOUT_SCAN_SOURCE triangles="+(least/3));
        }
        static void SmoothSharedNormals(Mesh mesh)
        {
            // FBX seams duplicate vertices. Smooth coincident positions AFTER simplification;
            // do not alter any vertex or triangle used by collision/depth checks.
            var v=mesh.vertices;var t=mesh.triangles;var sums=new Dictionary<Vector3,Vector3>();
            for(int i=0;i<t.Length;i+=3)
            {
                Vector3 n=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);
                for(int k=0;k<3;k++){Vector3 key=v[t[i+k]],old;sums.TryGetValue(key,out old);sums[key]=old+n;}
            }
            var normals=new Vector3[v.Length];for(int i=0;i<v.Length;i++)normals[i]=sums[v[i]].normalized;
            mesh.normals=normals;
        }
        static Material MaterialAsset(string name,string shaderName,Color color)
        {
            var shader=Shader.Find(shaderName);if(shader==null)throw new InvalidOperationException("Missing "+shaderName);
            string path=AssetsRoot+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
            if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);return m;
        }
        static void PersistMeshes(GameObject root)
        {
            var swaps=new Dictionary<Mesh,Mesh>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=f.sharedMesh;if(mesh==null||AssetDatabase.Contains(mesh))continue;
                Mesh saved;if(!swaps.TryGetValue(mesh,out saved))
                {
                    string name=mesh.name.Replace('/','-');string path=AssetsRoot+"/Meshes/"+name+".asset";
                    saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
                    else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);}
                    swaps.Add(mesh,saved);
                }
                f.sharedMesh=saved;
            }
            foreach(var c in root.GetComponentsInChildren<MeshCollider>())
            {Mesh m;if(c.sharedMesh!=null&&swaps.TryGetValue(c.sharedMesh,out m))c.sharedMesh=m;}
        }
        static void IncludeShaders()
        {
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Deepsea/BlockoutClay","Deepsea/BlockoutWater"})
            {
                var shader=Shader.Find(name);if(shader==null)throw new InvalidOperationException("Missing shader "+name);
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    if(message.severity==ShaderCompilerMessageSeverity.Error)throw new InvalidOperationException(name+": "+message.message);
                bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("深海灯塔/白模/仅重新构建程序")]
        public static void BuildPlayer()
        {
            PlayerSettings.enableFrameTimingStats=true;
            string directory="Builds/CoastBlockoutV1";Directory.CreateDirectory(directory);
            var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=directory+"/GuichaoBay-Blockout.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(r.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Whitebox build failed: "+r.summary.result);
            Directory.CreateDirectory("Verification/coast-blockout-v1");
            File.WriteAllText("Verification/coast-blockout-v1/build.json","{\"scene\":\""+Scene+"\",\"prefab\":\""+AssetsRoot+"/EastCoast.prefab\",\"unity\":\""+Application.unityVersion+"\",\"status\":\"built\"}");
            Debug.Log("COAST_BLOCKOUT_BUILD_OK");
        }
    }
}
