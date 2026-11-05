#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=====================================================================
深海灯塔·归潮湾 (v27) — 《雾岬岛·东岸》渔船提灯、栈桥马灯货箱与水下多彩珊瑚/巨藻 Blender 自动生成脚本
File: Tools/SeaHarborV27/generate_harbor_and_reef_v27.py
=====================================================================
使用方法 (无头命令行模式，由 GPT 本地终端直接执行):
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaHarborV27/generate_harbor_and_reef_v27.py -- --out-dir "Assets/Deepsea/Resources/SeaHarborV27"

实际产出：4 个 FBX（开放灯笼框架、木桶货箱组合、分支珊瑚簇、四叶海带束），
2 张颜色贴图（木纹 512x512、珊瑚色盘 256x64），4 个可编辑 .blend。
船尾灯架、栈桥灯柱、绳栏和屋檐由运行时生成；没有额外的整船/整栈桥 FBX。
模型使用 Y 向上、米为单位的作者坐标；四个 Basis 空节点用于 Unity 导入验证。
颜色贴图不等同于完整 PBR 纹理组。生成的新资产仍须实机检查。
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
        v = 1.0 - y / float(max(height - 1, 1))
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
    write_png_rgba(wood_path, 512, 512, wood_px)
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
    had_uv = bm.loops.layers.uv.get("UVMap") is not None
    uv_layer = bm.loops.layers.uv.get("UVMap") or bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        f.smooth = "Coral" in name or "Kelp" in name
        if not had_uv:
            for loop in f.loops:
                co = loop.vert.co
                n = f.normal
                loop[uv_layer].uv = ((co.x if abs(n.y) > .5 else co.z) * 2, co.y * 2 if abs(n.y) < .5 else co.z * 2)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    # Basis anchors let Unity bake the exact authored Y-up metres, independent of FBX conversion.
    for key, co in [("Origin",(0,0,0)),("Right",(1,0,0)),("Up",(0,1,0)),("Forward",(0,0,1))]:
        anchor=bpy.data.objects.new("Basis_"+key,None)
        anchor.location=co
        bpy.context.collection.objects.link(anchor)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    source_dir=os.path.join(os.getcwd(),"Tools","SeaHarborV27","Sources")
    os.makedirs(source_dir,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source_dir,name+".blend"))
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
    # Open cage: the runtime emissive core must be visible between the guard rails.
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
    # Five branching colonies, with secondary forks and low ruffled plates.
    # Keep closed caps and modest geometry; no crossed billboards or mirrored faces.
    for stem in range(5):
        angle=stem*math.tau/5+.25
        root=Vector((math.cos(angle)*.14,0,math.sin(angle)*.14))
        h=.55+(stem%3)*.10
        shoulder=root+Vector((math.cos(angle)*.10,h*.42,math.sin(angle)*.10))
        add_cylinder(bm,root,shoulder,.045,.033,8)
        for arm in range(3):
            a=angle+(arm-1)*.8
            middle=shoulder+Vector((math.cos(a)*.12,h*.26,math.sin(a)*.12))
            tip=middle+Vector((math.cos(a)*.09,h*(.25+.035*(arm%2)),math.sin(a)*.09))
            add_cylinder(bm,shoulder,middle,.029,.021,8)
            add_cylinder(bm,middle,tip,.021,.010,8)
            fork=middle+Vector((math.cos(a+.9)*.105,h*.17,math.sin(a+.9)*.105))
            add_cylinder(bm,middle,fork,.016,.007,7)
    for layer in range(3):
        cx,cz=-.23,.08
        y=.085+layer*.075
        radius=.23-layer*.043
        centre=bm.verts.new((cx,y,cz))
        upper=[];lower=[]
        for i in range(20):
            a=i*math.tau/20
            r=radius*(1+.075*math.sin(a*5+layer))
            yy=y+.020*math.sin(a*5+layer)+.022
            upper.append(bm.verts.new((cx+math.cos(a)*r,yy,cz+math.sin(a)*r)))
            lower.append(bm.verts.new((cx+math.cos(a)*r,yy-.018,cz+math.sin(a)*r)))
        base=bm.verts.new((cx,y-.020,cz))
        for i in range(20):
            j=(i+1)%20
            bm.faces.new((centre,upper[j],upper[i]))
            bm.faces.new((upper[i],upper[j],lower[j],lower[i]))
            bm.faces.new((base,lower[i],lower[j]))
    for f in bm.faces:
        for loop in f.loops:
            co = loop.vert.co
            loop[uv_layer].uv = (0.12, max(0.0, min(1.0, co.y / 0.78)))
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
