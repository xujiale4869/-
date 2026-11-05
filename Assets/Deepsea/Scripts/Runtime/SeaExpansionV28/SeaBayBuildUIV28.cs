using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Deepsea {
 public sealed partial class SeaBayExpansionAndBuildSpaceV28 {
  static readonly string[] Names={"延伸木栈桥","高杆暖光马灯","水下珊瑚花园","捕蟹浮标笼"};
  static readonly string[] Costs={"浮木 2","浮木 1 · 黄铜 1","潮汐石 2 · 珊瑚苗 1","浮木 1 · 潮汐石 1"};
  Canvas bayCanvas;GameObject planningPanel,normalHint,legend;TextMeshProUGUI inventory,hoverLabel,messageLabel;Button[] choices=new Button[4];Image[] choiceBack=new Image[4];
  TMP_FontAsset font;readonly Color ink=new Color(.91f,.94f,.89f),muted=new Color(.59f,.73f,.71f),gold=new Color(.94f,.78f,.48f);
  RectTransform UINode(string name,Transform parent,Rect r){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var rt=o.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(r.x,-r.y);rt.sizeDelta=r.size;return rt;}
  RectTransform Panel(string name,Transform parent,Rect r){var n=UINode(name,parent,r);n.gameObject.AddComponent<Image>().color=new Color(.026f,.075f,.09f,.97f);return n;}
  TextMeshProUGUI Label(string name,Transform parent,string text,Rect r,int size,Color color){var t=UINode(name,parent,r).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.enableWordWrapping=true;t.raycastTarget=false;return t;}
  Button UIButton(string title,Transform parent,Rect r,Action action){var n=UINode(title,parent,r);var image=n.gameObject.AddComponent<Image>();image.color=new Color(.08f,.18f,.20f);var b=n.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{game.Cue();action();});Label("Label",n,title,new Rect(14,8,r.width-28,r.height-16),21,ink);return b;}
  void SetupUI(){
   font=Resources.Load<TMP_FontAsset>("SeaLifeV21/SeaLifeChinese");var node=new GameObject("V28_BayPlanningUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));node.transform.SetParent(root,false);bayCanvas=node.GetComponent<Canvas>();bayCanvas.renderMode=RenderMode.ScreenSpaceOverlay;bayCanvas.sortingOrder=45;var scaler=node.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
   var panel=Panel("PlanningPanel",node.transform,new Rect(28,28,414,644));planningPanel=panel.gameObject;
   Label("Title",panel,"归潮湾  /  海湾规划",new Rect(22,20,370,42),29,gold);Label("Subtitle",panel,"选择设施，将你的港湾向海面延伸",new Rect(22,65,370,32),19,muted);
   inventory=Label("Materials",panel,"",new Rect(22,112,370,64),20,ink);
   for(int i=0;i<4;i++){int k=i;choices[i]=UIButton((i+1)+"  "+Names[i]+"\n"+Costs[i],panel,new Rect(18,191+i*73,378,65),()=>Select(k));choiceBack[i]=choices[i].GetComponent<Image>();}
   hoverLabel=Label("Hover",panel,"",new Rect(22,494,372,68),18,ink);messageLabel=Label("Feedback",panel,"",new Rect(22,563,372,54),18,gold);
   var footer=Panel("PlanningKeys",node.transform,new Rect(480,984,1000,62));Label("Keys",footer,"WASD 漫游   ·   滚轮缩放   ·   R 旋转   ·   左键建造   ·   右键拆除   ·   G / Esc 返回",new Rect(20,17,970,35),21,ink);footer.SetParent(panel,false);footer.anchoredPosition=new Vector2(452,-956);
   var leg=Panel("BayZones",node.transform,new Rect(1480,28,412,163));legend=leg.gameObject;Label("ZoneTitle",leg,"三片浅湾  /  一条外海航道",new Rect(20,14,370,30),23,gold);Label("Zones",leg,"A  暖沙浅坪 · 码头延伸\nB  南北礁坪 · 珊瑚花园\nC  新月环礁 · 外海锚地",new Rect(20,54,370,94),20,ink);
   var hint=Panel("BayHint",node.transform,new Rect(1510,26,382,60));normalHint=hint.gameObject;Label("GHint",hint,"G  海湾规划与建造",new Rect(16,15,350,36),23,gold);
   var refill=UIButton("补充测试建材 +20",panel,new Rect(22,608,370,28),()=>{if(Checking){data.wood+=20;data.stone+=20;data.coralSeed+=20;data.brass+=20;SaveLayout();}});refill.gameObject.SetActive(Checking);
   planningPanel.SetActive(false);legend.SetActive(false);
  }
  void RefreshBuildUI(){
   if(bayCanvas==null)return;bayCanvas.enabled=!game.InRoom;planningPanel.SetActive(godBuildMode);legend.SetActive(godBuildMode);normalHint.SetActive(!godBuildMode&&!game.Paused);
   if(!godBuildMode)return;inventory.text="建造物资\n浮木 "+data.wood+"    石 "+data.stone+"    苗 "+data.coralSeed+"    黄铜 "+data.brass;
   for(int i=0;i<4;i++)choiceBack[i].color=i==selected?new Color(.13f,.37f,.33f):new Color(.08f,.18f,.20f);
   hoverLabel.text=hasHover?hoverZone+" · 水深 "+(hoverDepth>50?"未探测":hoverDepth.ToString("F2")+" m")+"\n"+(hoverValid?"可建造  /  朝向 "+rotation*90+"°":hoverReason):"将鼠标移至海面，查看建造位置";
   messageLabel.text=Time.unscaledTime<messageUntil?message:"建造与拆除会自动保存。";
  }
  bool PointerOverPanel(){if(!godBuildMode)return false;var p=Input.mousePosition;float scale=Mathf.Sqrt(Screen.width/1920f*Screen.height/1080f);float x=p.x/scale,y=(Screen.height-p.y)/scale;return (x>=28&&x<=442&&y>=28&&y<=672)||(x>=1480&&y<=200)||(x>=480&&x<=1480&&y>=984);}
 }
}
