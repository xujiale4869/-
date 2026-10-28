using System.Collections.Generic;
using UnityEngine;

namespace Deepsea
{
    public sealed class WorldBuilder
    {
        public readonly List<WorldInteractable> Targets = new List<WorldInteractable>();
        public readonly Vector3 DockPosition = new Vector3(11, 0, -15);
        public Transform Root { get; private set; }
        public Transform Village { get; private set; }
        public Transform NetMount { get; private set; }
        public Transform FishPlatform { get; private set; }
        GameDirector game;
        Transform upgrades, visitors, beaconLight;
        Material seaMaterial;

        public void Build(GameDirector director)
        {
            game = director;
            Root = VisualFactory.Group("归潮湾 · 连续水面", director.transform, Vector3.zero);
            GameObject sea = new GameObject("海面 · 无碰撞");
            sea.transform.SetParent(Root,false);
            Mesh mesh = SeaGrid(700, 90);
            sea.AddComponent<MeshFilter>().sharedMesh = mesh;
            seaMaterial = new Material(Shader.Find("Deepsea/Ocean"));
            seaMaterial.SetColor("_DeepColor",game.settings.seaColor);
            seaMaterial.SetFloat("_WaveHeight",game.settings.waveHeight);
            sea.AddComponent<MeshRenderer>().sharedMaterial = seaMaterial;
            Village = VisualFactory.Group("归潮村", Root, Vector3.zero);
            VisualFactory.Reef(Village, new Vector3(0,0,-45),19,2.3f,101);
            VisualFactory.Reef(Village, new Vector3(-18,0,-52),8,1.7f,103);
            VisualFactory.House(Village,new Vector3(-7,2.5f,-46),17,1.15f);
            VisualFactory.House(Village,new Vector3(1,2.5f,-53),-12,.9f);
            VisualFactory.House(Village,new Vector3(-3,2.5f,-38),180,.86f);
            VisualFactory.House(Village,new Vector3(8,2.5f,-43),25,.83f,true);
            VisualFactory.Pier(Village,new Vector3(2,0,-24),23,4);
            VisualFactory.Pier(Village,new Vector3(10,0,-31),17,3,90);
            VisualFactory.Pier(Village,new Vector3(21,0,-28),9,4);
            NetMount = VisualFactory.Group("防御支撑位",Village,new Vector3(21,.7f,-28));
            FishPlatform = VisualFactory.Pier(Village,new Vector3(29,0,-17),6,4);
            for(int i=0;i<4;i++)
            {
                Transform crate=VisualFactory.Box("渔获筐",FishPlatform,new Vector3((i%2)*1.1f-.55f,1,(i/2)*1.4f-.7f),Vector3.one*.75f,VisualFactory.Wood);
                VisualFactory.Box("筐绳",crate,Vector3.zero,new Vector3(.09f,1.08f,1.08f),VisualFactory.Rope);
            }
            VisualFactory.Person(Village,new Vector3(1,.7f,-25),new Color(.38f,.45f,.41f));
            VisualFactory.Person(Village,new Vector3(8.5f,.7f,-31),new Color(.65f,.43f,.25f));
            for(int i=0;i<3;i++)
            {
                Transform post=VisualFactory.Cylinder("码头灯柱",Village,new Vector3(3,1.5f,-14-i*7),.08f,3,VisualFactory.Wood);
                VisualFactory.Sphere("港灯",post,new Vector3(0,.49f,0),Vector3.one*.2f,VisualFactory.Lamp,true);
            }
            upgrades=VisualFactory.Group("建设成果",Village,Vector3.zero);
            visitors=VisualFactory.Group("归来者与泊位船",Village,Vector3.zero);
            // Sparse islands leave multiple open approaches to every objective.
            Vector3[] reefs={
                new Vector3(-24,0,5),new Vector3(-30,0,20),new Vector3(-42,0,37),
                new Vector3(-62,0,51),new Vector3(-83,0,63),new Vector3(-88,0,74),
                new Vector3(46,0,33),new Vector3(62,0,48),new Vector3(72,0,64),
                new Vector3(47,0,73),new Vector3(64,0,89),new Vector3(90,0,54),
                new Vector3(-32,0,98),new Vector3(34,0,114),new Vector3(-65,0,-19)
            };
            for(int i=0;i<reefs.Length;i++) VisualFactory.Reef(Root,reefs[i],i%4==0?6:3.5f,.7f+i%3*.6f,400+i);
            BuildWreck(new Vector3(-72,0,64));
            BuildBeacon(new Vector3(4,0,115));
            AddSupplies("inner-crate-a","漂流船板",new Vector3(-8,0,12),3,0);
            AddSupplies("inner-crate-b","旧缆箱",new Vector3(24,0,25),3,0);
            AddSupplies("wreck-kit","可修复仪器",new Vector3(-60,0,61),2,1);
            AddSupplies("wreck-boards","残骸构件",new Vector3(-79,0,43),4,0);
            AddSupplies("reef-tools","遗失工具箱",new Vector3(75,0,81),2,1);
            AddSupplies("beacon-equipment","航标备用设备",new Vector3(12,0,100),3,1);
            AddRescue("fishing-boat-a",new Vector3(-55,0,80),2,-22);
            AddRescue("fishing-boat-b",new Vector3(37,0,101),2,55);
            AddHeavy(new Vector3(-91,0,51));
            Transform fishZone=VisualFactory.Group("荧鳞礁观察点",Root,new Vector3(58,0,61));
            WorldInteractable fish=fishZone.gameObject.AddComponent<WorldInteractable>();
            fish.id="fish-record";fish.displayName="荧鳞鱼观察";fish.kind=InteractionKind.FishRecord;fish.game=game;
            if(game.progress.completed.Contains(fish.id))fish.MarkTaken();
            Targets.Add(fish);
            Wildlife wildlife=Root.gameObject.AddComponent<Wildlife>();
            wildlife.game=game;
            RefreshHarbour();
            SetBeacon(game.progress.beacon);
        }

