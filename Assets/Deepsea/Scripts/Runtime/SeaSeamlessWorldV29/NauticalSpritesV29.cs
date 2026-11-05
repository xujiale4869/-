using UnityEngine;
namespace Deepsea { public sealed partial class SeaSeamlessWorldAndNauticalUIV29 {
        private Sprite BakeNauticalNineSliceSprite(string name, int w, int h, int radius, Color fillCol, Color borderCol, int borderThickness)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _owned.Add(tex);

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (w - 1 - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (h - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                    if (dist > 0.5f)
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                    else
                    {
                        float edgeAlpha = Mathf.Clamp01(0.5f - dist);
                        float borderDist = Mathf.Abs(dist + borderThickness * 0.6f);
                        float borderMask = Mathf.Clamp01(1.2f - borderDist / Mathf.Max(1f, borderThickness * 0.7f));
                        // 内部微弱次级细金线 (营造原图左下角双层线框的古典精致感)
                        float innerLine = Mathf.Clamp01(1.0f - Mathf.Abs(dist + borderThickness + 3.0f));

                        Color c = Color.Lerp(fillCol, borderCol, Mathf.Max(borderMask, innerLine * 0.35f));
                        c.a *= edgeAlpha;
                        pixels[y * w + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);

            Vector4 border = new Vector4(radius + 4, radius + 4, radius + 4, radius + 4);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            _owned.Add(sp);
            return sp;
        }
        private Sprite BakeVectorIconSprite(int iconType)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "V29_VectorIcon_" + iconType,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _owned.Add(tex);

            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y / (float)(size - 1)) * 2f - 1f; // -1 ~ +1
                for (int x = 0; x < size; x++)
                {
                    float u = (x / (float)(size - 1)) * 2f - 1f; // -1 ~ +1
                    float alpha = EvaluateIconSDF(iconType, u, v);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);

            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _owned.Add(sp);
            return sp;
        }
        private static float EvaluateIconSDF(int type, float u, float v)
        {
            float d = 10f;
            if (type == 0) // 0: 复刻原图左下角第1个【复古防风马灯】图标
            {
                // 顶部提环圆弧
                float ring = Mathf.Abs(Mathf.Sqrt(u * u + Mathf.Pow(v - 0.58f, 2f)) - 0.16f) - 0.045f;
                // 灯顶笠帽与底座横条
                float cap = BoxSDF(u, v - 0.34f, 0.36f, 0.06f);
                float baseBar = BoxSDF(u, v + 0.52f, 0.34f, 0.07f);
                // 灯身梯形玻璃框与内部暖火苗
                float bodyOuter = BoxSDF(u, v + 0.08f, 0.26f - v * 0.06f, 0.38f);
                float bodyInner = BoxSDF(u, v + 0.08f, 0.16f - v * 0.04f, 0.28f);
                float cage = Mathf.Max(bodyOuter, -bodyInner);
                float flame = Mathf.Sqrt(u * u * 1.8f + Mathf.Pow(v + 0.10f, 2f)) - 0.12f;
                d = Mathf.Min(Mathf.Min(ring, cap), Mathf.Min(baseBar, Mathf.Min(cage, flame)));
            }
            else if (type == 1) // 1: 复刻原图左下角第2个【斜交双环铁链扣】图标
            {
                // 旋转 40 度的两个交扣椭圆链环
                float c = 0.766f, s = 0.642f;
                float rx = u * c + v * s;
                float ry = -u * s + v * c;
                float link1 = Mathf.Abs( CapsuleRingSDF(rx + 0.22f, ry, 0.22f, 0.18f) ) - 0.065f;
                float link2 = Mathf.Abs( CapsuleRingSDF(rx - 0.22f, ry, 0.22f, 0.18f) ) - 0.065f;
                d = Mathf.Min(link1, link2);
            }
            else if (type == 2) // 2: 【海钓弯竿与垂线浮漂】图标
            {
                float rod = SegmentSDF(u, v, -0.58f, -0.55f, 0.38f, 0.58f) - 0.055f;
                float line = SegmentSDF(u, v, 0.38f, 0.58f, 0.42f, -0.22f) - 0.025f;
                float bobber = Mathf.Sqrt(Mathf.Pow(u - 0.42f, 2f) + Mathf.Pow(v + 0.28f, 2f)) - 0.13f;
                d = Mathf.Min(rod, Mathf.Min(line, bobber));
            }
            else if (type == 3) // 3: 【航海罗盘星芒与网格蓝图】图标
            {
                float outerRing = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - 0.62f) - 0.05f;
                float starV = BoxSDF(u, v, 0.07f - Mathf.Abs(v) * 0.08f, 0.52f);
                float starH = BoxSDF(u, v, 0.52f, 0.07f - Mathf.Abs(u) * 0.08f);
                d = Mathf.Min(outerRing, Mathf.Min(starV, starH));
            }
            else if (type == 4) // 4: 【木栈桥与桥桩】图标
            {
                float deck = BoxSDF(u, v - 0.12f, 0.68f, 0.09f);
                float p1 = BoxSDF(u + 0.42f, v + 0.24f, 0.08f, 0.34f);
                float p2 = BoxSDF(u - 0.42f, v + 0.24f, 0.08f, 0.34f);
                float wave = Mathf.Abs(v + 0.45f - Mathf.Sin(u * 5f) * 0.06f) - 0.04f;
                d = Mathf.Min(Mathf.Min(deck, p1), Mathf.Min(p2, wave));
            }
            else if (type == 5) // 5: 【水下鹿角珊瑚簇】图标
            {
                float stem = SegmentSDF(u, v, 0f, -0.58f, 0f, 0.48f) - 0.07f;
                float bL = SegmentSDF(u, v, 0f, -0.10f, -0.45f, 0.38f) - 0.065f;
                float bR = SegmentSDF(u, v, 0f, 0.02f, 0.46f, 0.42f) - 0.065f;
                float baseRock = BoxSDF(u, v + 0.56f, 0.48f, 0.08f);
                d = Mathf.Min(Mathf.Min(stem, bL), Mathf.Min(bR, baseRock));
            }
            else // 6: 【海湾捕蟹浮标与网笼】图标
            {
                float buoy = Mathf.Sqrt(u * u + Mathf.Pow(v - 0.35f, 2f)) - 0.20f;
                float rope = SegmentSDF(u, v, 0f, 0.15f, 0f, -0.18f) - 0.03f;
                float cage = Mathf.Abs(BoxSDF(u, v + 0.38f, 0.45f, 0.22f)) - 0.05f;
                d = Mathf.Min(buoy, Mathf.Min(rope, cage));
            }

            return Mathf.Clamp01(0.5f - d * 18.0f);
        }
        private static float BoxSDF(float u, float v, float hx, float hy)
        {
            float dx = Mathf.Abs(u) - hx;
            float dy = Mathf.Abs(v) - hy;
            return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f);
        }
        private static float SegmentSDF(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay;
            float bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(0.0001f, bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
        private static float CapsuleRingSDF(float px, float py, float halfLen, float radius)
        {
            float dx = Mathf.Max(Mathf.Abs(px) - halfLen, 0f);
            return Mathf.Sqrt(dx * dx + py * py) - radius;
        }
        private static RectTransform CreateUIRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
}}
