using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaWaterArtUpgradeV26 {
  [Serializable]class NativeTuningStateV262 {public string phase;public bool opened,changed,restored,closed;public int fishingStage;public float caustics;}
  IEnumerator NativeTuningReviewV262(){
   result.scope="Actual OS F7, slider and preset interaction with isolated fishing fixture";result.frames=0;
   _game.PlaceBoat(new Vector3(.8f,SeaLifeRuntime.Sea,5.2f),100);_game.SnapWaterCamera();_game.NoticeTime=0;
   _game.AutoInput=false;CheckV26(_game.BeginFishing(),"native test prepares actual fishing rig");
   var state=new NativeTuningStateV262{phase="await-open"};WriteNativeTuningStateV262(state);
   float until=Time.realtimeSinceStartup+80;float initial=causticsIntensity;
   while(Time.realtimeSinceStartup<until){
    if(!state.opened&&showTuningPanel){state.opened=true;yield return ShotV26("native-open");state.phase="await-slider";WriteNativeTuningStateV262(state);}
    if(state.opened&&!state.changed&&Mathf.Abs(causticsIntensity-initial)>.05f){state.changed=true;state.phase="await-preset";WriteNativeTuningStateV262(state);}
    if(state.changed&&!state.restored&&Mathf.Abs(causticsIntensity-.92f)<.0001f&&Mathf.Abs(glintIntensity-1.85f)<.0001f&&Mathf.Abs(glintFocus-72)<.0001f&&Mathf.Abs(foamIntensity-.95f)<.0001f&&Mathf.Abs(waterVisibility-.86f)<.0001f){state.restored=true;state.phase="await-close";WriteNativeTuningStateV262(state);}
    if(state.opened&&state.changed&&state.restored&&!showTuningPanel){state.closed=true;break;}
    if(_game.fishingStage!=1)break;
    yield return null;
   }
   CheckV26(state.opened,"OS F7 opens actual tuning panel");
   CheckV26(state.changed,"OS pointer changes actual caustics slider");
   CheckV26(state.restored,"OS preset button restores all five balanced values");
   CheckV26(state.closed,"OS F7 closes actual tuning panel");
   CheckV26(_game.fishingStage==1,"slider and preset clicks do not cast fishing line");
   CheckV26(Mathf.Abs(_v26WaterMaterial.GetFloat("_CausticsIntensity")-causticsIntensity)<.0001f,"tuned value reaches active water material");
   state.phase="done";WriteNativeTuningStateV262(state);yield return ShotV26("native-final");
  }
  void WriteNativeTuningStateV262(NativeTuningStateV262 state){state.fishingStage=_game.fishingStage;state.caustics=causticsIntensity;File.WriteAllText(Path.Combine(evidence,"native-input-state.json"),JsonUtility.ToJson(state,true));}
 }
}
