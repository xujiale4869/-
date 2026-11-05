using UnityEngine;
using UnityEngine.UI;
namespace Deepsea {
 public sealed partial class SeaLifeUI {
  public bool HudV29{get{return hud;}}
  public Canvas LegacyCanvasV29{get{return canvas;}}
  void SkinSurfaceV29(Image image){var s=SeaSeamlessWorldAndNauticalUIV29.Active;if(s==null||!s.UIReady)return;image.sprite=s.CardSprite;image.type=Image.Type.Sliced;image.color=Color.white;}
  void SkinButtonV29(Image image){var s=SeaSeamlessWorldAndNauticalUIV29.Active;if(s==null||!s.UIReady)return;image.sprite=s.SlotSprite;image.type=Image.Type.Sliced;image.color=Color.white;}
  public void ApplyNauticalV29(){if(root==null)return;foreach(var image in root.GetComponentsInChildren<Image>(true)){if(image.GetComponent<Button>()!=null)SkinButtonV29(image);else if(image.name=="当前窗口"||image.name=="钓鱼反馈"||image.name=="物品列表")SkinSurfaceV29(image);}}
  public void HidePersistentV29(){if(root==null)return;foreach(var name in new[]{"场所与收藏","情境操作","主要行动","短时反馈"}){var t=root.Find(name);if(t!=null)t.gameObject.SetActive(false);}canvas.enabled=hud;}
 }
}
