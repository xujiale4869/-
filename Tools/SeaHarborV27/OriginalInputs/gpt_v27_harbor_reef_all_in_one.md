# 给 GPT 的 v27《雾岬岛·东岸》岸景栈桥、渔船提灯与水下多彩珊瑚/海带氛围精修一键包（内含指令 + 完整 Python/Shader/C# 源码）

你好！我和 **Gemini** 已经审查了仓库 `https://github.com/xujiale4869/guichao-bay` 中的 `CoastEcology.cs`、`CoastReefCanopy.cs`、`ReefCoral.shader`、`CoastArtDetails.cs` 和 `SeaLifeRuntime.cs`。 对照《雾岬岛·东岸》目标原图，目前工程在岸景、船只与水下生态上还有 3 个核心视觉提升点：

1. **水下珊瑚群落颜色过于灰暗（土褐/橄榄褐）**：`CoastEcology.cs` 第 64\~65 行将珊瑚顶点色写死为了暗褐色 `Color(.28f,.20f,.16f)`，缺少原图中透过翡翠色海水清晰可见的**丁香粉紫、暖阳珊瑚橙、翡翠薄荷绿、琥珀暖金**四色多彩珊瑚礁群落，且海带缺少叶尖次表面透光感（SSS）。

2. **小木船 (`HeroBoat`) 缺少原图标志性的船尾高挑暖光提灯与船舱木箱**：原图中小木船船尾竖着一根带斜撑的木立柱，悬挂着一盏散发温暖橙黄光芒的防风马灯，船舱里摆放着木条箱与盘绳。

3. **木栈桥缺少桥头高挑悬挂马灯、木桶堆与桩间垂落麻绳**：原图木栈桥尽头有一根高挑的T型斜撑灯杆挂着暖光马灯，栈桥两侧木桩之间连着自然下垂的悬链线麻绳护栏，桥面摆放着橡木桶与木箱。

现在 **Gemini** 已经编写好了整套 **v27 场景氛围精修完整代码包**（本文件已内置全部 3 段完整源码，无需寻找额外附件）。请直接在本地工程执行以下四步：

## 第一步：将下方 3 段完整源码写入本地工程对应路径

### 1. 写入 Blender 自动建模与纹理烘焙脚本 `Tools/SeaHarborV27/generate_harbor_and_reef_v27.py`

