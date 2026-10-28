using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea.Editor
{
 public static class CoastMaterialsBuild
 {
  const string Root="Assets/Deepsea/Resources/CoastMaterials/";
  [MenuItem("深海灯塔/第三阶段/构建材质与光照版")]
  public static void Build()
  {
   if(!File.Exists(CoastBlockoutBuild.Scene))throw new Exception("Accepted whitebox scene missing");
   Directory.CreateDirectory(Root);AssetDatabase.Refresh();
   var source=Resources.Load<GameObject>("CoastSlice/coastal_cliff_01");
   if(source==null)throw new Exception("Existing coastal_cliff_01 resource missing");
   Material stone=source.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
   PrepareArtTextures();PrepareVisualRock();
   var sand=Resources.Load<Material>("CoastSlice/Sand");var timber=Resources.Load<Material>("CoastSlice/Timber");
   if(stone.GetTexture("_BaseMap")==null||sand==null||timber==null)throw new Exception("Existing texture inputs missing");
   Texture2D caustic=CausticTexture();Texture2D ripple=RippleTexture();
   Resources.Load<Material>("CoastMaterials/RockSurface").SetTexture("_CausticMap",caustic);
   var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline==null)throw new Exception("URP pipeline missing");
   pipeline.supportsCameraOpaqueTexture=true;pipeline.supportsCameraDepthTexture=true;var pipelineSettings=new SerializedObject(pipeline);var downsample=pipelineSettings.FindProperty("m_OpaqueDownsampling");
   int full=Array.FindIndex(downsample.enumNames,n=>n.Replace(" ","")=="None");if(full<0)throw new Exception("Opaque full resolution setting missing");downsample.enumValueIndex=full;pipelineSettings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
   Material rock=Surface("Rock",new Color(1.0f,1.04f,1.03f),0);
   rock.SetTexture("_CausticMap",caustic);rock.SetTexture("_BaseMap",ArtTexture("seaside_rock_diff_2k.jpg"));rock.SetTexture("_BumpMap",ArtTexture("seaside_rock_nor_gl_2k.jpg"));rock.SetTexture("_MaskMap",ArtTexture("seaside_rock_arm_2k.jpg"));rock.SetFloat("_UseMask",1);rock.SetFloat("_BumpStrength",.42f);rock.SetFloat("_Scale",.27f);rock.SetFloat("_Moss",.56f);rock.SetFloat("_TextureContrast",.82f);
   var ground=Surface("Ground",new Color(.81f,.87f,.86f),1);ground.CopyPropertiesFromMaterial(rock);ground.SetFloat("_Kind",1);ground.SetTexture("_SandMap",sand.GetTexture("_BaseMap"));ground.SetColor("_SandColor",new Color(.88f,.87f,.77f));
   var wood=Surface("Wood",new Color(.71f,.72f,.66f),2);wood.SetTexture("_BaseMap",timber.GetTexture("_BaseMap"));wood.SetFloat("_ObjectMapping",1);wood.SetFloat("_Scale",.55f);wood.SetFloat("_TextureContrast",.55f);
   var plaster=Surface("Plaster",new Color(1.0f,.98f,.92f),4);plaster.SetTexture("_BaseMap",ArtTexture("worn_plaster_wall_diff_2k.jpg"));plaster.SetTexture("_BumpMap",ArtTexture("worn_plaster_wall_nor_gl_2k.jpg"));plaster.SetTexture("_MaskMap",ArtTexture("worn_plaster_wall_arm_2k.jpg"));plaster.SetFloat("_UseMask",1);plaster.SetFloat("_BumpStrength",.16f);plaster.SetFloat("_Scale",.65f);EditorUtility.SetDirty(plaster);Surface("Metal",new Color(.09f,.16f,.17f),3);
   var boatWood=Surface("BoatWood",new Color(.90f,.77f,.57f),2);boatWood.CopyPropertiesFromMaterial(wood);boatWood.SetColor("_BaseColor",new Color(.90f,.77f,.57f));boatWood.SetFloat("_Scale",1.1f);boatWood.SetFloat("_TextureContrast",.78f);EditorUtility.SetDirty(boatWood);
   var paint=Surface("BoatPaint",new Color(.20f,.32f,.31f),2);paint.CopyPropertiesFromMaterial(boatWood);paint.SetColor("_BaseColor",new Color(.20f,.32f,.31f));EditorUtility.SetDirty(paint);
   Surface("Coat",new Color(.10f,.25f,.33f),3);Surface("Skin",new Color(.60f,.42f,.24f),3);Surface("Fish",new Color(.30f,.48f,.53f),3);
   var water=Get("Water","Deepsea/CoastFoundationWater");water.SetColor("_Shallow",new Color(.025f,.36f,.50f));water.SetColor("_Deep",new Color(.008f,.065f,.105f));water.SetTexture("_WaveMap",ripple);water.SetFloat("_Refraction",.0022f);
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"Look.asset");
   if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"Look.asset");}
   Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.ACES);
   ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}color.postExposure.Override(.16f);color.contrast.Override(9);color.saturation.Override(-5);
   EditorUtility.SetDirty(profile);
   foreach(string name in new[]{"Deepsea/CoastFoundation","Deepsea/CoastFoundationWater","Deepsea/CoastEcology","Deepsea/CoastRockSurface"})Include(name);
   foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Root+"Look.asset"))EditorUtility.SetDirty(asset);
   foreach(var m in new[]{rock,ground,wood,water})EditorUtility.SetDirty(m);
   // Flora and fish are instantiated at runtime, so scene-based stripping cannot infer their variants.
   var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
   var instancing=graphics.FindProperty("m_InstancingStripping");
   if(instancing==null)throw new Exception("Instancing stripping setting missing");
   Debug.Log("COAST_INSTANCING_MODES "+string.Join(",",instancing.enumNames));
   int keep=Array.FindIndex(instancing.enumNames,n=>n.Replace(" ","")=="KeepAll");
   if(keep<0)throw new Exception("KeepAll instancing mode missing");
   instancing.enumValueIndex=keep;graphics.ApplyModifiedPropertiesWithoutUndo();
   Debug.Log("COAST_ECOLOGY_KEEP_INSTANCING_VARIANTS");
   AssetDatabase.SaveAssets();PlayerSettings.enableFrameTimingStats=true;
   Directory.CreateDirectory("Builds/CoastArtV6");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CoastBlockoutBuild.Scene},locationPathName="Builds/CoastArtV6/GuichaoBay-ArtV6.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Coast material build failed");
   string prior="Builds/CoastBlockoutV1/ThirdParty",dest="Builds/CoastArtV6/ThirdParty";
   if(Directory.Exists(prior))foreach(var path in Directory.GetFiles(prior,"*",SearchOption.AllDirectories)){var target=dest+path.Substring(prior.Length);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,true);}
   Directory.CreateDirectory(dest+"/CoastArtV6");
   File.Copy(ArtRoot+"SOURCE-LICENSE.txt",dest+"/CoastArtV6/SOURCE-LICENSE.txt",true);
   File.Copy(ArtRoot+"texture-sources.json",dest+"/CoastArtV6/texture-sources.json",true);
   Debug.Log("COAST_MATERIALS_BUILD_OK");
  }
  static void PrepareVisualRock(){
   var source=Resources.Load<GameObject>("CoastSlice/boulder_01");MeshFilter chosen=null;int least=int.MaxValue;
   foreach(var f in source.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh!=null&&f.sharedMesh.triangles.Length<least){chosen=f;least=f.sharedMesh.triangles.Length;}
   if(chosen==null)throw new Exception("Boulder source missing");
   var mesh=UnityEngine.Object.Instantiate(chosen.sharedMesh);if(mesh.uv.Length!=mesh.vertexCount)throw new Exception("Boulder source UV missing");
   var v=mesh.vertices;var matrix=source.transform.worldToLocalMatrix*chosen.transform.localToWorldMatrix;var bounds=new Bounds(matrix.MultiplyPoint3x4(v[0]),Vector3.zero);
   for(int i=0;i<v.Length;i++){v[i]=matrix.MultiplyPoint3x4(v[i]);bounds.Encapsulate(v[i]);}
   var normalize=Matrix4x4.Scale(new Vector3(1/bounds.extents.x,1/bounds.size.y,1/bounds.extents.z))*Matrix4x4.Translate(-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*matrix;
   v=mesh.vertices;var normals=mesh.normals;var normalMatrix=normalize.inverse.transpose;
   for(int i=0;i<v.Length;i++)v[i]=normalize.MultiplyPoint3x4(v[i]);
   for(int i=0;i<normals.Length;i++)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;
   mesh.vertices=v;mesh.normals=normals;
   var reducer=new UnityMeshSimplifier.MeshSimplifier();reducer.Initialize(mesh);reducer.SimplifyMesh(Mathf.Min(1,3000f/(least/3f)));
   var surface=reducer.ToMesh();UnityEngine.Object.DestroyImmediate(mesh);surface.name="UV-preserving coastal boulder surface";surface.RecalculateTangents();surface.RecalculateBounds();
   if(surface.uv.Length!=surface.vertexCount||surface.tangents.Length!=surface.vertexCount)throw new Exception("Reduced rock lost texture coordinates");
   string path=Root+"RockSurface.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)AssetDatabase.CreateAsset(surface,path);else{EditorUtility.CopySerialized(surface,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(surface);surface=existing;}
   var lowReducer=new UnityMeshSimplifier.MeshSimplifier();lowReducer.Initialize(surface);lowReducer.SimplifyMesh(Mathf.Min(1,750f/(surface.triangles.Length/3f)));var low=lowReducer.ToMesh();low.name="UV-preserving coastal boulder small surface";low.RecalculateTangents();low.RecalculateBounds();
   string lowPath=Root+"RockSurfaceLow.asset";var lowExisting=AssetDatabase.LoadAssetAtPath<Mesh>(lowPath);if(lowExisting==null)AssetDatabase.CreateAsset(low,lowPath);else{EditorUtility.CopySerialized(low,lowExisting);EditorUtility.SetDirty(lowExisting);UnityEngine.Object.DestroyImmediate(low);}
   var original=chosen.GetComponent<MeshRenderer>().sharedMaterial;var mat=Get("RockSurface","Deepsea/CoastRockSurface");mat.enableInstancing=true;
   mat.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));mat.SetTexture("_BumpMap",original.GetTexture("_BumpMap"));
   var rough=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Deepsea/ThirdParty/CoastSlice/boulder_01/boulder_01_rough_2k.exr");if(rough==null)throw new Exception("Source rock roughness missing");
   mat.SetTexture("_RoughMap",rough);mat.SetTexture("_CoverMap",Resources.Load<Material>("CoastSlice/Ground").GetTexture("_DetailMap"));mat.SetColor("_Tint",new Color(.95f,1.02f,1.02f));mat.SetFloat("_NormalStrength",.5f);EditorUtility.SetDirty(mat);
   Debug.Log("COAST_VISUAL_ROCK sourceTriangles="+(least/3)+" reducedTriangles="+surface.triangles.Length/3+" vertices="+surface.vertexCount+" uv="+surface.uv.Length);
  }
  const string ArtRoot="Assets/Deepsea/ThirdParty/CoastArtV4/";
  static Texture2D ArtTexture(string name){var t=AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot+name);if(t==null)throw new Exception("Run Download-ArtTextures.ps1: "+name);return t;}
  static void PrepareArtTextures(){
   foreach(string id in new[]{"seaside_rock","worn_plaster_wall"})foreach(string kind in new[]{"diff","nor_gl","arm"}){
    string path=ArtRoot+id+"_"+kind+"_2k.jpg";
    var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new Exception("Missing art texture: "+path);
    importer.textureType=kind=="nor_gl"?TextureImporterType.NormalMap:TextureImporterType.Default;
    importer.sRGBTexture=kind=="diff";importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
   }
   Debug.Log("COAST_ART_TEXTURES_READY 2K diffuse + normal + AO/roughness for seaside rock and worn plaster");
  }
  static Texture2D RippleTexture(){
   const int size=256;string path=Root+"RippleSlopes.png";var tex=new Texture2D(size,size,TextureFormat.RGB24,false,true);var pixels=new Color32[size*size];
   int[] fx={3,5,7,11,13,17,19,23},fy={5,-3,11,-7,17,13,-19,9};
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float sx=0,sy=0;for(int k=0;k<fx.Length;k++){float phase=(x*fx[k]+y*fy[k])/(float)size*Mathf.PI*2+k*1.731f;float amp=.12f/(1+k*.25f);float inv=1/Mathf.Sqrt(fx[k]*fx[k]+fy[k]*fy[k]);sx+=Mathf.Cos(phase)*fx[k]*inv*amp;sy+=Mathf.Cos(phase)*fy[k]*inv*amp;}
    pixels[y*size+x]=new Color(Mathf.Clamp01(.5f+sx),Mathf.Clamp01(.5f+sy),1);
   }
   tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   Debug.Log("COAST_WATER_V6_READY full-resolution refraction + ripple texture + velocity wake");return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Texture2D CausticTexture()
  {
   const int size=512,cells=8;string path=Root+"CausticLight.png";
   var tex=new Texture2D(size,size,TextureFormat.RGB24,false,true);var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float u=x/(float)size*cells,v=y/(float)size*cells;int ix=Mathf.FloorToInt(u),iy=Mathf.FloorToInt(v);float first=99,second=99;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
     int cx=ix+dx,cy=iy+dy,wx=(cx+cells)%cells,wy=(cy+cells)%cells;
     uint h=(uint)(wx*374761393+wy*668265263);h=(h^(h>>13))*1274126177u;
     float px=cx+.18f+(h&65535)/65535f*.64f,py=cy+.18f+((h>>16)&65535)/65535f*.64f;
     float d=(u-px)*(u-px)+(v-py)*(v-py);
     if(d<first){second=first;first=d;}else if(d<second)second=d;
    }
    float edge=Mathf.Sqrt(second)-Mathf.Sqrt(first);byte light=(byte)(Mathf.Pow(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.13f)),2)*255);
    pixels[y*size+x]=new Color32(light,light,light,255);
   }
   tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Material Surface(string name,Color color,float kind)
  {var m=Get(name,"Deepsea/CoastFoundation");m.SetColor("_BaseColor",color);m.SetFloat("_Kind",kind);m.SetFloat("_BumpStrength",.19f);m.SetFloat("_Scale",.28f);EditorUtility.SetDirty(m);return m;}
  static Material Get(string name,string shader)
  {string path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}else m.shader=Shader.Find(shader);return m;}
  static void Include(string name)
  {
   var shader=Shader.Find(name);if(shader==null)throw new Exception("Shader missing "+name);
   foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==ShaderCompilerMessageSeverity.Error)throw new Exception(name+": "+message.message);
   var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=graphics.FindProperty("m_AlwaysIncludedShaders");
   for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)return;
   int n=list.arraySize;list.InsertArrayElementAtIndex(n);list.GetArrayElementAtIndex(n).objectReferenceValue=shader;graphics.ApplyModifiedPropertiesWithoutUndo();
  }
 }
}
