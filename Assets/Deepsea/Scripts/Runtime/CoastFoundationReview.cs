using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Deepsea {
 public sealed partial class CoastBlockoutSession {
  bool FoundationPreview { get { return Array.IndexOf(Environment.GetCommandLineArgs(),"-foundation-preview")>=0; } }
  string FoundationTag {
   get {
    var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-review-tag");
    string tag=index>=0&&index+1<args.Length?args[index+1]:"draft";
    foreach(char c in tag)if(!char.IsLetterOrDigit(c)&&c!='-')throw new InvalidOperationException("Invalid review tag");
    return tag;
   }
  }
  IEnumerator RunFoundationPreview() {
   Directory.CreateDirectory(Evidence);Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
   yield return new WaitForSecondsRealtime(2);showHud=false;if(monitor!=null)monitor.enabled=false;
   referenceView=true;weakPerspective=true;viewSize=15.5f;
   region.referenceFocus.position=CoastBlockoutLayout.Focus;ApplyCamera(true);yield return Shot("reference");
   appearance.SetGrey(true);yield return Shot("clay");appearance.SetGrey(false);
   region.referenceFocus.position=CoastBlockoutLayout.Focus+new Vector3(1.8f,-.5f,-2.8f);ApplyCamera(true);yield return Shot("presentation");
   region.referenceFocus.position=new Vector3(30,8,93);viewSize=6.4f;ApplyCamera(true);yield return Shot("tower-close");
   region.referenceFocus.position=new Vector3(65,-4,94);viewSize=10;ApplyCamera(true);yield return Shot("reef-close");
   region.referenceFocus.position=new Vector3(39,1,94);viewSize=9;ApplyCamera(true);yield return Shot("pier-close");
   File.WriteAllText(Evidence+"/review.json","{\"tag\":\""+FoundationTag+"\",\"failure\":\""+failure.Replace("\"","'")+"\",\"scope\":\"Visual review only; full acceptance is separate.\"}");
   Debug.Log("FOUNDATION_PREVIEW_COMPLETE "+FoundationTag+" failure="+failure);
   Application.Quit(string.IsNullOrEmpty(failure)?0:2);
  }
 }
}
