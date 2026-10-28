using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
 public sealed class CoastMapleLeaves:MonoBehaviour {
  public Vector3[] targets,anchors;public bool visible=true,paused,manual;public float manualTime;
  const int Count=30;const float WaterY=-.194f;float clock;Mesh leaf,ripple;Material leafMaterial,rippleMaterial;
  readonly Matrix4x4[] matrices=new Matrix4x4[Count];MaterialPropertyBlock block;
  public struct State{public Vector3 p;public Quaternion q;public float scale,rippleAge;public bool floating;}
  [Serializable]public class MotionAudit{public bool passed;public int samples,leaves,waterTargets,airborne,floating;public float contactJumpM,minimumFloatY;public string scope;}
  void Start(){block=new MaterialPropertyBlock();leaf=Resources.Load<Mesh>("CoastMaple/DriftLeaf");ripple=Resources.Load<Mesh>("CoastMaple/Ripple");leafMaterial=Resources.Load<Material>("CoastMaple/MapleLeaf");rippleMaterial=Resources.Load<Material>("CoastMaple/LeafRipple");}
  public State Evaluate(int i,float time){
   int k=i%targets.Length;Vector3 target=targets[k];int a=0;float best=float.MaxValue;
   for(int j=0;j<anchors.Length;j++){float d=(new Vector2(anchors[j].x-target.x,anchors[j].z-target.z)).sqrMagnitude;if(d<best){best=d;a=j;}}
   Vector3 source=anchors[a]+new Vector3(.8f,0,Mathf.Sin(i*2.4f)*.5f);float fall=9+(i%5)*.65f,life=fall+19,age=Mathf.Repeat(time+i*1.618034f,life),yaw=i*137.508f;float scale=.19f+(i%4)*.022f;
   if(age<fall){float t=age/fall;var p=Vector3.Lerp(source,new Vector3(target.x,WaterY,target.z),t);p.x+=Mathf.Sin(age*1.7f+i)*.23f*Mathf.Sin(t*Mathf.PI);p.z+=Mathf.Cos(age*1.1f+i)*.17f*Mathf.Sin(t*Mathf.PI);var q=Quaternion.Euler(22*Mathf.Sin(age*1.3f+i),yaw+age*26,48*Mathf.Cos(age*.9f+i));q=Quaternion.Slerp(q,Quaternion.Euler(0,yaw+fall*26,0),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,t)));return new State{p=p,q=q,scale=scale,rippleAge=-1};}
   float f=age-fall;return new State{p=new Vector3(target.x+f*.025f,WaterY+Mathf.Sin(f*.8f)*.0015f,target.z+Mathf.Sin(f*.25f)*.055f),q=Quaternion.Euler(Mathf.Sin(f)*1.2f,yaw+fall*26+f*2.1f,Mathf.Cos(f*.9f)*1.2f),scale=scale*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,19,f))),floating=true,rippleAge=f};
  }
  void LateUpdate(){if(!paused)clock+=Time.deltaTime;if(!visible||leaf==null||targets==null||targets.Length==0)return;float time=manual?manualTime:clock;
   for(int i=0;i<Count;i++){var s=Evaluate(i,time);matrices[i]=Matrix4x4.TRS(s.p,s.q,Vector3.one*s.scale);if(s.floating&&s.rippleAge<1.8f){float radius=.05f+s.rippleAge*.13f;block.SetFloat("_Opacity",.20f*(1-s.rippleAge/1.8f));Graphics.DrawMesh(ripple,Matrix4x4.TRS(new Vector3(s.p.x,-.188f,s.p.z),Quaternion.identity,Vector3.one*radius*2),rippleMaterial,0,null,0,block,ShadowCastingMode.Off,false);}}
   Graphics.DrawMeshInstanced(leaf,0,leafMaterial,matrices,Count,null,ShadowCastingMode.Off,true,0,null,LightProbeUsage.Off);
  }
  public MotionAudit Audit(){var r=new MotionAudit{passed=targets!=null&&targets.Length>0&&anchors!=null&&anchors.Length>0,leaves=Count,waterTargets=targets==null?0:targets.Length,minimumFloatY=999,scope="Deterministic continuous fall/contact/float states; water target positions independently sampled against the visible ground and new mineral geometry during asset build. Does not assert general gameplay integration."};if(!r.passed)return r;
   for(int i=0;i<Count;i++){float fall=9+(i%5)*.65f;var l=Evaluate(i,fall-i*1.618034f-.001f);var h=Evaluate(i,fall-i*1.618034f+.001f);r.contactJumpM=Mathf.Max(r.contactJumpM,Vector3.Distance(l.p,h.p));for(int j=0;j<420;j++){var s=Evaluate(i,j*.1f);r.samples++;if(s.floating){r.floating++;r.minimumFloatY=Mathf.Min(r.minimumFloatY,s.p.y);}else r.airborne++;if(float.IsNaN(s.p.x)||float.IsInfinity(s.p.y)||s.scale<0)r.passed=false;}}
   if(r.contactJumpM>.02f||r.minimumFloatY<-.199f)r.passed=false;return r;
  }
 }
}
