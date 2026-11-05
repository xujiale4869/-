// ============================================================================
// 深海灯塔·归潮湾 (v30) — 风格化动态斜切海洋 UI (Persona 灵感：高对比斜切双层叠影 + 潮汐青/暖阳金撞色)
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/NauticalSpritesV29.cs
// ============================================================================
using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        /// <summary>
        /// 烘焙 Persona 风格高辨识度【斜切双层错位撞色横幅/卡槽 Sprite】：
        /// - 摒弃平庸死板的直角/普通圆角黑框！
        /// - 采用锐利动感的平行四边形斜切角 (Skew Angle) + 右下错位亮色叠影层 (Offset Shadow Plate)
        ///   + 高对比撞色切边 (深海墨黑底 + 浪花白/琥珀金/潮汐青锐利斜切边)，一眼拉满视觉冲击力！
        /// </summary>
        private Sprite BakeNauticalNineSliceSprite(string name, int w, int h, int radius, Color fillCol, Color borderCol, float borderThickness)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _owned.Add(tex);

            bool isActiveStyle = name.Contains("Active");
            bool isCardStyle = name.Contains("Card");

            // Persona 标志性双层错位背板颜色：选中态用夺目的琥珀暖金背板，常态用潮汐青白几何切边
            Color accentShadowCol = isActiveStyle
                ? new Color(0.98f, 0.78f, 0.22f, 0.96f)
                : new Color(0.18f, 0.78f, 0.72f, 0.55f);

            Color[] pixels = new Color[w * h];
            float skewSlope = isCardStyle ? 0.14f : 0.22f;

            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    // 1. 计算带斜切倾角的平行四边形 SDF (Skewed Parallelogram Box)
                    float skewX = (ny * h * 0.5f) * skewSlope;
                    float cx = x - w * 0.5f - skewX * 0.35f;
                    float cy = y - h * 0.5f;

                    float halfW = w * 0.5f - 6f;
                    float halfH = h * 0.5f - 5f;

                    // 主面板 SDF 与右下错位叠影层 SDF (Persona 经典错位双层剪影)
                    float dMain = ChamferBoxSDF(cx + 1.5f, cy - 1.5f, halfW, halfH, Mathf.Max(3f, radius * 0.45f));
                    float dShadow = ChamferBoxSDF(cx - 3.0f, cy + 3.0f, halfW, halfH, Mathf.Max(3f, radius * 0.45f));

                    Color outCol = Color.clear;

                    // 先画底层错位撞色背板
                    if (dShadow < 0.5f)
                    {
                        float shAlpha = Mathf.Clamp01(0.5f - dShadow);
                        outCol = new Color(accentShadowCol.r, accentShadowCol.g, accentShadowCol.b, accentShadowCol.a * shAlpha);
                    }

                    // 再画上层主面板 + 锐利高对比几何斜切边框 + 左上角斜切装饰条
                    if (dMain < 0.5f)
                    {
                        float mainAlpha = Mathf.Clamp01(0.5f - dMain);
                        float borderDist = Mathf.Abs(dMain + borderThickness * 0.75f);
                        float borderMask = Mathf.Clamp01(1.3f - borderDist / Mathf.Max(1.0f, borderThickness * 0.8f));

                        // 左边缘或顶部的斜切高光装饰色带 (Persona 风格视觉重心切条)
                        float slashStripe = Mathf.Clamp01(1.0f - Mathf.Abs((cx + halfW - 6f) + cy * 0.45f) / 3.2f);
                        Color bodyFill = fillCol;
                        // 内部加入微弱的斜向扫描暗纹，消除纯色塑料感
                        float diagPattern = 0.5f + 0.5f * Mathf.Sin((x + y * 1.5f) * 0.9f);
                        bodyFill.r += diagPattern * 0.015f;
                        bodyFill.g += diagPattern * 0.022f;
                        bodyFill.b += diagPattern * 0.025f;

                        Color topLayer = Color.Lerp(bodyFill, borderCol, Mathf.Max(borderMask, slashStripe * (isActiveStyle ? 0.95f : 0.45f)));
                        topLayer.a *= mainAlpha;
                        outCol = Color.Lerp(outCol, topLayer, topLayer.a);
                        outCol.a = Mathf.Max(outCol.a, topLayer.a);
                    }

                    pixels[y * w + x] = outCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            int b = Mathf.Clamp(radius + 6, 8, Mathf.Min(w, h) / 3);
            Vector4 border = new Vector4(b, b, b, b);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            _owned.Add(sp);
            return sp;
        }

        private static float ChamferBoxSDF(float px, float py, float hx, float hy, float r)
        {
            float dx = Mathf.Abs(px) - (hx - r);
            float dy = Mathf.Abs(py) - (hy - r);
            return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
                 + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        private Sprite BakeVectorIconSprite(int iconType)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "V30_VectorIcon_" + iconType,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _owned.Add(tex);

            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y / (float)(size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float u = (x / (float)(size - 1)) * 2f - 1f;
                    // 1. 底层暗色斜切衬底阴影 (让图标在任何背景下都具备鲜明立体感)
                    float shadowAlpha = EvaluateIconSDF(iconType, u - 0.05f, v + 0.05f);
                    // 2. 上层锐利高对比主体图标
                    float mainAlpha = EvaluateIconSDF(iconType, u, v);

                    Color c = Color.clear;
                    if (shadowAlpha > 0.01f)
                    {
                        c = new Color(0.02f, 0.08f, 0.11f, shadowAlpha * 0.75f);
                    }
                    if (mainAlpha > 0.01f)
                    {
                        Color fg = new Color(1f, 0.98f, 0.92f, mainAlpha);
                        c = Color.Lerp(c, fg, mainAlpha);
                        c.a = Mathf.Max(c.a, mainAlpha);
                    }
                    px[y * size + x] = c;
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
            if (type == 0) // 0: 复古航海防风提灯
            {
                float ring = Mathf.Abs(Mathf.Sqrt(u * u + Mathf.Pow(v - 0.58f, 2f)) - 0.16f) - 0.048f;
                float cap = BoxSDF(u, v - 0.34f, 0.38f, 0.065f);
                float baseBar = BoxSDF(u, v + 0.52f, 0.36f, 0.075f);
                float bodyOuter = BoxSDF(u, v + 0.08f, 0.27f - v * 0.06f, 0.38f);
                float bodyInner = BoxSDF(u, v + 0.08f, 0.16f - v * 0.04f, 0.27f);
                float cage = Mathf.Max(bodyOuter, -bodyInner);
                float flame = Mathf.Sqrt(u * u * 1.7f + Mathf.Pow(v + 0.10f, 2f)) - 0.13f;
                d = Mathf.Min(Mathf.Min(ring, cap), Mathf.Min(baseBar, Mathf.Min(cage, flame)));
            }
            else if (type == 1) // 1: 斜交双环锚链扣
            {
                float c = 0.766f, s = 0.642f;
                float rx = u * c + v * s;
                float ry = -u * s + v * c;
                float link1 = Mathf.Abs(CapsuleRingSDF(rx + 0.22f, ry, 0.22f, 0.18f)) - 0.07f;
                float link2 = Mathf.Abs(CapsuleRingSDF(rx - 0.22f, ry, 0.22f, 0.18f)) - 0.07f;
                d = Mathf.Min(link1, link2);
            }
            else if (type == 2) // 2: 海钓弯竿与浮漂
            {
                float rod = SegmentSDF(u, v, -0.58f, -0.55f, 0.38f, 0.58f) - 0.06f;
                float line = SegmentSDF(u, v, 0.38f, 0.58f, 0.42f, -0.22f) - 0.028f;
                float bobber = Mathf.Sqrt(Mathf.Pow(u - 0.42f, 2f) + Mathf.Pow(v + 0.28f, 2f)) - 0.14f;
                d = Mathf.Min(rod, Mathf.Min(line, bobber));
            }
            else if (type == 3) // 3: 四芒星罗盘蓝图
            {
                float outerRing = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - 0.62f) - 0.055f;
                float starV = BoxSDF(u, v, 0.08f - Mathf.Abs(v) * 0.08f, 0.54f);
                float starH = BoxSDF(u, v, 0.54f, 0.08f - Mathf.Abs(u) * 0.08f);
                d = Mathf.Min(outerRing, Mathf.Min(starV, starH));
            }
            else if (type == 4) // 4: 木栈桥与桥桩
            {
                float deck = BoxSDF(u, v - 0.12f, 0.68f, 0.095f);
                float p1 = BoxSDF(u + 0.42f, v + 0.24f, 0.085f, 0.35f);
                float p2 = BoxSDF(u - 0.42f, v + 0.24f, 0.085f, 0.35f);
                float wave = Mathf.Abs(v + 0.45f - Mathf.Sin(u * 5f) * 0.06f) - 0.045f;
                d = Mathf.Min(Mathf.Min(deck, p1), Mathf.Min(p2, wave));
            }
            else if (type == 5) // 5: 水下珊瑚簇
            {
                float stem = SegmentSDF(u, v, 0f, -0.58f, 0f, 0.48f) - 0.075f;
                float bL = SegmentSDF(u, v, 0f, -0.10f, -0.45f, 0.38f) - 0.07f;
                float bR = SegmentSDF(u, v, 0f, 0.02f, 0.46f, 0.42f) - 0.07f;
                float baseRock = BoxSDF(u, v + 0.56f, 0.48f, 0.085f);
                d = Mathf.Min(Mathf.Min(stem, bL), Mathf.Min(bR, baseRock));
            }
            else // 6: 捕蟹浮标与网笼
            {
                float buoy = Mathf.Sqrt(u * u + Mathf.Pow(v - 0.35f, 2f)) - 0.21f;
                float rope = SegmentSDF(u, v, 0f, 0.15f, 0f, -0.18f) - 0.035f;
                float cage = Mathf.Abs(BoxSDF(u, v + 0.38f, 0.46f, 0.22f)) - 0.055f;
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
    }
}
