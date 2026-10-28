using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deepsea
{
    public sealed class CoastMaterialsAppearance:MonoBehaviour
    {
        public static bool Requested {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-coast-materials")>=0;}}
        public bool Grey {get;private set;}
        public int StyledRenderers {get{return styled.Count;}}
        readonly Dictionary<Renderer,Material[]> original=new Dictionary<Renderer,Material[]>();
        readonly Dictionary<Renderer,Material> styled=new Dictionary<Renderer,Material>();
        CoastBlockoutRegion region;GameDirector game;Volume volume;
        Material rock,ground,wood,plaster,metal,coat,skin,fish,water;
        public void Initialize(CoastBlockoutRegion area,GameDirector director)
        {
            region=area;game=director;
            // At the stair turn, overlapping treads expose a side edge of about .45 m.
            // Scope the increased step tolerance to this coast preview; keep accepted geometry.
            var walker=game.Expedition.Explorer.GetComponent<CharacterController>();
            walker.stepOffset=Mathf.Max(walker.stepOffset,.48f);
            Debug.Log("COAST_WALK_STEP_OFFSET="+walker.stepOffset);
            rock=Load("Rock");ground=Load("Ground");wood=Load("Wood");plaster=Load("Plaster");metal=Load("Metal");coat=Load("Coat");skin=Load("Skin");fish=Load("Fish");water=Load("Water");
            foreach(var r in area.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material m=rock;string n=r.name;
                if(r.transform==area.water)m=water;
                else if(n.StartsWith("地形块"))m=ground;
                else if(r.GetComponentInParent<CoastBlockoutSchool>()!=null)m=fish;
                else if(r.transform.IsChildOf(area.lighthouse))m=n.Contains("塔身")||n.Contains("台基")?plaster:metal;
                else if(n.Contains("桥面")||n.Contains("纵梁")||n.Contains("立柱")||n.Contains("过渡板"))m=wood;
                else if(n.Contains("护栏"))m=wood;
                Set(r,m);
            }
            foreach(var r in game.Boat.hull.GetComponentsInChildren<MeshRenderer>(true))
                Set(r,r.name.Contains("人物头")?skin:r.name.Contains("船中人物")?coat:wood);
            foreach(var old in FindObjectsOfType<Volume>())old.enabled=false;
            volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=Resources.Load<VolumeProfile>("CoastMaterials/Look");
            Shader.SetGlobalFloat("_CoastWaterDetails",1);StyleExplorer();SetGrey(false);
            if(CoastEcology.Requested)gameObject.AddComponent<CoastEcology>().Initialize(region,this);
            Debug.Log("COAST_MATERIALS_READY renderers="+StyledRenderers+" geometry="+area.layoutVersion);
        }
        static Material Load(string name)
        {var m=Resources.Load<Material>("CoastMaterials/"+name);if(m==null)throw new InvalidOperationException("Missing CoastMaterials/"+name);return m;}
        void Set(Renderer r,Material m)
        {
            if(!original.ContainsKey(r))original.Add(r,r.sharedMaterials);
            styled[r]=m;r.sharedMaterial=Grey?region.clay:m;
        }
        public void StyleExplorer()
        {
            if(game==null)return;
            foreach(var r in game.Expedition.Explorer.GetComponentsInChildren<Renderer>(true))
            {Material m=r.name.Contains("头")||r.name.Contains("手")?skin:coat;if(!styled.ContainsKey(r))Set(r,m);else if(r.sharedMaterial!=(Grey?region.clay:m))r.sharedMaterial=Grey?region.clay:m;}
        }
        public bool Owns(Renderer r){return styled.ContainsKey(r);}
        public void SetGrey(bool value)
        {
            Grey=value;
            foreach(var pair in styled)if(pair.Key!=null)pair.Key.sharedMaterials=value?original[pair.Key]:new[]{pair.Value};
            foreach(var light in FindObjectsOfType<Light>())if(light.type==LightType.Directional)
            {light.transform.rotation=Quaternion.Euler(48,-35,0);light.intensity=value?1.10f:1.75f;light.color=value?Color.white:new Color(1,.96f,.90f);light.shadows=LightShadows.Soft;}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=value?new Color(.46f,.47f,.49f):new Color(.34f,.46f,.52f);
            RenderSettings.ambientEquatorColor=value?new Color(.33f,.34f,.36f):new Color(.23f,.30f,.29f);
            RenderSettings.ambientGroundColor=value?new Color(.20f,.21f,.23f):new Color(.11f,.16f,.18f);
            RenderSettings.fog=false;
            game.Camera.allowHDR=!value;game.Camera.GetUniversalAdditionalCameraData().renderPostProcessing=!value;
            if(volume!=null)volume.enabled=!value;
        }
        void LateUpdate()
        {
            if(game==null)return;StyleExplorer();
            if(CoastEcology.Requested){
                // Keep the 3D pixel budget near 1080p when a high-DPI desktop enlarges the window.
                var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                float scale=Mathf.Clamp(Mathf.Sqrt(2073600f/Mathf.Max(1f,(float)Screen.width*Screen.height)),.5f,1f);
                if(pipeline!=null&&Mathf.Abs(pipeline.renderScale-scale)>.01f)pipeline.renderScale=scale;
            }
            game.Camera.backgroundColor=Grey?new Color(.31f,.36f,.39f):game.Camera.transform.position.y<-.22f?new Color(.018f,.10f,.13f):new Color(.25f,.38f,.43f);
        }
    }
}
