using UnityEngine;
namespace Deepsea
{
    public static class ToolModels
    {
        public static Transform Build(Transform parent,HandTool kind)
        {
            var t=VisualFactory.Group(kind.ToString(),parent,Vector3.zero);
            Color metal=new Color(.67f,.73f,.72f);
            if(kind==HandTool.Axe)
            {
                VisualFactory.Box("木斧柄",t,Vector3.zero,new Vector3(.065f,.62f,.065f),VisualFactory.PaleWood);
                VisualFactory.Box("宽金属斧刃",t,new Vector3(.09f,.23f,0),new Vector3(.32f,.18f,.07f),metal);
            }
            else if(kind==HandTool.Observe)
            {
                for(int i=-1;i<=1;i+=2){var b=VisualFactory.Cylinder("镜筒",t,new Vector3(i*.09f,0,.08f),.085f,.26f,VisualFactory.Roof);b.localRotation=Quaternion.Euler(90,0,0);VisualFactory.Sphere("镜片",t,new Vector3(i*.09f,0,.23f),new Vector3(.13f,.13f,.025f),metal);}
                VisualFactory.Box("镜桥",t,Vector3.zero,new Vector3(.2f,.08f,.08f),VisualFactory.Wood);
            }
            else if(kind==HandTool.Repair)
            {
                VisualFactory.Box("维修箱",t,new Vector3(0,-.13f,0),new Vector3(.32f,.22f,.19f),VisualFactory.Roof);
                VisualFactory.Box("提手",t,new Vector3(0,.03f,0),new Vector3(.18f,.08f,.06f),metal);
                VisualFactory.Box("扳手柄",t,new Vector3(.18f,.1f,0),new Vector3(.045f,.36f,.05f),metal);
                VisualFactory.Box("扳手口",t,new Vector3(.18f,.29f,0),new Vector3(.15f,.07f,.05f),metal);
            }
            else if(kind==HandTool.CutNet)
            {
                for(int i=-1;i<=1;i+=2){var a=VisualFactory.Box("钳柄与刃",t,new Vector3(i*.045f,0,0),new Vector3(.045f,.36f,.045f),i==1?metal:VisualFactory.Rope);a.localRotation=Quaternion.Euler(0,0,i*20);}
                VisualFactory.Sphere("铰轴",t,Vector3.zero,Vector3.one*.08f,metal);
            }
            else if(kind==HandTool.Buoy)
            {
                VisualFactory.Sphere("浮体",t,Vector3.zero,new Vector3(.3f,.19f,.3f),VisualFactory.Rope);
                VisualFactory.Cylinder("信号杆",t,new Vector3(0,.2f,0),.025f,.3f,metal);
                VisualFactory.Sphere("浮标灯",t,new Vector3(0,.38f,0),Vector3.one*.08f,VisualFactory.Lamp,true);
            }
            else
            {
                VisualFactory.Cylinder("食物罐",t,Vector3.zero,.12f,.23f,VisualFactory.Rope);
                VisualFactory.Cylinder("罐盖",t,new Vector3(0,.13f,0),.13f,.035f,metal);
                VisualFactory.Box("标签",t,new Vector3(0,0,.12f),new Vector3(.13f,.1f,.02f),VisualFactory.Roof);
            }
            return t;
        }
    }
}
