using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public sealed class CoastalCraftsmanshipUpgradeV32 : MonoBehaviour
    {
#if DEEPSEA_V32
        public static bool Enabled => true;
#else
        public static bool Enabled => false;
#endif

        const string ResourcePath = "SeaCraftV32/";
        const string ShaderName = "Deepsea/StylizedCoastalCraftV32";
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        readonly Dictionary<Vector3Int, Mesh> templates = new Dictionary<Vector3Int, Mesh>();
        Material wood, slate, stone, beam;

        [Serializable] sealed class Roles { public Role[] parts; }
        [Serializable] sealed class Role { public string role; }

        internal static CoastalCraftsmanshipUpgradeV32 For(Component host)
        {
            if (!Enabled) return null;
            return host.GetComponent<CoastalCraftsmanshipUpgradeV32>()
                ?? host.gameObject.AddComponent<CoastalCraftsmanshipUpgradeV32>();
        }

        void Awake()
        {
            var source = Resources.Load<Material>(ResourcePath + "CraftKeep");
            if (source == null || source.shader == null || source.shader.name != ShaderName || !source.shader.isSupported)
                throw new InvalidOperationException("The v32 coastal craft shader is missing or unsupported.");
            wood = CreateMaterial(source, "V32_WeatheredPlank", 0, new Color(.66f,.49f,.35f), .72f,.25f,.40f);
            slate = CreateMaterial(source, "V32_CoastalSlate", 1, new Color(.43f,.49f,.48f), .77f,.20f,.29f);
            stone = CreateMaterial(source, "V32_SeaStone", 2, new Color(.56f,.57f,.50f), .85f,.29f,.68f);
            beam = CreateMaterial(source, "V32_WeatheredBeam", 3, new Color(.53f,.38f,.27f), .79f,.26f,.53f);
        }

        Material CreateMaterial(Material source, string label, int mode, Color tint, float roughness, float relief, float moss)
        {
            var material = new Material(source) { name = label };
            material.SetFloat("_CraftMode", mode);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Roughness", roughness);
            material.SetFloat("_NormalStrength", relief);
            material.SetFloat("_MossStrength", moss);
            owned.Add(material);
            return material;
        }

        internal void AssignAuthored(GameObject target, string name, Material glass, Material brass, Material rope)
        {
            string path = "Village_V32_" + name;
            var mesh = Resources.Load<Mesh>(ResourcePath + "Baked/" + path);
            var source = Resources.Load<TextAsset>(ResourcePath + "Roles/" + path);
            var roles = source != null ? JsonUtility.FromJson<Roles>(source.text) : null;
            if (mesh == null || roles?.parts == null || roles.parts.Length != mesh.subMeshCount)
                throw new InvalidOperationException("Invalid baked craft mesh or material roles for " + path);
            target.GetComponent<MeshFilter>().sharedMesh = mesh;
            target.GetComponent<MeshRenderer>().sharedMaterials = roles.parts.Select(part =>
            {
                switch (part.role)
                {
                    case "Timber": return wood;
                    case "SlateRoof": return slate;
                    case "SeaStone": return stone;
                    case "WarmGlass": return glass;
                    case "Brass": return brass;
                    case "Rope": return rope;
                    case "Beam": return beam;
                    default: throw new InvalidOperationException("Unknown craft surface " + part.role);
                }
            }).ToArray();
        }

        internal void VillagePart(GameObject target, bool masonry, bool timber)
        {
            if (masonry)
            {
                if (target.name == "GroundedSeaStoneSkirt")
                    Set(target, Skirt(target.transform.localScale), stone);
                else
                    Set(target, Template(target.transform.localScale, 1, 0), stone);
                return;
            }
            if (!timber || target.name == "MarketHangingCord") return;
            if (target.name.StartsWith("ForecourtTread_", StringComparison.Ordinal)
                || target.name.StartsWith("ShoreAccessTread_", StringComparison.Ordinal))
                Set(target, Deck(target.transform.localScale), wood);
            else
                Set(target, Template(target.transform.localScale, 1, 0), beam);
        }

        internal void PierPart(GameObject target, int board)
        {
            var size = target.transform.localScale;
            switch (target.name)
            {
                case "WeatheredPlank":
                    Set(target, Template(size, 1, 1 + board % 4), wood);
                    break;
                case "DeckBearer":
                    Set(target, Template(size, 1, 0), beam);
                    break;
                case "SeabedPile":
                    target.GetComponent<MeshRenderer>().sharedMaterial = beam;
                    break;
            }
        }

        internal Mesh GalleryMesh(MeshRenderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.subMeshCount != 1
                || filter.sharedMesh.vertexCount != 24 || filter.sharedMesh.bounds.size != Vector3.one)
                return null;
            var size = renderer.transform.lossyScale;
            if (renderer.name == "小馆木平台") return Deck(size);
            if (renderer.name == "低矮栏墙") return GalleryWall(size);
            return Template(size, 1, 0);
        }

        internal Material GalleryMaterial(string label)
            => label == "小馆木平台" ? wood : label == "低矮栏墙" ? stone : beam;

        Mesh GalleryWall(Vector3 size)
        {
            var pieces = new Pieces();
            var bevel = new Vector3(.028f/size.x,.028f/size.y,.028f/size.z);
            pieces.Box(Vector3.zero, new Vector3(.995f,1f,.65f),bevel,.70f,0);
            const int rows = 3, columns = 8;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column <= columns; column++)
            {
                float left = Mathf.Max(0,(column-(row%2)*.5f)/columns);
                float right = Mathf.Min(1,(column+1-(row%2)*.5f)/columns);
                if (right-left < .004f) continue;
                pieces.Box(new Vector3((left+right)*.5f-.5f,(row+.5f)/rows-.5f,0),
                    new Vector3(right-left-.003f,1f/rows-.018f,1),bevel,Tint(row*11+column),0);
            }
            return Keep(pieces,"V32_GalleryStoneCourses");
        }

        internal void GalleryEave(GameObject target)
        {
            var shape = new Pieces();
            shape.Box(Vector3.zero, new Vector3(1,.52f,1), new Vector3(.012f,.12f,.012f), .75f, 0);
            const int rows = 7, columns = 8;
            float eaveSign = Mathf.Sign(target.transform.localPosition.x);
            for (int row = 0; row < rows; row++)
            for (int column = 0; column <= columns; column++)
            {
                float start = (column - (row % 2)*.5f)/columns;
                float left = Mathf.Max(0,start), right = Mathf.Min(1,start+1f/columns);
                if (right <= left) continue;
                var position = new Vector3(eaveSign*((row+.5f)/rows-.5f), .35f, (left+right)*.5f-.5f);
                var size = new Vector3(1f/rows+.003f,.30f,right-left-.005f);
                shape.Box(position, size, new Vector3(.013f,.06f,.010f), Tint(row*columns+column), 0);
            }
            Set(target, Keep(shape, "V32_GallerySlate"), slate);
        }

        internal bool TryPierMaterials(MeshRenderer renderer, out Material[] materials)
        {
            materials = renderer.sharedMaterials;
            if (materials.Length == 1 && renderer.name.StartsWith("EN_Pier_Wood_", StringComparison.Ordinal))
            {
                materials[0] = wood;
                return true;
            }

            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null) continue;
                string label = source.name.ToLowerInvariant();
                if (label.Contains("rope") || label.Contains("metal") || label.Contains("lantern")) continue;
                if (label.Contains("plank") || label.Contains("deck"))
                {
                    materials[i] = wood;
                    changed = true;
                }
                else if (label.Contains("wood") || label.Contains("timber") || label.Contains("桩"))
                {
                    materials[i] = beam;
                    changed = true;
                }
            }
            return changed;
        }

        Mesh Deck(Vector3 size) => Template(size, Mathf.Clamp(Mathf.RoundToInt(size.z/.19f), 2, 28), 0);

        Mesh Template(Vector3 size, int boards, int variant)
        {
            // A one-centimetre size bucket shares deck/beam geometry without changing the original bounds.
            var key = new Vector3Int(Mathf.RoundToInt(size.x*100), Mathf.RoundToInt(size.y*100), Mathf.RoundToInt(size.z*100));
            key.z = (key.z*29 + boards)*5 + variant;
            if (templates.TryGetValue(key, out Mesh mesh)) return mesh;
            var shape = new Pieces();
            var bevel = new Vector3(.018f/Mathf.Max(.02f,size.x), .012f/Mathf.Max(.02f,size.y), .018f/Mathf.Max(.02f,size.z));
            if (boards == 1)
                shape.Box(Vector3.zero, Vector3.one, bevel, variant == 0 ? .94f : Tint(variant), LongAxis(size));
            else
            {
                float pitch = 1f/boards;
                float seam = Mathf.Min(pitch*.10f,.012f/size.z);
                for (int i = 0; i < boards; i++)
                    shape.Box(new Vector3(0,.10f,(i+.5f)*pitch-.5f),
                        new Vector3(1,.8f,pitch-seam), bevel, Tint(i), 0);
                foreach (float x in new[] { -.36f, .36f })
                    shape.Box(new Vector3(x,-.39f,0), new Vector3(.07f,.22f,1), bevel, .76f, 2);
            }
            mesh = Keep(shape, boards == 1 ? "V32_ChamferedTimber" : "V32_SeparateDeckBoards");
            templates.Add(key, mesh);
            return mesh;
        }

        Mesh Skirt(Vector3 size)
        {
            var shape = new Pieces();
            var bevel = new Vector3(.035f/size.x, .035f/size.y, .035f/size.z);
            float depthX = .19f/size.x, depthZ = .19f/size.z;
            shape.Box(Vector3.zero, new Vector3(1-depthX,1,1-depthZ), bevel, .70f, 0);
            int rows = Mathf.Clamp(Mathf.RoundToInt(size.y/.26f),2,7);
            for (int row = 0; row < rows; row++)
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side < 2;
                float inset = alongX ? 0 : depthZ;
                int count = Mathf.Max(2,Mathf.RoundToInt((alongX ? size.x : size.z)/.61f));
                float pitch = (1-2*inset)/count;
                float sign = side % 2 == 0 ? -1 : 1;
                for (int j = 0; j <= count; j++)
                {
                    float left = Mathf.Max(inset,inset+(j-(row%2)*.5f)*pitch);
                    float right = Mathf.Min(1-inset,inset+(j+1-(row%2)*.5f)*pitch);
                    float span = right-left-.014f/(alongX ? size.x : size.z);
                    if (span <= 0) continue;
                    float u = (left+right)*.5f-.5f, y = (row+.5f)/rows-.5f;
                    var center = alongX ? new Vector3(u,y,sign*(1-depthZ)*.5f)
                                        : new Vector3(sign*(1-depthX)*.5f,y,u);
                    float height = 1f/rows-.014f/size.y;
                    var dims = alongX ? new Vector3(span,height,depthZ) : new Vector3(depthX,height,span);
                    shape.Box(center,dims,bevel,Tint(row*19+side*11+j),alongX ? 0 : 2);
                }
            }
            return Keep(shape, "V32_StoneFootingCourses");
        }

        static int LongAxis(Vector3 size) => size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
        static float Tint(int i) => .84f + (i*37%13)*.012f;

        static void Set(GameObject target, Mesh mesh, Material material)
        {
            target.GetComponent<MeshFilter>().sharedMesh = mesh;
            target.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        Mesh Keep(Pieces pieces, string name)
        {
            var mesh = pieces.Finish(name);
            owned.Add(mesh);
            return mesh;
        }

        void OnDestroy()
        {
            foreach (var asset in owned)
                if (asset != null) Destroy(asset);
        }

        sealed class Pieces
        {
            readonly List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<Color> colors = new List<Color>();
            readonly List<int> triangles = new List<int>();

            public void Box(Vector3 center, Vector3 size, Vector3 bevel, float tint, int grainAxis)
            {
                var half = size*.5f;
                var b = Vector3.Min(bevel,half*.35f);
                var rings = new Vector3[4][];
                rings[0] = Ring(center,-half.y,half.x-b.x,half.z-b.z,b.x*.5f,b.z*.5f);
                rings[1] = Ring(center,-half.y+b.y,half.x,half.z,b.x,b.z);
                rings[2] = Ring(center,half.y-b.y,half.x,half.z,b.x,b.z);
                rings[3] = Ring(center,half.y,half.x-b.x,half.z-b.z,b.x*.5f,b.z*.5f);
                Face(rings[0], tint, grainAxis);
                for (int level = 0; level < 3; level++)
                for (int i = 0; i < 8; i++)
                {
                    int j = (i+1)%8;
                    Face(new[] { rings[level][i],rings[level+1][i],rings[level+1][j],rings[level][j] }, tint, grainAxis);
                }
                Array.Reverse(rings[3]);
                Face(rings[3], tint, grainAxis);
            }

            static Vector3[] Ring(Vector3 center, float y, float x, float z, float bx, float bz)
            {
                return new[] {
                    center+new Vector3(-x+bx,y,-z), center+new Vector3(x-bx,y,-z),
                    center+new Vector3(x,y,-z+bz), center+new Vector3(x,y,z-bz),
                    center+new Vector3(x-bx,y,z), center+new Vector3(-x+bx,y,z),
                    center+new Vector3(-x,y,z-bz), center+new Vector3(-x,y,-z+bz)
                };
            }

            void Face(Vector3[] points, float tint, int grainAxis)
            {
                var normal = Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
                int first = vertices.Count;
                int planeNormal = LongAxis(new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z)));
                int u = grainAxis != planeNormal ? grainAxis : (planeNormal+1)%3;
                int v = 3-u-planeNormal;
                float ao = normal.y < -.1f ? .58f : normal.y < .25f ? .79f : 1f;
                foreach (var p in points)
                {
                    vertices.Add(p);
                    normals.Add(normal);
                    uv.Add(new Vector2(p[u],p[v]));
                    colors.Add(new Color(Mathf.Min(1,tint*1.025f),tint,tint*.98f,ao));
                }
                for (int i = 1; i < points.Length-1; i++)
                {
                    triangles.Add(first);
                    triangles.Add(first+i);
                    triangles.Add(first+i+1);
                }
            }

            public Mesh Finish(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0,uv);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles,0);
                return mesh;
            }
        }
    }
}
