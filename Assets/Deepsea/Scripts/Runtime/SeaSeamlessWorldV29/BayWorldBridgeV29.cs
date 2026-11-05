using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaBayExpansionAndBuildSpaceV28 {
  public BayExpansionSaveData DataV29{get{return data;}}
  public int SelectedV29{get{return selected;}}public int RotationV29{get{return rotation;}}
  public string HoverV29{get{return hasHover?hoverZone+" · "+(hoverDepth>50?"未探测":hoverDepth.ToString("F1")+"m")+" · "+(hoverValid?"可建造":hoverReason):"指向浅水格，左键建造 · 右键拆除";}}
  public string FeedbackV29{get{return Time.unscaledTime<messageUntil?message:"";}}
  public void SelectV29(int i){Select(i);}public void RotateV29(){rotation=(rotation+1)%4;}
  public bool CanPlaceV29(Vector2Int cell,int type){string reason;return CanPlace(cell,type,rotation,true,out reason);}
  public int BuildingCountV29{get{return buildings.Count;}}
  public Vector2Int FreeV29(int type){for(int z=-38;z<44;z++)for(int x=2;x<58;x++){var c=new Vector2Int(x,z);if(CanPlaceV29(c,type))return c;}return new Vector2Int(999,999);}
  public void PlanningViewV29(Vector3 at,float size){focus=at;godOrthoSize=size;}
 }
}
