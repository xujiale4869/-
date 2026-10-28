using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Deepsea
{
    public enum UiPage {None,Inventory,Loadout,Residents,Harbour,Site,Pause}
    // One Canvas owns every modal and restores the pause state it inherited.
    [DefaultExecutionOrder(-50)]
    public sealed class UIRoot : MonoBehaviour
    {
        public static readonly HandTool[] Tools={HandTool.Axe,HandTool.Repair,HandTool.CutNet,HandTool.Buoy,HandTool.Bait,HandTool.Observe};
        public static readonly string[] ToolNames={"工作斧","修理箱","割网钳","观测浮标","诱饵盒","望远镜"};
        public static readonly string[] ToolIcons={"axe","tool-case","scissors","radio-tower","fish","binoculars"};
        public static readonly string[] ToolUses={"砍伐本岛海松，拾取原木并带回村仓建设。","检查故障后，用随身维修构件修复观测站和青鹭号。","清除潮池缠网，打开可见捷径。附近有觅食兽时可等待、绕行或投饵。","在海面部署 18 米覆盖的观测点；近处再次使用可回收。","向鼠标位置投放食物，引开会觅食的生物，给清网或守港留出时间。","放大远处画面并随鼠标扫视岸线。R 收起；移动或切换工具退出。"};
        public GameDirector game;
        public UiPage Page {get;private set;}
        public bool IsOpen {get{return Page!=UiPage.None;}}
        public Canvas Canvas {get;private set;}
        CanvasScaler scaler;
        TMP_FontAsset font;
        RectTransform root,hud,modal,content;
        TextMeshProUGUI placeText,goalText,statusText,hintText,noticeText,scopeText,footer,workerStatus;
        readonly List<Image> toolCards=new List<Image>();
        readonly List<TextMeshProUGUI> toolLabels=new List<TextMeshProUGUI>();
        readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
        ItemLocation location=ItemLocation.Backpack;
        string selected="tool:0";
        bool inheritedPause;
        float uiScale=1;
        UiPage renderedPage;ItemLocation renderedLocation;
        RectTransform noticeRect;
        public readonly Color Ink=new Color(.86f,.93f,.92f),Dim=new Color(.57f,.70f,.71f),Gold=new Color(.97f,.76f,.43f);
        static readonly Color Panel=new Color(.035f,.085f,.105f,.99f),Card=new Color(.065f,.135f,.155f),Selected=new Color(.18f,.29f,.29f);
        public void Initialize(GameDirector director)
        {
            game=director;font=Resources.Load<TMP_FontAsset>("UI/ChineseUI");
            if(font==null)throw new InvalidOperationException("ChineseUI font was not prepared");
            var c=new GameObject("归潮湾 · 统一界面",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            c.transform.SetParent(transform,false);Canvas=c.GetComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=10;
            root=c.GetComponent<RectTransform>();scaler=c.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(FindObjectOfType<EventSystem>()==null)new GameObject("界面输入",typeof(EventSystem),typeof(StandaloneInputModule));
            uiScale=PlayerPrefs.GetFloat("DeepseaUIScale",1);SetScale(uiScale,false);
            BuildHud();
        }
        public void SetScale(float value,bool save=true)
        {
            uiScale=value;scaler.referenceResolution=new Vector2(1920,1080)/value;
            if(save){PlayerPrefs.SetFloat("DeepseaUIScale",value);PlayerPrefs.Save();}
        }
        RectTransform Node(Transform parent,string name)
        {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);return g.GetComponent<RectTransform>();}
        RectTransform Box(Transform parent,string name,Color color)
        {var r=Node(parent,name);r.gameObject.AddComponent<Image>().color=color;return r;}
        void Stretch(RectTransform r,float left=0,float right=0,float top=0,float bottom=0)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
        void Anchor(RectTransform r,Vector2 min,Vector2 max,Vector2 pivot,Vector2 size,Vector2 pos)
        {r.anchorMin=min;r.anchorMax=max;r.pivot=pivot;r.sizeDelta=size;r.anchoredPosition=pos;}
        void Size(Transform t,float height=-1,float width=-1,float flex=0)
        {var l=t.GetComponent<LayoutElement>();if(l==null)l=t.gameObject.AddComponent<LayoutElement>();if(height>=0){l.minHeight=height;l.preferredHeight=height;}if(width>=0){l.minWidth=width;l.preferredWidth=width;}l.flexibleWidth=flex;}
        void Layout(RectTransform r,bool horizontal,int gap=16,int pad=0)
        {
            HorizontalOrVerticalLayoutGroup l=horizontal?(HorizontalOrVerticalLayoutGroup)r.gameObject.AddComponent<HorizontalLayoutGroup>():r.gameObject.AddComponent<VerticalLayoutGroup>();
            l.spacing=gap;l.padding=new RectOffset(pad,pad,pad,pad);l.childControlHeight=true;l.childControlWidth=true;l.childForceExpandWidth=!horizontal;l.childForceExpandHeight=false;
        }
        TextMeshProUGUI Text(Transform p,string text,int size=20,Color? color=null,float height=-1)
        {
            var r=Node(p,text.Length>12?text.Substring(0,12):text);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color??Ink;t.enableWordWrapping=true;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Overflow;
            if(height>=0)Size(r,height);else {var le=r.gameObject.AddComponent<LayoutElement>();le.minHeight=size*1.5f;le.preferredHeight=-1;}
            return t;
        }
        Image Icon(Transform p,string id,int size=32)
        {
            Sprite s;if(!icons.TryGetValue(id,out s)){s=Resources.Load<Sprite>("UI/"+id);icons[id]=s;}
            var r=Node(p,id);var image=r.gameObject.AddComponent<Image>();image.sprite=s;image.color=Ink;image.preserveAspect=true;image.raycastTarget=false;Size(r,size,size);return image;
        }
        Button Button(Transform p,string title,Action action,bool enabled=true,string icon=null,float height=52)
        {
            var r=Box(p,title,Card);Size(r,height,-1,1);Layout(r,true,12,12);
            if(icon!=null)Icon(r,icon,26);
            var t=Text(r,title,20,enabled?Ink:Dim);t.alignment=TextAlignmentOptions.MidlineLeft;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.35f,1.25f,1.05f);colors.pressedColor=new Color(.75f,.8f,.8f);colors.selectedColor=new Color(1.2f,1.16f,1);colors.disabledColor=new Color(.7f,.7f,.7f);b.colors=colors;
            b.onClick.AddListener(()=>{action();});return b;
        }
        RectTransform Row(Transform p,float height=-1)
        {var r=Node(p,"横向布局");Layout(r,true);if(height>=0)Size(r,height);return r;}
        RectTransform Column(Transform p,float flex=1,float width=-1)
        {var r=Node(p,"纵向布局");Layout(r,false);Size(r,-1,width,flex);return r;}
        RectTransform Scroll(Transform p)
        {
            var view=Box(p,"滚动区域",new Color(.025f,.065f,.08f));var elem=view.gameObject.AddComponent<LayoutElement>();elem.flexibleHeight=1;elem.flexibleWidth=1;
            var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            view.gameObject.AddComponent<RectMask2D>();var contents=Node(view,"内容");contents.anchorMin=new Vector2(0,1);contents.anchorMax=Vector2.one;contents.pivot=new Vector2(.5f,1);contents.sizeDelta=Vector2.zero;Layout(contents,false,12,16);
            var fitter=contents.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.viewport=view;scroll.content=contents;
            var rail=Box(view,"滚动条",new Color(.1f,.2f,.22f));Anchor(rail,new Vector2(1,0),Vector2.one,new Vector2(1,.5f),new Vector2(7,-16),new Vector2(-3,0));
            var handle=Box(rail,"滑块",new Color(.5f,.65f,.63f));Stretch(handle);
            var bar=rail.gameObject.AddComponent<Scrollbar>();bar.targetGraphic=handle.GetComponent<Image>();bar.handleRect=handle;bar.direction=Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            return contents;
        }
        void BuildHud()
        {
            hud=Node(root,"HUD");Stretch(hud);
            var top=Box(hud,"当前目标",Panel);Anchor(top,new Vector2(0,1),new Vector2(0,1),new Vector2(0,1),new Vector2(480,150),new Vector2(24,-24));Layout(top,false,6,18);
            placeText=Text(top,"归潮湾",28,Gold,42);goalText=Text(top,"",20,Ink,60);
            var map=Box(hud,"小海图",Panel);Anchor(map,Vector2.one,Vector2.one,Vector2.one,new Vector2(245,230),new Vector2(-24,-24));Layout(map,false,4,14);Text(map,"近海海图 · M",20,Gold,30);var mini=Node(map,"地图");Size(mini,158);mini.gameObject.AddComponent<BayMapGraphic>().game=game;
            var bottom=Node(hud,"航行与工具");Anchor(bottom,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(-48,138),new Vector2(24,24));Layout(bottom,true,16);
            var status=Box(bottom,"状态",Panel);Size(status,138,260);Layout(status,false,8,14);statusText=Text(status,"",18,Ink,104);
            var toolbar=Node(bottom,"工具栏");Layout(toolbar,true,8);Size(toolbar,138,-1,1);
            for(int i=0;i<Tools.Length;i++)
            {
                int index=i;var r=Box(toolbar,ToolNames[i],Panel);Size(r,138,-1,1);Layout(r,false,2,10);
                var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(()=>game.Expedition.SelectTool(Tools[index]));
                Icon(r,ToolIcons[i],30);var label=Text(r,(i+1)+" "+ToolNames[i],18,Ink,30);toolLabels.Add(label);Text(r,i==3||i==4?"有数量":"可重复使用",14,Dim,22);toolCards.Add(r.GetComponent<Image>());
            }
            var hint=Box(hud,"附近动作",Panel);Anchor(hint,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(820,78),new Vector2(0,178));Layout(hint,false,0,14);hintText=Text(hint,"",20,Gold,48);
            var notice=Box(hud,"操作反馈",Panel);noticeRect=notice;Anchor(notice,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(780,85),new Vector2(0,-190));Layout(notice,false,0,14);noticeText=Text(notice,"",18,Ink,56);
            var scope=Node(hud,"远望状态");Anchor(scope,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(460,48),new Vector2(0,100));scopeText=Text(scope,"远望 × 2.6  ·  鼠标扫视 / R 收起",20,Gold,48);Stretch(scopeText.rectTransform);
            var bar=Row(top,0);bar.gameObject.SetActive(false);
        }
        public void Open(UiPage page)
        {
            if(page==UiPage.None){Close();return;}
            if(!IsOpen)inheritedPause=game.Paused;
            Page=page;game.HarbourMenu=page==UiPage.Harbour;
            if(page!=UiPage.Site)game.Expedition.ClearPanelState();
            game.SetPause(true);game.Boat.InputBlocked=true;game.Expedition.Explorer.Blocked=true;game.CameraFollow.Scoping=false;
            hud.gameObject.SetActive(false);Rebuild();
        }
        public void Close()
        {
            if(!IsOpen)return;
            Page=UiPage.None;game.HarbourMenu=false;game.Expedition.ClearPanelState();
            if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}
            game.SetPause(inheritedPause);hud.gameObject.SetActive(!inheritedPause);EventSystem.current.SetSelectedGameObject(null);
        }
        public void Rebuild()
        {
            var oldScrolls=modal==null?new ScrollRect[0]:modal.GetComponentsInChildren<ScrollRect>();
            float[] positions=new float[oldScrolls.Length];for(int i=0;i<oldScrolls.Length;i++)positions[i]=oldScrolls[i].verticalNormalizedPosition;
            bool retain=renderedPage==Page&&renderedLocation==location;
            if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}
            workerStatus=null;modal=Box(root,"唯一模态窗口",new Color(.006f,.02f,.03f,.96f));Stretch(modal);
            var frame=Box(modal,"管理面板",Panel);Stretch(frame,32,32,28,28);Layout(frame,false,16,24);
            var header=Row(frame,50);Text(header,Title(),30,Gold,46);Button(header,"关闭 · Esc",Close,true,"x",48);header.GetChild(0).GetComponent<LayoutElement>().flexibleWidth=1;var closeLayout=header.GetChild(1).GetComponent<LayoutElement>();closeLayout.preferredWidth=180;closeLayout.minWidth=180;closeLayout.flexibleWidth=0;
            Text(frame,(game.Expedition.AtVillage?"归潮村现场":game.Expedition.OnFoot?"雾岬岛 · 徒步":"浅湾 · 工作艇")+"  /  已暂停  /  Tab 背包",16,Dim,26);
            if(Page==UiPage.Inventory||Page==UiPage.Loadout||Page==UiPage.Residents)
            {var tabs=Row(frame,52);Button(tabs,"背包与仓储",()=>Open(UiPage.Inventory),true,"backpack");Button(tabs,"船只配装",()=>Open(UiPage.Loadout),true,"ship");Button(tabs,"居民协作",()=>Open(UiPage.Residents),true,"users");}
            content=Node(frame,"当前页面");Layout(content,false);var le=content.gameObject.AddComponent<LayoutElement>();le.flexibleHeight=1;
            if(Page==UiPage.Inventory)Inventory();else if(Page==UiPage.Loadout)Loadout();else if(Page==UiPage.Residents)Residents();else if(Page==UiPage.Harbour)Harbour();else if(Page==UiPage.Site)Site();else Pause();
            footer=Text(frame,game.Notice??"",16,Gold,52);
            UnityEngine.Canvas.ForceUpdateCanvases();
            var scrolls=modal.GetComponentsInChildren<ScrollRect>();if(retain)for(int i=0;i<Mathf.Min(scrolls.Length,positions.Length);i++)scrolls[i].verticalNormalizedPosition=positions[i];
            renderedPage=Page;renderedLocation=location;
            var first=modal.GetComponentInChildren<Button>();if(first!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        string Title(){return Page==UiPage.Inventory?"背包与仓储":Page==UiPage.Loadout?"一级近海工作艇":Page==UiPage.Residents?"归来的人，新的帮手":Page==UiPage.Harbour?"归潮村 · 港口与建设":Page==UiPage.Site?game.Expedition.OpenSite.name:"暂停 · 海在等你";}
        void Do(Action action){action();Rebuild();}
        string Loc(ItemLocation l){return l==ItemLocation.Backpack?"随身背包":l==ItemLocation.Ship?"工作艇船舱":"归潮村仓库";}
        int Count(string id,ItemLocation l){return id=="parts"?AssetRules.PartsAt(game.progress,game.trip,l):AssetRules.Stock(game.progress,id).At(l);}
        string ItemName(string id){return id=="parts"?"维修构件":id=="wood"?"原木":id=="bait"?"诱饵":"观测浮标";}
        string ItemIcon(string id){return id=="parts"?"wrench":id=="wood"?"logs":id=="bait"?"fish":"radio-tower";}
        public string TransferReason(string id,ItemLocation from,ItemLocation to)
        {
            if((from==ItemLocation.Village||to==ItemLocation.Village)?!game.Expedition.AtVillage:!game.Expedition.AtShip)return from==ItemLocation.Village||to==ItemLocation.Village?"请返回归潮村现场":"请回到工作艇旁";
            if(id=="wood"&&(from==ItemLocation.Village||to==ItemLocation.Village)&&!game.Timber.AtStore)return "请到村仓工具架旁交付或取用原木";
            if(Count(id,from)<1)return "来源库存为 0";
            if(id=="parts")
            {
                if(to==ItemLocation.Village)return "构件请靠泊卸载结算";
                if(to==ItemLocation.Backpack&&Count(id,to)>=6)return "背包构件已满 6/6";
                if(to==ItemLocation.Ship&&game.Expedition.CargoUsed>=game.settings.cargoSlots)return "船舱已满";
            }
            if(id=="wood"&&Count(id,to)>=(to==ItemLocation.Village?24:to==ItemLocation.Backpack?6:8))return "原木容器已满";
            return "";
        }
        public bool Transfer(string id,ItemLocation from,ItemLocation to)
        {
            string reason=TransferReason(id,from,to);if(reason!=""){game.Notify(reason);return false;}
            bool ok=id=="parts"?game.Expedition.MoveParts(from,to,1):game.Expedition.MoveItem(id,from,to);
            if(ok){game.Timber.Refresh();game.Notify("已将 1 "+ItemName(id)+"从"+Loc(from)+"移至"+Loc(to));}return ok;
        }
        void Inventory()
        {
            var row=Row(content);row.gameObject.AddComponent<LayoutElement>().flexibleHeight=1;
            var places=Scroll(row);var placeSize=places.parent.GetComponent<LayoutElement>();placeSize.minWidth=190;placeSize.preferredWidth=190;placeSize.flexibleWidth=0;places.GetComponent<VerticalLayoutGroup>().padding=new RectOffset(0,10,0,0);Text(places,"物品位置",18,Dim,30);
            foreach(ItemLocation l in new[]{ItemLocation.Backpack,ItemLocation.Ship,ItemLocation.Village}){var target=l;Button(places,Loc(l),()=>{location=target;Rebuild();},true,l==ItemLocation.Backpack?"backpack":l==ItemLocation.Ship?"ship":"warehouse",70);}
            Text(places,"查看各处库存；取放需要到场。\n\n原木容量\n随身 6 / 船舱 8 / 村仓 24",16,Dim,160);
            var list=Scroll(row);list.parent.gameObject.GetComponent<LayoutElement>().flexibleWidth=1;
            Text(list,Loc(location),24,Gold,42);Text(list,"可消耗物资",16,Dim,26);
            foreach(string id in new[]{"parts","wood","buoy","bait"}){string k=id;var b=Button(list,ItemName(id)+"    × "+Count(id,location),()=>{selected=k;Rebuild();},true,ItemIcon(id),64);if(selected==id)b.image.color=Selected;}
            if(location==ItemLocation.Backpack)
            {
                Text(list,"随身工具 · 已从村中工具架领用",16,Dim,40);
                for(int i=0;i<Tools.Length;i++)
                {int index=i;var b=Button(list,ToolNames[i]+(game.Expedition.Tool==Tools[i]?" · 手持":" · 已持有"),()=>{selected="tool:"+index;Rebuild();},true,ToolIcons[i],64);if(selected=="tool:"+i)b.image.color=Selected;}
            }
            var detail=Scroll(row);detail.parent.gameObject.GetComponent<LayoutElement>().flexibleWidth=1.2f;
            if(selected.StartsWith("tool:")&&location==ItemLocation.Backpack)
            {
                int i=int.Parse(selected.Substring(5));Icon(detail,ToolIcons[i],60);Text(detail,ToolNames[i],28,Gold,48);Text(detail,ToolUses[i],20,Ink,120);
                Text(detail,i==3||i==4?"有数量 / 当前随身 "+Count(i==3?"buoy":"bait",ItemLocation.Backpack):"归属：玩家随身\n可重复使用，无耐久消耗",18,Dim,70);
                Button(detail,game.Expedition.Tool==Tools[i]?"正在手持":"装备 · "+(i+1)+" 选择",()=>Do(()=>game.Expedition.SelectTool(Tools[i])),true,ToolIcons[i]);
                Text(detail,"关闭背包后按 R 使用；靠近基础对象也可按 E 取用合适工具。",18,Dim,80);
            }
            else
            {
                if(selected.StartsWith("tool:"))selected="parts";
                string id=selected;Icon(detail,ItemIcon(id),60);Text(detail,ItemName(id)+"  × "+Count(id,location),28,Gold,48);
                Text(detail,id=="parts"?"用于船体和观测设施维修。随身构件上限 6。":id=="wood"?"采伐本岛海松获得。拾取后带回村仓，可用 6 原木修复西侧装卸码头。":id=="bait"?ToolUses[4]:ToolUses[3],20,Ink,110);
                Text(detail,"来源位置："+Loc(location),18,Dim,34);
                if(id=="parts"){int pending=location==ItemLocation.Ship?game.trip.parts:location==ItemLocation.Backpack?game.trip.packParts:0;Text(detail,"其中本航次待结算 "+pending+" 件\n村仓只显示已拥有库存。",16,Dim,62);}
                foreach(ItemLocation target in new[]{ItemLocation.Backpack,ItemLocation.Ship,ItemLocation.Village})
                {
                    if(target==location)continue;var to=target;string reason=TransferReason(id,location,to);
                    Button(detail,"移动 1 件 → "+Loc(to),()=>Do(()=>Transfer(id,location,to)),reason=="","arrow-right");if(reason!="")Text(detail,reason,16,Dim,30);
                }
                if(id=="wood")Button(detail,"交付全部随身原木",()=>Do(()=>game.Timber.Deposit()),game.Timber.AtStore&&Count("wood",ItemLocation.Backpack)>0,"warehouse");
            }
        }
        void Loadout()
        {
            var c=Scroll(content);Text(c,"一级近海工作艇 · 功能位 "+game.progress.modules.Count+" / 2",26,Gold,48);
            Text(c,game.Expedition.CanChangeModules?"归潮村现场，可以配装。":"请回归潮村并减速，或下船后到工作坊配装。",18,Dim,44);
            string[] names={"观测模块","助力拖带模块","现场维修模块"};string[] uses={"短距探测范围增加 12 米。","提高拖带速度与缆索稳定性。","青鹭号现场维修由 3 构件降至 2。"};string[] ico={"radio-tower","anchor","wrench"};
            for(int i=0;i<3;i++)
            {var module=(ShipModule)i;bool installed=game.progress.modules.Contains(module),full=game.progress.modules.Count>=2&&!installed;Text(c,names[i]+(installed?" · 已装配":" · 未装配"),23,Ink,38);Text(c,uses[i],18,Dim,40);Button(c,installed?"取回模块":full?"先取回一个模块":"安装模块",()=>Do(()=>game.Expedition.ToggleModule(module)),game.Expedition.CanChangeModules&&!full,ico[i]);}
            Text(c,"接应船",26,Gold,48);
            foreach(var boat in game.progress.boats){var b=boat;Text(c,b.name+" · "+(b.repaired?"船况良好":"待修理"),20,Ink,36);if(!b.repaired)Button(c,"村仓 2 构件修船",()=>Do(()=>game.Expedition.RepairHarbourBoat(b.id)),game.Expedition.AtVillage&&game.progress.parts>=2,"wrench");}
            Button(c,game.progress.relayAssigned?"撤回东岸接应船":"安排已修船前往东岸接应",()=>Do(game.Expedition.AssignRelay),game.Expedition.AtVillage&&game.progress.boats.Exists(x=>x.repaired),"ship");
        }
        void Residents()
        {
            var c=Scroll(content);bool settled=game.progress.people.Exists(p=>p.id=="linzhou");Text(c,"林舟 · 船匠与木工备料",28,Gold,48);
            if(!settled){Text(c,game.trip.people.Exists(p=>p.id=="linzhou")?"随船返航中 · 请先靠泊安置":game.Expedition.Companion.State,22,Ink,50);Text(c,"救援后让林舟同行，等他抵达工作艇旁，再一起登船返村。",20,Dim,80);return;}
            Text(c,"已安置 · 使用自己的工作斧，与玩家工具分开。",18,Dim,40);
            workerStatus=Text(c,game.Timber.WorkerState,24,Ink,48);
            Button(c,game.progress.timber.assigned?"停止备料 · 先交付手中木料":"委托采木 · 本岛林地 → 归潮村仓库",()=>Do(()=>game.Timber.Assign()),game.Expedition.AtVillage,"axe",62);
            Text(c,"累计交付 "+game.progress.timber.delivered+" 原木  /  村仓现有 "+Count("wood",ItemLocation.Village)+"\n任务：本岛林地采木 → 拾取 → 搬回村仓；每次委托备料 6 根后停止。",20,Ink,100);
            Text(c,"你可以继续远征。林舟会在世界中行走、挥斧、搬运；交付的木料和你采集的木料用于同一座码头。",20,Dim,90);
            if(!game.Expedition.AtVillage)Text(c,"请回归潮村现场下达或变更工作。",18,Dim,38);
            Button(c,"定位林舟",()=>{Close();game.CameraFollow.SetWalking(game.Timber.Worker,true);},game.Timber.Worker!=null,"users");
            Button(c,game.progress.technicianAssigned?"结束船坞保养 · 休息":"安排船坞保养 · 航行耗油 −15%",()=>Do(game.Expedition.AssignTechnician),game.Expedition.AtVillage,"wrench",62);
            Text(c,"一次只承担一项工作。改派保养会停止新采伐，已经带回的成果保留。",18,Dim,64);
        }
        void Harbour()
        {
            var c=Scroll(content);Text(c,"村仓  原木 "+Count("wood",ItemLocation.Village)+" / 24   维修构件 "+game.progress.parts+"   设备 "+game.progress.equipment+"   记录 "+game.progress.records,22,Ink,50);
            bool built=game.progress.timber.pierBuilt;Text(c,built?"西侧装卸码头 · 已修复":"破损的西侧装卸码头",26,Gold,44);
            Text(c,built?"可走上新增平台，也可从西侧低速靠近后按 F 登岸。":"需要村仓 6 原木。玩家自采或委托林舟备料，均可用于建设。",20,Dim,65);
            Button(c,"交付随身原木",()=>Do(()=>game.Timber.Deposit()),game.Timber.AtStore&&Count("wood",ItemLocation.Backpack)>0,"logs");
            Button(c,built?"码头已完成":"使用 6 原木修复码头",()=>Do(()=>game.Timber.BuildPier()),!built&&Count("wood",ItemLocation.Village)>=6&&game.Expedition.AtVillage,"hammer");
            Button(c,"打开背包与仓储",()=>Open(UiPage.Inventory),true,"backpack");Button(c,"安排居民备料",()=>Open(UiPage.Residents),true,"users");
            Text(c,"港内设施",26,Gold,45);
            Build(c,Building.Winch,"船坞轻绞盘",game.progress.winch,game.settings.winchParts,game.settings.winchEquipment,0,"稳定拖带并回收重型设备。");
            Build(c,Building.Charts,"海图屋观测平台",game.progress.charts,game.settings.chartParts,0,game.settings.chartRecords,"扩大短距探测范围。");
            Build(c,Building.Net,"缆网台",game.progress.net,game.settings.netParts,game.settings.netEquipment,0,"保护外侧渔获平台，可主动演练。");
            if(game.progress.net)Button(c,"开始守港演练",()=>{Close();game.Defense.StartDrill();},!game.Defense.Active,"anchor");
        }
        void Build(Transform c,Building b,string name,bool exists,int parts,int equipment,int records,string use)
        {
            Text(c,name+(exists?" · 已完成":""),22,Ink,36);Text(c,parts+" 构件 + "+equipment+" 设备 + "+records+" 记录 / "+use,18,Dim,48);
            Button(c,exists?"已经建成":"建设 "+name,()=>Do(()=>{bool old=game.HarbourMenu;game.HarbourMenu=true;game.Build(b);game.HarbourMenu=old;}),!exists&&game.CanDock&&game.progress.parts>=parts&&game.progress.equipment>=equipment&&game.progress.records>=records,"hammer");
        }
        void Site()
        {
            var c=Scroll(content);var e=game.Expedition;Text(c,e.Inspection,22,Ink,110);
            Text(c,"随身维修构件 "+Count("parts",ItemLocation.Backpack)+" / 6 · 村仓不能远程供料",18,Dim,45);
            if(e.OpenSite.kind==IslandSiteKind.Station)
            {
                if(!game.progress.island.stationPowered)
                {Button(c,"检查电池",()=>Do(()=>e.Inspect(false)),true,"package");Button(c,"沿电缆检查接头",()=>Do(()=>e.Inspect(true)),true,"wrench");Button(c,"取用修理箱 · 用 2 构件接回接头",()=>Do(()=>{e.SelectTool(HandTool.Repair);e.RepairStation();}),game.progress.island.connectorDiagnosed&&Count("parts",ItemLocation.Backpack)>=2,"tool-case",64);}
                else
                {
                    Text(c,"天线 "+e.AntennaBearing.ToString("0")+"° · 求救 "+(e.SignalStrength(false)*100).ToString("0")+"% / 作业 "+(e.SignalStrength(true)*100).ToString("0")+"%",20,Gold,50);
                    var row=Row(c,54);Button(row,"向西转 10°",()=>Do(()=>e.SetBearing(Mathf.Repeat(e.AntennaBearing-10,360))));Button(row,"向东转 10°",()=>Do(()=>e.SetBearing(Mathf.Repeat(e.AntennaBearing+10,360))));
                    Button(c,"读取求救频道",()=>Do(()=>e.ReadSignal(false)),true,"radio-tower");Button(c,"读取作业回波",()=>Do(()=>e.ReadSignal(true)),true,"radio-tower");
                }
            }
            else
            {Button(c,"检查青鹭号故障",()=>Do(e.DiagnoseBoat),true,"ship");int cost=game.progress.modules.Contains(ShipModule.FieldRepair)?2:3;Button(c,"取用修理箱 · 现场维修 "+cost+" 构件",()=>Do(()=>{e.SelectTool(HandTool.Repair);e.RepairBoat();}),game.progress.island.boatDiagnosed&&!game.progress.island.boatRepaired&&Count("parts",ItemLocation.Backpack)>=cost,"wrench",64);Text(c,"也可先带林舟回船，再从海上拖回青鹭号。修船与救人分别完成。",20,Dim,90);}
        }
        void Pause()
        {
            var c=Scroll(content);Button(c,"继续",()=>{inheritedPause=false;Close();},true,"arrow-right");
            Text(c,"UI 缩放",24,Gold,44);var row=Row(c,52);foreach(float scale in new[]{1f,1.25f,1.5f}){float value=scale;Button(row,(value*100).ToString("0")+"%",()=>Do(()=>SetScale(value)));}
            Volume(c,"环境音乐",()=>game.Audio.MusicVolume,v=>game.Audio.MusicVolume=v);
            Volume(c,"船声与交互",()=>game.Audio.EffectsVolume,v=>game.Audio.EffectsVolume=v);
            Text(c,"村船接回会放弃本航次未结算收获，保留村庄建设与已拥有物资。",18,Dim,65);
            Button(c,"放弃本航次 · 请求村船接回",()=>{Close();game.EmergencyReturn();},true,"anchor",62);
            Button(c,"保存村庄并退出",()=>{game.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
            },true,"warehouse");
        }
        void Volume(Transform c,string title,Func<float> get,Action<float> set)
        {Text(c,title+" "+(get()*100).ToString("0")+"%",20,Ink,36);var row=Row(c,52);Button(row,"减少",()=>Do(()=>set(Mathf.Max(0,get()-.1f))));Button(row,"增加",()=>Do(()=>set(Mathf.Min(1,get()+.1f))));}
        void Update()
        {
            if(game==null)return;
            if(Input.GetKeyDown(KeyCode.Escape)){if(IsOpen)Close();else if(game.CameraFollow.Scoping)game.CameraFollow.Scoping=false;else Open(UiPage.Pause);}
            else if(Input.GetKeyDown(KeyCode.Tab)){if(IsOpen)Close();else Open(UiPage.Inventory);}
            if(IsOpen){if(footer!=null)footer.text=game.Notice;if(workerStatus!=null)workerStatus.text=game.Timber.WorkerState;return;}
            var e=game.Expedition;
            noticeRect.sizeDelta=new Vector2(Mathf.Min(780,Mathf.Max(360,root.rect.width-570)),85);
            // Returning from a locate command restores follow when movement resumes.
            if((Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.D))&&game.CameraFollow.target!=(e.OnFoot?e.Explorer.transform:game.Boat.transform))game.CameraFollow.SetWalking(e.OnFoot?e.Explorer.transform:game.Boat.transform,e.OnFoot);
            placeText.text=e.AtVillage?"归潮村":e.OnFoot?"雾岬岛":"归潮湾 · 浅海";
            goalText.text=game.progress.timber.pierBuilt?"自主远征 · 救援与调查海域":"修复西侧码头 · 村仓原木 "+Count("wood",ItemLocation.Village)+"/6\n本岛林地可自采，林舟安置后可委托";
            statusText.text=e.OnFoot?"徒步 · "+e.ToolName+"\n随身原木 "+Count("wood",ItemLocation.Backpack)+" / 6\nTab 背包 · Esc 暂停":"船体 "+(game.Boat.Health*100).ToString("0")+"% · 燃油 "+(game.Boat.Fuel*100).ToString("0")+"%\n货舱 "+e.CargoUsed+"/"+game.settings.cargoSlots+" · 乘员 "+game.trip.passengers+"/"+game.settings.passengerSlots+"\nTab 背包 · Q 探测";
            for(int i=0;i<Tools.Length;i++){toolCards[i].color=e.Tool==Tools[i]?Selected:Panel;toolLabels[i].color=e.Tool==Tools[i]?Gold:Ink;toolLabels[i].text=(i+1)+" "+ToolNames[i]+(i==3||i==4?" ×"+Count(i==3?"buoy":"bait",e.OnFoot?ItemLocation.Backpack:ItemLocation.Ship):"");}
            string hint=game.Timber.NearbyHint();if(hint=="")hint=e.OnFoot?(e.NearbySite!=null?"E "+e.NearbySite.name:e.AtShip?"F 登船 · Tab 装卸":"WASD 步行 · Shift 快走 · R 使用手持工具"):game.CanDock?"E 靠泊结算 · F 下船":game.Nearby!=null?game.Nearby.displayName+" · "+game.Nearby.Hint():e.NearestLanding()!=null?"F 主动下船":"W/S 推进倒车 · A/D 打舵 · Q 探测 · M 海图";
            hintText.text=game.CameraFollow.Overview?"海图俯看 · M 返回驾驶":game.Defense.Active?game.Defense.Status:hint;
            noticeText.text=Time.unscaledTime<game.NoticeUntil?game.Notice:"";noticeText.transform.parent.gameObject.SetActive(noticeText.text!="");scopeText.gameObject.SetActive(game.CameraFollow.Scoping);
        }
    }
    public sealed class BayMapGraphic : MaskableGraphic
    {
        public GameDirector game;
        void Update(){SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(game==null)return;Rect r=rectTransform.rect;
            Dot(vh,r,IslandTerrain.Center,38,new Color(.19f,.35f,.30f));Dot(vh,r,game.World.DockPosition,7,new Color(1,.76f,.43f));
            foreach(var l in game.Expedition.Landings)Dot(vh,r,l.berth,4,new Color(.9f,.7f,.4f));
            foreach(var t in game.World.Targets)if(t!=null&&!t.Taken&&t.gameObject.activeInHierarchy&&t.InView)Dot(vh,r,t.transform.position,4,new Color(.4f,.75f,.75f));
            Dot(vh,r,game.Boat.transform.position,7,Color.white);if(game.Expedition.OnFoot)Dot(vh,r,game.Expedition.ActivePosition,6,new Color(1,.65f,.25f));
        }
        void Dot(VertexHelper vh,Rect r,Vector3 p,float size,Color c)
        {float x=r.x+(p.x+110)/220*r.width,y=r.y+(p.z+60)/225*r.height;float h=size*.5f;int i=vh.currentVertCount;foreach(var v in new[]{new Vector2(x-h,y-h),new Vector2(x-h,y+h),new Vector2(x+h,y+h),new Vector2(x+h,y-h)})vh.AddVert(v,c,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}
