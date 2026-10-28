using UnityEngine;
namespace Deepsea {
// Lightweight authored joints; does not drive the existing movement controller.
public sealed class CoastHeroRig:MonoBehaviour {
 public Transform leftLeg,rightLeg,leftKnee,rightKnee,leftArm,rightArm,leftElbow,rightElbow;
 public bool seated;public float speed;float phase;
 void LateUpdate(){
  phase+=Time.deltaTime*Mathf.Lerp(1,8,Mathf.Clamp01(speed/3));
  float stride=Mathf.Sin(phase)*Mathf.Clamp01(speed/2)*25;
  leftLeg.localRotation=Quaternion.Euler(seated?-72:stride,0,0);
  rightLeg.localRotation=Quaternion.Euler(seated?-72:-stride,0,0);
  leftKnee.localRotation=Quaternion.Euler(seated?72:Mathf.Max(0,-stride)*1.25f,0,0);
  rightKnee.localRotation=Quaternion.Euler(seated?72:Mathf.Max(0,stride)*1.25f,0,0);
  leftArm.localRotation=Quaternion.Euler(seated?-10:-stride*.6f,0,seated?-8:0);
  rightArm.localRotation=Quaternion.Euler(seated?-10:stride*.6f,0,seated?8:0);
  leftElbow.localRotation=rightElbow.localRotation=Quaternion.Euler(seated?-15:-8,0,0);
 }
}
}