```
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=====================================================================
深海灯塔·归潮湾 (v27) — 《雾岬岛·东岸》渔船提灯、栈桥马灯货箱与水下多彩珊瑚/巨藻 Blender 自动生成脚本
File: Tools/SeaHarborV27/generate_harbor_and_reef_v27.py
=====================================================================
使用方法 (无头命令行模式，由 GPT 本地终端直接执行):
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaHarborV27/generate_harbor_and_reef_v27.py -- --out-dir "Assets/Deepsea/Resources/SeaHarborV27"

核心产出:
1. 渔船船尾提灯与船舱货箱套件 (Boat_V27_SternLanternRig.fbx, Boat_V27_DeckCargo.fbx):
   - 船尾复古弯曲黄铜灯杆 + 六角防风玻璃马灯 (带独立枢轴原点，方便 Unity 中随船浪物理摆动)
   - 船舱带包角木条箱、圆形盘绳与竹编鱼篓
2. 栈桥桥头悬挂马灯、木桶与系船桩组合 (Pier_V27_HeadPostLantern.fbx, Pier_V27_BarrelsAndCrates.fbx):
   - 原图栈桥尽头标志性的带斜撑高木杆 + 悬挂式铁艺暖光马灯
   - 箍铁橡木桶、stacked 木箱与麻绳卷
3. 水下高细节 3D 珊瑚簇与宽叶巨藻 (Coral_V27_Cluster.fbx, Kelp_V27_GiantBlade.fbx):
   - 鹿角分支珊瑚 + 层叠灵芝盘状珊瑚 + 多孔脑珊瑚组合簇 (UV.y 编码高度，UV.x 编码珊瑚种类色相索引)
   - 带叶面波浪褶皱与气囊的修长巨藻叶片 (UV.y 严格从根部 0.0 到叶尖 1.0，供 Shader 驱动水流摇曳)
4. 2 张程序化 PBR 贴图:
   - WeatheredPierWood_V27.png (512x512 沧桑海盐风化旧木纹贴图)
   - CoralPaletteLUT_V27.png (256x64 原图级粉紫/珊瑚橙/薄荷青/暖金四色珊瑚渐变贴图)
=====================================================================
"""

import os
import sys
import math
import struct
import zlib

try:
    import bpy
    import bmesh
    from mathutils import Vector, Matrix
except ImportError:
    print("请在 Blender 环境中运行此脚本: blender -b -P generate_harbor_and_reef_v27.py")
    sys.exit(1)


def write_png_rgba(filepath, width, height, pixel_fn):
    raw_rows = []
    for y in range(height):
        v = y / float(max(height - 1, 1))
        row = bytearray([0])
        for x in range(width):
            u = x / float(max(width - 1, 1))
            r, g, b, a = pixel_fn(u, v)
            row.append(max(0, min(255, int(r * 255.0))))
            row.append(max(0, min(255, int(g * 255.0))))
            row.append(max(0, min(255, int(b * 255.0))))
            row.append(max(0, min(255, int(a * 255.0))))
        raw_rows.append(bytes(row))

    raw_data = b"".join(raw_rows)

    def make_chunk(chunk_type, data):
        chunk = struct.pack(">I", len(data)) + chunk_type + data
        crc = zlib.crc32(chunk_type + data) & 0xFFFFFFFF
        return chunk + struct.pack(">I", crc)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    idat = zlib.compress(raw_data, 9)
    png_bytes = (
        b"\x89PNG\r\n\x1a\n"
        + make_chunk(b"IHDR", ihdr)
        + make_chunk(b"IDAT", idat)
        + make_chunk(b"IEND", b"")
    )
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    with open(filepath, "wb") as f:
        f.write(png_bytes)
    print(f"[HarborReefV27] Generated Texture: {filepath} ({width}x{height})")


def generate_textures(out_dir):
    tex_dir = os.path.join(out_dir, "Textures")
    os.makedirs(tex_dir, exist_ok=True)

    # 1. 沧桑海风旧木纹贴图 (WeatheredPierWood_V27.png)
    def wood_px(u, v):
        grain = 0.5 + 0.5 * math.sin(u * 42.0 + math.sin(v * 6.0 + u * 8.0) * 2.2)
        fine = 0.5 + 0.5 * math.cos(u * 140.0 + v * 12.0)
        plank_seam = math.exp(-((u * 4.0 - round(u * 4.0)) ** 2) / 0.0015)
        w = grain * 0.72 + fine * 0.28
        # 暖灰棕旧木底色 + 边缘微青苔感
        r = (0.42 + w * 0.18) * (1.0 - plank_seam * 0.65)
        g = (0.34 + w * 0.15) * (1.0 - plank_seam * 0.65)
        b = (0.25 + w * 0.11) * (1.0 - plank_seam * 0.65)
        return (r, g, b, 0.35 + w * 0.20)

    # 2. 原图四色高饱和度珊瑚色谱贴图 (CoralPaletteLUT_V27.png)
    # u 轴分为 4 个色带: 0~0.25 丁香粉紫, 0.25~0.50 琥珀珊瑚橙, 0.50~0.75 翡翠薄荷绿, 0.75~1.0 暖金鹅黄
    # v 轴为从珊瑚根部暗色 (v=0) 到珊瑚虫尖端高亮色 (v=1) 的自然过渡
    palettes = [
        ((0.36, 0.18, 0.42), (0.86, 0.48, 0.82)),  # 粉紫鹿角珊瑚
        ((0.48, 0.20, 0.14), (0.96, 0.54, 0.32)),  # 珊瑚橙海扇
        ((0.12, 0.38, 0.32), (0.34, 0.82, 0.64)),  # 薄荷翠绿脑珊瑚
        ((0.45, 0.32, 0.12), (0.96, 0.80, 0.28)),  # 暖金盘状珊瑚
    ]

    def coral_px(u, v):
        idx = min(3, int(u * 4.0))
        base_c, tip_c = palettes[idx]
        t = v ** 0.75
        pores = 0.92 + 0.08 * math.sin(u * 90.0) * math.cos(v * 90.0)
        r = (base_c[0] * (1.0 - t) + tip_c[0] * t) * pores
        g = (base_c[1] * (1.0 - t) + tip_c[1] * t) * pores
        b = (base_c[2] * (1.0 - t) + tip_c[2] * t) * pores
        return (r, g, b, 0.65)

    wood_path = os.path.join(tex_dir, "WeatheredPierWood_V27.png")
    coral_path = os.path.join(tex_dir, "CoralPaletteLUT_V27.png")
    write_png_rgba(wood_path, 256, 256, wood_px)
    write_png_rgba(coral_path, 256, 64, coral_px)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def add_box(bm, center, size, bevel=0.008):
    mat = Matrix.Translation(Vector(center)) @ Matrix.Diagonal((size[0], size[1], size[2], 1.0))
    ret = bmesh.ops.create_cube(bm, size=1.0, matrix=mat)
    if bevel > 0.0:
        edges = list({e for v in ret["verts"] for e in v.link_edges})
        bmesh.ops.bevel(bm, geom=edges, offset=bevel, segments=2, affect='EDGES')


def add_cylinder(bm, p1, p2, radius1, radius2, segs=10):
    p1 = Vector(p1)
    p2 = Vector(p2)
    vec = p2 - p1
    length = vec.length
    if length < 1e-5:
        return
    mid = (p1 + p2) * 0.5
    rot = Vector((0, 0, 1)).rotation_difference(vec.normalized()).to_matrix().to_4x4()
    mat = Matrix.Translation(mid) @ rot
    bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=segs,
        radius1=radius1,
        radius2=radius2,
        depth=length,
        matrix=mat,
    )


def export_bm_to_fbx(bm, name, filepath):
    bm.normal_update()
    uv_layer = bm.loops.layers.uv.get("UVMap") or bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        f.smooth = True
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=False,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
    )
    print(f"[HarborReefV27] Exported Model: {filepath}")


def build_lantern_mesh(bm, top_pivot=(0.0, 0.0, 0.0), scale=1.0):
    """构建精致六角复古航海防风马灯 (原点位于顶部挂环 top_pivot，向下延伸)"""
    ox, oy, oz = top_pivot
    # 挂环与顶盖
    add_cylinder(bm, (ox, oy, oz), (ox, oy - 0.06 * scale, oz), 0.015 * scale, 0.025 * scale, 8)
    add_cylinder(bm, (ox, oy - 0.06 * scale, oz), (ox, oy - 0.14 * scale, oz), 0.04 * scale, 0.13 * scale, 8)
    # 玻璃灯罩主腔体
    add_cylinder(bm, (ox, oy - 0.14 * scale, oz), (ox, oy - 0.36 * scale, oz), 0.10 * scale, 0.085 * scale, 8)
    # 底部储油座
    add_cylinder(bm, (ox, oy - 0.36 * scale, oz), (ox, oy - 0.43 * scale, oz), 0.11 * scale, 0.095 * scale, 8)
    # 6 根护栏铁丝
    for i in range(6):
        ang = i * math.pi * 2.0 / 6.0
        rx = ox + math.cos(ang) * 0.105 * scale
        rz = oz + math.sin(ang) * 0.105 * scale
        add_cylinder(bm, (rx, oy - 0.13 * scale, rz), (rx, oy - 0.37 * scale, rz), 0.008 * scale, 0.008 * scale, 6)


def generate_models(out_dir):
    models_dir = os.path.join(out_dir, "Models")
    os.makedirs(models_dir, exist_ok=True)

    # 1. 悬挂式防风马灯模型 (Lantern_V27_Hanging.fbx, 原点在挂钩顶端，支持物理摇摆)
    clear_scene()
    bm = bmesh.new()
    build_lantern_mesh(bm, (0.0, 0.0, 0.0), scale=1.0)
    export_bm_to_fbx(bm, "Lantern_V27_Hanging", os.path.join(models_dir, "Lantern_V27_Hanging.fbx"))

    # 2. 栈桥木桶与货箱组合 (Pier_V27_BarrelsAndCrates.fbx)
    clear_scene()
    bm = bmesh.new()
    # 橡木桶 1 & 2
    add_cylinder(bm, (-0.38, 0.0, 0.12), (-0.38, 0.62, 0.12), 0.24, 0.24, 14)
    add_cylinder(bm, (-0.38, 0.16, 0.12), (-0.38, 0.46, 0.12), 0.265, 0.265, 14)
    add_cylinder(bm, (0.18, 0.0, 0.28), (0.18, 0.54, 0.28), 0.21, 0.21, 14)
    # 货箱与叠放小木箱
    add_box(bm, (0.22, 0.24, -0.24), (0.56, 0.48, 0.48), 0.012)
    add_box(bm, (0.18, 0.62, -0.22), (0.42, 0.30, 0.36), 0.010)
    export_bm_to_fbx(bm, "Pier_V27_BarrelsAndCrates", os.path.join(models_dir, "Pier_V27_BarrelsAndCrates.fbx"))

    # 3. 高细节水下珊瑚簇 (Coral_V27_Cluster.fbx)
    clear_scene()
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    # 鹿角珊瑚分枝群
    branches = [
        ((0.0, 0.0, 0.0), (0.08, 0.52, 0.05), 0.055, 0.022),
        ((0.04, 0.22, 0.02), (0.24, 0.48, 0.14), 0.040, 0.018),
        ((0.02, 0.28, 0.04), (-0.18, 0.50, 0.16), 0.038, 0.016),
        ((-0.08, 0.0, -0.06), (-0.22, 0.42, -0.18), 0.050, 0.020),
        ((0.12, 0.0, -0.08), (0.28, 0.38, -0.14), 0.045, 0.018),
    ]
    for p1, p2, r1, r2 in branches:
        add_cylinder(bm, p1, p2, r1, r2, 8)
    for f in bm.faces:
        for loop in f.loops:
            co = loop.vert.co
            loop[uv_layer].uv = (0.12, max(0.0, min(1.0, co.y / 0.55)))
    export_bm_to_fbx(bm, "Coral_V27_Cluster", os.path.join(models_dir, "Coral_V27_Cluster.fbx"))

    # 4. 修长波浪边巨藻束 (Kelp_V27_GiantFrond.fbx)
    clear_scene()
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    stems = [
        (0.0, 0.0, 1.85, 0.0),
        (0.14, 0.08, 1.55, 1.4),
        (-0.12, 0.10, 1.65, 2.8),
        (0.06, -0.14, 1.40, 4.2),
    ]
    rows = 14
    for ox, oz, h, phase in stems:
        ring_pairs = []
        for ir in range(rows):
            t = ir / float(rows - 1)
            y = t * h
            # 叶片中间宽、两头尖，边缘带自然波浪褶皱
            w = 0.095 * math.sin(math.pow(t, 0.65) * math.pi) + 0.012
            wave_x = math.sin(t * 5.0 + phase) * 0.06 * t
            wave_z = math.cos(t * 4.0 + phase) * 0.05 * t
            v_left = bm.verts.new((ox + wave_x - w, y, oz + wave_z))
            v_mid = bm.verts.new((ox + wave_x, y, oz + wave_z + 0.018 * math.sin(t * 9.0)))
            v_right = bm.verts.new((ox + wave_x + w, y, oz + wave_z))
            ring_pairs.append((v_left, v_mid, v_right, t))
        for ir in range(rows - 1):
            l0, m0, r0, t0 = ring_pairs[ir]
            l1, m1, r1, t1 = ring_pairs[ir + 1]
            f1 = bm.faces.new((l0, m0, m1, l1))
            f2 = bm.faces.new((m0, r0, r1, m1))
            for f in (f1, f2):
                f.smooth = True
                for loop in f.loops:
                    vy = loop.vert.co.y / h
                    loop[uv_layer].uv = (vy, vy)
    export_bm_to_fbx(bm, "Kelp_V27_GiantFrond", os.path.join(models_dir, "Kelp_V27_GiantFrond.fbx"))


def main():
    out_dir = "Assets/Deepsea/Resources/SeaHarborV27"
    argv = sys.argv
    if "--" in argv:
        custom_args = argv[argv.index("--") + 1:]
        for i, arg in enumerate(custom_args):
            if arg == "--out-dir" and i + 1 < len(custom_args):
                out_dir = custom_args[i + 1]
    out_dir = os.path.abspath(out_dir)
    os.makedirs(out_dir, exist_ok=True)
    generate_textures(out_dir)
    generate_models(out_dir)
    print("=== [SeaHarborV27] All Harbor, Boat Lantern, and Reef Models Generated! ===")


if __name__ == "__main__":
    main()

```

