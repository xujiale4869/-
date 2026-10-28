using UnityEngine;
namespace Deepsea {
 [DefaultExecutionOrder(700)]
 public sealed class SeaLifeFishSwimmerV25:MonoBehaviour {
  public int speciesId;public float tailAmplitude=.085f,tailFrequency=7;
  public float Phase{get;private set;}
  Vector3 lastPosition,lastForward;float bank;Renderer meshRenderer;MaterialPropertyBlock block;bool pathDriven;SeaLifeRuntime owner;
  static readonly int phaseID=Shader.PropertyToID("_SwimPhase"),ampID=Shader.PropertyToID("_TailAmplitude");
  public void Configure(SeaLifeRuntime runtime,int species,float amplitude,float frequency){owner=runtime;speciesId=species;tailAmplitude=amplitude;tailFrequency=frequency;}
  void Awake(){meshRenderer=GetComponent<Renderer>();block=new MaterialPropertyBlock();Phase=Random.value*Mathf.PI*2+.01f;}
  void Start(){pathDriven=GetComponent<SeaLifeFishMotion>()!=null;}
  void OnEnable(){lastPosition=transform.position;lastForward=transform.forward;bank=0;}
  void LateUpdate(){
   float dt=Time.deltaTime;if(dt<.0001f||meshRenderer==null||(owner!=null&&owner.Paused))return;
   float speed=Mathf.Min(3,(transform.position-lastPosition).magnitude/dt);lastPosition=transform.position;
   Phase=Mathf.Repeat(Phase+dt*tailFrequency*Mathf.Clamp(.45f+speed*.85f,.45f,2.2f),Mathf.PI*2);
   // Path driver owns heading. Never multiply a new yaw into last frame's rotation.
   Vector3 forward=transform.forward;float rate=Vector3.SignedAngle(lastForward,forward,Vector3.up)/dt;lastForward=forward;
   bank=Mathf.Lerp(bank,Mathf.Clamp(-rate*.12f,-14,14),1-Mathf.Exp(-6*dt));
   if(pathDriven)transform.rotation=transform.rotation*Quaternion.AngleAxis(bank,Vector3.forward);
   meshRenderer.GetPropertyBlock(block);block.SetFloat(phaseID,Phase);block.SetFloat(ampID,tailAmplitude);meshRenderer.SetPropertyBlock(block);
  }
 }
}
