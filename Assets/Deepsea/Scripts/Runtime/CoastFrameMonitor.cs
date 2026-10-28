using UnityEngine;
namespace Deepsea {public sealed class CoastFrameMonitor:MonoBehaviour {
 bool visible=true;double elapsed;int count;string label="测量帧率…";GUIStyle style;
 void Update(){if(Input.GetKeyDown(KeyCode.F8))visible=!visible;elapsed+=Time.unscaledDeltaTime;count++;if(elapsed>=.5){label=(count/elapsed).ToString("0")+" FPS · "+(elapsed*1000/count).ToString("0.0")+" ms   F8";elapsed=0;count=0;}}
 void OnGUI(){if(!visible)return;if(style==null){style=new GUIStyle(GUI.skin.box){fontSize=16,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(.94f,.82f,.56f);}GUI.Box(new Rect(Screen.width*.5f-124,10,248,32),label,style);}
}}
