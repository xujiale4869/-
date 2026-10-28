using System;
using UnityEngine;
namespace Deepsea {
// Runtime source arrays survive Unity's unused-render-channel stripping on imported meshes.
public sealed class CoastKelpData:ScriptableObject {
 public Vector3[] vertices;public Vector2[] uv;public Color[] colors;public int[] triangles;public Bounds bounds;
 public void Validate(){
  if(vertices==null||uv==null||colors==null||triangles==null||vertices.Length==0||uv.Length!=vertices.Length||colors.Length!=vertices.Length||triangles.Length%3!=0)throw new InvalidOperationException("Ribbon source attributes missing");
  foreach(int index in triangles)if(index<0||index>=vertices.Length)throw new InvalidOperationException("Ribbon source index invalid");
 }
}
}
