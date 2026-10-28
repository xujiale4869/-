using UnityEngine;

namespace Deepsea
{
    public sealed class GameHud : MonoBehaviour
    {
        public GameDirector game;
        GUIStyle title, body, small, button, label, panel;
        Font font;
        Texture2D pixel;
        bool initialized;
        readonly Color ink=new Color(.81f,.9f,.89f);
        readonly Color dim=new Color(.48f,.64f,.64f);
        readonly Color gold=new Color(.93f,.75f,.46f);

        void Initialize()
        {
            if(initialized)return;
            initialized=true;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Microsoft YaHei UI","PingFang SC","Noto Sans CJK SC","Arial"},20);
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
            title=Style(28,gold);title.fontStyle=FontStyle.Bold;
            body=Style(19,ink);small=Style(15,dim);
            label=Style(18,ink);label.alignment=TextAnchor.MiddleCenter;
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=19,padding=new RectOffset(12,12,8,8)};
            button.normal.textColor=ink;button.hover.textColor=gold;
            panel=new GUIStyle(GUI.skin.box);panel.normal.background=pixel;panel.normal.textColor=ink;
        }
        GUIStyle Style(int size,Color color)
        {
            GUIStyle s=new GUIStyle(GUI.skin.label){font=font,fontSize=size,wordWrap=true};s.normal.textColor=color;return s;
        }
        void Box(Rect rect)
        {
            GUI.color=new Color(.018f,.061f,.074f,.91f);GUI.Box(rect,GUIContent.none,panel);GUI.color=Color.white;
            GUI.color=new Color(.36f,.54f,.53f,.45f);GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,1),pixel);GUI.color=Color.white;
        }
        void Text(Rect rect,string text,GUIStyle style){GUI.Label(rect,text,style);}

        void OnGUI()
        {
            if(game==null||game.Boat==null)return;
            Initialize();
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            Matrix4x4 previous=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
            float width=Screen.width/scale,height=Screen.height/scale;
            Box(new Rect(24,24,390,143));
            Text(new Rect(43,36,340,40),"归潮湾",title);
            Text(new Rect(43,77,345,26),"浅海试航  /  一级近海工作艇",small);
            Text(new Rect(43,111,350,48),"船上：构件 "+game.trip.parts+"   设备 "+game.trip.equipment+
                "\n乘员 "+game.trip.passengers+" / "+game.settings.passengerSlots+"    货舱 "+game.trip.CargoUsed+" / "+game.settings.cargoSlots,body);
            DrawMap(new Rect(width-267,24,243,269));
            Box(new Rect(24,height-143,375,94));
            Text(new Rect(42,height-132,340,25),"航速 "+game.Boat.Speed.ToString("0.0")+"    燃油 "+(game.Boat.Fuel*100).ToString("0")+"%    船体 "+(game.Boat.Health*100).ToString("0")+"%",body);
            Text(new Rect(42,height-102,340,36),game.Boat.Tow!=null?"拖带中 · X 解缆 · 港口交付":"自由航行 · Q 短距探测 · L 船灯",small);
            Text(new Rect(25,height-35,width-50,30),"W 推进  S 减速 / 倒车  A D 打舵  鼠标转灯  E 交互  Q 探测  M 俯看海图  滚轮缩放  Esc 暂停",small);
            string action="";
            if(game.CanDock)action="E 手动靠泊 · 卸载、接应、维修与建设";
            else if(game.Nearby!=null)action=game.Nearby.displayName+"\n"+game.Nearby.Hint();
            if(action!=""&&!game.HarbourMenu&&!game.Paused)
            {
                Box(new Rect(width*.5f-270,height-129,540,75));
                Text(new Rect(width*.5f-253,height-117,506,58),action,label);
            }
            if(Time.unscaledTime<game.NoticeUntil&&!game.HarbourMenu&&!game.Paused)
            {
                Box(new Rect(width*.5f-355,29,710,83));
                Text(new Rect(width*.5f-332,42,664,60),game.Notice,body);
            }
            if(game.CameraFollow.Overview&&!game.Paused&&!game.HarbourMenu)
            {Box(new Rect(width*.5f-250,130,500,49));Text(new Rect(width*.5f-236,141,472,35),"海图俯看 · M 回到驾驶（驾驶暂时停用）",label);}
            if(game.Defense.Active&&!game.Paused)
            {Box(new Rect(width*.5f-340,128,680,60));Text(new Rect(width*.5f-320,140,640,43),game.Defense.Status,body);}
            DrawWorldLabels(width,height,scale);
            if(game.HarbourMenu)DrawHarbour(width,height);
            if(game.Paused)DrawPause(width,height);
            GUI.matrix=previous;
        }

        void DrawMap(Rect rect)
        {
            Box(rect);
            Text(new Rect(rect.x+14,rect.y+10,220,26),"近海海图 · M 俯看",body);
            Rect chart=new Rect(rect.x+18,rect.y+45,207,186);
            GUI.color=new Color(.05f,.12f,.14f);GUI.DrawTexture(chart,pixel);GUI.color=Color.white;
            Text(new Rect(chart.x+93,chart.y-1,20,20),"N",small);
            MapDot(chart,game.World.DockPosition,gold,7);
            MapDot(chart,new Vector3(4,0,115),game.progress.beacon?gold:dim,5);
            foreach(WorldInteractable target in game.World.Targets)
                if(target!=null&&!target.Taken&&target.gameObject.activeInHierarchy&&target.InView)
                    MapDot(chart,target.transform.position,target.kind==InteractionKind.Rescue?gold:new Color(.43f,.72f,.71f),4);
            MapDot(chart,game.Boat.transform.position,ink,6);
            Text(new Rect(rect.x+14,rect.y+235,220,26),"西 残骸 / 东 礁带 / 北 航标",small);
        }
        void MapDot(Rect chart,Vector3 world,Color color,float size)
        {
            float x=chart.x+(world.x+110)/220*chart.width;
            float y=chart.y+(135-world.z)/195*chart.height;
            if(!chart.Contains(new Vector2(x,y)))return;
            GUI.color=color;GUI.DrawTexture(new Rect(x-size*.5f,y-size*.5f,size,size),pixel);GUI.color=Color.white;
        }
        void DrawWorldLabels(float width,float height,float scale)
        {
            if(game.Paused||game.HarbourMenu||game.CameraFollow.Overview)return;
            foreach(WorldInteractable target in game.World.Targets)
            {
                if(target==null||target.Taken||!target.gameObject.activeInHierarchy||!target.InView)continue;
                Vector3 p=game.Camera.WorldToScreenPoint(target.transform.position+Vector3.up*2);
                if(p.z<0)continue;
                Vector2 at=new Vector2(p.x/scale,(Screen.height-p.y)/scale);
                if(at.x<15||at.x>width-15||at.y<175||at.y>height-165)continue;
                if(Vector3.Distance(game.Boat.transform.position,target.transform.position)>45)continue;
                GUI.color=new Color(.015f,.06f,.075f,.84f);GUI.DrawTexture(new Rect(at.x-80,at.y-5,160,28),pixel);GUI.color=Color.white;
                Text(new Rect(at.x-78,at.y-2,156,25),target.displayName,label);
            }
        }

        void DrawHarbour(float width,float height)
        {
            float x=width*.5f-355,y=height*.5f-278;
            Box(new Rect(x,y,710,552));
            Text(new Rect(x+30,y+21,650,42),"归潮村 · 已经靠泊",title);
            Text(new Rect(x+30,y+72,650,55),"村仓  构件 "+game.progress.parts+" / 设备 "+game.progress.equipment+" / 记录 "+game.progress.records+
                "\n居民 "+game.progress.residents+" 人  ·  救回船体 "+game.progress.recoveredBoats+" 艘  ·  结算航次 "+game.progress.voyages,body);
            BuildButton(new Rect(x+30,y+145,650,62),Building.Winch,"船坞轻绞盘",game.progress.winch,
                game.settings.winchParts+" 构件 + "+game.settings.winchEquipment+" 设备   /   更稳拖带，可运重型设备");
            BuildButton(new Rect(x+30,y+221,650,62),Building.Charts,"海图屋观测平台",game.progress.charts,
                game.settings.chartParts+" 构件 + "+game.settings.chartRecords+" 记录   /   探测范围 "+game.settings.scanRadius+" → "+game.settings.improvedScanRadius);
            BuildButton(new Rect(x+30,y+297,650,62),Building.Net,"缆网台",game.progress.net,
                game.settings.netParts+" 构件 + "+game.settings.netEquipment+" 设备   /   保护外侧渔获浮台");
            Text(new Rect(x+30,y+372,650,51),game.Notice,small);
            if(game.progress.net)
            {
                GUI.enabled=!game.Defense.Active;
                if(GUI.Button(new Rect(x+30,y+430,305,49),"开始守港演练",button))game.Defense.StartDrill();
                GUI.enabled=true;
            }
            if(GUI.Button(new Rect(x+365,y+430,315,49),"整理完毕 · 继续出航",button))game.HarbourMenu=false;
            Text(new Rect(x+30,y+495,650,31),"基础维修与补给免费。建设跨出航保留；探索顺序由你选择。",small);
        }
        void BuildButton(Rect rect,Building building,string name,bool built,string description)
        {
            GUI.enabled=!built;
            if(GUI.Button(rect,(built?"已建成 · ":"建设 · ")+name+"\n"+description,button))game.Build(building);
            GUI.enabled=true;
        }

        void DrawPause(float width,float height)
        {
            float x=width*.5f-240,y=height*.5f-270;
            Box(new Rect(x,y,480,541));
            Text(new Rect(x+28,y+22,424,42),"暂停 · 海在等你",title);
            if(GUI.Button(new Rect(x+28,y+88,424,44),"继续",button))game.SetPause(false);
            Text(new Rect(x+28,y+155,424,26),"环境音乐 "+(game.Audio.MusicVolume*100).ToString("0")+"%",body);
            game.Audio.MusicVolume=GUI.HorizontalSlider(new Rect(x+30,y+193,420,20),game.Audio.MusicVolume,0,1);
            Text(new Rect(x+28,y+228,424,26),"船声与交互 "+(game.Audio.EffectsVolume*100).ToString("0")+"%",body);
            game.Audio.EffectsVolume=GUI.HorizontalSlider(new Rect(x+30,y+266,420,20),game.Audio.EffectsVolume,0,1);
            Text(new Rect(x+28,y+302,424,56),"村船接回会放弃本航次未结算收获，保留港内永久建设。",small);
            if(GUI.Button(new Rect(x+28,y+367,424,47),"放弃本航次 · 请求村船接回",button))game.EmergencyReturn();
            if(GUI.Button(new Rect(x+28,y+432,424,47),"保存村庄并退出",button))
            {
                game.Save();
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
                #else
                Application.Quit();
                #endif
            }
            Text(new Rect(x+28,y+494,424,30),"本航次收获须先靠泊；退出不会自动卸货。",small);
        }
    }
}
