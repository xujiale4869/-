using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaWaterArtUpgradeV26 {
  // Match the supplied IMGUI panel; retain mouse ownership while a slider is dragged out.
  public static bool PointerOverTuningPanelV262 {
   get {
    var active=Active;
    if(active==null||!active.isActiveAndEnabled||!active.Ready||!active.showTuningPanel)return false;
    var mouse=Input.mousePosition;
    return GUIUtility.hotControl!=0||new Rect(24f,80f,360f,310f).Contains(new Vector2(mouse.x,Screen.height-mouse.y));
   }
  }
 }
}
