using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
 public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
  int coastCount,reefCount,coralCount,kelpCount,mapleCount;
  public void PrepareWorld(SeaLifeRuntime g,Transform parent){
   game=g;bay=g.GetComponent<SeaBayExpansionAndBuildSpaceV28>();native=Resources.Load<NativeBayHeightfieldV29>("SeaSeamlessV29/NativeHeightfield");if(native==null||native.coveredSamples<100)throw new InvalidOperationException("Missing actual native terrain heightfield");
   worldRoot=new GameObject("V29_ContinuousCoastAndArchipelago").transform;worldRoot.SetParent(parent,false);
   nativeSand=new Material(Resources.Load<Material>("CoastRefinement/Sand")){name="V29_NativeSand"};_owned.Add(nativeSand);
   nativeRock=new Material(Resources.Load<Material>("CoastRefinement/Rock")){name="V29_NativeReefRock"};_owned.Add(nativeRock);
   var budget=Resources.Load<Material>("SeaSurfaceV24/BudgetCoastShowcaseSurface");if(budget!=null){nativeSand.shader=budget.shader;nativeRock.shader=budget.shader;}
   rocks=new Mesh[6];for(int i=0;i<rocks.Length;i++){rocks[i]=Resources.Load<Mesh>("SeaSeamlessV29/Rock"+i);if(rocks[i]==null)throw new InvalidOperationException("Missing normalized native rock "+i);}
   waterFilter=game.world.Find("ShowcaseWater").GetComponent<MeshFilter>();priorWaterMesh=waterFilter.sharedMesh;priorWaterPosition=waterFilter.transform.position;priorWaterScale=waterFilter.transform.localScale;
   waterFilter.transform.localScale=Vector3.one;waterFilter.transform.position=new Vector3(40,SeaLifeRuntime.Sea,10);var water=GridMesh("V29_420m_Water",-210,210,-210,210,240,240,(x,z)=>0);var wb=water.bounds;wb.Expand(Vector3.up*3);water.bounds=wb;waterFilter.sharedMesh=water;
   AddMesh("V29_DeepFloor",GridMesh("V29_DeepFloor",-170,250,-200,220,1,1,(x,z)=>SeaLifeRuntime.Sea-20),nativeSand,Vector3.zero,Vector3.one,false);
   AddMesh("V29_WesternLandContinuation",GridMesh("V29_WesternLandContinuation",-170,-36,-200,220,67,105,HeightAt),nativeSand,Vector3.zero,Vector3.one,true);
   AddMesh("V29_ContinuousContinentalShelf",GridMesh("V29_ContinuousContinentalShelf",-36,145,-105,115,226,275,HeightAt),nativeSand,Vector3.zero,Vector3.one,true);
   PopulateCoastAndBarrier();
  }
        private float BaseArchipelagoHeight(float x, float z)
        {
            float sea = SeaLifeRuntime.Sea; // -0.22f
            float deepAbyss = sea - 13.5f;

            // 1. 延续原版西侧主岛海岸线脉络 (在 z ∈ [-100, 110] 上形成曲折自然的岬角与海湾)
            float mainShoreX = -11.5f
                + 4.2f * Mathf.Sin(z * 0.055f + 0.4f)
                + 2.4f * Mathf.Cos(z * 0.13f - 0.8f)
                - 6.5f * Mathf.Exp(-Mathf.Pow((z + 32f) / 18f, 2f)) // 南侧天然半月内湾凹入
                + 5.2f * Mathf.Exp(-Mathf.Pow((z - 42f) / 16f, 2f)); // 北侧望海岬角向外海探出

            // 在原版核心区 (z ∈ [-16, 20], x < 8) 略微压低新海床，让原版精修岸岩与沙滩 100% 露在最上方，绝不穿模遮挡原场景！
            float corePreserveMask = Mathf.Exp(-Mathf.Pow((x + 6f) / 14f, 4f) - Mathf.Pow((z - 2f) / 18f, 4f));

            // 2. 西岸大陆架缓坡与可建造翡翠浅湾 (从岸线向外海平缓延伸 18~28米，水深控制在 0.65m ~ 2.1m)
            float distFromShore = x - mainShoreX;
            float shelfWidth = 22.0f
                + 8.5f * Mathf.Exp(-Mathf.Pow((z - 4.0f) / 16f, 2f))   // 栈桥正前方天然形成宽阔的扇形浅水沙洲
                + 10.0f * Mathf.Exp(-Mathf.Pow((z + 32f) / 20f, 2f))   // 南侧半月浅水湾台地
                + 7.5f * Mathf.Exp(-Mathf.Pow((z - 34f) / 18f, 2f));   // 北侧珊瑚浅坪

            float normalizedShelf = Mathf.Clamp01(distFromShore / Mathf.Max(8f, shelfWidth));
            // 岸上自然隆起，水下形成宽阔平缓的浅滩台地，再向海沟平滑过渡
            float coastalShelfY;
            if (distFromShore < 0f)
            {
                // 陆地延展区 (南北两侧延伸的新岸坡)
                float landRise = Mathf.Clamp01(-distFromShore / 14f);
                coastalShelfY = Mathf.Lerp(sea + 0.15f, sea + 4.8f, Mathf.SmoothStep(0f, 1f, landRise));
            }
            else
            {
                // 浅湾平缓建造区 (normalizedShelf 0.0 ~ 0.72 保持在 sea - 0.75m 到 sea - 2.1m 的黄金翡翠水深)
                float flatT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedShelf / 0.72f));
                float dropT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normalizedShelf - 0.68f) / 0.32f));
                float shallowY = Mathf.Lerp(sea - 0.55f, sea - 1.95f, flatT);
                coastalShelfY = Mathf.Lerp(shallowY, deepAbyss, dropT);
            }

            // 3. 延续原版右侧蜿蜒深海海沟 (Trench Spine) 与外海屏障礁脉 (Outer Barrier Reef Ridge)
            //    原版海沟位于 x ≈ 8.5m 附近；我们让这条海沟沿 S 型蜿蜒贯穿南北，在海沟东侧自然隆起外海礁脉与群岛浅滩！
            float trenchCenterX = 9.2f + 3.2f * Mathf.Sin(z * 0.075f - 0.3f) + 0.12f * Mathf.Abs(z);
            float outerRidgeX = trenchCenterX + 14.5f + 3.5f * Mathf.Cos(z * 0.06f);

            // 外海屏障礁脉与环礁浅湾群 (x ∈ [20, 78])
            float distToOuterRidge = Mathf.Abs(x - outerRidgeX);
            float ridgeWidth = 13.5f + 5.5f * Mathf.Sin(z * 0.08f + 1.2f);
            float outerRidgeMask = 1.0f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.35f, 1f, distToOuterRidge / ridgeWidth));
            // 在栈桥正东方向 (z ∈ [0, 9]) 留出一条宽约 9m 的天然通海深水航道口，让内湾与外海既相连又有礁脉环抱！
            float channelPass = 1.0f - 0.75f * Mathf.Exp(-Mathf.Pow((z - 4.5f) / 5.2f, 2f));
            float outerRidgeY = Mathf.Lerp(deepAbyss, sea - 1.15f, outerRidgeMask * channelPass);

            // 4. 远东外海星罗群岛与远洋浅滩台地 (x ∈ [45, 92])
            float easternLagoonMask = Mathf.Exp(-Mathf.Pow((x - 56f) / 22f, 2f) - Mathf.Pow((z + 8f) / 38f, 2f));
            float northArchipelagoMask = Mathf.Exp(-Mathf.Pow((x - 48f) / 18f, 2f) - Mathf.Pow((z - 46f) / 24f, 2f));
            float southArchipelagoMask = Mathf.Exp(-Mathf.Pow((x - 42f) / 20f, 2f) - Mathf.Pow((z + 52f) / 24f, 2f));
            float outerIslandsMask = Mathf.Clamp01(easternLagoonMask + northArchipelagoMask * 0.9f + southArchipelagoMask * 0.9f);
            float outerLagoonY = Mathf.Lerp(deepAbyss, sea - 1.30f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .85f, outerIslandsMask)));

            // 5. 多尺度自然海沙波纹与礁盘肌理
            float sandRipple = 0.08f * Mathf.Sin(x * 0.45f + Mathf.Cos(z * 0.35f))
                             + 0.05f * Mathf.Cos(z * 0.55f - x * 0.25f);

            float combinedY = Mathf.Max(coastalShelfY, Mathf.Max(outerRidgeY, outerLagoonY)) + sandRipple;

            // 在原版核心岸线正下方自动微降 0.35m，确保原版精细海床与岸石无任何穿模闪烁
            combinedY -= corePreserveMask * 0.45f;
            float trench = 1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.7f,5.2f,Mathf.Abs(x-trenchCenterX)));
            combinedY = Mathf.Lerp(combinedY, deepAbyss+.4f,trench);
            return Mathf.Clamp(combinedY, deepAbyss, sea + 5.5f);
        }
  Mesh GridMesh(string name,float minX,float maxX,float minZ,float maxZ,int nx,int nz,Func<float,float,float> height){
   var v=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[v.Length];var tr=new int[nx*nz*6];int k=0;
   for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float xx=Mathf.Lerp(minX,maxX,x/(float)nx),zz=Mathf.Lerp(minZ,maxZ,z/(float)nz);int i=z*(nx+1)+x;v[i]=new Vector3(xx,height(xx,zz),zz);uv[i]=new Vector2(xx*.35f,zz*.35f);}
   for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;tr[k++]=a;tr[k++]=c;tr[k++]=b;tr[k++]=b;tr[k++]=c;tr[k++]=d;}
   var m=new Mesh{name=name,indexFormat=v.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.vertices=v;m.uv=uv;m.triangles=tr;m.RecalculateNormals();m.RecalculateBounds();_owned.Add(m);return m;
  }
  public float HeightAt(float x,float z){
   float y=BaseArchipelagoHeight(x,z),ny,d;
   // The native source contains rectangular backdrop tiles as well as the curated
   // core. Fade the sampling authority before those tile boundaries, rather than
   // projecting their cut edges into the new coastline and outer lagoon.
   float core=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,34,Mathf.Abs(z-2))))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(28,42,x)));
   if(native.Probe(x,z,out ny,out d,true)&&d<14){float blend=(1-Mathf.SmoothStep(0,1,d/14))*core;y=Mathf.Lerp(y,ny-.09f,blend);}
   float landMask=Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,24,Mathf.Abs(z-2)))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(ShoreX(z)-5,ShoreX(z)-1,x)));
   if(landMask>0){float support=y;for(int j=0;j<25;j++){float radius=j==0?0:2+(j-1)/8*3,angle=j*45*Mathf.Deg2Rad,sx=x+Mathf.Cos(angle)*radius,sz=z+Mathf.Sin(angle)*radius;if(sx>=0)continue;float top,dist;if(native.Probe(sx,sz,out top,out dist)&&dist<10&&top>SeaLifeRuntime.Sea+.8f)support=Mathf.Max(support,top-.08f-(radius+dist)*.20f);}y=Mathf.Lerp(y,support,landMask);}
   return y;
  }
  GameObject AddMesh(string name,Mesh mesh,Material mat,Vector3 position,Vector3 scale,bool collision,Transform parent=null){var o=new GameObject(name);o.transform.SetParent(parent!=null?parent:worldRoot,false);o.transform.localPosition=position;o.transform.localScale=scale;o.AddComponent<MeshFilter>().sharedMesh=mesh;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;if(collision)o.AddComponent<MeshCollider>().sharedMesh=mesh;return o;}
  float ShoreX(float z){return -11.5f+4.2f*Mathf.Sin(z*.055f+.4f)+2.4f*Mathf.Cos(z*.13f-.8f)-6.5f*Mathf.Exp(-Mathf.Pow((z+32)/18,2))+5.2f*Mathf.Exp(-Mathf.Pow((z-42)/16,2));}
  float VisibleBed(float x,float z){var p=new Vector3(x,20,z);var hits=Physics.RaycastAll(p,Vector3.down,50,~0,QueryTriggerInteraction.Ignore);float y=-100;foreach(var h in hits){if(h.transform.IsChildOf(game.boat)||h.transform.IsChildOf(game.walker))continue;y=Mathf.Max(y,h.point.y);}return y;}
  // Sample the actual visible collision surface: dry rock caps and deep trench
  // positions are rejected, then nearby submerged ledges are searched deterministically.
  bool TryFloraBed(Vector3 center,int seed,float minDepth,out Vector3 point){for(int j=0;j<13;j++){float radius=j==0?0:1.4f+(j-1)*.45f,angle=(seed*137.5f+j*111.25f)*Mathf.Deg2Rad,x=center.x+Mathf.Cos(angle)*radius,z=center.z+Mathf.Sin(angle)*radius,y=VisibleBed(x,z),depth=SeaLifeRuntime.Sea-y;if(depth>=minDepth&&depth<=6.5f){point=new Vector3(x,y,z);return true;}}point=Vector3.zero;return false;}
  void PopulateCoastAndBarrier(){
   var rng=new System.Random(290511);Func<float,float,float> rand=(a,b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());var flora=new List<Vector3>();
   for(int i=0;i<46;i++){bool south=i%2==0;float t=(i/2)/22f,z=Mathf.Lerp(south?-27:28,south?-88:94,t),x=ShoreX(z)+rand(-2,1.3f);float y=HeightAt(x,z);if(y<SeaLifeRuntime.Sea-.6f)y=SeaLifeRuntime.Sea-.6f;var m=rocks[i%6];float height=rand(2.0f,4.1f);var o=AddMesh("V29_CoastalCliff_"+i,m,nativeRock,new Vector3(x,y-.45f,z),new Vector3(rand(3.8f,6.0f),height,rand(3.4f,5.3f)),true);o.transform.localRotation=Quaternion.Euler(0,rand(0,360),0);coastCount++;}
   for(int i=0;i<64;i++){float z=Mathf.Lerp(-85,98,i/63f);if(z>.5f&&z<8.5f)continue;float trench=9.2f+3.2f*Mathf.Sin(z*.075f-.3f)+.12f*Mathf.Abs(z),x=trench+14.5f+3.5f*Mathf.Cos(z*.06f)+rand(-2.5f,3.5f),y=HeightAt(x,z);bool island=i%7==0;float top=island?SeaLifeRuntime.Sea+rand(.55f,1.7f):Mathf.Min(y+rand(.5f,1.2f),SeaLifeRuntime.Sea-.4f);float h=Mathf.Max(.5f,top-y+.15f);var o=AddMesh("V29_BarrierReef_"+i,rocks[(i+2)%6],nativeRock,new Vector3(x,y-.15f,z),new Vector3(rand(3.2f,5.2f),h,rand(3.2f,5.0f)),true);o.transform.localRotation=Quaternion.Euler(0,rand(0,360),0);reefCount++;flora.Add(new Vector3(x,0,z));}
   Physics.SyncTransforms();var reefShader=Resources.Load<Material>("SeaHarborV27/ReefShaderKeep").shader;var palette=Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27");var coralMats=new Material[4];for(int i=0;i<4;i++){var m=new Material(reefShader){name="V29_Coral_"+i,enableInstancing=true};m.SetColor("_BaseColor",Color.white);m.SetTexture("_PaletteMap",palette);m.SetFloat("_PaletteBand",i);m.SetFloat("_SwayAmplitude",.045f);_owned.Add(m);coralMats[i]=m;}
   var kelpMat=new Material(reefShader){name="V29_GreenKelp",enableInstancing=true};kelpMat.SetColor("_BaseColor",Color.white);kelpMat.SetFloat("_IsKelp",1);kelpMat.SetFloat("_SwayAmplitude",.16f);_owned.Add(kelpMat);
   var cm=Resources.Load<Mesh>("SeaHarborV27/Baked/Coral_V27_Cluster");var km=Resources.Load<Mesh>("SeaHarborV27/Baked/Kelp_V27_GiantFrond");int n=0;foreach(var p in flora){Vector3 at;if(coralCount<36&&TryFloraBed(p,n,.80f,out at)){float budget=SeaLifeRuntime.Sea-.30f-at.y,scale=Mathf.Min(rand(.9f,1.7f),budget/Mathf.Max(.1f,cm.bounds.max.y));var c=AddMesh("V29_Coral_"+n,cm,coralMats[n%4],at-Vector3.up*.02f,Vector3.one*scale,false);c.transform.localRotation=Quaternion.Euler(0,rand(0,360),0);coralCount++;}if(n%2==0&&kelpCount<18&&TryFloraBed(p+new Vector3(2.3f,0,1.1f),n+5,1.0f,out at)){float height=Mathf.Min(rand(1.2f,2.0f),SeaLifeRuntime.Sea-.35f-at.y);AddMesh("V29_Kelp_"+n,km,kelpMat,at-Vector3.up*.02f,new Vector3(.8f,height/1.85f,.8f),false);kelpCount++;}n++;}
   // Reuse a complete native maple tree, normalized as a two-material group by the editor.
   Mesh trunk=Resources.Load<Mesh>("SeaSeamlessV29/MapleBark"),leaves=Resources.Load<Mesh>("SeaSeamlessV29/MapleLeaves");if(trunk!=null&&leaves!=null){Material barkMat=Resources.Load<Material>("CoastMaple/MapleBark"),leafMat=Resources.Load<Material>("CoastMaple/MapleLeaf");for(int i=0;i<12;i++){float z=i<6?Mathf.Lerp(-30,-84,i/5f):Mathf.Lerp(31,90,(i-6)/5f),x=ShoreX(z)-rand(4.5f,7),y=HeightAt(x,z);if(y<SeaLifeRuntime.Sea+.2f)continue;var t=new GameObject("V29_RedMaple_"+i).transform;t.SetParent(worldRoot,false);t.localPosition=new Vector3(x,y-.05f,z);t.localRotation=Quaternion.Euler(0,rand(0,360),0);t.localScale=Vector3.one*rand(.85f,1.15f);AddMesh("Bark",trunk,barkMat,Vector3.zero,Vector3.one,false,t);AddMesh("Leaves",leaves,leafMat,Vector3.zero,Vector3.one,false,t);mapleCount++;}}
   Physics.SyncTransforms();
  }
 }
}
