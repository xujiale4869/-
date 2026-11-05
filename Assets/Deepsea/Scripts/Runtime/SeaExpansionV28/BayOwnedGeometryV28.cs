using System.Collections.Generic;
using UnityEngine;
namespace Deepsea {
 public sealed class BayOwnedGeometryV28:MonoBehaviour {
  public readonly List<Mesh> meshes=new List<Mesh>();
  void OnDestroy(){foreach(var m in meshes)if(m!=null)Destroy(m);}
 }
}