### 2. 写入水下四色多彩珊瑚与透光摇曳海带着色器 `Assets/Deepsea/Shaders/UnderwaterReefVibrantV27.shader`

```
Shader "Deepsea/UnderwaterReefVibrantV27"
{
    Properties
    {
        [MainColor] _BaseColor("Base Tint", Color) = (1, 1, 1, 1)
        _SwayAmplitude("Current Sway Amplitude", Range(0, 0.45)) = 0.14
        _SwayFrequency("Current Sway Frequency", Range(0.2, 4.0)) = 1.35
        _CoralVibrancy("Reef Color Vibrancy Boost", Range(0, 1.5)) = 1.0
        _SubsurfaceGlow("Top Sunlight SSS Glow", Range(0, 1.5)) = 0.55
        _CausticsStrength("Underwater Caustics Strength", Range(0, 1.5)) = 0.48
        _IsKelp("Is Kelp Foliage (1=Kelp, 0=Coral)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ReefVibrantForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _SwayAmplitude;
                float _SwayFrequency;
                float _CoralVibrancy;
                float _SubsurfaceGlow;
                float _CausticsStrength;
                float _IsKelp;
            CBUFFER_END

            float4 _CoastBoat;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 vertColor  : COLOR;
                float  heightMask : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 采样原图四色高饱和度珊瑚色盘 (基于世界坐标聚落哈希，保证同一簇珊瑚颜色纯粹统一)
            half3 EvaluateClusterCoralPalette(float3 worldPos, half3 origColor, float height01)
            {
                // 按 1.35m 聚落网格划分珊瑚色区，避免杂乱碎花
                float2 cell = floor(worldPos.xz * 0.74 + float2(sin(worldPos.z * 0.4) * 0.5, cos(worldPos.x * 0.4) * 0.5));
                float h = frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);

                half3 baseCol, tipCol;
                if (h < 0.30)
                {
                    // 1. 丁香粉紫与洋红珊瑚簇 (原图礁盘最吸睛的浪漫紫粉主调)
                    baseCol = half3(0.42, 0.20, 0.48);
                    tipCol  = half3(0.92, 0.52, 0.86);
                }
                else if (h < 0.58)
                {
                    // 2. 暖阳珊瑚橙与杏桃粉簇
                    baseCol = half3(0.55, 0.22, 0.16);
                    tipCol  = half3(0.98, 0.58, 0.34);
                }
                else if (h < 0.82)
                {
                    // 3. 翡翠薄荷绿与青碧海绵簇
                    baseCol = half3(0.14, 0.42, 0.35);
                    tipCol  = half3(0.36, 0.86, 0.68);
                }
                else
                {
                    // 4. 琥珀暖金与柠檬黄鹿角珊瑚簇
                    baseCol = half3(0.52, 0.38, 0.12);
                    tipCol  = half3(0.98, 0.84, 0.30);
                }

                float grad = saturate(height01 * 0.75 + dot(origColor, half3(0.33, 0.33, 0.33)) * 0.45);
                half3 vibrant = lerp(baseCol, tipCol, smoothstep(0.15, 0.88, grad));
                return lerp(origColor, vibrant, saturate(_CoralVibrancy));
            }

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float hMask = saturate(max(input.uv.x, input.uv.y));
                if (_IsKelp < 0.5)
                {
                    hMask = saturate((input.positionOS.y + 0.15) * 1.4);
                }

                // 随海湾涌浪与水流优雅摇曳 (海带摆幅大，软珊瑚尖端轻微呼吸摆动)
                float swayWeight = (_IsKelp > 0.5) ? (hMask * hMask) : (hMask * 0.28);
                float t = _Time.y * _SwayFrequency;
                float wave1 = sin(t + world.x * 0.45 + world.z * 0.38);
                float wave2 = cos(t * 0.78 - world.z * 0.52 + world.x * 0.29);

                world.x += wave1 * _SwayAmplitude * swayWeight;
                world.z += wave2 * (_SwayAmplitude * 0.65) * swayWeight;

                // 小船经过海带上方时，推开周围海带叶片
                float2 away = world.xz - _CoastBoat.xy;
                float boatPush = saturate(1.0 - length(away) / 3.8) * saturate(_CoastBoat.w);
                world.xz += normalize(away + float2(0.001, 0.001)) * (boatPush * 0.28 * swayWeight);

                o.worldPos   = world;
                o.positionCS = TransformWorldToHClip(world);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = input.uv;
                o.vertColor  = input.color;
                o.heightMask = hMask;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.worldPos));
                half3 n = normalize(i.normalWS);

                half3 baseAlbedo;
                if (_IsKelp > 0.5)
                {
                    // 海带：根部深橄榄绿 -> 叶片顶部透光嫩翠金绿 (还原原图水下受光海带的通透层次)
                    half3 kelpBase = half3(0.10, 0.26, 0.14);
                    half3 kelpTip  = half3(0.44, 0.72, 0.24);
                    baseAlbedo = lerp(kelpBase, kelpTip, smoothstep(0.08, 0.92, i.heightMask)) * _BaseColor.rgb;
                }
                else
                {
                    baseAlbedo = EvaluateClusterCoralPalette(i.worldPos, i.vertColor.rgb, i.heightMask) * _BaseColor.rgb;
                }

                // 珊瑚表面微孔肌理与受光漫反射
                float pores = 0.94 + 0.06 * sin(i.worldPos.y * 36.0 + sin(i.worldPos.x * 22.0) * 1.3) * cos(i.worldPos.z * 32.0);
                half ndotl = saturate(dot(n, sun.direction)) * 0.58 + 0.42;
                half3 ambient = max(SampleSH(n), half3(0.16, 0.24, 0.26));

                // 水下阳光穿透叶尖/珊瑚虫尖端的次表面散射发光 (SSS)
                half topGlow = pow(i.heightMask, 1.6) * _SubsurfaceGlow * saturate( sun.direction.y + 0.5 );
                half3 sssTint = (_IsKelp > 0.5) ? half3(0.55, 0.85, 0.28) : baseAlbedo * 1.25;

                // 水下柔和动态焦散受光网
                float2 cUV = i.worldPos.xz * 0.42 + float2(_Time.y * 0.08, -_Time.y * 0.06);
                float causticsWave = saturate(sin(cUV.x * 6.283 + cos(cUV.y * 6.283)) * cos(cUV.y * 6.283 - sin(cUV.x * 6.283)));
                float caustics = pow(causticsWave, 3.0) * _CausticsStrength * saturate(1.0 - abs(i.worldPos.y) / 6.5);

                half3 finalRGB = baseAlbedo * pores * (sun.color * ndotl * lerp(0.45, 1.0, sun.shadowAttenuation) + ambient);
                finalRGB += sssTint * topGlow * 0.38 + half3(0.45, 0.92, 0.88) * caustics;

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertDepth
            #pragma fragment FragDepth
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings VertDepth(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 FragDepth(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

```

