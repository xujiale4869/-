using UnityEngine;

namespace Deepsea
{
    public sealed class ExpeditionHud : MonoBehaviour
    {
        public GameDirector game;
        ExpeditionController expedition { get { return game.Expedition; } }
        GUIStyle title,body,small,button;
        Texture2D pixel;
        int page;
        bool initialized;
        void Initialize()
        {
            if(initialized)return;initialized=true;
            Font font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","PingFang SC","Noto Sans CJK SC","Arial"},19);
            title=new GUIStyle(GUI.skin.label){font=font,fontSize=26,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.93f,.76f,.46f);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};body.normal.textColor=new Color(.82f,.9f,.87f);
            small=new GUIStyle(body){fontSize=15};small.normal.textColor=new Color(.56f,.69f,.67f);
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17,wordWrap=true,padding=new RectOffset(8,8,6,6)};
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
        }
        void Panel(Rect rect)
        {GUI.color=new Color(.018f,.055f,.067f,.97f);GUI.DrawTexture(rect,pixel);GUI.color=Color.white;}
        void Label(float x,float y,float width,float height,string value,GUIStyle style){GUI.Label(new Rect(x,y,width,height),value,style);}
        bool Button(float x,float y,float width,float height,string value){return GUI.Button(new Rect(x,y,width,height),value,button);}
        void OnGUI()
        {
            if(game==null||expedition==null)return;Initialize();
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);Matrix4x4 old=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            if(!game.Paused&&!game.HarbourMenu)
            {
                string hint=expedition.OnFoot?(expedition.NearbySite!=null?"E "+expedition.NearbySite.name:"沿踏石环路探索 · F 在当前泊位登船"):
                    (expedition.NearestLanding()!=null?"F 下船 · "+expedition.NearestLanding().name:"Tab 查看资产与配装 · 雾岬岛可从南、东两岸登陆");
                Panel(new Rect(width*.5f-325,height-205,650,52));Label(width*.5f-306,height-195,612,42,hint,body);
                if(expedition.OnFoot)
                {
                    Panel(new Rect(24,height-257,385,81));
                    Label(40,height-248,350,57,"手持："+expedition.ToolName+"（1—5 切换）\nR 使用工具 · 构件在背包："+AssetRules.PartsAt(game.progress,game.trip,ItemLocation.Backpack),body);
                }
            }
            if(expedition.AssetsOpen)DrawAssets(width,height);
            else if(expedition.OpenSite!=null)DrawSite(width,height);
            GUI.matrix=old;
        }

        void DrawAssets(float width,float height)
        {
            float x=width*.5f-470,y=height*.5f-355;
            Panel(new Rect(x,y,940,710));
            Label(x+26,y+18,730,40,"城镇资产与远征准备",title);
            if(Button(x+798,y+20,116,37,"关闭 / Tab"))expedition.ClosePanels();
            string where=expedition.AtVillage?"归潮村现场":expedition.AtShip?"工作艇旁":"远程查看 · 到物品所在位置才可装卸";
            Label(x+26,y+66,880,28,where+"   ·   查看期间已安全暂停",small);
            string[] names={"仓库与工具","船只与配装","居民与协作","设施与海图"};
            for(int i=0;i<names.Length;i++)if(Button(x+26+i*223,y+108,212,42,(page==i?"● ":"")+names[i]))page=i;
            if(page==0)InventoryPage(x+28,y+173);
            else if(page==1)BoatPage(x+28,y+173);
            else if(page==2)ResidentPage(x+28,y+173);
            else DiscoveryPage(x+28,y+173);
            Label(x+28,y+627,882,53,game.Notice,small);
        }
        void InventoryPage(float x,float y)
        {
            Label(x,y,880,28,"构件位置：村仓 "+game.progress.parts+"  /  船舱 "+AssetRules.PartsAt(game.progress,game.trip,ItemLocation.Ship)+
                "  /  背包 "+AssetRules.PartsAt(game.progress,game.trip,ItemLocation.Backpack)+"（最多 6）",body);
            if(Button(x,y+39,270,43,"村仓 → 船舱：2 构件"))expedition.MoveParts(ItemLocation.Village,ItemLocation.Ship,2);
            if(Button(x+291,y+39,270,43,"船舱 → 背包：2 构件"))expedition.MoveParts(ItemLocation.Ship,ItemLocation.Backpack,2);
            if(Button(x+582,y+39,270,43,"背包 → 船舱：2 构件"))expedition.MoveParts(ItemLocation.Backpack,ItemLocation.Ship,2);
            Label(x,y+97,866,42,"观察镜、修理工具、割网工具随身，可重复使用。快捷键 1 / 2 / 3，不占船载模块位。",small);
            ItemRow(x,y+151,"buoy","观测浮标","保留附近回波，靠近后选浮标按 R 可回收。");
            ItemRow(x,y+250,"bait","诱饵盒","岸上引开礁背兽；守港时也可把嗜食兽引向食物。");
            Label(x,y+354,868,58,"新回收物在靠泊前属于本航次。维修只用实际携带的构件；装卸会移动库存，不会复制一份到另一处。",small);
            if(game.CanDock&&Button(x,y+417,420,40,"在村码头卸载并结算本航次")){expedition.ClosePanels();game.Dock();}
            if(!expedition.OnFoot&&game.progress.relayAssigned&&Button(x+440,y+417,410,40,"东岸接应船：请求一次补给"))
                if(!expedition.UseRelay())game.Notify("需要抵达雾岬岛东岸，且本航次尚未使用接应补给。");
        }
        void ItemRow(float x,float y,string id,string name,string use)
        {
            ItemStock stock=AssetRules.Stock(game.progress,id);
            Label(x,y,860,27,name+"  ·  村仓 "+stock.village+" / 船舱 "+stock.ship+" / 背包 "+stock.backpack,body);
            Label(x,y+29,862,25,use,small);
            if(Button(x,y+59,200,35,"村仓 → 船舱"))expedition.MoveItem(id,ItemLocation.Village,ItemLocation.Ship);
            if(Button(x+220,y+59,200,35,"船舱 → 背包"))expedition.MoveItem(id,ItemLocation.Ship,ItemLocation.Backpack);
            if(Button(x+440,y+59,200,35,"背包 → 船舱"))expedition.MoveItem(id,ItemLocation.Backpack,ItemLocation.Ship);
            if(Button(x+660,y+59,200,35,"船舱 → 村仓"))expedition.MoveItem(id,ItemLocation.Ship,ItemLocation.Village);
        }
        void BoatPage(float x,float y)
        {
            Label(x,y,866,52,"当前：一级近海工作艇   ·   功能位 "+game.progress.modules.Count+" / 2\n基本航行、接人和调查始终可用。模块改变处理办法与效率。",body);
            string[] names={"观测模块","助力拖带模块","现场维修模块"};
            string[] effects={"探测范围增加 12；适合读线索与寻找海上回波。","拖带速度与缆索稳定性提升；重件仍需船坞轻绞盘。","青鹭号现场修复由 3 构件减少为 2；基本修理工具也能修复。"};
            for(int i=0;i<3;i++)
            {
                ShipModule module=(ShipModule)i;bool installed=game.progress.modules.Contains(module);
                if(Button(x,y+77+i*74,282,58,(installed?"取回 · ":"装配 · ")+names[i]))expedition.ToggleModule(module);
                Label(x+303,y+88+i*74,555,46,effects[i],small);
            }
            Label(x,y+313,866,27,"已回村的船体：",body);
            int row=0;
            foreach(NamedBoat boat in game.progress.boats)
            {
                if(row>=2)break;
                Label(x,y+350+row*41,578,38,boat.name+" · "+boat.berth+" · "+(boat.repaired?"已修好，可接应":"故障，保留船体"),body);
                if(!boat.repaired&&Button(x+590,y+349+row*41,268,34,"村仓 2 构件维修"))expedition.RepairHarbourBoat(boat.id);
                row++;
            }
            if(row==0)Label(x,y+350,866,55,game.progress.island.boatRepaired?"青鹭号已恢复航行，正在自行返村。人员仍需单独接应。":"可在东岸修好青鹭号，也可拖回。两种办法最终保留同一艘船。",small);
            if(Button(x,y+432,858,39,game.progress.relayAssigned?"撤回东岸接应船":"安排一艘已修船体到雾岬岛东岸接应"))expedition.AssignRelay();
        }
        void ResidentPage(float x,float y)
        {
            Label(x,y,866,30,"居民 "+game.progress.residents+" 人  /  本航次乘员 "+game.trip.passengers,body);
            Label(x,y+44,866,57,"林舟："+(game.progress.people.Exists(p=>p.id=="linzhou")?(game.progress.technicianAssigned?"在船坞保养一级船":"在归潮村接应屋"):
                expedition.Companion.State)+"\n人实际到达工作艇旁才占乘员位；接人不要求先修站或修艇。",small);
            float rowY=y+130;
            foreach(NamedResident person in game.progress.people)
            {Label(x,rowY,870,52,person.name+"  ·  "+person.home+"\n"+person.skill,body);rowY+=63;if(rowY>y+350)break;}
            if(Button(x,y+381,865,49,game.progress.technicianAssigned?"林舟休息 · 结束保养协作":"安排林舟保养工作艇 · 航行耗油减少 15%"))expedition.AssignTechnician();
        }
        void DiscoveryPage(float x,float y)
        {
            IslandProgress p=game.progress.island;
            Label(x,y,865,80,"雾岬岛：南岸旧码头 / 东岸避风湾，环路始终连通。\n观测站："+(p.stationPowered?"供电在线，天线可转向":"接头损坏，便携接收器可提供粗略方向")+
                "\n潮池："+(p.netCleared?"缠网已清除，捷径开放":"可绕环路，或引开觅食兽后清网"),body);
            Label(x,y+112,865,74,"信号与回访：\n"+(p.signalRead?"求救扇区在东岸约 95°。":"站点修复不是救人前置，可先从东岸寻找痕迹。")+
                "\n"+(p.echoRead?"作业设备回波在西北约 336°，可回船验证。":"北坡能观察到西北回波；修站可进一步确定方位。"),body);
            Label(x,y+229,865,90,"已建设施："+(game.progress.winch?"轻绞盘 ":"")+(game.progress.charts?"海图屋 ":"")+(game.progress.net?"缆网台":"")+
                "\n在外观测浮标："+game.progress.nodes.Count+"；带着浮标决定部署点，近处可回收。\n生态认识："+(p.wildlifeLearned?"觅食兽会转向食物，可在岸上和守港时复用。":"先观察它自主觅食；无需记录也可以尝试诱饵。"),body);
            if(Button(x,y+377,865,46,"返回海图俯看 · 保留自选目标")){expedition.ClosePanels();game.CameraFollow.Overview=true;}
        }

        void DrawSite(float width,float height)
        {
            float x=width*.5f-375,y=height*.5f-279;Panel(new Rect(x,y,750,558));
            Label(x+26,y+18,550,40,expedition.OpenSite.name,title);
            if(Button(x+598,y+21,126,36,"离开 / Esc")){expedition.ClosePanels();return;}
            Label(x+26,y+72,696,62,expedition.Inspection,body);
            if(Button(x+26,y+148,214,40,"1 观察镜"))expedition.SelectTool(HandTool.Observe);
            if(Button(x+264,y+148,214,40,"2 修理工具"))expedition.SelectTool(HandTool.Repair);
            if(Button(x+502,y+148,214,40,"3 割网工具"))expedition.SelectTool(HandTool.CutNet);
            Label(x+26,y+199,698,32,"手持："+expedition.ToolName+"  /  背包构件："+AssetRules.PartsAt(game.progress,game.trip,ItemLocation.Backpack),small);
            if(expedition.OpenSite.kind==IslandSiteKind.Station)
            {
                if(!game.progress.island.stationPowered)
                {
                    if(Button(x+26,y+249,320,49,"观察电池指示"))expedition.Inspect(false);
                    if(Button(x+374,y+249,342,49,"沿电缆检查松脱接头"))expedition.Inspect(true);
                    if(Button(x+26,y+323,690,54,"修复已定位的接头 · 背包 2 构件"))expedition.RepairStation();
                    Label(x+26,y+409,697,76,"便携接收器仍可读粗略方位。没有构件时，可以先去找人，或到维修棚取得板材。",small);
                }
                else
                {
                    Label(x+26,y+247,696,29,"天线朝向："+expedition.AntennaBearing.ToString("0")+"°（0°北 / 90°东）",body);
                    float value=GUI.HorizontalSlider(new Rect(x+27,y+292,683,20),expedition.AntennaBearing,0,360);expedition.SetBearing(value);
                    Label(x+26,y+330,696,30,"求救强度 "+(expedition.SignalStrength(false)*100).ToString("0")+"%   /   作业回波 "+(expedition.SignalStrength(true)*100).ToString("0")+"%",body);
                    if(Button(x+26,y+381,320,48,"读取求救扇区"))expedition.ReadSignal(false);
                    if(Button(x+374,y+381,342,48,"读取作业回波"))expedition.ReadSignal(true);
                }
            }
            else if(expedition.OpenSite.kind==IslandSiteKind.Boat)
            {
                if(Button(x+26,y+250,690,48,"检查青鹭号供电与船体"))expedition.DiagnoseBoat();
                if(Button(x+26,y+324,690,48,"现场修复 · 原船恢复航行"))expedition.RepairBoat();
                Label(x+26,y+412,695,78,"也可先带林舟回你的船，从海面接引青鹭号拖回。修复花构件、节省拖带；拖回保留现场材料、承担返航负载。",small);
            }
        }
    }
}
