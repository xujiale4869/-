using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    // Authored silhouettes built from meshes, rather than importing the previous
    // submarine atlas into a surface boat. All decoration stays in world space.
    public static class VisualFactory
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static readonly Color Wood = new Color(.25f, .20f, .17f);
        public static readonly Color PaleWood = new Color(.48f, .40f, .29f);
        public static readonly Color Roof = new Color(.18f, .29f, .30f);
        public static readonly Color Rock = new Color(.24f, .32f, .33f);
        public static readonly Color Rope = new Color(.66f, .57f, .38f);
        public static readonly Color Lamp = new Color(1f, .71f, .36f);

        public static Material Material(Color color, bool glow = false)
        {
            string key = color.ToString("F4") + glow;
            Material result;
            if (materials.TryGetValue(key, out result) && result != null) return result;
            Shader shader = Shader.Find("Deepsea/StylizedSurface");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            result = new Material(shader) { name = key };
            result.SetColor("_BaseColor", color);
            result.SetColor("_Color", color);
            result.SetFloat("_Glow", glow ? .7f : 0f);
            result.SetFloat("_Smoothness", .32f);
            materials[key] = result;
            return result;
        }

        public static Transform Group(string name, Transform parent, Vector3 position)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            return t;
        }

        public static GameObject MeshObject(string name, Mesh mesh, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = Material(color);
            return obj;
        }

        public static Transform Box(string name, Transform parent, Vector3 position, Vector3 size, Color color, bool collision = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            Collider collider = obj.GetComponent<Collider>();
            if (!collision) { collider.enabled = false; Object.Destroy(collider); }
            return obj.transform;
        }

        public static Transform Cylinder(string name, Transform parent, Vector3 position,
            float radius, float height, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            Collider collider = obj.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            return obj.transform;
        }

        public static Transform Sphere(string name, Transform parent, Vector3 position, Vector3 size, Color color, bool glow = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color, glow);
            Collider collider = obj.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            return obj.transform;
        }

        public static Mesh Prism(Vector2[] outline, float bottom, float top, float topScale = 1)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int n = outline.Length;
            // Independent vertices per side give controlled, faceted silhouettes.
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int b = vertices.Count;
                vertices.Add(new Vector3(outline[i].x, bottom, outline[i].y));
                vertices.Add(new Vector3(outline[j].x, bottom, outline[j].y));
                vertices.Add(new Vector3(outline[j].x * topScale, top, outline[j].y * topScale));
                vertices.Add(new Vector3(outline[i].x * topScale, top, outline[i].y * topScale));
                triangles.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            int center = vertices.Count;
            vertices.Add(new Vector3(0, top, 0));
            for (int i = 0; i < n; i++)
                vertices.Add(new Vector3(outline[i].x * topScale, top, outline[i].y * topScale));
            for (int i = 0; i < n; i++)
                triangles.AddRange(new[] { center, center + 1 + (i + 1) % n, center + 1 + i });
            Mesh mesh = new Mesh { name = "Authored prism" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Transform Reef(Transform parent, Vector3 position, float radius, float height, int seed)
        {
            var random = new System.Random(seed);
            Vector2[] outline = new Vector2[11];
            for (int i = 0; i < outline.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / outline.Length;
                float r = radius * (.83f + (float)random.NextDouble() * .25f);
                outline[i] = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * r;
            }
            Mesh mesh = Prism(outline, -.8f, height, .68f);
            Transform reef = MeshObject("湿润露礁", mesh, parent, Rock).transform;
            reef.localPosition = position;
            // A convex hull of the same mesh is the only obstacle. No background
            // island, seabed mark or atmospheric layer carries a collider.
            MeshCollider collider = reef.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            Transform cap = MeshObject("苔藓岩面", Prism(outline, height, height + .16f, .64f), reef,
                new Color(.33f, .39f, .31f)).transform;
            cap.localScale = new Vector3(.69f, 1, .69f);
            return reef;
        }

        public static Transform House(Transform parent, Vector3 position, float yaw, float scale, bool chart = false)
        {
            Transform house = Group(chart ? "海图屋" : "木石屋舍", parent, position);
            house.localRotation = Quaternion.Euler(0, yaw, 0);
            house.localScale = Vector3.one * scale;
            Box("石基", house, new Vector3(0, .35f, 0), new Vector3(3.8f, .7f, 3.4f), Rock);
            Box("旧船板墙", house, new Vector3(0, 1.65f, 0), new Vector3(3.5f, 2.2f, 3.1f), PaleWood);
            Vector3[] v = {
                new Vector3(-2.1f,2.8f,-1.9f),new Vector3(2.1f,2.8f,-1.9f),new Vector3(0,4.1f,-1.9f),
                new Vector3(-2.1f,2.8f,1.9f),new Vector3(2.1f,2.8f,1.9f),new Vector3(0,4.1f,1.9f)
            };
            Mesh roof = new Mesh { name = "双坡屋顶" };
            roof.vertices = v;
            roof.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 4 };
            roof.RecalculateNormals();
            MeshObject("深青瓦面", roof, house, chart ? new Color(.28f,.39f,.37f) : Roof);
            Box("门", house, new Vector3(.45f, 1.28f, -1.57f), new Vector3(.78f, 1.8f, .08f), Wood);
            Transform window = Box("暖窗", house, new Vector3(-.8f, 1.75f, -1.59f), new Vector3(.65f, .72f, .08f), Lamp);
            window.GetComponent<Renderer>().sharedMaterial = Material(Lamp, true);
            Box("窗横梁", window, Vector3.zero, new Vector3(1.04f,.07f,1.1f), Wood);
            Box("烟囱", house, new Vector3(.9f, 3.9f, .5f), new Vector3(.6f, 1.3f, .6f), Rock);
            for (int i = 0; i < 7; i++)
                Box("板缝", house, new Vector3(0, .65f + i * .32f, -1.57f), new Vector3(3.51f,.025f,.045f), Wood);
            return house;
        }

        public static Transform Pier(Transform parent, Vector3 position, float length, float width, float yaw = 0)
        {
            Transform pier = Group("公共栈桥", parent, position);
            pier.localRotation = Quaternion.Euler(0, yaw, 0);
            Box("平台结构", pier, new Vector3(0,.4f,0), new Vector3(width,.42f,length), Wood, true);
            int n = Mathf.CeilToInt(length / .52f);
            for (int i = 0; i < n; i++)
                Box("铺板", pier, new Vector3(0,.65f,-length*.5f + (i+.5f)*length/n),
                    new Vector3(width-.05f,.09f,length/n-.045f), i % 3 == 0 ? PaleWood * .87f : PaleWood);
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    Cylinder("桥桩", pier, new Vector3(i*width*.44f,.4f,j*length*.38f), .15f, 2.9f, Wood);
            return pier;
        }

        public static Transform BoatVisual(Transform parent, bool workboat)
        {
            Transform model = Group(workboat ? "一级工作艇船身" : "故障渔船船身", parent, Vector3.zero);
            Vector2[] outline = {
                new Vector2(-.8f,-2),new Vector2(.8f,-2),new Vector2(1.03f,-1),
                new Vector2(.93f,.85f),new Vector2(.42f,1.8f),new Vector2(0,2.35f),
                new Vector2(-.42f,1.8f),new Vector2(-.93f,.85f),new Vector2(-1.03f,-1)
            };
            Color hull = workboat ? new Color(.23f,.43f,.45f) : new Color(.43f,.25f,.18f);
            MeshObject("收尖船壳", Prism(outline,-.48f,.55f,.9f), model, hull);
            MeshObject("木甲板", Prism(outline,.55f,.64f,.78f), model, PaleWood);
            for (int i = 0; i < 7; i++)
                Box("甲板条", model, new Vector3(0,.65f,-1.35f+i*.35f), new Vector3(1.2f,.025f,.035f), Wood);
            Box("驾驶舱", model, new Vector3(0,1.08f,-.55f), new Vector3(1.25f,.93f,1.35f), new Color(.61f,.62f,.53f));
            Box("舱顶", model, new Vector3(0,1.6f,-.55f), new Vector3(1.43f,.18f,1.6f), Roof);
            Transform window = Box("驾驶窗", model, new Vector3(0,1.22f,.14f), new Vector3(.94f,.36f,.045f), Lamp * .8f);
            window.GetComponent<Renderer>().sharedMaterial = Material(Lamp*.65f,true);
            Box("舷窗", model, new Vector3(.645f,1.22f,-.5f), new Vector3(.035f,.34f,.63f), new Color(.15f,.3f,.33f));
            Cylinder("排气管", model, new Vector3(-.4f,1.4f,-1.24f), .09f, 1.15f, Wood);
            Cylinder("桅杆", model, new Vector3(.35f,1.65f,.45f), .035f, 2, PaleWood);
            Sphere("桅灯", model, new Vector3(.35f,2.67f,.45f), Vector3.one*.15f, Lamp, true);
            Cylinder("缆盘", model, new Vector3(.25f,.8f,-1.6f), .25f,.24f, Rope);
            for (int side = -1; side <= 1; side += 2)
                for (int z = -1; z <= 1; z++)
                    Sphere("橡胶护舷", model, new Vector3(side*.94f,.47f,z*.85f), new Vector3(.18f,.42f,.32f), new Color(.11f,.14f,.14f));
            return model;
        }

        public static Transform Person(Transform parent, Vector3 position, Color coat)
        {
            Transform person = Group("居民", parent, position);
            Cylinder("外套", person, new Vector3(0,.6f,0), .19f,.65f,coat);
            Sphere("头",person,new Vector3(0,1.09f,0),Vector3.one*.28f,new Color(.73f,.56f,.40f));
            Cylinder("帽",person,new Vector3(0,1.25f,0),.19f,.1f,Roof);
            for(int i=-1;i<=1;i+=2)
                Box("鞋",person,new Vector3(i*.1f,.18f,-.05f),new Vector3(.13f,.3f,.27f),Wood);
            return person;
        }

        public static LineRenderer Line(string name, Transform parent, float width, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            Shader shader = Shader.Find("Deepsea/SoftLine");
            line.sharedMaterial = new Material(shader) { color = color };
            line.startColor = line.endColor = color;
            line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            return line;
        }

        public static void Beam(Transform lamp)
        {
            int segments = 30, rings = 12;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for(int r=0;r<rings;r++)
            {
                float distance = .35f + r * 1.25f;
                float half = distance * .38f;
                for(int i=0;i<=segments;i++)
                {
                    float f = i/(float)segments;
                    vertices.Add(new Vector3(Mathf.Lerp(-half,half,f),-.68f,distance));
                    colors.Add(new Color(1,.82f,.52f,Mathf.Pow(Mathf.Sin(f*Mathf.PI),2)*Mathf.Exp(-distance*.2f)*.16f));
                }
            }
            for(int r=0;r<rings-1;r++)
                for(int i=0;i<segments;i++)
                {
                    int a=r*(segments+1)+i,b=a+segments+1;
                    triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
                }
            Mesh mesh=new Mesh{name="柔边短距灯束"};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            GameObject beam=MeshObject("近海船灯散射",mesh,lamp,Lamp);
            beam.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Deepsea/SoftLine"));
            Light light=lamp.gameObject.AddComponent<Light>();
            light.type=LightType.Spot;light.color=Lamp;light.range=18;light.spotAngle=42;light.intensity=1.8f;
            light.shadows=LightShadows.None;
            light.transform.localRotation=Quaternion.Euler(9,0,0);
        }
    }
}
