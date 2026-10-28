// Adapted from SebLague/Boids, Assets/Scripts/Boid.cs (2019), MIT.
// Copyright (c) 2019 Sebastian Lague. See ThirdParty/MarineLife/SebBoids-LICENSE.txt.
// Adaptation: planar steering for a small CPU fish school; no compute shader dependency.
using UnityEngine;
namespace Deepsea {
public static class SchoolSteering {
 static Vector3 SteerTowards(Vector3 vector,Vector3 velocity,float maxSpeed,float maxForce){
  vector.y=0;
  Vector3 desired=vector.normalized*maxSpeed-velocity;
  return Vector3.ClampMagnitude(desired,maxForce);
 }
 public static Vector3 Acceleration(Vector3 velocity,Vector3 heading,Vector3 separation,
   Vector3 centre,Vector3 position,int neighbours,Vector3 target,float speed){
  Vector3 acceleration=SteerTowards(target-position,velocity,speed,2.2f)*.65f;
  if(neighbours>0){
   centre/=neighbours;
   acceleration+=SteerTowards(heading,velocity,speed,2.2f)*.7f;
   acceleration+=SteerTowards(centre-position,velocity,speed,2.2f)*.45f;
   acceleration+=SteerTowards(separation,velocity,speed,2.2f)*1.8f;
  }
  return acceleration;
 }
}
}
