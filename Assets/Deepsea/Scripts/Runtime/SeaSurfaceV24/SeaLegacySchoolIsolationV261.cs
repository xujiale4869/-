using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaWaterArtUpgradeV26 {
  // The preserved v21 decorative school also circles at (13,-2.8,11).
  // Isolate that exact legacy group so the supplied S corridor can be read.
  readonly List<GameObject> legacySchoolV261 = new List<GameObject>();
  void OnEnable(){StartCoroutine(IsolateLegacySchoolV261());}
  IEnumerator IsolateLegacySchoolV261(){
   while(!Ready)yield return null;
   foreach(var motion in FindObjectsOfType<SeaLifeFishMotion>()){
    if(motion.gameObject.name!="沿用海岬鱼群表现")continue;
    legacySchoolV261.Add(motion.gameObject);motion.gameObject.SetActive(false);
   }
  }
  void OnDisable(){foreach(var fish in legacySchoolV261)if(fish!=null)fish.SetActive(true);legacySchoolV261.Clear();}
 }
}