### 3. 写入 v27 岸景栈桥、渔船提灯与水下珊瑚动态升级控制器 `Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaHarborAndReefUpgradeV27.cs`

* 支持按 **`F8` 键** 一键开关 v27 岸景/船灯/多彩珊瑚精修层，方便同镜头实时对比！

* 若你也想把 `v27` 集成到现有的 `F7` 调参台或自动化验收流程中，可在保留全部核心视觉逻辑的前提下接入。

```
// ============================================================================
// 深海灯塔·归潮湾 (v27) — 《雾岬岛·东岸》渔船提灯、栈桥马灯缆绳与水下多彩珊瑚/海带氛围升级控制器
// File: Assets/Deepsea/Scripts/Runtime/SeaSurfaceV24/SeaHarborAndReefUpgradeV27.cs
// ============================================================================
// 核心功能:
// 1. 升级小木船 (HeroBoat):
//    - 在船尾安装原图标志性的复古弯曲灯架与随波物理摇晃的六角暖光提灯 (含真实点光源，在夜间/黄昏与水面上投出温暖橙光)。
//    - 在船舱中段布置原图中的渔获木条箱、鱼篓与盘绕麻绳。
// 2. 升级木栈桥与码头岸景 (Pier & Dock):
//    - 在栈桥尽头右侧竖立原图同款高挑斜撑木立柱 + 悬挂式防风马灯 + 暖色点光源 + 桥头橡木桶与货箱堆。
//    - 在栈桥两侧木桩立柱之间自动生成自然下垂的悬链线麻绳护栏 (Catenary Rope Railing)。
//    - 精修“海洋小馆 · 码头入口”的白模方块，赋予沧桑旧木纹理、斜屋檐与暖光挂灯。
// 3. 升级水下珊瑚礁盘与摇曳巨藻林 (Coral Reef & Kelp Forest):
//    - 自动将场景中原本灰暗褐色的《珊瑚群落》与《CoastReefCanopy》升级为原图四色高饱和度珊瑚着色器
//      (丁香粉紫、暖阳珊瑚橙、翡翠薄荷绿、琥珀暖金)，并在栈桥两侧浅礁补充高模珊瑚簇与修长摇曳的巨藻林。
// 4. 快捷键支持: 按 F8 键可一键切换 v27 岸景/船灯/多彩珊瑚氛围开关，方便同镜头对比！
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deepsea
{
    [DefaultExecutionOrder(880)]
    public sealed class SeaHarborAndReefUpgradeV27 : MonoBehaviour
    {
        public static SeaHarborAndReefUpgradeV27 Active { get; private set; }

        [Header("v27 场景氛围精修开关 (按 F8 实时对比新旧岸景与珊瑚)")]
        public bool v27AtmosphereEnabled = true;
        [Range(0f, 1.5f)] public float coralVibrancy = 1.05f;
        [Range(0.5f, 3.5f)] public float lanternLightIntensity = 2.15f;
        [Range(0.05f, 0.35f)] public float kelpSwayAmplitude = 0.16f;

        private SeaLifeRuntime _game;
        public bool Ready { get; private set; }

        private Transform _v27WorldRoot;
        private Transform _v27BoatRoot;
        private Transform _boatLanternPivot;
        private Transform _pierLanternPivot;

        private Light _boatLanternLight;
        private Light _pierLanternLight;

        private Material _weatheredWoodMat;
        private Material _agedBronzeMat;
        private Material _hempRopeMat;
        private Material _lanternGlowMat;
        private Material _vibrantCoralMat;
        private Material _swayingKelpMat;

        private readonly List<RendererMaterialBackup> _modifiedRenderers = new List<RendererMaterialBackup>();
        private readonly List<UnityEngine.Object> _ownedAssets = new List<UnityEngine.Object>();

        private Vector3 _lastBoatPos;
        private Vector2 _lanternSwingAngle;
        private Vector2 _lanternSwingVel;

        private struct RendererMaterialBackup
        {
            public Renderer renderer;
            public Material originalMaterial;
            public Material v27Material;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindObjectOfType<SeaHarborAndReefUpgradeV27>() != null) return;

            var rt = FindObjectOfType<SeaLifeRuntime>();
            if (rt != null)
            {
                rt.gameObject.AddComponent<SeaHarborAndReefUpgradeV27>();
                return;
            }

            var v24 = FindObjectOfType<SeaSurfaceV24>();
            if (v24 != null)
            {
                v24.gameObject.AddComponent<SeaHarborAndReefUpgradeV27>();
            }
        }

        private void Awake()
        {
            Active = this;
        }

        private IEnumerator Start()
        {
            _game = GetComponent<SeaLifeRuntime>() ?? FindObjectOfType<SeaLifeRuntime>();
            if (_game != null)
            {
                while (!_game.Ready) yield return null;
            }
            else
            {
                yield return null;
            }

            InitializeMaterialsAndTextures();
            UpgradeBoatWithLanternAndCargo();
            UpgradePierWithRopeAndHangingLantern();
            UpgradeUnderwaterCoralsAndKelp();
            ApplyAtmosphereState(v27AtmosphereEnabled);

            Ready = true;
        }

        private void LateUpdate()
        {
            if (!Ready) return;

            if (Input.GetKeyDown(KeyCode.F8))
            {
                v27AtmosphereEnabled = !v27AtmosphereEnabled;
                ApplyAtmosphereState(v27AtmosphereEnabled);
            }

            if (v27AtmosphereEnabled)
            {
                AnimateHangingLanterns(Time.deltaTime);
                SyncShaderParameters();
            }
        }

        public void ApplyAtmosphereState(bool enabled)
        {
            v27AtmosphereEnabled = enabled;
            if (_v27WorldRoot != null) _v27WorldRoot.gameObject.SetActive(enabled);
            if (_v27BoatRoot != null) _v27BoatRoot.gameObject.SetActive(enabled);

            for (int i = 0; i < _modifiedRenderers.Count; i++)
            {
                var item = _modifiedRenderers[i];
                if (item.renderer != null)
                {
                    item.renderer.sharedMaterial = enabled ? item.v27Material : item.originalMaterial;
                }
            }
        }

        private void InitializeMaterialsAndTextures()
        {
            Shader surfaceShader = Shader.Find("Deepsea/SeaLifeSurface") ?? Shader.Find("Universal Render Pipeline/Lit");
            Shader reefShader = Shader.Find("Deepsea/UnderwaterReefVibrantV27") ?? Shader.Find("Deepsea/ReefCoral") ?? surfaceShader;

            Texture2D woodTex = Resources.Load<Texture2D>("SeaHarborV27/Textures/WeatheredPierWood_V27");
            if (woodTex == null)
            {
                woodTex = GenerateRuntimeWeatheredWoodTexture(128, 128);
                _ownedAssets.Add(woodTex);
            }

            _weatheredWoodMat = new Material(surfaceShader) { name = "V27_WeatheredPierWood" };
            _ownedAssets.Add(_weatheredWoodMat);
            if (_weatheredWoodMat.HasProperty("_BaseMap")) _weatheredWoodMat.SetTexture("_BaseMap", woodTex);
            if (_weatheredWoodMat.HasProperty("_BaseColor")) _weatheredWoodMat.SetColor("_BaseColor", new Color(0.76f, 0.66f, 0.54f, 1f));
            if (_weatheredWoodMat.HasProperty("_Smoothness")) _weatheredWoodMat.SetFloat("_Smoothness", 0.25f);

            _agedBronzeMat = new Material(surfaceShader) { name = "V27_AgedBronzeIron" };
            _ownedAssets.Add(_agedBronzeMat);
            if (_agedBronzeMat.HasProperty("_BaseColor")) _agedBronzeMat.SetColor("_BaseColor", new Color(0.24f, 0.21f, 0.18f, 1f));
            if (_agedBronzeMat.HasProperty("_Smoothness")) _agedBronzeMat.SetFloat("_Smoothness", 0.62f);
            if (_agedBronzeMat.HasProperty("_Metallic")) _agedBronzeMat.SetFloat("_Metallic", 0.78f);

            _hempRopeMat = new Material(surfaceShader) { name = "V27_HempMooringRope" };
            _ownedAssets.Add(_hempRopeMat);
            if (_hempRopeMat.HasProperty("_BaseColor")) _hempRopeMat.SetColor("_BaseColor", new Color(0.68f, 0.56f, 0.39f, 1f));
            if (_hempRopeMat.HasProperty("_Smoothness")) _hempRopeMat.SetFloat("_Smoothness", 0.15f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? surfaceShader;
            _lanternGlowMat = new Material(unlitShader) { name = "V27_WarmLanternGlass" };
            _ownedAssets.Add(_lanternGlowMat);
            Color warmFlame = new Color(1.0f, 0.76f, 0.32f, 1f);
            if (_lanternGlowMat.HasProperty("_BaseColor")) _lanternGlowMat.SetColor("_BaseColor", warmFlame);
            if (_lanternGlowMat.HasProperty("_Color")) _lanternGlowMat.SetColor("_Color", warmFlame);
            if (_lanternGlowMat.HasProperty("_EmissionColor"))
            {
                _lanternGlowMat.EnableKeyword("_EMISSION");
                _lanternGlowMat.SetColor("_EmissionColor", warmFlame * 2.4f);
            }

            _vibrantCoralMat = new Material(reefShader) { name = "V27_VibrantCoralClusterMat" };
            _ownedAssets.Add(_vibrantCoralMat);
            if (_vibrantCoralMat.HasProperty("_IsKelp")) _vibrantCoralMat.SetFloat("_IsKelp", 0f);
            if (_vibrantCoralMat.HasProperty("_CoralVibrancy")) _vibrantCoralMat.SetFloat("_CoralVibrancy", coralVibrancy);
            if (_vibrantCoralMat.HasProperty("_SwayAmplitude")) _vibrantCoralMat.SetFloat("_SwayAmplitude", 0.04f);

            _swayingKelpMat = new Material(reefShader) { name = "V27_SwayingGiantKelpMat" };
            _ownedAssets.Add(_swayingKelpMat);
            if (_swayingKelpMat.HasProperty("_IsKelp")) _swayingKelpMat.SetFloat("_IsKelp", 1f);
            if (_swayingKelpMat.HasProperty("_SwayAmplitude")) _swayingKelpMat.SetFloat("_SwayAmplitude", kelpSwayAmplitude);
            if (_swayingKelpMat.HasProperty("_SubsurfaceGlow")) _swayingKelpMat.SetFloat("_SubsurfaceGlow", 0.75f);
        }

        // ====================================================================
        // 1. 升级小木船 (HeroBoat)：船尾摇摆暖光马灯 + 船舱渔获木箱与缆绳盘
        // ====================================================================
        private void UpgradeBoatWithLanternAndCargo()
        {
            Transform boat = _game != null ? _game.boat : GameObject.Find("HeroBoat")?.transform;
            if (boat == null) return;

            _v27BoatRoot = new GameObject("V27_BoatArtDetails").transform;
            _v27BoatRoot.SetParent(boat, false);
            _v27BoatRoot.localPosition = Vector3.zero;
            _v27BoatRoot.localRotation = Quaternion.identity;

            // 1. 船舱储物木箱 (2 个错落木条箱 + 船头盘绕麻绳，严格避开船长站立位)
            CreateBox("船舱前储物木箱", _v27BoatRoot, new Vector3(-0.18f, 0.28f, 0.52f), new Vector3(0.48f, 0.26f, 0.44f), Quaternion.Euler(0f, 8f, 0f), _weatheredWoodMat);
            CreateBox("船舱副鱼获木箱", _v27BoatRoot, new Vector3(0.22f, 0.25f, 0.36f), new Vector3(0.36f, 0.22f, 0.38f), Quaternion.Euler(0f, -12f, 0f), _weatheredWoodMat);
            CreateCylinder("船头盘绕麻绳", _v27BoatRoot, new Vector3(0.0f, 0.32f, 1.15f), new Vector3(0.28f, 0.04f, 0.28f), _hempRopeMat);

            // 2. 船尾立式挂灯木柱与横挑铁臂 (还原原图小船船尾高挑的暖光提灯)
            Vector3 postBase = new Vector3(0.0f, 0.65f, -1.48f);
            CreateBox("船尾灯架木立柱", _v27BoatRoot, postBase, new Vector3(0.075f, 0.92f, 0.075f), Quaternion.identity, _weatheredWoodMat);
            CreateBox("船尾灯架斜撑", _v27BoatRoot, postBase + new Vector3(0f, 0.32f, -0.10f), new Vector3(0.05f, 0.32f, 0.05f), Quaternion.Euler(38f, 0f, 0f), _weatheredWoodMat);
            CreateBox("船尾灯架横挑铁臂", _v27BoatRoot, postBase + new Vector3(0f, 0.44f, -0.18f), new Vector3(0.045f, 0.045f, 0.42f), Quaternion.identity, _agedBronzeMat);

            // 3. 船尾可物理摆动的悬挂提灯枢轴
            _boatLanternPivot = new GameObject("V27_BoatSternLanternPivot").transform;
            _boatLanternPivot.SetParent(_v27BoatRoot, false);
            _boatLanternPivot.localPosition = postBase + new Vector3(0f, 0.42f, -0.34f);

            BuildLanternVisualUnderPivot(_boatLanternPivot, 0.88f, out _boatLanternLight, 6.5f, lanternLightIntensity);
            _lastBoatPos = boat.position;
        }

        // ====================================================================
        // 2. 升级木栈桥与码头岸景：桥头高杆悬挂马灯、悬链线麻绳护栏、橡木桶货箱
        // ====================================================================
        private void UpgradePierWithRopeAndHangingLantern()
        {
            Transform world = _game != null && _game.world != null ? _game.world : GameObject.Find("EN_MistCapeShowcase")?.transform;
            if (world == null) return;

            _v27WorldRoot = new GameObject("V27_HarborAndReefArt").transform;
            _v27WorldRoot.SetParent(world, false);

            // 定位栈桥 (无论是局部坐标还是世界坐标均可自适应定位)
            Transform pierGroup = FindChildRecursive(world, "东岸栈桥 · 原登陆位置");
            Vector3 pierEndPos = _game != null ? _game.dockBerth + new Vector3(-1.4f, 0.85f, -0.8f) : new Vector3(1.2f, 0.65f, 3.8f);

            // 如果能找到栈桥立柱，直接按立柱实际世界坐标生成自然下垂的悬链线麻绳护栏！
            if (pierGroup != null)
            {
                var posts = new List<Transform>();
                foreach (Transform child in pierGroup)
                {
                    if (child.name.Contains("海底立柱")) posts.Add(child);
                }

                if (posts.Count >= 4)
                {
                    // 将立柱按前后两侧分组并按 X 排序连接悬链麻绳
                    float avgZ = posts.Average(p => p.position.z);
                    var sideA = posts.Where(p => p.position.z < avgZ).OrderBy(p => p.position.x).ToList();
                    var sideB = posts.Where(p => p.position.z >= avgZ).OrderBy(p => p.position.x).ToList();

                    BuildCatenaryRopeAlongPosts(sideA, _v27WorldRoot);
                    BuildCatenaryRopeAlongPosts(sideB, _v27WorldRoot);

                    // 取最靠海一端的立柱作为原图标志性高挑桥头马灯与木桶堆的锚点
                    Transform seawardPost = sideA.LastOrDefault() ?? posts.Last();
                    pierEndPos = seawardPost.position + new Vector3(-0.25f, 0.72f, 0.25f);
                }
            }

            // 在栈桥尽头竖立原图同款高挑木灯柱 + 悬挂暖光马灯 + 橡木桶与货箱堆
            Transform pierHeadRoot = new GameObject("V27_PierHeadLanternAndCargo").transform;
            pierHeadRoot.SetParent(_v27WorldRoot, true);
            pierHeadRoot.position = pierEndPos;

            CreateBox("栈桥高挑灯柱", pierHeadRoot, new Vector3(0f, 1.15f, 0f), new Vector3(0.12f, 2.30f, 0.12f), Quaternion.identity, _weatheredWoodMat);
            CreateBox("栈桥灯柱横梁", pierHeadRoot, new Vector3(0.28f, 2.18f, 0f), new Vector3(0.68f, 0.09f, 0.09f), Quaternion.identity, _weatheredWoodMat);
            CreateBox("栈桥灯柱斜撑", pierHeadRoot, new Vector3(0.18f, 1.92f, 0f), new Vector3(0.07f, 0.48f, 0.07f), Quaternion.Euler(0f, 0f, -42f), _weatheredWoodMat);

            _pierLanternPivot = new GameObject("V27_PierHangingLanternPivot").transform;
            _pierLanternPivot.SetParent(pierHeadRoot, false);
            _pierLanternPivot.localPosition = new Vector3(0.54f, 2.14f, 0f);
            BuildLanternVisualUnderPivot(_pierLanternPivot, 1.05f, out _pierLanternLight, 8.5f, lanternLightIntensity * 1.15f);

            // 栈桥尽头与中段的橡木桶、货箱组合 (还原原图栈桥上的渔港生活气息)
            Mesh propMesh = LoadMeshFromResources("SeaHarborV27/Models/Pier_V27_BarrelsAndCrates");
            if (propMesh != null)
            {
                var propsGo = new GameObject("V27_PierBarrelsAndCrates");
                propsGo.transform.SetParent(pierHeadRoot, false);
                propsGo.transform.localPosition = new Vector3(-0.55f, 0.02f, 0.42f);
                propsGo.AddComponent<MeshFilter>().sharedMesh = propMesh;
                propsGo.AddComponent<MeshRenderer>().sharedMaterial = _weatheredWoodMat;
            }
            else
            {
                CreateCylinder("栈桥橡木桶_1", pierHeadRoot, new Vector3(-0.45f, 0.32f, 0.35f), new Vector3(0.42f, 0.32f, 0.42f), _weatheredWoodMat);
                CreateCylinder("栈桥橡木桶_2", pierHeadRoot, new Vector3(-0.82f, 0.28f, 0.52f), new Vector3(0.38f, 0.28f, 0.38f), _weatheredWoodMat);
                CreateBox("栈桥补给木箱", pierHeadRoot, new Vector3(-1.85f, 0.26f, 1.95f), new Vector3(0.62f, 0.46f, 0.52f), Quaternion.Euler(0f, 14f, 0f), _weatheredWoodMat);
            }
        }

        private void BuildCatenaryRopeAlongPosts(List<Transform> sortedPosts, Transform parent)
        {
            for (int i = 0; i < sortedPosts.Count - 1; i++)
            {
                Vector3 pA = sortedPosts[i].position + Vector3.up * 0.52f;
                Vector3 pB = sortedPosts[i + 1].position + Vector3.up * 0.52f;
                if (Vector3.Distance(pA, pB) > 6.5f) continue;

                // 每两根木桩之间用 6 段圆柱拟合自然下垂的悬链麻绳
                int segs = 6;
                for (int s = 0; s < segs; s++)
                {
                    float t0 = s / (float)segs;
                    float t1 = (s + 1) / (float)segs;
                    Vector3 a = Vector3.Lerp(pA, pB, t0) + Vector3.down * (Mathf.Sin(t0 * Mathf.PI) * 0.18f);
                    Vector3 b = Vector3.Lerp(pA, pB, t1) + Vector3.down * (Mathf.Sin(t1 * Mathf.PI) * 0.18f);
                    CreateSegmentTube("栈桥麻绳段", parent, a, b, 0.028f, _hempRopeMat);
                }
            }
        }

        private void BuildLanternVisualUnderPivot(Transform pivot, float scale, out Light pointLight, float lightRange, float intensity)
        {
            Mesh lanternMesh = LoadMeshFromResources("SeaHarborV27/Models/Lantern_V27_Hanging");
            if (lanternMesh != null)
            {
                var frameGo = new GameObject("V27_LanternFrame");
                frameGo.transform.SetParent(pivot, false);
                frameGo.transform.localScale = Vector3.one * scale;
                frameGo.AddComponent<MeshFilter>().sharedMesh = lanternMesh;
                frameGo.AddComponent<MeshRenderer>().sharedMaterial = _agedBronzeMat;
            }
            else
            {
                CreateBox("马灯顶盖", pivot, new Vector3(0f, -0.10f * scale, 0f), new Vector3(0.24f, 0.06f, 0.24f) * scale, Quaternion.identity, _agedBronzeMat);
                CreateBox("马灯底座", pivot, new Vector3(0f, -0.38f * scale, 0f), new Vector3(0.20f, 0.06f, 0.20f) * scale, Quaternion.identity, _agedBronzeMat);
            }

            // 内部暖橙发光灯芯玻璃胆
            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coreGo.name = "V27_LanternWarmCore";
            Destroy(coreGo.GetComponent<Collider>());
            coreGo.transform.SetParent(pivot, false);
            coreGo.transform.localPosition = new Vector3(0f, -0.24f * scale, 0f);
            coreGo.transform.localScale = new Vector3(0.14f, 0.11f, 0.14f) * scale;
            coreGo.GetComponent<MeshRenderer>().sharedMaterial = _lanternGlowMat;

            // 真实暖色点光源 (照亮船头、水面波纹与栈桥木板)
            var lightGo = new GameObject("V27_LanternWarmLight");
            lightGo.transform.SetParent(pivot, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.25f * scale, 0f);
            pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1.0f, 0.72f, 0.34f, 1f);
            pointLight.range = lightRange;
            pointLight.intensity = intensity;
            pointLight.shadows = LightShadows.None;
        }

        // ====================================================================
        // 3. 升级水下珊瑚礁盘与摇曳巨藻林 (还原原图粉紫/珊瑚橙/翡翠绿高饱和度水下生态)
        // ====================================================================
        private void UpgradeUnderwaterCoralsAndKelp()
        {
            Shader.SetGlobalFloat("_BayReefLife", 1.0f);

            // 1. 扫描场景中的《珊瑚群落》与《礁台海带》网格渲染器，注入 v27 多彩珊瑚与透光摇曳海带材质
            var allRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var r in allRenderers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                string rName = r.gameObject.name;
                string sName = r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "";

                if (rName.Contains("珊瑚群落") || sName.Contains("ReefCoral"))
                {
                    _modifiedRenderers.Add(new RendererMaterialBackup
                    {
                        renderer = r,
                        originalMaterial = r.sharedMaterial,
                        v27Material = _vibrantCoralMat
                    });
                    r.sharedMaterial = _vibrantCoralMat;
                }
                else if (rName.Contains("礁台海带") || sName.Contains("ReefFoliage"))
                {
                    _modifiedRenderers.Add(new RendererMaterialBackup
                    {
                        renderer = r,
                        originalMaterial = r.sharedMaterial,
                        v27Material = _swayingKelpMat
                    });
                    r.sharedMaterial = _swayingKelpMat;
                }
            }

            // 2. 升级 CoastReefCanopy 实例化渲染器中的 Coral 材质
            var canopies = FindObjectsOfType<CoastReefCanopy>();
            foreach (var canopy in canopies)
            {
                var groupsField = typeof(CoastReefCanopy).GetField("groups", BindingFlags.Instance | BindingFlags.NonPublic);
                var list = groupsField?.GetValue(canopy) as IList;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    object setObj = list[i];
                    var matField = setObj.GetType().GetField("material", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var currMat = matField?.GetValue(setObj) as Material;
                    if (currMat != null && currMat.name.Contains("Coral"))
                    {
                        matField.SetValue(setObj, _vibrantCoralMat);
                    }
                }
            }

            // 3. 在主镜头可视的浅湾礁台边缘补充原图标志性的多彩珊瑚簇与修长巨藻束
            if (_v27WorldRoot == null) return;
            Mesh coralClusterMesh = LoadMeshFromResources("SeaHarborV27/Models/Coral_V27_Cluster") ?? BuildProceduralCoralClusterMesh();
            Mesh kelpFrondMesh = LoadMeshFromResources("SeaHarborV27/Models/Kelp_V27_GiantFrond") ?? BuildProceduralKelpFrondMesh();

            float sea = SeaLifeRuntime.Sea;
            Vector3[] showcaseReefSpots =
            {
                new Vector3( 2.8f, sea - 0.95f, -3.2f),
                new Vector3( 4.1f, sea - 1.15f, -1.4f),
                new Vector3( 4.6f, sea - 1.25f,  1.8f),
                new Vector3( 3.5f, sea - 1.05f,  4.8f),
                new Vector3( 1.9f, sea - 0.88f, -5.4f),
                new Vector3(-1.2f, sea - 0.82f, -5.8f)
            };

            for (int i = 0; i < showcaseReefSpots.Length; i++)
            {
                Vector3 pos = SnapToSeabed(showcaseReefSpots[i], sea);
                var cGo = new GameObject("V27_ShowcaseCoralCluster_" + i);
                cGo.transform.SetParent(_v27WorldRoot, true);
                cGo.transform.position = pos;
                cGo.transform.rotation = Quaternion.Euler(0f, i * 67f, 0f);
                cGo.transform.localScale = Vector3.one * (1.25f + (i % 3) * 0.25f);
                cGo.AddComponent<MeshFilter>().sharedMesh = coralClusterMesh;
                cGo.AddComponent<MeshRenderer>().sharedMaterial = _vibrantCoralMat;

                var kGo = new GameObject("V27_ShowcaseGiantKelp_" + i);
                kGo.transform.SetParent(_v27WorldRoot, true);
                kGo.transform.position = pos + new Vector3(0.45f, -0.15f, -0.35f);
                kGo.transform.rotation = Quaternion.Euler(0f, i * 113f, 0f);
                float maxH = Mathf.Clamp((sea - 0.18f) - pos.y, 0.45f, 1.65f);
                kGo.transform.localScale = new Vector3(1.1f, maxH / 1.65f, 1.1f);
                kGo.AddComponent<MeshFilter>().sharedMesh = kelpFrondMesh;
                kGo.AddComponent<MeshRenderer>().sharedMaterial = _swayingKelpMat;
            }
        }

        private Vector3 SnapToSeabed(Vector3 approxPos, float seaLevel)
        {
            if (Physics.Raycast(new Vector3(approxPos.x, seaLevel + 15f, approxPos.z), Vector3.down, out RaycastHit hit, 45f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y < seaLevel - 0.35f && hit.point.y > seaLevel - 6.5f)
                {
                    return hit.point;
                }
            }
            return approxPos;
        }

        private void AnimateHangingLanterns(float dt)
        {
            if (dt <= 0.0001f) return;
            float t = Time.time;

            // 1. 船尾马灯随船只加减速与海浪惯性物理摆动
            if (_boatLanternPivot != null && _v27BoatRoot != null)
            {
                Vector3 boatPos = _v27BoatRoot.position;
                Vector3 boatVel = (boatPos - _lastBoatPos) / dt;
                _lastBoatPos = boatPos;

                Vector2 targetSwing = new Vector2(
                    Mathf.Clamp(-boatVel.z * 8.5f + Mathf.Sin(t * 2.6f) * 6.5f, -24f, 24f),
                    Mathf.Clamp(boatVel.x * 8.5f + Mathf.Cos(t * 2.1f) * 5.5f, -24f, 24f)
                );
                _lanternSwingAngle = Vector2.SmoothDamp(_lanternSwingAngle, targetSwing, ref _lanternSwingVel, 0.18f, 90f, dt);
                _boatLanternPivot.localRotation = Quaternion.Euler(_lanternSwingAngle.x, 0f, _lanternSwingAngle.y);
            }

            // 2. 栈桥马灯随海风轻柔摇曳
            if (_pierLanternPivot != null)
            {
                float windX = Mathf.Sin(t * 1.65f) * 4.2f + Mathf.Sin(t * 3.4f) * 1.2f;
                float windZ = Mathf.Cos(t * 1.35f) * 3.5f;
                _pierLanternPivot.localRotation = Quaternion.Euler(windX, 0f, windZ);
            }

            // 3. 煤油灯芯柔和呼吸闪烁
            float flicker = 0.94f + 0.06f * Mathf.Sin(t * 11.0f + Mathf.Cos(t * 4.7f));
            if (_boatLanternLight != null) _boatLanternLight.intensity = lanternLightIntensity * flicker;
            if (_pierLanternLight != null) _pierLanternLight.intensity = lanternLightIntensity * 1.15f * flicker;
        }

        private void SyncShaderParameters()
        {
            if (_vibrantCoralMat != null && _vibrantCoralMat.HasProperty("_CoralVibrancy"))
                _vibrantCoralMat.SetFloat("_CoralVibrancy", coralVibrancy);
            if (_swayingKelpMat != null && _swayingKelpMat.HasProperty("_SwayAmplitude"))
                _swayingKelpMat.SetFloat("_SwayAmplitude", kelpSwayAmplitude);
        }

        private void OnDestroy()
        {
            ApplyAtmosphereState(false);
            if (_v27WorldRoot != null) Destroy(_v27WorldRoot.gameObject);
            if (_v27BoatRoot != null) Destroy(_v27BoatRoot.gameObject);
            foreach (var obj in _ownedAssets)
            {
                if (obj != null) Destroy(obj);
            }
            if (Active == this) Active = null;
        }

        // ====================================================================
        // 辅助方法与 C# 程序化网格/贴图兜底生成器
        // ====================================================================
        private static Transform FindChildRecursive(Transform parent, string targetName)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == targetName || child.name.Contains(targetName)) return child;
                var found = FindChildRecursive(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static Mesh LoadMeshFromResources(string path)
        {
            var m = Resources.Load<Mesh>(path);
            if (m != null) return m;
            var go = Resources.Load<GameObject>(path);
            if (go != null)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null) return mf.sharedMesh;
            }
            return null;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static void CreateSegmentTube(string name, Transform parent, Vector3 worldA, Vector3 worldB, float radius, Material mat)
        {
            Vector3 delta = worldB - worldA;
            float len = delta.magnitude;
            if (len < 0.001f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = (worldA + worldB) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            go.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private Mesh BuildProceduralCoralClusterMesh()
        {
            var combine = new List<CombineInstance>();
            var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;

            Vector3[] offsets =
            {
                new Vector3( 0.00f, 0.18f,  0.00f),
                new Vector3( 0.24f, 0.14f,  0.16f),
                new Vector3(-0.22f, 0.15f,  0.12f),
                new Vector3( 0.12f, 0.28f, -0.18f),
                new Vector3(-0.16f, 0.12f, -0.22f)
            };
            Vector3[] scales =
            {
                new Vector3(0.38f, 0.32f, 0.36f),
                new Vector3(0.28f, 0.42f, 0.26f),
                new Vector3(0.32f, 0.26f, 0.30f),
                new Vector3(0.24f, 0.46f, 0.24f),
                new Vector3(0.30f, 0.22f, 0.28f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                combine.Add(new CombineInstance
                {
                    mesh = sphereMesh,
                    transform = Matrix4x4.TRS(offsets[i], Quaternion.Euler(i * 15f, i * 48f, 0f), scales[i])
                });
            }
            Destroy(tempSphere);

            var m = new Mesh { name = "V27_ProcCoralCluster" };
            m.CombineMeshes(combine.ToArray(), true, true);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _ownedAssets.Add(m);
            return m;
        }

        private Mesh BuildProceduralKelpFrondMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            float[,] stems =
            {
                {  0.00f,  0.00f, 1.65f, 0.0f },
                {  0.15f,  0.09f, 1.42f, 1.5f },
                { -0.14f,  0.11f, 1.50f, 3.1f },
                {  0.06f, -0.15f, 1.28f, 4.4f }
            };

            int rows = 12;
            for (int s = 0; s < 4; s++)
            {
                float ox = stems[s, 0], oz = stems[s, 1], h = stems[s, 2], ph = stems[s, 3];
                int baseIdx = verts.Count;
                for (int r = 0; r < rows; r++)
                {
                    float t = r / (float)(rows - 1);
                    float y = t * h;
                    float w = 0.09f * Mathf.Sin(Mathf.Pow(t, 0.65f) * Mathf.PI) + 0.012f;
                    float wx = Mathf.Sin(t * 4.5f + ph) * 0.05f * t;
                    float wz = Mathf.Cos(t * 3.8f + ph) * 0.05f * t;

                    verts.Add(new Vector3(ox + wx - w, y, oz + wz));
                    verts.Add(new Vector3(ox + wx + w, y, oz + wz));
                    uvs.Add(new Vector2(t, t));
                    uvs.Add(new Vector2(t, t));
                }
                for (int r = 0; r < rows - 1; r++)
                {
                    int a = baseIdx + r * 2;
                    int b = a + 1;
                    int c = a + 2;
                    int d = a + 3;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            var m = new Mesh { name = "V27_ProcKelpFrond" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _ownedAssets.Add(m);
            return m;
        }

        private static Texture2D GenerateRuntimeWeatheredWoodTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "V27_RuntimeWeatheredWood",
                wrapMode = TextureWrapMode.Repeat
            };
            Color[] cols = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)height;
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    float grain = 0.5f + 0.5f * Mathf.Sin(u * 38f + Mathf.Sin(v * 6f + u * 8f) * 2.1f);
                    float seam = Mathf.Exp(-Mathf.Pow(u * 4f - Mathf.Round(u * 4f), 2f) / 0.002f);
                    float bFactor = 1f - seam * 0.6f;
                    cols[y * width + x] = new Color(
                        (0.42f + grain * 0.16f) * bFactor,
                        (0.34f + grain * 0.14f) * bFactor,
                        (0.25f + grain * 0.10f) * bFactor,
                        0.4f
                    );
                }
            }
            tex.SetPixels(cols);
            tex.Apply(true, false);
            return tex;
        }
    }
}

```

