using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea.Editor {
 public static class CoastRefinementBuild {
  const string Root="Assets/Deepsea/Resources/CoastRefinement/";
  const string Source="Assets/Deepsea/ThirdParty/CoastArtV16/";
  [Serializable]class Archive{public Record[] meshes;public Vector3[] fishPath;public int corals,kelp;}
  [Serializable]class Record{public string name,material,group;public Vector3[] vertices,normals;public Vector2[] uv;public Color[] colors;public int[] triangles;}
  struct Corner:IEquatable<Corner>{public Vector3 p,n;public Vector2 uv;public Color c;public bool Equals(Corner b){return p.Equals(b.p)&&n.Equals(b.n)&&uv.Equals(b.uv)&&c.Equals(b.c);}public override bool Equals(object b){return b is Corner&&Equals((Corner)b);}public override int GetHashCode(){unchecked{return ((p.GetHashCode()*397^n.GetHashCode())*397^uv.GetHashCode())*397^c.GetHashCode();}}}
  static Mesh Indexed(Record r){
   var map=new Dictionary<Corner,int>();var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var remap=new int[r.vertices.Length];
   for(int i=0;i<r.vertices.Length;i++){var key=new Corner{p=r.vertices[i],n=r.normals[i],uv=r.uv[i],c=r.colors[i]};int found;if(!map.TryGetValue(key,out found)){found=positions.Count;map.Add(key,found);positions.Add(key.p);normals.Add(key.n);uv.Add(key.uv);colors.Add(key.c);}remap[i]=found;}
   var indices=new int[r.triangles.Length];for(int i=0;i<indices.Length;i++)indices[i]=remap[r.triangles[i]];
   var mesh=new Mesh{name=r.name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  [MenuItem("深海灯塔/美术样板/构建灯塔岸石精修")]
  public static void Build(){
   if(!File.Exists(Source+"CoastalShowcase.json"))throw new Exception("Run v16 Blender scene build first");
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   string pine=Root+"CoastalPine-Atlas.png";File.Copy(Source+"CoastalPine-Atlas.png",pine,true);AssetDatabase.ImportAsset(pine);
   var importer=AssetImporter.GetAtPath(pine) as TextureImporter;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.4f;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
   string timber=Root+"CoastalTimber-Albedo.png";File.Copy(Source+"CoastalTimber-Albedo.png",timber,true);AssetDatabase.ImportAsset(timber);
   var timberImport=AssetImporter.GetAtPath(timber) as TextureImporter;timberImport.sRGBTexture=true;timberImport.mipmapEnabled=true;timberImport.wrapMode=TextureWrapMode.Repeat;timberImport.filterMode=FilterMode.Trilinear;timberImport.anisoLevel=4;timberImport.maxTextureSize=2048;timberImport.textureCompression=TextureImporterCompression.CompressedHQ;timberImport.SaveAndReimport();
   string coralAtlas=Root+"CoralTissue-Atlas.png";File.Copy(Source+"CoralTissue-Atlas.png",coralAtlas,true);AssetDatabase.ImportAsset(coralAtlas);var ci=AssetImporter.GetAtPath(coralAtlas) as TextureImporter;ci.sRGBTexture=true;ci.mipmapEnabled=true;ci.wrapMode=TextureWrapMode.Clamp;ci.filterMode=FilterMode.Trilinear;ci.maxTextureSize=2048;ci.anisoLevel=4;ci.textureCompression=TextureImporterCompression.CompressedHQ;ci.SaveAndReimport();
   var data=JsonUtility.FromJson<Archive>(File.ReadAllText(Source+"CoastalShowcase.json"));if(data==null||data.meshes==null)throw new Exception("Invalid showcase mesh archive");
   var records=new List<Record>();foreach(var rec in data.meshes)if(rec.group!="Land")records.Add(rec);
   var patch=JsonUtility.FromJson<Archive>(File.ReadAllText("Assets/Deepsea/ThirdParty/CoastArtV17/LandPatch.json"));records.AddRange(patch.meshes);
   ImportRefinementTextures();
   var root=new GameObject("EN_MistCapeShowcase");int total=0,sourceVertices=0,indexedVertices=0;
   try {
    foreach(var r in records){
     if(r.vertices.Length!=r.normals.Length||r.uv.Length!=r.vertices.Length||r.colors.Length!=r.vertices.Length)throw new Exception("Mesh channels mismatch: "+r.name);
     var mesh=Indexed(r);sourceVertices+=r.vertices.Length;indexedVertices+=mesh.vertexCount;
     string path=Root+r.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
     var g=new GameObject(r.name);g.transform.SetParent(root.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var render=g.AddComponent<MeshRenderer>();render.sharedMaterial=Material(r.material,r.group);render.shadowCastingMode=ShadowCastingMode.On;render.receiveShadows=true;total+=r.triangles.Length/3;
    }
    PrefabUtility.SaveAsPrefabAsset(root,Root+"Environment.prefab");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
   var sceneData=AssetDatabase.LoadAssetAtPath<CoastShowcaseData>(Root+"SceneData.asset");if(sceneData==null){sceneData=ScriptableObject.CreateInstance<CoastShowcaseData>();AssetDatabase.CreateAsset(sceneData,Root+"SceneData.asset");}sceneData.fishPath=data.fishPath;sceneData.sourceTriangles=total;sceneData.corals=data.corals;sceneData.kelp=data.kelp;EditorUtility.SetDirty(sceneData);
   Material("Silver","");Material("GoldFish","");
   var look=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"Look.asset");if(look==null){look=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(look,Root+"Look.asset");}
   Tonemapping tone;if(!look.TryGet(out tone)){tone=look.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,look);}tone.mode.Override(TonemappingMode.ACES);
   ColorAdjustments color;if(!look.TryGet(out color)){color=look.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,look);}color.postExposure.Override(.12f);color.contrast.Override(12);color.saturation.Override(0);
   Bloom bloom;if(!look.TryGet(out bloom)){bloom=look.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,look);}bloom.intensity.Override(.12f);bloom.threshold.Override(1.2f);EditorUtility.SetDirty(look);
   foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(Root+"Look.asset"))EditorUtility.SetDirty(obj);
   Include("Deepsea/CoastRefinedArchitecture");Include("Deepsea/CoastRefinedStone");Include("Deepsea/CoastShowcaseSurface");Include("Deepsea/ShowcaseFoam");Include("Deepsea/CoastMarine");Include("Deepsea/CoastFoundationWater");Include("Deepsea/CoastArchitecture");Include("Deepsea/CoastHeroCloth");
   AssetDatabase.SaveAssets();
   var samplePipeline=PreparePipeline();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var controller=new GameObject("海岸独立美术样板");controller.AddComponent<CoastRefinementRuntime>();AuthorScene();
   const string scenePath="Assets/Deepsea/Scenes/CoastArtRefined.unity";EditorSceneManager.SaveScene(scene,scenePath);
   PlayerSettings.enableFrameTimingStats=true;Directory.CreateDirectory("Builds/CoastArtV17");
   var previousPipeline=GraphicsSettings.renderPipelineAsset;var previousQuality=QualitySettings.renderPipeline;var previousName=PlayerSettings.productName;BuildReport result;
   try{GraphicsSettings.renderPipelineAsset=samplePipeline;QualitySettings.renderPipeline=samplePipeline;PlayerSettings.productName="深海灯塔 · 灯塔与岸石精修 v17";
    result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName="Builds/CoastArtV17/MistCape-Materials.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   }finally{GraphicsSettings.renderPipelineAsset=previousPipeline;QualitySettings.renderPipeline=previousQuality;PlayerSettings.productName=previousName;AssetDatabase.SaveAssets();}
   if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Independent art sample build failed");
   Directory.CreateDirectory("Verification/coast-art-v17-build");File.WriteAllText("Verification/coast-art-v17-build/mesh-summary.json","{\"sourceTriangles\":"+total+",\"meshBatches\":"+records.Count+",\"corals\":"+data.corals+",\"kelp\":"+data.kelp+",\"sourceCornerVertices\":"+sourceVertices+",\"indexedVertices\":"+indexedVertices+"}");
   Debug.Log("COAST_SHOWCASE_BUILD_OK triangles="+total+" batches="+data.meshes.Length);
  }
  static Material Material(string role,string group){
   string key=group=="LandPlants"?"Fern":group=="Foreground"||group=="ForegroundPine"?"Foreground"+role:role;string path=Root+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Deepsea/CoastShowcaseSurface");if(shader==null)throw new Exception("Showcase surface shader missing");
   if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
   if(role=="FocusRock"||role=="ContactSoil"||role=="FootingStone"){
    var scan=Resources.Load<Material>("CoastMaterials/RockSurface");m.shader=Shader.Find("Deepsea/CoastRefinedStone");m.enableInstancing=true;
    m.SetFloat("_Mode",role=="FocusRock"?9:role=="ContactSoil"?10:11);m.SetTexture("_BaseMap",scan.GetTexture("_BaseMap"));m.SetTexture("_BumpMap",scan.GetTexture("_BumpMap"));m.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Rock-Roughness.exr"));m.SetTexture("_CoverMap",Resources.Load<Material>("CoastMaterials/Ground").GetTexture("_CoverMap"));m.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));m.SetColor("_BaseColor",new Color(.97f,1.00f,.97f));m.SetFloat("_NormalStrength",role=="ContactSoil"?0:.65f);m.SetFloat("_UseMask",0);m.SetFloat("_Roughness",.83f);m.SetFloat("_Metallic",0);m.SetFloat("_Sway",0);m.SetFloat("_Emission",0);EditorUtility.SetDirty(m);return m;
   }
   if(role=="Foam"){m.shader=Shader.Find("Deepsea/ShowcaseFoam");m.SetColor("_Tint",new Color(.74f,.85f,.80f,.65f));EditorUtility.SetDirty(m);return m;}
   if(role=="Kelp"&&group=="Kelp"){m.shader=Shader.Find("Deepsea/CoastMarine");m.CopyPropertiesFromMaterial(Resources.Load<Material>("CoastMarine/Kelp"));EditorUtility.SetDirty(m);return m;}
   m.enableInstancing=true;m.SetTexture("_BaseMap",Texture2D.whiteTexture);m.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));m.SetColor("_BaseColor",Color.white);m.SetFloat("_Mode",7);m.SetFloat("_Scale",.30f);m.SetFloat("_UseMask",0);m.SetFloat("_NormalStrength",0);m.SetFloat("_Roughness",.82f);m.SetFloat("_Metallic",0);m.SetFloat("_Sway",0);m.SetFloat("_Emission",0);m.SetFloat("_Cutoff",.4f);
   var ground=Resources.Load<Material>("CoastMaterials/Ground");var rock=Resources.Load<Material>("CoastMaterials/Rock");
   if(role=="Rock"||role=="RockCliff"){
    var scan=role=="Rock"?Resources.Load<Material>("CoastMaterials/RockSurface"):Resources.Load<GameObject>("CoastSlice/coastal_cliff_01").GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
    m.SetTexture("_BaseMap",scan.GetTexture("_BaseMap"));m.SetTexture("_BumpMap",scan.GetTexture("_BumpMap"));m.SetFloat("_Mode",8);m.SetFloat("_UseMask",0);m.SetFloat("_NormalStrength",.55f);m.SetFloat("_Roughness",.84f);m.SetColor("_BaseColor",new Color(.98f,1.02f,1.02f));
   }else if(role=="Sand"){
    m.SetTexture("_BaseMap",rock.GetTexture("_BaseMap"));m.SetTexture("_BumpMap",rock.GetTexture("_BumpMap"));m.SetTexture("_MaskMap",rock.GetTexture("_MaskMap"));m.SetFloat("_UseMask",1);m.SetFloat("_NormalStrength",.55f);m.SetFloat("_Mode",role=="Sand"?1:0);m.SetColor("_BaseColor",new Color(.94f,.94f,.88f));m.SetTexture("_SandMap",Resources.Load<Material>("CoastSlice/Sand").GetTexture("_BaseMap"));m.SetTexture("_CoverMap",ground.GetTexture("_CoverMap"));
   }else if(role=="Wood"){
    m.SetFloat("_Mode",2);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CoastalTimber-Albedo.png"));m.SetColor("_BaseColor",new Color(.78f,.73f,.64f));m.SetFloat("_Roughness",.78f);
   }else if(role=="Bark"){m.SetFloat("_Mode",3);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Deepsea/ThirdParty/CoastSlice/island_tree_01/island_tree_01_diff_1k.jpg"));m.SetColor("_BaseColor",new Color(.68f,.62f,.51f));}
   else if(role=="Foliage"){m.SetFloat("_Mode",4);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CoastalPine-Atlas.png"));m.SetColor("_BaseColor",new Color(.62f,.77f,.57f));m.SetFloat("_Sway",.035f);m.SetFloat("_Roughness",.90f);}
   else if(role=="Kelp"){m.SetFloat("_Mode",6);m.SetTexture("_BaseMap",Resources.Load<Material>("CoastMarine/Kelp").GetTexture("_BaseMap"));m.SetColor("_BaseColor",new Color(.85f,.97f,.79f));m.SetFloat("_Sway",group=="LandPlants"?.025f:.12f);}
   else if(role.StartsWith("Coral")){
    m.SetFloat("_Mode",5);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"CoralTissue-Atlas.png"));m.SetColor("_BaseColor",new Color(.90f,.92f,.90f));var cell=role=="CoralLavender"?new Vector4(0,.5f,.5f,.5f):role=="CoralGold"?new Vector4(.5f,.5f,.5f,.5f):role=="CoralMint"?new Vector4(0,0,.5f,.5f):new Vector4(.5f,0,.5f,.5f);m.SetVector("_AtlasCell",cell);m.SetFloat("_Roughness",.85f);
   }else if(role=="Iron"){m.SetColor("_BaseColor",new Color(.065f,.075f,.060f));m.SetFloat("_Metallic",.5f);m.SetFloat("_Roughness",.55f);}
   else if(role=="Rope")m.SetColor("_BaseColor",new Color(.46f,.38f,.24f));
   else if(role=="Glow"){m.SetColor("_BaseColor",new Color(1,.57f,.18f));m.SetFloat("_Emission",3.5f);}
   else if(role=="Silver"){m.SetColor("_BaseColor",new Color(.79f,.88f,.90f));m.SetFloat("_Metallic",.35f);m.SetFloat("_Roughness",.26f);}
   else if(role=="GoldFish"){m.SetColor("_BaseColor",new Color(.95f,.64f,.19f));m.SetFloat("_Roughness",.38f);}
   if(group=="Foreground"||group=="ForegroundPine"){var c=m.GetColor("_BaseColor");m.SetColor("_BaseColor",new Color(c.r*.43f,c.g*.48f,c.b*.50f,1));}
   EditorUtility.SetDirty(m);return m;
  }
  static void ImportRefinementTextures(){
   File.Copy("Assets/Deepsea/ThirdParty/CoastArtV17/Limewash-Albedo.png",Root+"Limewash-Albedo.png",true);
   File.Copy("Assets/Deepsea/ThirdParty/CoastSlice/boulder_01/boulder_01_rough_2k.exr",Root+"Rock-Roughness.exr",true);AssetDatabase.Refresh();
   foreach(var name in new[]{"Limewash-Albedo.png","Rock-Roughness.exr"}){var importer=AssetImporter.GetAtPath(Root+name) as TextureImporter;importer.sRGBTexture=!name.EndsWith(".exr");importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();}
  }
  static void RefineTower(GameObject tower){
   foreach(var render in tower.GetComponentsInChildren<MeshRenderer>()){
    var source=render.sharedMaterial;if(source==null)continue;string role=source.name;string path=Root+"Tower_"+role+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}else EditorUtility.CopySerialized(source,mat);
    if(role=="Plaster"||role=="Stone"||role=="Trim"||role=="Iron"||role=="Roof"){
     mat.shader=Shader.Find("Deepsea/CoastRefinedArchitecture");mat.SetFloat("_WeatherRole",role=="Plaster"?0:role=="Stone"||role=="Trim"?1:2);mat.SetTexture("_StoneMap",Resources.Load<Material>("CoastMaterials/RockSurface").GetTexture("_BaseMap"));
     if(role=="Plaster"){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Limewash-Albedo.png"));mat.SetColor("_BaseColor",new Color(.84f,.86f,.83f));mat.SetFloat("_TextureWeight",1);mat.SetFloat("_NormalStrength",.23f);mat.SetFloat("_UseMask",0);mat.SetFloat("_Smoothness",.12f);}
     if(role=="Trim"){var stone=Resources.Load<Material>("CoastFoundation/Stone");mat.SetTexture("_BaseMap",stone.GetTexture("_BaseMap"));mat.SetColor("_BaseColor",new Color(.66f,.68f,.63f));mat.SetFloat("_TextureWeight",.75f);}
    }
    render.sharedMaterial=mat;EditorUtility.SetDirty(mat);
   }
  }
  static void Include(string name){var s=Shader.Find(name);if(s==null)throw new Exception("Shader missing "+name);var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=settings.FindProperty("m_AlwaysIncludedShaders");for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==s)return;list.InsertArrayElementAtIndex(list.arraySize);list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=s;settings.ApplyModifiedPropertiesWithoutUndo();}
  static void AuthorScene(){
   var env=PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CoastRefinement/Environment")) as GameObject;env.name="EN_MistCapeShowcase";
   var tower=PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CoastFoundation/Lighthouse")) as GameObject;tower.name="HeroLighthouse";tower.transform.SetParent(env.transform);tower.transform.position=new Vector3(-24.4f,3.8f,4);tower.transform.rotation=Quaternion.Euler(0,180,0);tower.transform.localScale=Vector3.one*.9f;RefineTower(tower);
   var boat=new GameObject("HeroBoat").transform;boat.SetParent(env.transform);boat.position=new Vector3(2.4f,-.18f,4.8f);boat.rotation=Quaternion.Euler(0,110,0);boat.localScale=Vector3.one*1.25f;
   var hull=PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CoastHero/Rowboat")) as GameObject;hull.transform.SetParent(boat,false);var captain=PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("CoastHero/Fisherman")) as GameObject;captain.transform.SetParent(boat,false);captain.transform.localPosition=new Vector3(0,-.32f,.02f);captain.GetComponent<CoastHeroRig>().seated=true;
   var water=new Mesh{name="Showcase water"};water.vertices=new[]{new Vector3(-150,-.22f,-120),new Vector3(-150,-.22f,150),new Vector3(150,-.22f,150),new Vector3(150,-.22f,-120)};water.triangles=new[]{0,1,2,0,2,3};water.RecalculateBounds();string path=Root+"WaterPlane.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(water,path);else{EditorUtility.CopySerialized(water,old);UnityEngine.Object.DestroyImmediate(water);water=old;EditorUtility.SetDirty(old);}var w=new GameObject("ShowcaseWater");w.transform.SetParent(env.transform);w.AddComponent<MeshFilter>().sharedMesh=water;var wr=w.AddComponent<MeshRenderer>();wr.sharedMaterial=Resources.Load<Material>("CoastMaterials/Water");wr.shadowCastingMode=ShadowCastingMode.Off;
   var cam=new GameObject("ShowcaseCamera").AddComponent<Camera>();cam.tag="MainCamera";cam.allowHDR=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.20f,.25f);cam.nearClipPlane=.15f;cam.farClipPlane=350;cam.fieldOfView=2*Mathf.Atan(16f/104)*Mathf.Rad2Deg;var q=Quaternion.Euler(42,-18,0);cam.transform.SetPositionAndRotation(new Vector3(0,-.8f,0)-q*Vector3.forward*104,q);var cameraData=cam.GetUniversalAdditionalCameraData();cameraData.requiresColorTexture=true;cameraData.requiresDepthTexture=true;cameraData.renderPostProcessing=true;
   var light=new GameObject("ShowcaseSun").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(38,-42,0);light.color=new Color(1,.94f,.82f);light.intensity=2.4f;light.shadows=LightShadows.Soft;light.shadowNormalBias=.10f;
   var volume=new GameObject("ShowcaseLook").AddComponent<Volume>();volume.isGlobal=true;volume.priority=200;volume.sharedProfile=Resources.Load<VolumeProfile>("CoastRefinement/Look");RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.23f,.34f,.43f);RenderSettings.ambientEquatorColor=new Color(.16f,.23f,.24f);RenderSettings.ambientGroundColor=new Color(.09f,.12f,.12f);AssetDatabase.SaveAssets();
  }
  static UniversalRenderPipelineAsset PreparePipeline(){
   var baseline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Deepsea/Config/BayPipeline.asset");
   var originalSettings=new SerializedObject(baseline);var source=originalSettings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
   string rendererPath=Root+"SampleRenderer.asset";var renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
   if(renderer==null){renderer=UnityEngine.Object.Instantiate(source);renderer.name="SampleRenderer";renderer.rendererFeatures.Clear();AssetDatabase.CreateAsset(renderer,rendererPath);}
   var type=typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
   ScriptableRendererFeature ao=null;foreach(var f in renderer.rendererFeatures)if(f!=null&&f.GetType()==type)ao=f;
   if(ao==null){ao=ScriptableObject.CreateInstance(type) as ScriptableRendererFeature;ao.name="Coastal contact shadows";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);}
   var settings=new SerializedObject(ao);var s=settings.FindProperty("m_Settings");s.FindPropertyRelative("Source").enumValueIndex=0;s.FindPropertyRelative("NormalSamples").enumValueIndex=2;s.FindPropertyRelative("AfterOpaque").boolValue=true;s.FindPropertyRelative("Downsample").boolValue=true;s.FindPropertyRelative("Intensity").floatValue=.85f;s.FindPropertyRelative("Radius").floatValue=.65f;s.FindPropertyRelative("Falloff").floatValue=250;s.FindPropertyRelative("DirectLightingStrength").floatValue=.15f;settings.ApplyModifiedPropertiesWithoutUndo();ao.Create();EditorUtility.SetDirty(ao);
   AssetDatabase.SaveAssets();string guid;long localId;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ao,out guid,out localId);var rs=new SerializedObject(renderer);var map=rs.FindProperty("m_RendererFeatureMap");map.arraySize=renderer.rendererFeatures.Count;for(int i=0;i<map.arraySize;i++)map.GetArrayElementAtIndex(i).longValue=localId;rs.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
   string pipelinePath=Root+"SamplePipeline.asset";var p=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);if(p==null){p=UnityEngine.Object.Instantiate(baseline);p.name="SamplePipeline";AssetDatabase.CreateAsset(p,pipelinePath);}
   var ps=new SerializedObject(p);ps.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;ps.FindProperty("m_MainLightShadowmapResolution").intValue=2048;ps.FindProperty("m_ShadowCascadeCount").intValue=2;ps.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(p);AssetDatabase.SaveAssets();return p;
  }
 }
}