        public BoatController CreateBoat()
        {
            Transform boat=VisualFactory.Group("玩家一级近海工作艇",Root,new Vector3(11,0,-22));
            Rigidbody body=boat.gameObject.AddComponent<Rigidbody>();
            BoxCollider collider=boat.gameObject.AddComponent<BoxCollider>();
            collider.center=new Vector3(0,.4f,0);collider.size=new Vector3(1.8f,1.2f,3.7f);
            BoatController controller=boat.gameObject.AddComponent<BoatController>();
            controller.game=game;
            controller.hull=VisualFactory.BoatVisual(boat,true);
            controller.stern=VisualFactory.Group("拖缆出点",boat,new Vector3(0,.56f,-2));
            controller.lamp=VisualFactory.Group("可独立转动的船灯",boat,new Vector3(0,.8f,.75f));
            VisualFactory.Beam(controller.lamp);
            boat.gameObject.AddComponent<BoatWake>().boat=controller;
            return controller;
        }

        WorldInteractable AddTarget(Transform root,string id,string name,InteractionKind kind)
        {
            WorldInteractable target=root.gameObject.AddComponent<WorldInteractable>();
            target.id=id;target.displayName=name;target.kind=kind;target.game=game;
            Targets.Add(target);
            if(game.progress.completed.Contains(id)){target.MarkTaken();root.gameObject.SetActive(false);}
            return target;
        }

        void AddSupplies(string id,string name,Vector3 position,int parts,int equipment)
        {
            Transform root=VisualFactory.Group(name,Root,position);
            VisualFactory.Box("浮箱",root,new Vector3(0,.28f,0),new Vector3(1.3f,.8f,1.1f),equipment>0?VisualFactory.Roof:VisualFactory.PaleWood);
            VisualFactory.Box("扎带",root,new Vector3(0,.3f,0),new Vector3(.12f,.85f,1.13f),VisualFactory.Rope);
            VisualFactory.Cylinder("浮标",root,new Vector3(.9f,.4f,.2f),.16f,.9f,VisualFactory.Rope);
            WorldInteractable target=AddTarget(root,id,name,InteractionKind.Supplies);
            target.parts=parts;target.equipment=equipment;
        }

