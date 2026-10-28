using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string MainScene="Assets/Deepsea/Scenes/GuichaoBay.unity";
        static ProjectSetup(){EditorApplication.delayCall+=EnsureProject;}

        [MenuItem("深海灯塔/准备浅海工程")]
        public static void EnsureProject()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            const string rendererPath="Assets/Deepsea/Config/BayRenderer.asset";
            const string pipelinePath="Assets/Deepsea/Config/BayPipeline.asset";
            UniversalRenderPipelineAsset pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(pipeline==null)
            {
                UniversalRendererData renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer,rendererPath);
                pipeline=UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name="归潮湾 URP";
                pipeline.msaaSampleCount=4;pipeline.supportsHDR=true;
                pipeline.shadowDistance=150;
                SerializedObject pipelineSettings=new SerializedObject(pipeline);
                pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue=true;
                pipelineSettings.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)LightRenderingMode.PerPixel;
                pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline,pipelinePath);
            }
            GraphicsSettings.renderPipelineAsset=pipeline;
            QualitySettings.renderPipeline=pipeline;
            QualitySettings.vSyncCount=1;
            PlayerSettings.companyName="GuichaoStudio";
            PlayerSettings.productName="归潮湾 - 深海灯塔";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
            SerializedObject graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Deepsea/StylizedSurface","Deepsea/Ocean","Deepsea/SoftLine"})
            {
                Shader shader=Shader.Find(name);
                if(shader==null)throw new InvalidOperationException("Missing shader: "+name);
                bool exists=false;
                for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
                if(!exists){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MainScene,true)};
            AssetDatabase.SaveAssets();
        }

        [MenuItem("深海灯塔/打开归潮湾")]
        public static void OpenBay(){EnsureProject();EditorSceneManager.OpenScene(MainScene);}

        [MenuItem("深海灯塔/构建 Windows 试玩版")]
        public static void BuildWindows()
        {
            EnsureProject();
            string directory=Path.GetFullPath("Builds/Windows");Directory.CreateDirectory(directory);
            var options=new BuildPlayerOptions{scenes=new[]{MainScene},locationPathName=Path.Combine(directory,"GuichaoBay.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};
            var report=BuildPipeline.BuildPlayer(options);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: "+report.summary.result);
            Debug.Log("DEEPSEA_BUILD_OK "+report.summary.outputPath);
        }

        public static void Verify()
        {
            EnsureProject();
            Verification.RunRules();
            ExpeditionVerification.RunRules();
            var scene=EditorSceneManager.OpenScene(MainScene);
            if(scene.GetRootGameObjects().Length!=1||UnityEngine.Object.FindObjectOfType<GameDirector>()==null)
                throw new InvalidOperationException("Main scene has no director");
            if(UnityEngine.Object.FindObjectOfType<GameDirector>().settings==null)
                throw new InvalidOperationException("Main scene settings reference is missing");
            foreach(string name in new[]{"Deepsea/Ocean","Deepsea/StylizedSurface","Deepsea/SoftLine"})
            {
                Shader shader=Shader.Find(name);
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    if(message.severity==ShaderCompilerMessageSeverity.Error)
                        throw new InvalidOperationException(name+": "+message.message);
            }
            Directory.CreateDirectory("Verification");
            File.WriteAllText("Verification/unity-import-result.json","{\"unityVersion\":\""+Application.unityVersion+"\",\"rules\":\"passed\",\"scene\":\"passed\",\"shaderImport\":\"passed\"}");
            Debug.Log("DEEPSEA_IMPORT_OK");
        }
    }
}
