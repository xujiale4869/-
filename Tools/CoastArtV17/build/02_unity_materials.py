from pathlib import Path
import os
P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
S=P/'Assets/Deepsea/Scripts';H=P/'Assets/Deepsea/Shaders'
arch=(H/'CoastArchitecture.shader').read_text(encoding='utf-8-sig').replace('Deepsea/CoastArchitecture','Deepsea/CoastRefinedArchitecture')
arch=arch.replace('_BaseMap("Base colour"','_StoneMap("Exposed limestone",2D)="white"{} _WeatherRole("Plaster Stone Metal",Float)=0\n  _BaseMap("Base colour"')
arch=arch.replace('TEXTURE2D(_BaseMap);','TEXTURE2D(_StoneMap);SAMPLER(sampler_StoneMap);float _ShowcaseClay;\n float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}\n float Noise(float2 p){float2 k=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(k),Hash(k+float2(1,0)),f.x),lerp(Hash(k+float2(0,1)),Hash(k+1),f.x),f.y);}\n float Grain(float3 p){return Noise(p.xz+p.y*.37)*.64+Noise(p.xy*2.1+p.z*.4)*.24+Noise(p.zy*5.1)*.12;}\n TEXTURE2D(_BaseMap);')
arch=arch.replace('float _TextureWeight,','float _WeatherRole,_TextureWeight,')
arch=arch.replace('half4 colour:COLOR;};\n V Vert','half4 colour:COLOR;float3 local:TEXCOORD5;};\n V Vert')
arch=arch.replace('o.colour=a.colour;return o;','o.colour=a.colour;o.local=a.p.xyz;return o;')
arch=arch.replace('InputData input=(InputData)0;', '''float3 p=i.local;float weather=Grain(p*2.5);
  if(_WeatherRole<.5){
   float lower=1-smoothstep(.65,1.9,p.y);float chip=smoothstep(.57,.76,weather)*(.10+.90*lower);
   half3 stone=SAMPLE_TEXTURE2D(_StoneMap,sampler_StoneMap,i.uv*1.7).rgb;stone=lerp(dot(stone,half3(.299,.587,.114)).xxx,stone,.2)*half3(.64,.66,.61);
   surface.albedo=lerp(surface.albedo,stone,chip*.88);
   float dripShape=(.45+.55*Noise(float2(p.x*95,p.y*.33)))*(1-smoothstep(.12,.35,abs(p.x+.03*sin(p.y*2))));
   float dripY=max((1-smoothstep(1.83,1.98,p.y))*smoothstep(.72,1.85,p.y),(1-smoothstep(3.98,4.13,p.y))*smoothstep(2.9,4.0,p.y));
   float drip=dripShape*dripY*smoothstep(.9,1.2,p.z);surface.albedo*=1-drip*.28;
   float damp=(1-smoothstep(.45,1.1,p.y))*(.48+.52*weather);surface.albedo*=lerp(half3(1,1,1),half3(.69,.73,.66),damp*.50);surface.smoothness=lerp(.11,.24,damp);surface.occlusion*=1-chip*.07;
  }else if(_WeatherRole<1.5){surface.albedo*=.94+.06*weather;surface.smoothness=.14;}
  else{float oxidation=smoothstep(.48,.78,Grain(p*13));surface.albedo=lerp(surface.albedo,half3(.17,.12,.065),oxidation*.10);surface.smoothness=lerp(surface.smoothness,.21,oxidation);}
  if(_ShowcaseClay>.5){surface.albedo=half3(.52,.52,.52);n=geom;surface.smoothness=.05;surface.metallic=0;}
  InputData input=(InputData)0;''')
(H/'CoastRefinedArchitecture.shader').write_text(arch,encoding='utf-8')
stone=(H/'CoastShowcaseSurface.shader').read_text(encoding='utf-8-sig').replace('Deepsea/CoastShowcaseSurface','Deepsea/CoastRefinedStone')
stone=stone.replace('if(abs(_Mode-8)<.1){','if(abs(_Mode-8)<.1||abs(_Mode-9)<.1||abs(_Mode-11)<.1){')
stone=stone.replace(' float wet=1-smoothstep', ''' if(abs(_Mode-9)<.1){
  float patch=.50+.22*sin(i.w.x*.74+sin(i.w.z*.63)*2)+.17*sin(i.w.z*1.4+i.w.y*.6);
  float moss=smoothstep(.48,.83,geom.y)*smoothstep(.45,.72,patch)*smoothstep(.35,1.3,i.w.y);
  half3 mossColour=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.8).rgb;half g=dot(mossColour,half3(.299,.587,.114));mossColour=lerp(g.xxx,mossColour,.25)*half3(.39,.47,.22);
  albedo=lerp(albedo,mossColour,moss*.38);
  half3 soil=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.31).rgb;soil=lerp(dot(soil,half3(.299,.587,.114)).xxx,soil,.25)*half3(.32,.42,.21);
  albedo=lerp(albedo,soil,saturate(i.c.r)*.63);
  float wetBand=1-smoothstep(.05,.85+patch*.18,i.w.y);albedo*=lerp(half3(1,1,1),half3(.59,.66,.65),wetBand*.72);
  rough=lerp(.70+.24*SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,i.uv).r,.33,wetBand*.85);
 }
 if(abs(_Mode-10)<.1){half3 cover=SAMPLE_TEXTURE2D(_CoverMap,sampler_CoverMap,i.w.xz*.31).rgb;cover=lerp(dot(cover,half3(.299,.587,.114)).xxx,cover,.25)*half3(.32,.42,.21);half3 stone=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.w.xz*.31).rgb;albedo=lerp(cover,stone*.60,i.c.r*.26);rough=.93;}
 if(abs(_Mode-11)<.1){albedo*=half3(.81,.84,.79);rough=.83;}
 float wet=1-smoothstep''')
