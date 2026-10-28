using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Deepsea
{
    public sealed partial class CoastBlockoutSession
    {
        bool UncappedNavigation {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-whitebox-uncapped-navigation")>=0;}}
        string Evidence { get { return FoundationPreview?"Verification/coast-art-v15-"+FoundationTag:MaterialMode&&UncappedNavigation?"Verification/coast-art-v15-uncapped":MaterialMode?(VisualCheck?"Verification/coast-art-v15-visual":"Verification/coast-art-v15"):(VisualCheck?"Verification/coast-blockout-natural-visual":"Verification/coast-blockout-natural-v3"); } }
        [Serializable]sealed class BlockoutReport
        {
            public bool passed,fullResolutionOpaque,opaqueColourRequested;
            public int shelfChunks,shelfTriangles,shelfVisualRouteSamples,shelfVisualObstructions,deepShoalFish;
            public int foundationTriangles,foundationMaterials,foundationColliders;public int visualGroundTriangles;public float visualGroundPathHeightChange;
            public int reefCorals,reefCaps,reefTriangles,reefRejected,reefRouteSamples,reshapedReefs,generatedReefVariants,generatedReefInstances;public float reefRouteClearance,reefHighestTip;
            public string failure,resolution,gpu,scene,sharedPrefab,navigation,validationScope;
            public int rockSmallSurfaces,rockSurfaces,rockSurfaceTriangles,ecologySaplings;public float rockSurfaceFitError;
            public bool heroMaterialsIntact,heroEnvelopeSafe,captainHiddenWhileWalking,heroGreyComparison;public int heroBoatTriangles,heroCharacterTriangles,heroAddedColliders;
            public int kelpModelTriangles,marineRootsChecked,kelpAnchorsRejected;public float kelpRootGap,kelpRootSink,highestMarineTip;
            public int shoreFinishRocks,shoreFinishTriangles,marineTriangles,marineUvVertices;public float shoreFinishClearance;public bool marineMaterialsSupported;
            public int habitatGrass,habitatShrubs,habitatTriangles,habitatDrawGroups,fernTrianglesPerPlant;public float habitatPathClearance;
            public int artTriangles,artRenderers,artColliders;
            public int terrainTriangles,rockMasses,walkingPoints,cavePathSamples,actualFishBodySamples,scannedRockMeshes,styledRenderers;
            public string appearanceVersion,benchmarkWindow,benchmarkRenderResolution;
            public float benchmarkRenderScale;
            public int navigationFrameLimit,ecologyTrees,ecologyFerns,ecologyGrass,ecologyKelp,ecologyCorals,ecologyFish,ecologyDrawGroups,ecologyRouteSamples,ecologyObstructions;
            public float characterStepOffset;
            public List<string> stairSamples=new List<string>();
            public bool cavePassageClear;
            public float sailingDistance,walkDistance,walkVerticalError;
            public double fps,meanMs,p95Ms,p99Ms;
            public List<string> habitatPerformance=new List<string>();
            public List<string> reached=new List<string>(),depthSamples=new List<string>();
        }
        void Require(bool ok,string message){if(!ok){failure+=message+"; ";Debug.LogWarning("BLOCKOUT_REQUIRE "+message);}}
        IEnumerator Shot(string name)
        {
            bool old=showHud;showHud=false;if(monitor!=null)monitor.enabled=false;
            for(int i=0;i<3;i++)yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();int pink=0,visiblePixels=0;
            foreach(var p in tex.GetPixels32()){if(p.r>245&&p.b>245&&p.g<12)pink++;if(p.r+p.g+p.b>24)visiblePixels++;}
            Require(pink<tex.width*tex.height*.001f,"Unsupported material pixels: "+name);
            Require(visiblePixels>tex.width*tex.height*.05f,"Blank render capture: "+name);
            File.WriteAllBytes(Evidence+"/"+name+".png",tex.EncodeToPNG());Destroy(tex);
            showHud=old;if(monitor!=null)monitor.enabled=old;
        }
        IEnumerator RunCheck()
        {
            if(FoundationPreview){yield return RunFoundationPreview();yield break;}
            Directory.CreateDirectory(Evidence);Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            var report=new BlockoutReport{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,sharedPrefab=region.layoutVersion,terrainTriangles=region.terrainTriangles,rockMasses=region.rockMasses,gpu=SystemInfo.graphicsDeviceName,appearanceVersion=MaterialMode?"CoastMaterials-Stage3-v15-CoastalLandform":"Accepted-whitebox-v3",styledRenderers=appearance!=null?appearance.StyledRenderers:0,validationScope=VisualCheck?"Rendering only; controller/physics acceptance is recorded separately in "+(MaterialMode?"coast-art-v15":"coast-blockout-natural-v3"):"Rendering, timed frame sample, original boat controller and full walking/boarding course"};
            if(MaterialMode){var foundation=GetComponent<CoastFoundationAssets>();Require(foundation!=null&&foundation.Ready,"Foundation architecture missing");if(foundation!=null){report.foundationTriangles=foundation.Triangles;report.foundationMaterials=foundation.MaterialGroups;report.foundationColliders=foundation.AddedColliders;Require(foundation.Triangles>10000&&foundation.Triangles<=30000&&foundation.MaterialGroups==8&&foundation.AddedColliders==0,"Foundation architecture budget or collider invalid");}}
            foreach(var f in region.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null&&f.sharedMesh.name.Contains("扫描形体"))report.scannedRockMeshes++;
            if(MaterialMode){report.visualGroundTriangles=CoastGroundSmoothing.VisualTriangles;report.visualGroundPathHeightChange=CoastGroundSmoothing.MaxDryDeviation;
             Require(report.visualGroundTriangles>74992&&report.visualGroundTriangles<=310000,"Visual ground tessellation invalid");
             Require(report.visualGroundPathHeightChange<.12f,"Visual shore intrudes land walking course");
             foreach(var f in region.GetComponentsInChildren<MeshFilter>())if(f.name.StartsWith("地形块")){var c=f.GetComponent<MeshCollider>();Require(c!=null&&c.sharedMesh!=f.sharedMesh,"Original ground collision separation lost");}}
            var surface=GetComponent<CoastRockSurfaces>();
            if(MaterialMode){Require(surface!=null&&surface.Ready,"Rock surfaces missing");if(surface!=null){report.rockSmallSurfaces=surface.SmallSurfaces;report.rockSurfaces=surface.Surfaces;report.rockSurfaceTriangles=surface.Triangles;report.rockSurfaceFitError=surface.FitError;Require(surface.Surfaces==77&&surface.FitError<.004f,"Rock surface alignment invalid");}}
            var shelf=GetComponent<CoastShelfLandform>();if(MaterialMode){Require(shelf!=null&&shelf.Ready,"Continuous shelf missing");if(shelf!=null){report.shelfChunks=shelf.Chunks;report.shelfTriangles=shelf.Triangles;Require(shelf.Chunks==8&&shelf.Triangles<=60000&&shelf.AddedColliders==0,"Shelf geometry budget or collision changed");}}
            var finish=GetComponent<CoastShoreFinish>();if(MaterialMode){Require(finish!=null&&finish.Ready,"Shore finish missing");if(finish!=null){report.shoreFinishRocks=finish.RockCount;report.shoreFinishTriangles=finish.Triangles;report.shoreFinishClearance=finish.MinimumVisualPathClearance;Require(finish.RockCount>=12&&finish.Triangles<200000,"Shore finish missing or over budget");Require(finish.MinimumVisualPathClearance>=2.6f&&finish.AddedColliders==0,"Shore finish blocks path");}}
            var canopy=GetComponent<CoastReefCanopy>();
            if(MaterialMode&&CoastEcology.Requested){Require(canopy!=null&&canopy.Ready,"Reef canopy unavailable");if(canopy!=null){
                report.reefCorals=canopy.Corals;report.reefCaps=canopy.RockCaps;report.reefTriangles=canopy.Triangles;report.reefRejected=canopy.Rejected;report.reefRouteSamples=canopy.RouteSamples;report.reefRouteClearance=canopy.MinimumRouteClearance;report.reefHighestTip=canopy.HighestTip;
                Require(canopy.Corals>=100&&canopy.RockCaps>=8,"Reef canopy too sparse");Require(canopy.Triangles<=600000,"Reef canopy over triangle budget");Require(canopy.MinimumRouteClearance>=.72f&&canopy.HighestTip<-.40f&&canopy.AddedColliders==0,"Reef canopy obstructs routes or breaches the water");
            }if(surface!=null){report.reshapedReefs=surface.ReshapedReefs;report.generatedReefVariants=surface.GeneratedVariants;report.generatedReefInstances=surface.GeneratedInstances;Require(surface.ReshapedReefs>=20&&surface.GeneratedVariants==6&&surface.Triangles<=600000,"Generated reef library missing or over budget");}}
            var hero=GetComponent<CoastHeroVisuals>();if(MaterialMode){Require(hero!=null&&hero.Ready,"Hero visual layer missing");if(hero!=null){report.heroBoatTriangles=hero.BoatTriangles;report.heroCharacterTriangles=hero.CharacterTriangles;report.heroAddedColliders=hero.AddedColliders;report.heroEnvelopeSafe=hero.EnvelopeSafe;Require(hero.MaterialsSupported,"Hero shader unsupported");Require(hero.AddedColliders==0&&hero.EnvelopeSafe,"Hero layer changed physical envelope");}}
            var art=GetComponent<CoastArtDetails>();
            if(MaterialMode){Require(art!=null&&art.Ready,"Art details missing");if(art!=null){report.artTriangles=art.Triangles;report.artRenderers=art.Renderers;report.artColliders=art.AddedColliders;Require(art.AddedColliders==0,"Art must not add collision");}}
            if(MaterialMode){var data=game.Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();report.opaqueColourRequested=data.requiresColorTexture;var waterPipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;report.fullResolutionOpaque=waterPipeline!=null&&waterPipeline.opaqueDownsampling==UnityEngine.Rendering.Universal.Downsampling.None;Require(report.opaqueColourRequested&&report.fullResolutionOpaque,"Refraction colour buffer not configured");}
            var habitat=GetComponent<CoastHabitat>();if(MaterialMode&&CoastEcology.Requested){Require(habitat!=null&&habitat.Ready,"Habitat layer missing");if(habitat!=null){report.habitatGrass=habitat.Grass;report.habitatShrubs=habitat.Shrubs;report.habitatTriangles=habitat.Triangles;report.habitatDrawGroups=habitat.DrawGroups;report.habitatPathClearance=habitat.MinimumPathClearance;Require(habitat.Grass>=120&&habitat.Shrubs>=12,"Land habitat too sparse");Require(habitat.MinimumPathClearance>2.05f&&habitat.AddedColliders==0,"Habitat intrudes walking strip");}}
            var eco=GetComponent<CoastEcology>();
            if(CoastEcology.Requested){
                Require(eco!=null&&eco.Ready,"Ecology layer not ready");
                if(eco!=null){report.deepShoalFish=eco.DeepFishCount;Require(eco.DeepFishCount>=80,"Distant shoal unavailable");report.shelfVisualRouteSamples=eco.VisualRouteSamples;report.shelfVisualObstructions=eco.VisualRouteObstructions;Require(eco.VisualRouteSamples>0&&eco.VisualRouteObstructions==0,"Visible shelf blocks fish route");report.kelpAnchorsRejected=eco.KelpAnchorsRejected;report.kelpModelTriangles=eco.KelpModelTriangles;report.marineRootsChecked=eco.MarineRootsChecked;report.kelpRootGap=eco.MaximumKelpRootGap;report.kelpRootSink=eco.MaximumKelpRootSink;report.highestMarineTip=eco.HighestUnderwaterTip;Require(eco.KelpModelTriangles>0&&eco.KelpModelTriangles<=1000,"Kelp ribbon model missing or over budget");Require(eco.MarineRootsChecked==eco.KelpClumps&&eco.MaximumKelpRootGap<.12f&&eco.MaximumKelpRootSink<.20f,"Kelp holdfast does not meet seabed");Require(eco.HighestUnderwaterTip<-.40f,"Marine plants breach waterline");report.marineMaterialsSupported=eco.MarineMaterialsSupported;report.marineTriangles=eco.MarineTriangles;report.marineUvVertices=eco.MarineUvVertices;Require(eco.MarineMaterialsSupported&&eco.MarineUvVertices>1000,"Textured marine surface missing");report.fernTrianglesPerPlant=eco.FernTrianglesPerPlant;Require(eco.FernTrianglesPerPlant>0&&eco.FernTrianglesPerPlant<=1500,"Fern mesh budget exceeded");report.ecologySaplings=eco.Saplings;report.ecologyTrees=eco.Trees;report.ecologyFerns=eco.Ferns;report.ecologyGrass=eco.GrassTufts;report.ecologyKelp=eco.KelpClumps;report.ecologyCorals=eco.Corals;report.ecologyFish=eco.FishCount;report.ecologyDrawGroups=eco.DrawGroups;report.ecologyRouteSamples=eco.RouteSamples;report.ecologyObstructions=eco.RouteObstructions;
                Require(eco.Trees>=3&&eco.Ferns>=25,"Coastal flora missing");Require(eco.GrassTufts>=40&&eco.KelpClumps>=15&&eco.Corals>=20,"Underwater habitat missing");Require(eco.FishCount>=60&&eco.RouteObstructions==0,"Fish diversity or clear route missing");Require(eco.AddedColliders==0,"Visual ecology must not alter collision");}
            }
            var explorer=game.Expedition.Explorer;var boat=game.Boat;
            report.characterStepOffset=explorer.GetComponent<CharacterController>().stepOffset;
            foreach(var p in new[]{new Vector2(29.25f,96.64f),new Vector2(28.85f,96.72f),new Vector2(28.6f,96.8f),new Vector2(28.15f,96.95f)}){
                var stairHits=Physics.RaycastAll(new Vector3(p.x,10,p.y),Vector3.down,12).Where(h=>h.collider.transform.IsChildOf(region.transform)).OrderBy(h=>h.distance).ToArray();
                if(stairHits.Length>0)report.stairSamples.Add(p+" height="+stairHits[0].point.y+" collider="+stairHits[0].collider.name);
            }
            var body=boat.GetComponent<Rigidbody>();
            boat.AutomationInput=true;boat.AutomationThrottle=0;boat.AutomationRudder=0;
            explorer.AutomationInput=true;explorer.AutomationDirection=Vector3.zero;
            Require(GameDirector.VerificationMode,"Save isolation missing");
            foreach(var r in region.GetComponentsInChildren<MeshRenderer>())
            {Require(r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported,"Missing or unsupported material: "+r.name);if(MaterialMode)Require(appearance!=null&&appearance.Owns(r),"Unstyled material renderer: "+r.name);else if(r.transform!=region.water)Require(r.sharedMaterial==region.clay,"Mixed solid materials: "+r.name);}
            foreach(var p in new[]{new Vector2(55,94),new Vector2(CoastBlockoutLayout.Spine(98),98),new Vector2((CoastBlockoutLayout.Spine(82)+CoastBlockoutLayout.FarSpine(82))*.5f,82)})
            {
                float h=CoastBlockoutLayout.Height(p.x,p.y);
                var hits=Physics.RaycastAll(new Vector3(p.x,20,p.y),Vector3.down,70).Where(hit=>hit.collider.transform.IsChildOf(region.transform)).OrderBy(hit=>hit.distance).ToArray();
                bool ground=hits.Length>0;
                Require(ground,"Missing physical sea floor at "+p);
                if(ground){var hit=hits[0];Require(Mathf.Abs(hit.point.y-h)<.55f,"Sea floor depth mismatch at "+p);report.depthSamples.Add(p+" layout="+h+" physical="+hit.point.y+" collider="+hit.collider.name);}
            }
            // Inspect the ACTUAL fish route against all new geometry, including tunnel walls.
            report.cavePassageClear=true;
            for(int i=0;i<160;i++)
            {
                Vector3 p=CoastBlockoutSchool.Path(i/160f);
                foreach(var hit in Physics.OverlapSphere(p,.85f))if(hit.transform.IsChildOf(region.transform))
                {report.cavePassageClear=false;Require(false,"Fish route obstructed at "+p+" by "+hit.name);break;}
                // Verify each fish's actual spread and body envelope, not just the centreline.
                for(int fish=0;fish<CoastBlockoutSchool.Fish;fish++)
                {
                    Vector3 forward;Vector3 bodyPoint=CoastBlockoutSchool.FishPosition(i/160f,fish,out forward);
                    foreach(var hit in Physics.OverlapSphere(bodyPoint,.56f))if(hit.transform.IsChildOf(region.transform))
                    {report.cavePassageClear=false;Require(false,"Fish body obstructed at "+bodyPoint+" by "+hit.name);break;}
                    report.actualFishBodySamples++;
                }
                report.cavePathSamples++;
            }
            // A visible opening also needs a solid roof and side wall, not missing mesh faces.
            Vector3 middle=CoastBlockoutLayout.CaveFocus;
            Require(Physics.RaycastAll(middle,Vector3.up,12).Any(h=>h.collider.name=="鱼群贯通岩洞"),"Cave ceiling missing");
            Require(Physics.RaycastAll(middle,Vector3.Cross(Vector3.up,CoastBlockoutLayout.CaveAxis),8).Any(h=>h.collider.name=="鱼群贯通岩洞"),"Cave inner side wall missing");
            referenceView=true;weakPerspective=true;focus=region.referenceFocus.position;ApplyCamera(true);
            for(int i=0;i<45;i++)yield return new WaitForEndOfFrame();
            if(MaterialMode){report.heroMaterialsIntact=hero!=null&&hero.MaterialsIntact;Require(report.heroMaterialsIntact,"Hero material slots overwritten");}if(MaterialMode){region.referenceFocus.position-=new Vector3(1.8f,-.5f,-2.8f);ApplyCamera(true);yield return Shot("reference-baseline");region.referenceFocus.position+=new Vector3(1.8f,-.5f,-2.8f);ApplyCamera(true);}yield return Shot("reference");
            if(canopy!=null){canopy.Visible=false;yield return Shot("reef-canopy-off");canopy.Visible=true;}
            if(finish!=null){finish.SetVisible(false);yield return Shot("shore-finish-off");finish.SetVisible(true);Vector3 oldFocus=region.referenceFocus.position;float oldSize=viewSize;region.referenceFocus.position=new Vector3(27,3,106);viewSize=8.6f;ApplyCamera(true);yield return Shot("shore-close");region.referenceFocus.position=new Vector3(51,-2,105);viewSize=7.5f;ApplyCamera(true);yield return Shot("reef-close");region.referenceFocus.position=oldFocus;viewSize=oldSize;ApplyCamera(true);}
            if(habitat!=null){habitat.SetEnabled(false);yield return Shot("habitat-off");habitat.SetEnabled(true);Vector3 oldFocus=region.referenceFocus.position;float oldSize=viewSize;region.referenceFocus.position=new Vector3(28,3.0f,101);viewSize=8.6f;ApplyCamera(true);yield return Shot("habitat-close");region.referenceFocus.position=oldFocus;viewSize=oldSize;ApplyCamera(true);}
            if(MaterialMode){Vector3 oldFocus=region.referenceFocus.position;float oldSize=viewSize;region.referenceFocus.position=game.Boat.transform.position+Vector3.up*.55f;viewSize=4.4f;ApplyCamera(true);yield return Shot("boat-close");region.referenceFocus.position=oldFocus;viewSize=oldSize;ApplyCamera(true);}
            if(eco!=null){eco.Visible=false;yield return Shot("ecology-off");eco.Visible=true;}
            if(MaterialMode){Shader.SetGlobalFloat("_CoastWaterDetails",0);yield return Shot("water-details-off");Shader.SetGlobalFloat("_CoastWaterDetails",1);yield return new WaitForSeconds(1.5f);yield return Shot("water-motion");}
            if(MaterialMode)
            {
                // Inspect the sampled opaque depth in the standalone renderer, rather than
                // assuming the transparent material is seeing the camera depth texture.
                var cameraData=game.Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                bool post=cameraData.renderPostProcessing;cameraData.renderPostProcessing=false;
                Shader.SetGlobalFloat("_CoastWaterDepthProbe",1);yield return Shot("water-depth");
                Shader.SetGlobalFloat("_CoastWaterDepthProbe",0);cameraData.renderPostProcessing=post;
            }
            if(MaterialMode){appearance.SetGrey(true);yield return Shot("grey-comparison");report.heroGreyComparison=hero!=null&&!hero.Visible;Require(report.heroGreyComparison,"Hero grey toggle failed");appearance.SetGrey(false);}
            region.water.gameObject.SetActive(false);yield return Shot("no-water");region.water.gameObject.SetActive(true);
            weakPerspective=false;ApplyCamera(true);yield return Shot("orthographic");weakPerspective=true;ApplyCamera(true);
            caveView=true;ApplyCamera(true);yield return new WaitForSeconds(5);yield return Shot("cave-water");
            region.water.gameObject.SetActive(false);yield return Shot("cave-no-water");
            region.water.gameObject.SetActive(true);caveView=false;ApplyCamera(true);
            if(VisualCheck)
            {
                region.referenceFocus.position=new Vector3(42,1,95);viewSize=9.5f;ApplyCamera(true);yield return Shot("pier-close");
                report.resolution=Screen.width+"x"+Screen.height;report.passed=failure=="";report.failure=failure;
                File.WriteAllText(Evidence+"/visual-result.json",JsonUtility.ToJson(report,true));Debug.Log("COAST_BLOCKOUT_VISUAL "+JsonUtility.ToJson(report));
                Application.Quit(report.passed?0:1);yield break;
            }

            // Benchmark the real scene, without screenshot/PNG encoding inside timed frames.
            for(int i=0;i<180;i++)yield return new WaitForEndOfFrame();
            var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            report.benchmarkRenderScale=pipeline!=null?pipeline.renderScale:1;
            report.benchmarkWindow=Screen.width+"x"+Screen.height;
            report.benchmarkRenderResolution=Mathf.RoundToInt(Screen.width*report.benchmarkRenderScale)+"x"+Mathf.RoundToInt(Screen.height*report.benchmarkRenderScale);
            var samples=new double[1200];double last=Time.realtimeSinceStartupAsDouble;
            for(int i=0;i<samples.Length;i++){yield return new WaitForEndOfFrame();double now=Time.realtimeSinceStartupAsDouble;samples[i]=(now-last)*1000;last=now;}
            report.meanMs=samples.Average();report.fps=1000/report.meanMs;Array.Sort(samples);report.p95Ms=samples[1140];report.p99Ms=samples[1188];

            if(habitat!=null){
                for(int mode=0;mode<3;mode++){
                    Shader.SetGlobalFloat("_CoastHabitatDetails",mode==1?1:0);habitat.FoliageVisible=mode==0;
                    for(int warm=0;warm<60;warm++)yield return new WaitForEndOfFrame();
                    double begin=Time.realtimeSinceStartupAsDouble;
                    for(int frame=0;frame<360;frame++)yield return new WaitForEndOfFrame();
                    double ms=(Time.realtimeSinceStartupAsDouble-begin)*1000/360;
                    report.habitatPerformance.Add((mode==0?"cover-off":mode==1?"new-foliage-off":"both-off")+" fps="+(1000/ms).ToString("F2")+" meanMs="+ms.ToString("F3"));
                }
                habitat.FoliageVisible=true;habitat.SetEnabled(true);
            }

            // The player is capped at 60 FPS; keep the benchmark uncapped, then
            // exercise controls under the same frame limit as the playable build.
            Application.targetFrameRate=UncappedNavigation?-1:60;report.navigationFrameLimit=Application.targetFrameRate;
            for(int i=0;i<30;i++)yield return new WaitForEndOfFrame();
            Vector3 start=boat.transform.position;float startTime=Time.time;var probe=boat.gameObject.AddComponent<CoastNavigationProbe>();
            boat.AutomationThrottle=.45f;
            while(Time.time-startTime<6)yield return new WaitForEndOfFrame();boat.AutomationThrottle=0;
            report.sailingDistance=Vector3.Distance(start,boat.transform.position);
            report.navigation="start="+start+" end="+boat.transform.position+" seconds="+(Time.time-startTime)+" physicsSteps="+probe.steps+" contacts="+string.Join(",",probe.contacts);
            Require(report.sailingDistance>10,"Controller-driven boat failed: "+report.navigation);
            Require(probe.contacts.Count==0,"Boat hit an obstruction in berth approach: "+report.navigation);
            yield return Shot("sailing");

            boat.Moor(region.berth.position,Quaternion.Euler(0,90,0));yield return new WaitForEndOfFrame();
            Require(game.Expedition.Disembark(),"Original Disembark failed");
            if(game.Expedition.OnFoot)
            {
                yield return new WaitForEndOfFrame();if(MaterialMode){report.captainHiddenWhileWalking=hero!=null&&!hero.CaptainVisible;Require(report.captainHiddenWhileWalking,"Captain remains aboard during walking");Require(hero!=null&&hero.MaterialsIntact,"Walking hero materials overwritten");}
                yield return WalkCourse(report,false);yield return Shot("walking-lighthouse");
                if(MaterialMode){Vector3 oldFocus=region.referenceFocus.position;float oldSize=viewSize;region.referenceFocus.position=explorer.transform.position+Vector3.up*.8f;viewSize=3.0f;ApplyCamera(true);yield return Shot("walker-close");region.referenceFocus.position=oldFocus;viewSize=oldSize;ApplyCamera(true);}
                yield return WalkCourse(report,true);explorer.AutomationDirection=Vector3.zero;
                for(int i=0;i<30;i++)yield return new WaitForEndOfFrame();
                Require(game.Expedition.AtShip,"Walking return missed original boarding radius");
                Require(game.Expedition.BoardShip(),"Original BoardShip failed after walking route");
            }
            report.walkingPoints=report.reached.Count;
            boat.Moor(new Vector3(54.5f,0,94),Quaternion.Euler(0,110,0));boat.Unmoor();
            region.referenceFocus.position=new Vector3(42,1,95);viewSize=9.5f;ApplyCamera(true);yield return Shot("pier-close");
            region.referenceFocus.position=CoastBlockoutLayout.Focus;viewSize=15.5f;ApplyCamera(true);
            report.resolution=Screen.width+"x"+Screen.height;report.passed=failure=="";report.failure=failure;
            File.WriteAllText(Evidence+"/result.json",JsonUtility.ToJson(report,true));Debug.Log("COAST_BLOCKOUT_CHECK "+JsonUtility.ToJson(report));
            Application.Quit(report.passed?0:1);
        }
        IEnumerator WalkCourse(BlockoutReport report,bool back)
        {
            var explorer=game.Expedition.Explorer;Vector3 previous=explorer.transform.position;
            for(int n=0;n<CoastBlockoutLayout.Walk.Length;n++)
            {
                int i=back?CoastBlockoutLayout.Walk.Length-1-n:n;Vector3 goal=CoastBlockoutLayout.Walk[i];
                float deadline=Time.realtimeSinceStartup+10;
                while(Time.realtimeSinceStartup<deadline)
                {
                    Vector3 delta=goal-explorer.transform.position;delta.y=0;if(delta.magnitude<.32f)break;
                    explorer.AutomationDirection=delta.normalized;
                    yield return new WaitForEndOfFrame();
                    report.walkDistance+=Vector3.Distance(previous,explorer.transform.position);previous=explorer.transform.position;
                }
                explorer.AutomationDirection=Vector3.zero;
                Vector3 remaining=goal-explorer.transform.position;remaining.y=0;
                float error=Mathf.Abs(goal.y-explorer.transform.position.y);report.walkVerticalError=Mathf.Max(report.walkVerticalError,error);
                bool reached=remaining.magnitude<.55f&&error<.8f;
                Require(reached,"Walk waypoint blocked: index="+i+" back="+back+" goal="+goal+" actor="+explorer.transform.position);
                report.reached.Add((back?"return ":"outward ")+i+" "+explorer.transform.position+" reached="+reached);
                if(!reached)yield break;
            }
        }
    }
}
