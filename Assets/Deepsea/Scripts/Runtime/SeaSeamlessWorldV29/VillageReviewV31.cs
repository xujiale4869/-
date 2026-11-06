using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Deepsea { public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
 bool cameraV31;Vector3 viewTargetV31;float viewSizeV31;
 void CameraReviewV31(){if(!cameraV31)return;game.view.orthographic=true;game.view.orthographicSize=viewSizeV31;game.view.transform.rotation=Quaternion.Euler(48,-32,0);game.view.transform.position=viewTargetV31-game.view.transform.forward*85;}
 IEnumerator VillageFrameV31(string name,Vector3 target,float size){viewTargetV31=target;viewSizeV31=size;cameraV31=true;yield return new WaitForSeconds(.2f);yield return Capture(name);cameraV31=false;}
 IEnumerator VillageReviewV31(){
  if(!IsV31)yield break;
  Check(v31VillageHouseCount==3&&housesV31.Count==3,"v31 three grounded coastal occupational houses exist");
  Check(housesV31.All(h=>h.Find("GroundedSeaStoneSkirt").localScale.y<=1.6f),"v31 houses occupy gentle plots without cliff-height foundation walls");
  Check(_v31FloraMeshes.Distinct().Count()==6&&speciesV31.All(c=>c>0),"v31 six distinct authored marine morphologies actually placed");
  Check(v31DiverseFloraCount>=110&&v31ShorelineEcotoneCount>=20,"v31 actual biodiversity and shoreline transition counts");
  bool tips=true,roots=true;foreach(var t in floraV31){foreach(var p in t.GetComponent<MeshFilter>().sharedMesh.vertices)tips &= t.TransformPoint(p).y<SeaLifeRuntime.Sea-.22f;roots &= Mathf.Abs(t.position.y-(V31Ground(t.position.x,t.position.z)-.025f))<.005f;}
  Check(tips,"v31 every transformed marine vertex stays submerged with sway reserve");Check(roots,"v31 marine roots use actual visible bed height");
  bool authored=housesV31.All(t=>{var f=t.GetChild(0).GetComponent<MeshFilter>();return f!=null&&f.sharedMesh.name.StartsWith("Village_")&&f.GetComponent<MeshRenderer>().sharedMaterials.Length==f.sharedMesh.subMeshCount&&f.sharedMesh.subMeshCount>=4;});Check(authored,"v31 generated house triangles and separate material roles are actually rendered");
  Check(_v31Root.GetComponentsInChildren<Collider>().All(c=>(c.transform.position-game.dockBerth).sqrMagnitude>30),"v31 new building colliders leave the old docking berth clear");
  int oldObjects=_v31Root.GetComponentsInChildren<Transform>(true).Length,oldAssets=_owned.Count;var scales=floraV31.Select(t=>t.localScale).ToArray();BuildVillageAndBiodiversityV31();Check(_v31Root.GetComponentsInChildren<Transform>(true).Length==oldObjects&&_owned.Count==oldAssets&&floraV31.Select((t,i)=>t.localScale==scales[i]).All(x=>x),"v31 repeated initialization does not duplicate geometry, materials or multiply scales");
  Check(_v31Root.Find("V31_CoastalNpcVillage/V31_VillageStonePaths").GetComponentsInChildren<MeshRenderer>().Length==2,"v31 stepping stones and posts merge into two static material batches");
  File.WriteAllText(Path.Combine(evidence,"village-stats.json"),"{\"houses\":"+v31VillageHouseCount+",\"marinePlacements\":"+v31DiverseFloraCount+",\"shorePlacements\":"+v31ShorelineEcotoneCount+",\"species\":["+string.Join(",",speciesV31.Select(x=>x.ToString()).ToArray())+"],\"source\":\"authored Baked meshes from same Blender source triangles\"}");
  yield return VillageFrameV31("11-village-panorama",new Vector3(-6,1,0),37);
  yield return VillageFrameV31("12-south-boatwright",housesV31[0].position+Vector3.right*3,13);
  yield return VillageFrameV31("13-tavern-and-ecotone",housesV31[1].position+Vector3.right*4,13);
  yield return VillageFrameV31("02-village-and-flora",housesV31[2].position+Vector3.right*5,18);
  for(int i=0;i<6;i++)if(samplesV31[i]!=null)yield return VillageFrameV31("14-flora-"+i,samplesV31[i].position+Vector3.up*.5f,3.5f);
  game.SnapWaterCamera();
 }
}}