        void AddRescue(string id,Vector3 position,int people,float heading)
        {
            Transform root=VisualFactory.Group("遇险小船",Root,position);
            root.rotation=Quaternion.Euler(0,heading,0);
            VisualFactory.BoatVisual(root,false);
            root.gameObject.AddComponent<Rigidbody>();
            BoxCollider c=root.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.4f,0);c.size=new Vector3(1.8f,1,3.7f);
            Towable tow=root.gameObject.AddComponent<Towable>();
            tow.isBoat=true;tow.Bow=VisualFactory.Group("船首缆点",root,new Vector3(0,.5f,2));
            WorldInteractable target=AddTarget(root,id,"故障渔船",InteractionKind.Rescue);
            target.people=people;target.towable=tow;
            if(!game.progress.completed.Contains(id+"-people"))
                VisualFactory.Person(root,new Vector3(-.25f,.66f,.8f),new Color(.65f,.40f,.27f));
        }

        void AddHeavy(Vector3 position)
        {
            Transform root=VisualFactory.Group("残骸中的重型设备",Root,position);
            VisualFactory.Box("旧浮台",root,new Vector3(0,.25f,0),new Vector3(3,.65f,4),VisualFactory.Wood);
            VisualFactory.Cylinder("设备主体",root,new Vector3(0,1.1f,0),.7f,1.4f,VisualFactory.Roof);
            VisualFactory.Box("支架",root,new Vector3(0,1.9f,0),new Vector3(2,.24f,1.4f),VisualFactory.PaleWood);
            root.gameObject.AddComponent<Rigidbody>();
            BoxCollider c=root.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.5f,0);c.size=new Vector3(3,1.5f,4);
            Towable tow=root.gameObject.AddComponent<Towable>();tow.heavy=true;
            tow.Bow=VisualFactory.Group("设备拖环",root,new Vector3(0,.5f,2));
            WorldInteractable target=AddTarget(root,"heavy-winch","重型作业设备",InteractionKind.HeavySalvage);
            target.towable=tow;target.parts=6;target.equipment=2;
        }

        void BuildWreck(Vector3 position)
        {
            Transform wreck=VisualFactory.Group("西侧沉船滩",Root,position);
            Transform hull=VisualFactory.BoatVisual(wreck,false);
            hull.localScale=new Vector3(2.6f,1.4f,4.2f);
            hull.localRotation=Quaternion.Euler(0,-38,21);
            hull.localPosition=new Vector3(0,-.5f,0);
            // Visible damaged ship has a matched obstacle, while seabed scenery does not.
            BoxCollider c=wreck.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.4f,0);c.size=new Vector3(4.5f,2,11);
            wreck.localRotation=Quaternion.Euler(0,-38,0);
        }

        void BuildBeacon(Vector3 position)
        {
            Transform reef=VisualFactory.Reef(Root,position,4,1.4f,88);
            Transform beacon=VisualFactory.Group("湾口旧航标",reef,new Vector3(0,1.5f,0));
            VisualFactory.Cylinder("灯塔架",beacon,new Vector3(0,3,0),.28f,6,VisualFactory.Wood);
            VisualFactory.Box("观测台",beacon,new Vector3(0,5.5f,0),new Vector3(2,.3f,2),VisualFactory.Roof);
            beaconLight=VisualFactory.Sphere("航标灯",beacon,new Vector3(0,6,0),Vector3.one*.8f,VisualFactory.Lamp,true);
            Transform interaction=VisualFactory.Group("航标维修浮台",Root,position+new Vector3(7,0,-1));
            VisualFactory.Pier(interaction,Vector3.zero,3,2);
            AddTarget(interaction,"bay-beacon","湾口旧航标",InteractionKind.Beacon);
        }

        public void SetBeacon(bool repaired){if(beaconLight!=null)beaconLight.gameObject.SetActive(repaired);}

        public void RefreshHarbour()
        {
            foreach(Transform child in upgrades)Object.Destroy(child.gameObject);
            foreach(Transform child in visitors)Object.Destroy(child.gameObject);
            if(game.progress.winch)
            {
                Transform crane=VisualFactory.Group("已建轻绞盘",upgrades,new Vector3(8,.7f,-30));
                VisualFactory.Cylinder("绞盘架",crane,new Vector3(0,2,0),.18f,4,VisualFactory.PaleWood);
                Transform arm=VisualFactory.Box("吊臂",crane,new Vector3(1.4f,3.8f,0),new Vector3(3.3f,.22f,.25f),VisualFactory.PaleWood);
                arm.localRotation=Quaternion.Euler(0,0,12);
                VisualFactory.Cylinder("轮盘",crane,new Vector3(0,.8f,0),.5f,.6f,VisualFactory.Rope);
                LineRenderer rope=VisualFactory.Line("吊缆",crane,.045f,VisualFactory.Rope);
                rope.positionCount=2;rope.SetPosition(0,crane.position+new Vector3(2.8f,4.2f,0));rope.SetPosition(1,crane.position+new Vector3(2.8f,1.5f,0));
            }
            if(game.progress.charts)
            {
                Transform station=VisualFactory.Group("已建观测平台",upgrades,new Vector3(-7,3,-39));
                VisualFactory.Cylinder("观测杆",station,new Vector3(0,2.5f,0),.08f,5,VisualFactory.PaleWood);
                VisualFactory.Sphere("观测灯",station,new Vector3(0,5,0),Vector3.one*.3f,VisualFactory.Lamp,true);
                VisualFactory.Box("海图桌",station,new Vector3(1,1,0),new Vector3(2,.18f,1.5f),new Color(.66f,.65f,.48f));
            }
            if(game.progress.net)
            {
                Transform net=VisualFactory.Group("已建缆网台",upgrades,NetMount.position);
                VisualFactory.Cylinder("网台底座",net,new Vector3(0,.35f,0),.65f,.7f,VisualFactory.Roof);
                VisualFactory.Box("网具发射臂",net,new Vector3(0,1.2f,0),new Vector3(.4f,.45f,2.1f),VisualFactory.PaleWood);
                VisualFactory.Sphere("卷网",net,new Vector3(0,.85f,-.8f),new Vector3(1.2f,.65f,.5f),VisualFactory.Rope);
            }
            for(int i=0;i<Mathf.Min(6,game.progress.residents-5);i++)
                VisualFactory.Person(visitors,new Vector3(-1+(i%3)*.7f,.7f,-30-(i/3)*1.2f),new Color(.38f+i*.03f,.42f,.32f));
            for(int i=0;i<Mathf.Min(3,game.progress.recoveredBoats);i++)
            {
                Transform berth=VisualFactory.Group("救回的泊位船",visitors,new Vector3(16+i*3.5f,0,-35));
                VisualFactory.BoatVisual(berth,false);
            }
        }

        static Mesh SeaGrid(float size,int cells)
        {
            int side=cells+1;
            Vector3[] v=new Vector3[side*side];Vector2[] uv=new Vector2[v.Length];
            int[] triangles=new int[cells*cells*6];
            for(int z=0;z<side;z++)for(int x=0;x<side;x++)
            {int i=z*side+x;v[i]=new Vector3((x/(float)cells-.5f)*size,-.22f,(z/(float)cells-.5f)*size);uv[i]=new Vector2(x/(float)cells,z/(float)cells);}
            int k=0;
            for(int z=0;z<cells;z++)for(int x=0;x<cells;x++)
            {int i=z*side+x;triangles[k++]=i;triangles[k++]=i+side;triangles[k++]=i+1;triangles[k++]=i+1;triangles[k++]=i+side;triangles[k++]=i+side+1;}
            Mesh m=new Mesh{name="海面规则网格"};m.vertices=v;m.uv=uv;m.triangles=triangles;m.RecalculateNormals();m.RecalculateBounds();
            return m;
        }
    }
}