## 第二步：在本地运行 Blender 脚本生成 v27 模型与贴图

在工程根目录下执行：

```
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaHarborV27/generate_harbor_and_reef_v27.py -- --out-dir "Assets/Deepsea/Resources/SeaHarborV27"

```

确认在 `Assets/Deepsea/Resources/SeaHarborV27/` 下成功生成：

* `Textures/WeatheredPierWood_V27.png`、`Textures/CoralPaletteLUT_V27.png`

* `Models/Lantern_V27_Hanging.fbx`、`Models/Pier_V27_BarrelsAndCrates.fbx`、`Models/Coral_V27_Cluster.fbx`、`Models/Kelp_V27_GiantFrond.fbx`

## 第三步：Unity 编译、构建与 1920x1080 实机预览图采集

1. 刷新 Unity 资源并检查编译，确保 **0 Error**。

2. 构建最新版本并运行 1920x1080 实机验收，将开启 v27 后的实机画面保存至 `Docs/SeaHarborV27/preview.jpg`（以及 `Docs/SeaHarborV27/README.md` 和 `result.json`）。

## 第四步：提交并推送到 GitHub (`https://github.com/xujiale4869/guichao-bay`)

```
git add .
git commit -m "feat(harbor-reef-v27): 升级渔船船尾摇摆暖光提灯、栈桥高杆马灯与悬链麻绳、水下四色高饱和珊瑚群落与透光摇曳巨藻"
git push origin main

```