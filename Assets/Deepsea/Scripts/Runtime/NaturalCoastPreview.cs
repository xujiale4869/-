using System;using System.Collections;using UnityEngine;
namespace Deepsea {
public sealed class NaturalCoastPreview:MonoBehaviour {
 static bool Requested{get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-natural-preview")>=0;}}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Before(){if(Requested)GameDirector.VerificationMode=true;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void After(){if(Requested)FindObjectOfType<GameDirector>().gameObject.AddComponent<NaturalCoastPreview>();}
 IEnumerator Start(){while(OceanSurface.Active==null||!OceanSurface.Active.BathymetryReady)yield return null;var game=GetComponent<GameDirector>();game.UI.Close();game.HarbourMenu=false;game.SetPause(false);game.Boat.Moor(new Vector3(54,0,91),Quaternion.identity);game.CameraFollow.SetWalking(game.Boat.transform,false);game.Notify("雾岬岛东岸体验：WASD 航行，滚轮调整镜头，低速靠近东岸码头按 F 登陆。本次体验不保存进度。");Debug.Log("NATURAL_PREVIEW_READY");}
}
}
