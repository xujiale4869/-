using UnityEngine;
namespace Deepsea {
 public sealed partial class SeaLifeRuntime {
  public SeaLifeNavigationV23 navigationV23;
  bool DriveNavigationV23(){if(navigationV23==null||!navigationV23.enabled||!navigationV23.NavigationReady||navigationV23.Baseline)return false;navigationV23.Drive();return true;}
  public void FollowWaterV23(SeaLifeNavigationV23 water){
   if(Paused)return;
   if(bobber!=null&&bobber.gameObject.activeSelf){var p=bobber.position;
    if(fishingStage==3)p.y+=water.Height(p)-Sea;
    else if(fishingStage==4)p.y=water.Height(p)+.01f;
    else if(fishingStage==2){float k=1-Mathf.SmoothStep(0,.5f,Mathf.Max(0,p.y-Sea));p.y+=(water.Height(p)-Sea)*k;}
    bobber.position=p;if(fishingStage==4&&fishTarget!=null)fishTarget.position=p+Vector3.down*.22f;
    if(fishingLine.enabled)for(int i=0;i<18;i++){float t=i/17f;var v=Vector3.Lerp(rodTip.position,p,t);v.y-=Mathf.Sin(t*Mathf.PI)*(fishingStage==4?.04f:.16f);fishingLine.SetPosition(i,v);}
   }
   foreach(var marker in rippleMarkers){var ring=marker.GetComponentInChildren<LineRenderer>();if(ring==null)continue;for(int i=0;i<ring.positionCount;i++){var p=ring.GetPosition(i);p.y=water.Height(p)+.025f;ring.SetPosition(i,p);}}
   var buoy=actorRoot.Find("打捞小浮标");if(buoy!=null){var p=buoy.position;p.y=water.Height(p)+.17f;buoy.position=p;}
  }
 }
}