(H/'CoastRefinedStone.shader').write_text(stone,encoding='utf-8')
builder=(S/'Editor/CoastShowcaseBuild.cs').read_text(encoding='utf-8-sig').replace('CoastShowcaseBuild','CoastRefinementBuild').replace('Resources/CoastShowcase/','Resources/CoastRefinement/').replace('"CoastShowcase/','"CoastRefinement/').replace('CoastShowcaseRuntime','CoastRefinementRuntime').replace('CoastArtShowcase.unity','CoastArtRefined.unity').replace('CoastArtV16/MistCape-ArtSample.exe','CoastArtV17/MistCape-Materials.exe').replace('"Builds/CoastArtV16"','"Builds/CoastArtV17"').replace('独立美术样板 v16','灯塔与岸石精修 v17').replace('coast-art-v16-build','coast-art-v17-build').replace('构建独立海岸场景','构建灯塔岸石精修')
needle='var root=new GameObject("EN_MistCapeShowcase");'
builder=builder.replace(needle,'''var records=new List<Record>();foreach(var rec in data.meshes)if(rec.group!="Land")records.Add(rec);
   var patch=JsonUtility.FromJson<Archive>(File.ReadAllText("Assets/Deepsea/ThirdParty/CoastArtV17/LandPatch.json"));records.AddRange(patch.meshes);
   ImportRefinementTextures();
   var root=new GameObject("EN_MistCapeShowcase");''')
builder=builder.replace('foreach(var r in data.meshes)','foreach(var r in records)').replace('data.meshes.Length+','records.Count+')
builder=builder.replace('   Material("Silver","");','   Material("Silver","");')
builder=builder.replace('   Include("Deepsea/CoastShowcaseSurface");','   Include("Deepsea/CoastRefinedArchitecture");Include("Deepsea/CoastRefinedStone");Include("Deepsea/CoastShowcaseSurface");')
builder=builder.replace('tower.transform.localScale=Vector3.one*.9f;','tower.transform.localScale=Vector3.one*.9f;RefineTower(tower);')
builder=builder.replace('   if(role=="Foam")', '''   if(role=="FocusRock"||role=="ContactSoil"||role=="FootingStone"){
    var scan=Resources.Load<Material>("CoastMaterials/RockSurface");m.shader=Shader.Find("Deepsea/CoastRefinedStone");m.enableInstancing=true;
    m.SetFloat("_Mode",role=="FocusRock"?9:role=="ContactSoil"?10:11);m.SetTexture("_BaseMap",scan.GetTexture("_BaseMap"));m.SetTexture("_BumpMap",scan.GetTexture("_BumpMap"));m.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Rock-Roughness.exr"));m.SetTexture("_CoverMap",Resources.Load<Material>("CoastMaterials/Ground").GetTexture("_CoverMap"));m.SetTexture("_CausticMap",Resources.Load<Texture2D>("CoastMaterials/CausticLight"));m.SetColor("_BaseColor",new Color(.97f,1.00f,.97f));m.SetFloat("_NormalStrength",role=="ContactSoil"?0:.65f);m.SetFloat("_UseMask",0);m.SetFloat("_Roughness",.83f);m.SetFloat("_Metallic",0);m.SetFloat("_Sway",0);m.SetFloat("_Emission",0);EditorUtility.SetDirty(m);return m;
   }
   if(role=="Foam")''')
extra='''  static void ImportRefinementTextures(){
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
'''
builder=builder.replace('  static void Include(string name)',extra+'  static void Include(string name)')
(S/'Editor/CoastRefinementBuild.cs').write_text(builder,encoding='utf-8')
runtime=(S/'Runtime/CoastShowcaseRuntime.cs').read_text(encoding='utf-8-sig').replace('CoastShowcaseRuntime','CoastRefinementRuntime').replace('"CoastShowcase/','"CoastRefinement/').replace('coast-art-v16-','coast-art-v17-')
runtime=runtime.replace('if(Input.GetKeyDown(KeyCode.F4))','if(Input.GetKeyDown(KeyCode.Alpha5)){focus=new Vector3(-24.4f,7.1f,4);yaw=-18;pitch=42;size=4.8f;underwater=false;}if(Input.GetKeyDown(KeyCode.Alpha6)){focus=new Vector3(-18,2.7f,7.7f);yaw=-18;pitch=42;size=5.2f;underwater=false;}\n    if(Input.GetKeyDown(KeyCode.F4))')
runtime=runtime.replace('focus=new Vector3(10,-1,-9);size=9;yield return Capture("reef");','focus=new Vector3(10,-1,-9);size=9;yield return Capture("reef");\n   focus=new Vector3(-24.4f,7.1f,4);size=4.8f;yield return Capture("lighthouse");focus=new Vector3(-18,2.7f,7.7f);size=5.2f;yield return Capture("rock-detail");')
runtime=runtime.replace('4 水下   拖动','4 水下   5 灯塔   6 岸石   拖动')
(S/'Runtime/CoastRefinementRuntime.cs').write_text(runtime,encoding='utf-8')
print('REFINEMENT_SOURCES_CREATED')
