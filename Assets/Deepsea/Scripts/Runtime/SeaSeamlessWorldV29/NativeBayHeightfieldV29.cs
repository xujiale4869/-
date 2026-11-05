using UnityEngine;
namespace Deepsea {
 public sealed class NativeBayHeightfieldV29:ScriptableObject {
  public int width,height,sourceMeshes,coveredSamples;public float minX=-36,minZ=-40,step=.5f,northShore=-8,southShore=-8;
  public float[] values,safeValues,distances;public int[] nearest;
  public bool Probe(float x,float z,out float y,out float distance,bool conservative=false){
   var sampled=conservative&&safeValues!=null&&safeValues.Length==values.Length?safeValues:values;
   float fx=(x-minX)/step,fz=(z-minZ)/step;int ix=Mathf.Clamp(Mathf.RoundToInt(fx),0,width-1),iz=Mathf.Clamp(Mathf.RoundToInt(fz),0,height-1),i=iz*width+ix;
   if(nearest==null||i>=nearest.Length||nearest[i]<0){y=-14;distance=1000;return false;}
   int n=nearest[i];y=sampled[n];distance=distances[i]+Vector2.Distance(new Vector2(x,z),new Vector2(minX+ix*step,minZ+iz*step));
   // Bilinear interpolation on fully covered cells, bounded nearest fallback at native holes.
   int a=Mathf.Clamp(Mathf.FloorToInt(fx),0,width-2),b=Mathf.Clamp(Mathf.FloorToInt(fz),0,height-2),k=b*width+a;
   if(fx>=0&&fz>=0&&fx<width-1&&fz<height-1&&values[k]>-90&&values[k+1]>-90&&values[k+width]>-90&&values[k+width+1]>-90){float tx=Mathf.Clamp01(fx-a),tz=Mathf.Clamp01(fz-b);y=Mathf.Lerp(Mathf.Lerp(sampled[k],sampled[k+1],tx),Mathf.Lerp(sampled[k+width],sampled[k+width+1],tx),tz);distance=0;}
   return true;
  }
 }
}
