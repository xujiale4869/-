using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Deepsea {
 // Diagnostic only: records URP's actual camera descriptor without adding a render pass.
 public sealed class SeaFrameDescriptorV24:ScriptableRendererFeature {
  public static bool HasNative;public static UnityEngine.RenderTextureDescriptor Native,Latest;
  public override void Create(){}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){
   if(data.cameraData.camera.cameraType!=UnityEngine.CameraType.Game)return;
   Latest=data.cameraData.cameraTargetDescriptor;
   if(data.cameraData.camera.targetTexture==null){Native=Latest;HasNative=true;}
  }
 }
}
