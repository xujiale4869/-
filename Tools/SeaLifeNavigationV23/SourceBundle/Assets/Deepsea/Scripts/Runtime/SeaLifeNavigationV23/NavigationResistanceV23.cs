// Adapted from Habrador/Unity-Boat-physics-Tutorial, BoatPhysicsMath.cs,
// commit 29f8c8d531e34062aea66bbc2d0627696b68b0bf.
// Copyright (c) 2020 Erik Nordeus. MIT License; see ThirdParty/Habrador/LICENSE.
using UnityEngine;
namespace Deepsea {
 public static class NavigationResistanceV23 {
  public static float ResistanceCoefficient(float speed,float length){
   float reynolds=Mathf.Max(1000,Mathf.Abs(speed)*Mathf.Max(.1f,length)/.000001f);
   return .075f/Mathf.Pow(Mathf.Log10(reynolds)-2,2);
  }
  public static float ViscousAcceleration(float speed,float mass,float wettedArea,float length){
   if(float.IsNaN(speed)||float.IsInfinity(speed))return 0;
   return -.5f*1000*Mathf.Abs(speed)*speed*wettedArea*ResistanceCoefficient(speed,length)/Mathf.Max(.01f,mass);
  }
 }
}
