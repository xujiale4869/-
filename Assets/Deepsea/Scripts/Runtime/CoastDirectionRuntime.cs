using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
 public sealed class CoastDirectionRuntime:MonoBehaviour {
  GameObject baseline;GameObject[] refinedGeometry;bool originalView;
  Camera view;Transform world,boat;GameObject waterObject;Light sun;Volume volume;CoastShowcaseData data;Mesh fishMesh;Material silver,gold;
  readonly List<string> errors=new List<string>();readonly List<double> frames=new List<double>();
  Vector3 focus=new Vector3(0,-.8f,0),baseBoat=new Vector3(2.4f,-.18f,4.8f);float yaw=-18,pitch=42,size=16,clock;bool grey,ui=true,underwater;
  Font font;int triangleCount;const int SilverFish=144,GoldFish=34;
  bool Check {get{return Has("-showcase-check");}}bool Preview {get{return Has("-showcase-preview");}}
  static bool Has(string s){return Array.IndexOf(Environment.GetCommandLineArgs(),s)>=0;}
  string Evidence {get{var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-review-tag");string tag=i>=0&&i+1<a.Length?a[i+1]:"final";if(tag.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'))throw new Exception("Invalid review tag");return "Verification/coast-art-v18-"+tag;}}
  [Serializable]class Report {public bool passed;public string failure,resolution,gpu,scope;public int triangles,corals,kelp,fish;public double fps,p95Ms,p99Ms;public float renderScale;}
  void Awake(){Application.logMessageReceived+=Log;}
  void Log(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(m);}
  IEnumerator Start(){
   var samplePipeline=Resources.Load<UniversalRenderPipelineAsset>("CoastRefinement/SamplePipeline");if(samplePipeline!=null){GraphicsSettings.renderPipelineAsset=samplePipeline;QualitySettings.renderPipeline=samplePipeline;}
   QualitySettings.vSyncCount=0;Application.targetFrameRate=(Check||Preview)?-1:60;
   var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   if(pipeline!=null){pipeline.renderScale=1;pipeline.msaaSampleCount=4;pipeline.shadowDistance=160;}
   var source=Resources.Load<GameObject>("CoastRefinement/Environment");if(source==null)throw new Exception("Showcase environment missing");
   var authored=GameObject.Find("EN_MistCapeShowcase");world=authored!=null?authored.transform:Instantiate(source).transform;world.name="雾岬岛 · 独立三维美术样板";data=Resources.Load<CoastShowcaseData>("CoastRefinement/SceneData");
   foreach(var f in world.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)triangleCount+=f.sharedMesh.triangles.Length/3;
   var tower=world.Find("HeroLighthouse");if(tower==null){tower=Instantiate(Resources.Load<GameObject>("CoastFoundation/Lighthouse"),world).transform;tower.name="HeroLighthouse";}tower.position=new Vector3(-24.4f,3.8f,4);tower.rotation=Quaternion.Euler(0,180,0);tower.localScale=Vector3.one*.9f;
   boat=world.Find("HeroBoat");if(boat==null){boat=new GameObject("HeroBoat").transform;boat.SetParent(world);Instantiate(Resources.Load<GameObject>("CoastHero/Rowboat"),boat,false);var captain=Instantiate(Resources.Load<GameObject>("CoastHero/Fisherman"),boat,false);captain.transform.localPosition=new Vector3(0,-.32f,.02f);captain.GetComponent<CoastHeroRig>().seated=true;}boat.position=baseBoat;boat.rotation=Quaternion.Euler(0,110,0);boat.localScale=Vector3.one*1.25f;
   var authoredWater=world.Find("ShowcaseWater");waterObject=authoredWater!=null?authoredWater.gameObject:new GameObject("ShowcaseWater");if(authoredWater==null){waterObject.transform.SetParent(world);var water=new Mesh{name="Showcase water"};water.vertices=new[]{new Vector3(-150,-.22f,-120),new Vector3(-150,-.22f,150),new Vector3(150,-.22f,150),new Vector3(150,-.22f,-120)};water.triangles=new[]{0,1,2,0,2,3};water.RecalculateBounds();waterObject.AddComponent<MeshFilter>().sharedMesh=water;var wr=waterObject.AddComponent<MeshRenderer>();wr.sharedMaterial=Resources.Load<Material>("CoastMaterials/Water");wr.shadowCastingMode=ShadowCastingMode.Off;}
   var cameraObject=GameObject.Find("ShowcaseCamera");if(cameraObject==null)cameraObject=new GameObject("ShowcaseCamera");view=cameraObject.GetComponent<Camera>();if(view==null)view=cameraObject.AddComponent<Camera>();view.tag="MainCamera";view.allowHDR=true;view.backgroundColor=new Color(.10f,.20f,.25f);view.clearFlags=CameraClearFlags.SolidColor;view.nearClipPlane=.15f;view.farClipPlane=350;
   var additional=view.GetUniversalAdditionalCameraData();additional.requiresColorTexture=true;additional.requiresDepthTexture=true;additional.renderPostProcessing=true;view.allowMSAA=true;
   var lightObject=GameObject.Find("ShowcaseSun");if(lightObject==null)lightObject=new GameObject("ShowcaseSun");sun=lightObject.GetComponent<Light>();if(sun==null)sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowBias=.018f;sun.shadowNormalBias=.10f;
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.23f,.34f,.43f);RenderSettings.ambientEquatorColor=new Color(.16f,.23f,.24f);RenderSettings.ambientGroundColor=new Color(.09f,.12f,.12f);RenderSettings.fog=false;
   var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.15f,.20f,.23f));RenderSettings.ambientProbe=probe;
   var lookObject=GameObject.Find("ShowcaseLook");if(lookObject==null)lookObject=new GameObject("ShowcaseLook");volume=lookObject.GetComponent<Volume>();if(volume==null)volume=lookObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=200;volume.sharedProfile=Resources.Load<VolumeProfile>("CoastRefinement/Look");
   Shader.SetGlobalFloat("_CoastShelfEnabled",0);Shader.SetGlobalFloat("_CoastWaterDetails",1);Shader.SetGlobalFloat("_CoastWaterDepthProbe",0);Shader.SetGlobalFloat("_ShowcaseClay",0);
   fishMesh=MakeFish();silver=Resources.Load<Material>("CoastRefinement/Silver");gold=Resources.Load<Material>("CoastRefinement/GoldFish");
   PrepareComparison();font=Font.CreateDynamicFontFromOSFont("Microsoft YaHei",24);SetGrey(false);ApplyCamera();
   foreach(var r in world.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m==null||!m.shader.isSupported)errors.Add("Unsupported material on "+r.name);
   Debug.Log("SHOWCASE_READY sourceTriangles="+data.sourceTriangles+" coral="+data.corals+" kelp="+data.kelp+" fish="+(SilverFish+GoldFish)+" originalGameplayLoaded=false");
   if(Check||Preview){yield return Verify();}
  }
  int artLook=1,lastLook=1;
  void PrepareComparison(){SetArtLook(1);}
  void SetArtLook(int look){artLook=look;originalView=look==0;if(look>0)lastLook=look;Shader.SetGlobalFloat("_CoastArtLook",look);}
  void SetComparison(bool original){SetArtLook(original?0:lastLook);}
  void SetGrey(bool value){grey=value;Shader.SetGlobalFloat("_ShowcaseClay",value?1:0);waterObject.SetActive(!value);foreach(var r in world.GetComponentsInChildren<MeshRenderer>())if(r.name.Contains("_Foam_")||r.name.Contains("_Kelp_"))r.enabled=!value;sun.transform.rotation=Quaternion.Euler(value?45:38,-42,0);sun.intensity=value?1.2f:2.4f;sun.color=value?Color.white:new Color(1,.94f,.82f);if(volume!=null)volume.enabled=!value;}
  void Update(){if(view==null)return;clock+=Time.deltaTime;
   if(!Check&&!Preview){
    if(Input.GetKeyDown(KeyCode.Alpha1)){focus=new Vector3(0,-.8f,0);yaw=-18;pitch=42;size=16;underwater=false;}
    if(Input.GetKeyDown(KeyCode.Alpha2)){focus=new Vector3(-17,3,3);yaw=-18;pitch=42;size=8.6f;underwater=false;}
    if(Input.GetKeyDown(KeyCode.Alpha3)){focus=new Vector3(10,-1,-9);yaw=-18;pitch=42;size=9;underwater=false;}
    if(Input.GetKeyDown(KeyCode.Alpha4)){underwater=!underwater;}
    if(Input.GetKeyDown(KeyCode.Alpha5)){focus=new Vector3(-24.4f,7.1f,4);yaw=-18;pitch=42;size=4.8f;underwater=false;}if(Input.GetKeyDown(KeyCode.Alpha6)){focus=new Vector3(-18,2.7f,7.7f);yaw=-18;pitch=42;size=5.2f;underwater=false;}
    if(Input.GetKeyDown(KeyCode.Alpha7))SetArtLook(1);if(Input.GetKeyDown(KeyCode.Alpha8))SetArtLook(2);
    if(Input.GetKeyDown(KeyCode.F7))SetComparison(!originalView);if(Input.GetKeyDown(KeyCode.F4))SetGrey(!grey);if(Input.GetKeyDown(KeyCode.F10))ui=!ui;
    if(Input.GetMouseButton(0)){yaw+=Input.GetAxis("Mouse X")*2.0f;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*1.3f,12,78);}
    size=Mathf.Clamp(size-Input.mouseScrollDelta.y,4,28);
    var right=Quaternion.Euler(0,yaw,0)*Vector3.right;var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
    float speed=(Input.GetKey(KeyCode.LeftShift)?14:6)*Time.deltaTime;
    focus+=(right*Input.GetAxisRaw("Horizontal")+forward*Input.GetAxisRaw("Vertical"))*speed;
    if(Input.GetKey(KeyCode.Q))focus.y-=speed;if(Input.GetKey(KeyCode.E))focus.y+=speed;
   }
   boat.position=baseBoat+new Vector3(Mathf.Sin(clock*.22f)*.9f,Mathf.Sin(clock*1.1f)*.035f,Mathf.Cos(clock*.22f)*.25f);boat.rotation=Quaternion.Euler(Mathf.Sin(clock*.8f)*.8f,110+Mathf.Sin(clock*.3f)*1.8f,Mathf.Sin(clock*.95f)*.65f);
   Shader.SetGlobalVector("_CoastBoat",new Vector4(boat.position.x,boat.position.z,.7f,1));Shader.SetGlobalVector("_CoastBoatHeading",new Vector4(boat.forward.x,boat.forward.z,0,0));ApplyCamera();
  }
  void ApplyCamera(){if(view==null)return;waterObject.SetActive(!grey&&!underwater);view.backgroundColor=underwater?new Color(.018f,.10f,.13f).gamma:new Color(.10f,.20f,.25f);if(underwater){view.fieldOfView=58;view.transform.SetPositionAndRotation(new Vector3(17,-3.6f,-5),Quaternion.Euler(16,-8,0));return;}var q=Quaternion.Euler(pitch,yaw,0);view.orthographic=false;view.fieldOfView=2*Mathf.Atan(size/104)*Mathf.Rad2Deg;view.transform.SetPositionAndRotation(focus-q*Vector3.forward*104,q);}
  Vector3 FishPoint(int i,float phase,bool small){
   if(small){float a=phase+i*2.399963f;return new Vector3(-3+Mathf.Sin(a)*2.4f,-1.4f+Mathf.Sin(i)*.12f,7+Mathf.Cos(a)*3.1f);}
   float t=Mathf.Repeat(phase*.045f+i/(float)SilverFish,1)*data.fishPath.Length;int k=Mathf.FloorToInt(t);var p=Vector3.Lerp(data.fishPath[k%data.fishPath.Length],data.fishPath[(k+1)%data.fishPath.Length],t-k);float r=.14f+(i%5)*.085f;return p+new Vector3(Mathf.Sin(i*2.3f+phase*.3f)*r,Mathf.Cos(i*1.7f)*.16f,Mathf.Cos(i*2.3f+phase*.3f)*r);
  }
  void LateUpdate(){if(view==null||grey)return;DrawFish(SilverFish,false,silver);DrawFish(GoldFish,true,gold);}
  void DrawFish(int count,bool small,Material mat){var matrices=new Matrix4x4[count];for(int i=0;i<count;i++){var p=FishPoint(i,clock,small);var n=FishPoint(i,clock+.04f,small);var dir=n-p;if(dir.sqrMagnitude<.00001f)dir=Vector3.forward;float s=small?.28f:.22f;matrices[i]=Matrix4x4.TRS(p,Quaternion.LookRotation(dir),Vector3.one*s);}Graphics.DrawMeshInstanced(fishMesh,0,mat,matrices,count,null,ShadowCastingMode.Off,true,0,null,LightProbeUsage.Off);}
  static Mesh MakeFish(){
   var v=new List<Vector3>();var t=new List<int>();int sides=10;float[] z={-.35f,-.18f,.02f,.22f,.38f},r={.015f,.11f,.14f,.085f,.01f};
   for(int j=0;j<z.Length;j++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*r[j],Mathf.Sin(a)*r[j]*1.3f,z[j]));}
   for(int j=0;j<z.Length-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;t.AddRange(new[]{a,b,b+sides,a,b+sides,a+sides});}
   int n=v.Count;v.AddRange(new[]{new Vector3(0,0,-.30f),new Vector3(0,.20f,-.51f),new Vector3(0,-.20f,-.51f),new Vector3(0,.15f,.03f),new Vector3(0,.27f,-.12f),new Vector3(0,.04f,-.21f)});t.AddRange(new[]{n,n+1,n+2,n+3,n+4,n+5});
   var mesh=new Mesh{name="PR_ShoalFish_LOD0"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,v.Select(p=>new Vector2(p.z+.5f,p.y+.5f)).ToList());mesh.SetColors(v.Select(p=>Color.white).ToList());mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  IEnumerator Capture(string name){for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();int pink=0,visible=0;foreach(var p in tex.GetPixels32()){if(p.r>245&&p.b>245&&p.g<12)pink++;if(p.r+p.g+p.b>30)visible++;}if(pink>tex.width*tex.height*.001f)errors.Add("Unsupported pixels: "+name);if(visible<tex.width*tex.height*.08f)errors.Add("Blank frame: "+name);File.WriteAllBytes(Evidence+"/"+name+".png",tex.EncodeToPNG());Destroy(tex);}
  [Serializable]class PaletteReport{public bool passed;public string failure,scope,resolution,gpu;public int triangles;public float renderScale;public Report[] looks;}
  IEnumerator Verify(){
   Directory.CreateDirectory(Evidence);Screen.SetResolution(1920,1080,FullScreenMode.Windowed);ui=false;yield return new WaitForSecondsRealtime(2);
   SetArtLook(0);yield return Capture("reference-before");SetArtLook(1);yield return Capture("reference-A");SetArtLook(2);yield return Capture("reference-B");
   focus=new Vector3(-17,3,3);size=8.6f;SetArtLook(0);yield return Capture("shore-before");SetArtLook(1);yield return Capture("shore-A");SetArtLook(2);yield return Capture("shore-B");
   focus=new Vector3(-24.4f,7.1f,4);size=4.8f;SetArtLook(1);yield return Capture("lighthouse-A");SetArtLook(2);yield return Capture("lighthouse-B");
   underwater=true;SetArtLook(1);yield return Capture("underwater-A");SetArtLook(2);yield return Capture("underwater-B");underwater=false;focus=new Vector3(0,-.8f,0);yaw=-18;pitch=42;size=16;
   var reports=new List<Report>();
   for(int look=1;look<=2;look++){
    SetArtLook(look);frames.Clear();var report=new Report{scope=look==1?"A: golden-orange sun / vivid green / cream stone / blue-violet shade":"B: peach sun / teal foliage / rose stone / violet-blue shade",resolution=Screen.width+"x"+Screen.height,gpu=SystemInfo.graphicsDeviceName,triangles=data.sourceTriangles,corals=data.corals,kelp=data.kelp,fish=SilverFish+GoldFish,renderScale=1};
    if(Check){for(int j=0;j<60;j++)yield return new WaitForEndOfFrame();for(int j=0;j<1200;j++){yield return new WaitForEndOfFrame();frames.Add(Time.unscaledDeltaTime*1000.0);}var a=frames.OrderBy(x=>x).ToArray();report.fps=1000/a.Average();report.p95Ms=a[(int)(a.Length*.95)];report.p99Ms=a[(int)(a.Length*.99)];}
    report.passed=errors.Count==0;report.failure=string.Join("; ",errors);reports.Add(report);
   }
   var result=new PaletteReport{passed=errors.Count==0,failure=string.Join("; ",errors),scope="Two actual Unity land colour studies on unchanged v17 meshes. No final shape restyling or gameplay integration claimed. Shared sun/post/water/underwater materials unchanged.",resolution=Screen.width+"x"+Screen.height,gpu=SystemInfo.graphicsDeviceName,triangles=data.sourceTriangles,renderScale=1,looks=reports.ToArray()};
   File.WriteAllText(Evidence+"/result.json",JsonUtility.ToJson(result,true));Debug.Log("PALETTE_CHECK "+JsonUtility.ToJson(result));Application.Quit(result.passed?0:2);
  }
  void OnGUI(){if(view==null||!ui)return;float scale=Screen.width/1920f;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));var style=new GUIStyle(GUI.skin.label){font=font,fontSize=27,normal={textColor=new Color(.94f,.91f,.79f)}};GUI.Label(new Rect(32,26,600,45),originalView?"v17 原配色对照":artLook==1?"配色 A · 金橙童话":"配色 B · 桃金蓝绿",style);if(Time.timeSinceLevelLoad<12){style.fontSize=17;style.normal.textColor=new Color(.82f,.86f,.84f);GUI.Label(new Rect(32,1020,1750,38),"1 全景   2 岸边   3 礁台   4 水下   5 灯塔   6 岸石   拖动观察 / 滚轮缩放   WASD 平移   7 配色A   8 配色B   F7 原配色对照   F4 白模   F10 隐藏文字",style);}GUI.matrix=Matrix4x4.identity;}
  void OnDestroy(){Application.logMessageReceived-=Log;Shader.SetGlobalFloat("_ShowcaseClay",0);Shader.SetGlobalFloat("_CoastArtLook",0);}
 }
}
