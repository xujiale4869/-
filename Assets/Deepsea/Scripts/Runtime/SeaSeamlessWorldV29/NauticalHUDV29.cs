using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Deepsea {
 public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
  Canvas nauticalCanvas;RectTransform drawer;TextMeshProUGUI subtitle,context,hoverStatus,materialText;TextMeshProUGUI[] pills=new TextMeshProUGUI[5],toolLabels=new TextMeshProUGUI[4];Button[] toolButtons=new Button[4],cards=new Button[4];Image[] toolImages=new Image[4],cardImages=new Image[4];TextMeshProUGUI[] selectedMarkers=new TextMeshProUGUI[4];GameObject contextPanel,quickActions;float nextUIRefresh;
  readonly Color ivory=new Color(.95f,.92f,.85f),gold=new Color(.88f,.75f,.48f),dim=new Color(.68f,.77f,.73f);
  TextMeshProUGUI Text(Transform parent,string name,string content,Rect rect,int size,Color color){var rt=CreateUIRect(name,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(rect.x,-rect.y),rect.size);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=_cnFont;t.text=content;t.fontSize=size;t.color=color;t.enableWordWrapping=true;t.enableAutoSizing=true;t.fontSizeMin=Mathf.Max(16,size-3);t.fontSizeMax=size;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Ellipsis;t.outlineWidth=.10f;t.outlineColor=new Color32(8,20,22,220);return t;}
  RectTransform Surface(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,Sprite sprite,bool blocks=true){var rt=CreateUIRect(name,parent,anchor,pivot,pos,size);var i=rt.gameObject.AddComponent<Image>();i.sprite=sprite;i.type=Image.Type.Sliced;i.raycastTarget=blocks;return rt;}
  Button IconButton(Transform parent,string name,Rect rect,Sprite icon,string label,Action action,out Image image){var rt=CreateUIRect(name,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(rect.x,-rect.y),rect.size);image=rt.gameObject.AddComponent<Image>();image.sprite=_iconSlotSprite;image.type=Image.Type.Sliced;var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(1,.93f,.76f);colors.pressedColor=new Color(.60f,.77f,.70f);colors.disabledColor=new Color(.55f,.61f,.60f,.75f);b.colors=colors;b.onClick.AddListener(()=>{game.Cue();action();});var ir=CreateUIRect("Icon",rt,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-9),new Vector2(38,38));var ii=ir.gameObject.AddComponent<Image>();ii.sprite=icon;ii.color=gold;ii.raycastTarget=false;var title=Text(rt,"KeyLabel",label,new Rect(3,50,rect.width-6,30),17,ivory);title.alignment=TextAlignmentOptions.Center;return b;}
  void BuildNauticalUI(){
   _cnFont=Resources.Load<TMP_FontAsset>("SeaLifeV21/SeaLifeChinese");
   _roundedCardSprite=BakeNauticalNineSliceSprite("V29_BrassCard",96,96,14,new Color(.035f,.085f,.10f,.94f),new Color(.72f,.60f,.37f,.94f),2);
   _pillBadgeSprite=BakeNauticalNineSliceSprite("V29_ResourcePill",64,40,14,new Color(.035f,.08f,.09f,.94f),new Color(.65f,.56f,.36f,.65f),1);
   _iconSlotSprite=BakeNauticalNineSliceSprite("V29_ToolSlot",72,72,12,new Color(.035f,.08f,.09f,.91f),new Color(.65f,.56f,.37f,.90f),2);
   _iconSlotActiveSprite=BakeNauticalNineSliceSprite("V29_ActiveSlot",72,72,12,new Color(.08f,.23f,.20f,.97f),new Color(.94f,.80f,.49f,1),2);
   icons=new Sprite[7];for(int i=0;i<7;i++)icons[i]=BakeVectorIconSprite(i);
   var node=new GameObject("V29_NauticalHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));node.transform.SetParent(transform,false);nauticalCanvas=node.GetComponent<Canvas>();nauticalCanvas.renderMode=RenderMode.ScreenSpaceOverlay;nauticalCanvas.sortingOrder=48;var sc=node.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1920,1080);sc.matchWidthOrHeight=.5f;var root=node.GetComponent<RectTransform>();
   var header=CreateUIRect("PoeticHeader",root,new Vector2(0,1),new Vector2(0,1),new Vector2(34,-26),new Vector2(840,140));
   Surface(header,"SubtitleBacking",new Vector2(0,1),new Vector2(0,1),new Vector2(-3,-56),new Vector2(810,34),_pillBadgeSprite,false);
   Text(header,"RegionTitle","雾岬岛 · 东岸",new Rect(0,0,490,56),34,ivory);subtitle=Text(header,"RegionSubtitle","红枫内湾 · 海岸小栈",new Rect(1,56,800,32),19,dim);
   float x=0;var widths=new[]{144,126,132,152,128};for(int i=0;i<5;i++){var p=Surface(header,"ResourcePill"+i,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-96),new Vector2(widths[i],36),_pillBadgeSprite,false);pills[i]=Text(p,"ResourceValue","",new Rect(6,3,widths[i]-12,30),18,i==0?gold:ivory);pills[i].alignment=TextAlignmentOptions.Center;x+=widths[i]+8;}
   var dock=CreateUIRect("Tools",root,new Vector2(0,0),new Vector2(0,0),new Vector2(32,24),new Vector2(356,84));
   string[] labels={"L 提灯","2 挂钩","1 钓竿","B 蓝图"};Action[] actions={ToggleLantern,()=>{if(game.InRoom)game.ToggleCloseView();else game.SelectTool(1);},()=>{if(game.InRoom)game.OpenPanel(1);else game.SelectTool(0);},()=>{if(game.InRoom)game.OpenPanel(1);else bay.ToggleGodBuildMode(!bay.godBuildMode);}};
   for(int i=0;i<4;i++){int n=i;toolButtons[i]=IconButton(dock,"Tool"+i,new Rect(i*88,0,78,82),icons[i],labels[i],actions[n],out toolImages[i]);toolLabels[i]=toolButtons[i].transform.Find("KeyLabel").GetComponent<TextMeshProUGUI>();}
   var quick=CreateUIRect("QuickActions",root,new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-28),new Vector2(370,44));
   quickActions=quick.gameObject;SmallButton(quick,"Tab 鱼箱",new Rect(0,0,116,42),()=>game.OpenPanel(2));SmallButton(quick,"F 上下船",new Rect(126,0,116,42),()=>game.ToggleBoard());SmallButton(quick,"暂停",new Rect(252,0,116,42),()=>{game.Paused=!game.Paused;Time.timeScale=game.Paused?0:1;game.ui.RebuildPanel();});
   var prompt=Surface(root,"Context",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,28),new Vector2(700,68),_roundedCardSprite,false);contextPanel=prompt.gameObject;context=Text(prompt,"ContextText","",new Rect(16,8,668,52),20,ivory);context.alignment=TextAlignmentOptions.Center;
   drawer=Surface(root,"BlueprintDrawer",new Vector2(1,0),new Vector2(1,0),new Vector2(-26,24),new Vector2(660,248),_roundedCardSprite);
   Text(drawer,"DrawerTitle","海湾营造蓝图",new Rect(18,9,240,36),23,gold);SmallButton(drawer,"R 旋转",new Rect(384,9,118,32),bay.RotateV29);SmallButton(drawer,"G 返回",new Rect(512,9,130,32),()=>bay.ToggleGodBuildMode(false));
   materialText=Text(drawer,"BuildMaterials","",new Rect(18,43,624,28),17,dim);
   string[] names={"木栈桥","暖光马灯","珊瑚花园","捕蟹笼"},costs={"浮木 2","木 1 · 铜 1","石 2 · 苗 1","木 1 · 石 1"};int[] iconIndex={4,0,5,6};
   for(int i=0;i<4;i++){int n=i;cards[i]=IconButton(drawer,"BuildCard"+i,new Rect(18+i*158,77,150,114),icons[iconIndex[i]],(i+1)+" "+names[i],()=>bay.SelectV29(n),out cardImages[i]);var label=cards[i].transform.Find("KeyLabel").GetComponent<TextMeshProUGUI>();label.fontSize=18;var t=Text(cards[i].transform,"Cost",costs[i],new Rect(3,78,144,30),17,dim);t.alignment=TextAlignmentOptions.Center;selectedMarkers[i]=Text(cards[i].transform,"Selected","◆",new Rect(9,6,24,32),18,gold);}
   hoverStatus=Text(drawer,"HoverStatus","",new Rect(18,207,624,30),18,ivory);drawer.gameObject.SetActive(false);
  }
  void SmallButton(Transform parent,string title,Rect r,Action action){var p=CreateUIRect(title,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(r.x,-r.y),r.size);var image=p.gameObject.AddComponent<Image>();image.sprite=_pillBadgeSprite;image.type=Image.Type.Sliced;var b=p.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>{if(game.Busy&&title!="暂停")return;game.Cue();action();});var t=Text(p,"Label",title,new Rect(5,1,r.width-10,Mathf.Max(30,r.height-2)),18,ivory);t.alignment=TextAlignmentOptions.Center;}
  void RefreshNauticalUI(){
   game.ui.HidePersistentV29();nauticalCanvas.enabled=game.ui.HudV29;if(Time.unscaledTime<nextUIRefresh)return;nextUIRefresh=Time.unscaledTime+.08f;bool planning=bay.godBuildMode,modal=game.Panel!=0||game.Paused;
   quickActions.SetActive(!planning&&!modal);drawer.gameObject.SetActive(planning&&!modal);contextPanel.SetActive(!planning&&!modal);if(planning)subtitle.text="海湾营造 · WASD 漫游 · 滚轮缩放 · G / B 返回";else{var p=game.ActivePosition;subtitle.text=game.InRoom?"潮汐小馆 · 收集海湾的故事":p.x>27?"外海屏障礁脉 · 风从岛链间穿过":p.z<-22?"南湾半月浅滩 · 沿岸寻潮":p.z>26?"北岬珊瑚浅湾 · 远礁相连":"红枫内湾 · 海岸小栈";}
   var d=bay.DataV29;pills[0].text="金币 "+game.save.coins;pills[1].text="活鱼 "+game.CarryCount+" / 6";pills[2].text="浮木 "+d.wood;pills[3].text="石 / 苗 "+d.stone+" / "+d.coralSeed;pills[4].text="黄铜 "+d.brass;
   for(int i=0;i<4;i++){bool active=i==0?LanternOn:i==1?!planning&&game.SelectedTool==1:i==2?!planning&&game.SelectedTool==0:planning;toolImages[i].sprite=active?_iconSlotActiveSprite:_iconSlotSprite;toolButtons[i].interactable=!modal&&!game.Busy&&(i!=1||!game.OnFoot);bool selected=bay.SelectedV29==i;cardImages[i].sprite=selected?_iconSlotActiveSprite:_iconSlotSprite;selectedMarkers[i].gameObject.SetActive(selected);}
   toolLabels[1].text=game.InRoom?"V 观赏":"2 挂钩";toolLabels[2].text=game.InRoom?"B 展缸":"1 钓竿";
   context.text=game.Busy?game.Hint:Time.unscaledTime<game.NoticeTime?game.Notice:game.Hint+(!game.InRoom&&!game.OnFoot&&!game.Busy?"  ·  滚轮看远海":"");materialText.text="浮木 "+d.wood+"    潮汐石 "+d.stone+"    珊瑚苗 "+d.coralSeed+"    黄铜 "+d.brass;hoverStatus.text=bay.FeedbackV29.Length>0?bay.FeedbackV29:bay.HoverV29;
  }
 }
}
