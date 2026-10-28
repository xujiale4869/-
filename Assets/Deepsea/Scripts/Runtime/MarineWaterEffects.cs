using UnityEngine;
using UnityEngine.Rendering;
namespace Deepsea {
/// <summary>Small pooled impacts; all ripple centres use the same water clock.</summary>
public sealed class MarineWaterEffects : MonoBehaviour {
 public const int Capacity=12;
 readonly Vector4[] ripples=new Vector4[Capacity];
 ParticleSystem droplets;Material splashMaterial;int next;
 public int ImpactCount {get;private set;}
 public int ActiveRipples {get;private set;}
 public void Initialize(){
  for(int i=0;i<Capacity;i++)ripples[i]=new Vector4(0,0,-1000,0);
  var go=new GameObject("飞鱼入水 · 水滴");go.transform.SetParent(transform,false);go.layer=4;
  droplets=go.AddComponent<ParticleSystem>();droplets.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=droplets.main;main.playOnAwake=false;main.loop=true;main.duration=1;
  main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=128;
  main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.65f);
  main.startSize=new ParticleSystem.MinMaxCurve(.055f,.13f);main.gravityModifier=.7f;
  var emission=droplets.emission;emission.enabled=false;
  var shape=droplets.shape;shape.enabled=false;
  var col=droplets.colorOverLifetime;col.enabled=true;
  var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.6f,.9f,.87f),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});col.color=gradient;
  var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
  renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  splashMaterial=new Material(Shader.Find("Deepsea/MarineSplash"));renderer.sharedMaterial=splashMaterial;
  droplets.Play();
 }
 public void Impact(Vector3 position,float strength=1){
  position.y=OceanSurface.Height(position,Time.time)+.055f;
  ripples[next]=new Vector4(position.x,position.z,Time.time,strength);next=(next+1)%Capacity;
  ImpactCount++;
  for(int i=0;i<9;i++){
   float a=i*Mathf.PI*2/9+ImpactCount*.43f;
   var e=new ParticleSystem.EmitParams{
    position=position,velocity=new Vector3(Mathf.Sin(a)*(.6f+i%3*.22f),1.6f+i%4*.22f,Mathf.Cos(a)*(.6f+i%3*.22f)),
    startColor=new Color(.73f,.95f,.93f,.85f)
   };
   droplets.Emit(e,1);
  }
  Publish();
 }
 void LateUpdate(){Publish();}
 void Publish(){
  ActiveRipples=0;int last=0;
  for(int i=0;i<Capacity;i++)if(ripples[i].w>0){
   if(Time.time-ripples[i].z>3.4f)ripples[i].w=0;
   else{ActiveRipples++;last=i+1;}
  }
  Shader.SetGlobalInt("_BayRippleCount",last);Shader.SetGlobalVectorArray("_BayRipples",ripples);
 }
 void OnDestroy(){Shader.SetGlobalInt("_BayRippleCount",0);if(splashMaterial!=null)Destroy(splashMaterial);}
}
}
