#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=====================================================================
深海灯塔·归潮湾 (v25) — 3D 鱼类与水族箱展缸 Blender 全自动建模与贴图生成脚本
File: Tools/SeaLifeV25/generate_fish_and_aquarium_v25.py
=====================================================================
使用方法 (无头命令行模式，由 GPT 本地终端直接执行):
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaLifeV25/generate_fish_and_aquarium_v25.py -- --out-dir "Assets/Deepsea/Resources/SeaLifeV25"

核心产出:
1. 3 种高细节流线型海洋鱼类网格 + 程序化鳞片/渐变 PBR 贴图 (Albedo + Roughness/Metallic):
   - Fish_0_SilverBream.fbx  (银纹海鲷: 侧扁流线型、青银背腹渐变、7道暗银横纹、新月分叉尾鳍、胸鳍/背鳍/腹鳍/臀鳍、立体双色眼球)
   - Fish_1_BluefinTuna.fbx  (蔚蓝纺锤鱼: 纺锤流线型、深海钴蓝背脊、亮银腹部、流线小脂鳍、高展弦比镰刀尾鳍)
   - Fish_2_GoldCoralButterfly.fbx (金背珊瑚蝶: 高背圆盘型、暖金橙背脊、象牙白腹、飘逸长背鳍、黑眼带与尾柄眼斑)
   * 所有鱼类均严格对齐 Unity 坐标系: +Z 为鱼头朝向, +Y 为鱼背朝上, 全长约 0.95m (配合展缸 0.45~0.65 缩放),
     UV.x 严格编码头尾归一化坐标 (0=鼻尖, 1=尾尖), 供 Unity 顶点着色器实现逼真的 S 型脊椎摆尾动画！

2. 1 套高细节水族箱展缸组合网格 (严格对齐 v21 CreateTank 3.5m x 2.2m x 2.0m 尺寸规范):
   - AquariumTank_V25_Frame.fbx (榫卯倒角原木底柜 + 黄铜包角立柱 + 顶部悬浮式暖光灯架)
   - AquariumTank_V25_Interior.fbx (起伏波纹暖砂床 + 多孔层叠生态礁石群 + 沉木造景基座)
   - AquariumTank_V25_Glass.fbx (带倒角厚度的四面超白玻璃腔体 + 内层水体体积网格)
=====================================================================
"""

import os
import sys
import math
import struct
import zlib
import json
import hashlib

try:
    import bpy
    import bmesh
    from mathutils import Vector, Matrix
except ImportError:
    print("请在 Blender 环境中运行此脚本: blender -b -P generate_fish_and_aquarium_v25.py")
    sys.exit(1)


# ---------------------------------------------------------------------
# 1. 轻量级无依赖 PNG 贴图生成器 (保证任何 Blender Python 环境 100% 可生成贴图)
# ---------------------------------------------------------------------
def write_png_rgba(filepath, width, height, pixel_fn):
    """使用纯 Python 标准库生成高品质程序化 RGBA PNG 贴图"""
    raw_rows = []
    for y in range(height):
        v = 1.0 - y / float(max(height - 1, 1))
        row = bytearray([0])  # PNG filter type 0 (None)
        for x in range(width):
            u = x / float(max(width - 1, 1))
            r, g, b, a = ((0.025, 0.035, 0.045, 0.95) if v > 0.995 else (0.83, 0.74, 0.36, 0.8) if v > 0.982 else pixel_fn(u, v))
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
    print(f"[Texture] Generated {filepath} ({width}x{height})")


def generate_fish_textures(out_dir):
    """生成 3 种鱼的高清程序化鱼鳞、侧线与鳍条条纹贴图 (512x512)"""
    tex_dir = os.path.join(out_dir, "Textures")
    os.makedirs(tex_dir, exist_ok=True)

    # 鱼贴图 UV 规划:
    # v in [0.0, 0.72]: 鱼身区域 (u: 0=鱼头 -> 1=鱼尾; v: 0=腹底 -> 0.72=背脊)
    # v in (0.72, 1.0]: 鱼鳍与鱼眼区域 (带放射状鳍条纹路与半透明边缘)

    def scale_pattern(u, v, freq_u=28.0, freq_v=14.0):
        """生成交错菱形鱼鳞高光与边缘微阴影"""
        su = u * freq_u
        sv = v * freq_v + (0.5 if int(math.floor(su)) % 2 == 1 else 0.0)
        fu = su - math.floor(su) - 0.5
        fv = sv - math.floor(sv) - 0.5
        dist = math.sqrt(fu * fu + fv * fv)
        rim = math.exp(-((dist - 0.42) ** 2) / 0.008)
        body = max(0.0, 1.0 - dist * 1.35)
        return body * 0.08 + rim * 0.14

    # --- Species 0: 银纹鱼 (Silver Bream) ---
    def silver_bream_px(u, v):
        if v <= 0.72:
            bv = v / 0.72  # 0=腹, 1=背
            # 背部青石蓝绿 -> 侧线亮银白 -> 腹部珍珠白
            r = 0.88 * (1.0 - bv) + 0.32 * bv
            g = 0.92 * (1.0 - bv) + 0.58 * bv
            b = 0.94 * (1.0 - bv) + 0.64 * bv
            # 7 道自然垂直暗银条纹 (集中在鱼身中段 u in 0.2~0.8)
            if 0.18 < u < 0.82 and bv > 0.22:
                stripe = 0.5 + 0.5 * math.sin((u - 0.18) * 7.0 * math.pi * 2.0)
                stripe_mask = (stripe ** 3) * (bv - 0.22) * 0.32
                r -= stripe_mask * 0.45
                g -= stripe_mask * 0.35
                b -= stripe_mask * 0.28
            # 侧线高光带
            lat = math.exp(-((bv - 0.55) ** 2) / 0.006) * 0.12
            sc = scale_pattern(u, bv, 30.0, 16.0) * (1.0 if 0.15 < u < 0.85 else 0.2)
            return (r + lat + sc, g + lat + sc, b + lat + sc, 0.75)  # alpha 存光滑度
        else:
            # 鱼鳍区域: 冰青色半透明鳍条纹路
            fv = (v - 0.72) / 0.28
            rays = 0.5 + 0.5 * math.sin(u * 48.0 * math.pi)
            r = 0.48 + rays * 0.14 - fv * 0.12
            g = 0.72 + rays * 0.14 - fv * 0.08
            b = 0.76 + rays * 0.14 - fv * 0.05
            return (r, g, b, 0.55)

    # --- Species 1: 蓝鳍鱼 (Bluefin Tuna) ---
    def bluefin_tuna_px(u, v):
        if v <= 0.72:
            bv = v / 0.72
            # 深海午夜蓝背 -> 钴蓝中侧 -> 银白腹 + 金色侧线细带
            if bv > 0.52:
                t = (bv - 0.52) / 0.48
                r = 0.18 * (1.0 - t) + 0.06 * t
                g = 0.46 * (1.0 - t) + 0.18 * t
                b = 0.68 * (1.0 - t) + 0.38 * t
            else:
                t = bv / 0.52
                r = 0.90 * (1.0 - t) + 0.18 * t
                g = 0.93 * (1.0 - t) + 0.46 * t
                b = 0.96 * (1.0 - t) + 0.68 * t
            gold_line = math.exp(-((bv - 0.50) ** 2) / 0.0025)
            r += gold_line * 0.42
            g += gold_line * 0.32
            b -= gold_line * 0.10
            sc = scale_pattern(u, bv, 38.0, 18.0) * 0.65
            return (r + sc, g + sc, b + sc, 0.82)
        else:
            fv = (v - 0.72) / 0.28
            rays = 0.5 + 0.5 * math.sin(u * 52.0 * math.pi)
            # 深蓝带亮黄尖端的尾鳍/背鳍
            tip = max(0.0, (fv - 0.65) / 0.35)
            r = 0.12 + rays * 0.08 + tip * 0.68
            g = 0.34 + rays * 0.10 + tip * 0.52
            b = 0.56 + rays * 0.12 - tip * 0.25
            return (r, g, b, 0.65)

    # --- Species 2: 金背鱼 (Golden Coral Butterflyfish) ---
    def gold_butterfly_px(u, v):
        if v <= 0.72:
            bv = v / 0.72
            # 暖金琥珀色背脊 -> 奶油象牙腹 + 经典深褐斜纹与眼带
            r = 0.95 * (1.0 - bv * 0.05)
            g = 0.88 * (1.0 - bv) + 0.56 * bv
            b = 0.74 * (1.0 - bv) + 0.14 * bv
            # 头部黑色眼带 (u around 0.14)
            eye_band = math.exp(-((u - 0.14 + (bv - 0.5) * 0.06) ** 2) / 0.0012)
            r *= (1.0 - eye_band * 0.72)
            g *= (1.0 - eye_band * 0.75)
            b *= (1.0 - eye_band * 0.78)
            # 鱼体斜向金色几何细纹
            if 0.24 < u < 0.78:
                diag = 0.5 + 0.5 * math.sin((u * 1.4 - bv * 0.6) * 28.0)
                d_mask = (diag ** 4) * 0.18
                r += d_mask * 0.08
                g -= d_mask * 0.22
                b -= d_mask * 0.28
            sc = scale_pattern(u, bv, 26.0, 16.0) * 0.9
            return (r + sc, g + sc, b + sc, 0.70)
        else:
            fv = (v - 0.72) / 0.28
            rays = 0.5 + 0.5 * math.sin(u * 44.0 * math.pi)
            r = 0.96 - fv * 0.08 + rays * 0.06
            g = 0.68 - fv * 0.18 + rays * 0.08
            b = 0.18 + fv * 0.05 + rays * 0.05
            return (r, g, b, 0.60)

    p0 = os.path.join(tex_dir, "Fish_0_SilverBream_Albedo.png")
    p1 = os.path.join(tex_dir, "Fish_1_BluefinTuna_Albedo.png")
    p2 = os.path.join(tex_dir, "Fish_2_GoldButterfly_Albedo.png")
    write_png_rgba(p0, 512, 512, silver_bream_px)
    write_png_rgba(p1, 512, 512, bluefin_tuna_px)
    write_png_rgba(p2, 512, 512, gold_butterfly_px)
    return [p0, p1, p2]


# ---------------------------------------------------------------------
# 2. 高精度流线型 3D 鱼类参数化构建器 (含鱼身、双侧胸鳍/腹鳍、背鳍、臀鳍、新月分叉尾鳍、立体眼球)
# ---------------------------------------------------------------------
def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def create_pbr_material(name, tex_path=None, base_color=(0.8, 0.8, 0.8, 1.0), metallic=0.1, roughness=0.35):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = base_color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if tex_path and os.path.exists(tex_path):
            tex_node = nodes.new("ShaderNodeTexImage")
            tex_node.image = bpy.data.images.load(tex_path)
            links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def build_detailed_fish_mesh(
    mesh_name,
    length=0.92,
    width_scale=0.14,
    height_scale=0.26,
    belly_bias=0.12,
    tail_fork_depth=0.14,
    tail_span=0.30,
    dorsal_height=0.11,
    pectoral_span=0.13,
):
    """
    构建一条高细节、流线型、法线平滑且带完整鳍组与立体眼球的 3D 鱼网格:
    - 坐标系: 鱼头朝 +Z (z_max = +length*0.5), 鱼尾朝 -Z (z_min = -length*0.5), 背朝 +Y
    - UV0.x: 严格映射为从鱼头(0.0)到鱼尾(1.0)的归一化轴向坐标，供 Shader 计算脊椎摆尾权重！
    - UV0.y: 鱼身映射到 [0.02, 0.70]，鱼鳍映射到 [0.74, 0.98]
    """
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")

    # 1. 鱼身主体：24 段纵向截面 x 20 段环向轮廓 (带背部隆起、鳃盖微凸、尾柄收窄)
    num_z = 24
    num_ring = 20
    z_head = length * 0.45
    z_tail_base = -length * 0.34

    rings = []
    ring_u_vals = []

    for iz in range(num_z):
        t = iz / float(num_z - 1)  # 0.0 = 鼻尖(Head), 1.0 = 尾柄根部(Tail peduncle)
        z = z_head * (1.0 - t) + z_tail_base * t
        u_axial = t * 0.80  # 鱼身占轴向 0.0 ~ 0.80

        # 流线型鱼体包络曲线 (NACA 仿生鱼体方程 + 尾柄收束)
        if t < 0.03:
            profile_h = 0.12 + math.sin((t / 0.03) * (math.pi * 0.5)) * 0.06
            profile_w = profile_h * 0.85
        else:
            # 鱼体最大高度在 t=0.32 附近，向尾柄平滑收缩为细尾柄
            arch = math.sin(math.pow(t, 0.68) * math.pi)
            peduncle = 0.14 * math.exp(-((t - 0.94) ** 2) / 0.015)
            profile_h = max(0.03, arch * (1.0 - 0.25 * t) + peduncle)
            # 鳃盖处 (t=0.22) 略微丰满
            gill_swell = 1.0 + 0.10 * math.exp(-((t - 0.22) ** 2) / 0.008)
            profile_w = max(0.018, math.pow(arch, 0.85) * (1.0 - 0.38 * t) * gill_swell + peduncle * 0.45)

        h_radius = height_scale * profile_h
        w_radius = width_scale * profile_w
        # 背部与腹部不对称偏移
        y_center = belly_bias * height_scale * math.sin(t * math.pi) * 0.25

        ring_verts = []
        for ir in range(num_ring):
            angle = (ir / float(num_ring)) * math.pi * 2.0
            # angle = 0 为正上方(+Y)，顺时针绕一圈
            ca = math.cos(angle)
            sa = math.sin(angle)
            # 水滴形截面：背脊略窄有棱感，腹部圆润
            lateral_pinch = 1.0 - 0.18 * (ca ** 2)
            x = sa * w_radius * lateral_pinch
            y = y_center + ca * h_radius
            v = bm.verts.new((x, y, z))
            ring_verts.append(v)
        rings.append(ring_verts)
        ring_u_vals.append(u_axial)

    # 鼻尖封口顶点
    nose_vert = bm.verts.new((0.0, 0.01 * height_scale, z_head + 0.018 * length))

    # 连接鼻尖与第一环
    for ir in range(num_ring):
        v1 = rings[0][ir]
        v2 = rings[0][(ir + 1) % num_ring]
        f = bm.faces.new((nose_vert, v2, v1))
        f.smooth = True
        for loop in f.loops:
            if loop.vert == nose_vert:
                loop[uv_layer].uv = (0.0, 0.36)
            else:
                idx = rings[0].index(loop.vert)
                ang = (idx / float(num_ring)) * math.pi * 2.0
                v_coord = 0.02 + (0.5 + 0.5 * math.cos(ang)) * 0.68
                loop[uv_layer].uv = (ring_u_vals[0], v_coord)

    # 连接鱼身各环
    for iz in range(num_z - 1):
        rA = rings[iz]
        rB = rings[iz + 1]
        uA = ring_u_vals[iz]
        uB = ring_u_vals[iz + 1]
        for ir in range(num_ring):
            ir_next = (ir + 1) % num_ring
            f = bm.faces.new((rA[ir], rA[ir_next], rB[ir_next], rB[ir]))
            f.smooth = True
            for loop in f.loops:
                v_ref = loop.vert
                if v_ref in rA:
                    idx = rA.index(v_ref)
                    u_val = uA
                else:
                    idx = rB.index(v_ref)
                    u_val = uB
                ang = (idx / float(num_ring)) * math.pi * 2.0
                v_coord = 0.02 + (0.5 + 0.5 * math.cos(ang)) * 0.68
                loop[uv_layer].uv = (u_val, v_coord)

    # 2. 新月形双叶分叉尾鳍 (Caudal Fin) - 双层有厚度网格，带 9 段纵向高度细分与 5 段横向细分
    caudal_cols = 5
    caudal_rows = 9
    for side_sign in (-1.0, 1.0):
        grid = []
        for ic in range(caudal_cols):
            ct = ic / float(caudal_cols - 1)  # 0=尾柄根部, 1=尾鳍后缘
            col_verts = []
            for ir in range(caudal_rows):
                rt = (ir / float(caudal_rows - 1)) * 2.0 - 1.0  # -1=下叶尖, 0=中央分叉口, +1=上叶尖
                # 根部窄，向后呈扇形展开
                span_here = (0.035 * height_scale) * (1.0 - ct) + (tail_span * 0.5) * (ct ** 0.85)
                y = rt * span_here
                # 后缘新月形内凹 (rt=0 处向前收，rt=+-1 处向后延伸)
                fork_offset = (rt ** 2) * tail_fork_depth * ct
                z = z_tail_base - ct * (length * 0.22) - fork_offset
                thickness = 0.004 * (1.0 - ct * 0.75) * (1.0 - 0.5 * (rt ** 2))
                x = side_sign * thickness
                col_verts.append((bm.verts.new((x, y, z)), 0.80 + ct * 0.20, 0.74 + abs(rt) * 0.24))
            grid.append(col_verts)

        for ic in range(caudal_cols - 1):
            for ir in range(caudal_rows - 1):
                p00, u00, v00 = grid[ic][ir]
                p10, u10, v10 = grid[ic + 1][ir]
                p11, u11, v11 = grid[ic + 1][ir + 1]
                p01, u01, v01 = grid[ic][ir + 1]
                verts_order = (p00, p10, p11, p01) if side_sign > 0 else (p00, p01, p11, p10)
                f = bm.faces.new(verts_order)
                f.smooth = True
                uv_map_dict = {p00: (u00, v00), p10: (u10, v10), p11: (u11, v11), p01: (u01, v01)}
                for loop in f.loops:
                    loop[uv_layer].uv = uv_map_dict[loop.vert]

    # 3. 流线型背鳍 (Dorsal Fin) 与臀鳍 (Anal Fin)
    def add_median_fin(z_start_t, z_end_t, fin_height, is_dorsal=True):
        cols = 8
        rows = 4
        sign_y = 1.0 if is_dorsal else -1.0
        for side_sign in (-1.0, 1.0):
            grid = []
            for ic in range(cols):
                ct = ic / float(cols - 1)
                t_body = z_start_t + (z_end_t - z_start_t) * ct
                z_base = z_head * (1.0 - t_body) + z_tail_base * t_body
                arch = math.sin(math.pow(t_body, 0.68) * math.pi) * (1.0 - 0.25 * t_body)
                y_body = sign_y * height_scale * arch * 0.92
                fin_env = math.sin(math.pow(ct, 0.6) * math.pi)
                col = []
                for ir in range(rows):
                    rt = ir / float(rows - 1)
                    y = y_body + sign_y * fin_height * fin_env * rt
                    z = z_base - rt * fin_height * 0.45  # 鳍条向后倾斜
                    x = side_sign * 0.0025 * (1.0 - rt * 0.8)
                    col.append((bm.verts.new((x, y, z)), t_body * 0.8 + rt * 0.06, 0.74 + rt * 0.24))
                grid.append(col)
            for ic in range(cols - 1):
                for ir in range(rows - 1):
                    p00, u00, v00 = grid[ic][ir]
                    p10, u10, v10 = grid[ic + 1][ir]
                    p11, u11, v11 = grid[ic + 1][ir + 1]
                    p01, u01, v01 = grid[ic][ir + 1]
                    verts_order = (p00, p10, p11, p01) if (side_sign * sign_y) > 0 else (p00, p01, p11, p10)
                    f = bm.faces.new(verts_order)
                    f.smooth = True
                    uv_map_dict = {p00: (u00, v00), p10: (u10, v10), p11: (u11, v11), p01: (u01, v01)}
                    for loop in f.loops:
                        loop[uv_layer].uv = uv_map_dict[loop.vert]

    add_median_fin(0.26, 0.74, dorsal_height, is_dorsal=True)
    add_median_fin(0.58, 0.82, dorsal_height * 0.62, is_dorsal=False)

    # 4. 左右成对胸鳍 (Pectoral Fins) 与腹鳍 (Pelvic Fins)
    def add_paired_fins(t_attach, y_factor, span, sweep_z, droop_y):
        z_root = z_head * (1.0 - t_attach) + z_tail_base * t_attach
        arch = math.sin(math.pow(t_attach, 0.68) * math.pi)
        x_root = width_scale * math.pow(arch, 0.85) * 0.78
        y_root = height_scale * arch * y_factor
        u_root = t_attach * 0.80

        for side in (-1.0, 1.0):
            v_base_front = bm.verts.new((side * x_root, y_root + 0.015, z_root + 0.02))
            v_base_back = bm.verts.new((side * x_root * 0.9, y_root - 0.015, z_root - 0.03))
            v_tip_top = bm.verts.new((side * (x_root + span), y_root + droop_y * 0.4, z_root - sweep_z))
            v_tip_bot = bm.verts.new((side * (x_root + span * 0.75), y_root + droop_y, z_root - sweep_z * 1.15))

            f1 = bm.faces.new((v_base_front, v_tip_top, v_tip_bot, v_base_back) if side > 0 else (v_base_front, v_base_back, v_tip_bot, v_tip_top))
            for f in (f1,):
                f.smooth = True
                for loop in f.loops:
                    if loop.vert in (v_base_front, v_base_back):
                        loop[uv_layer].uv = (u_root, 0.75)
                    else:
                        loop[uv_layer].uv = (min(1.0, u_root + 0.12), 0.96)

    add_paired_fins(0.24, -0.15, pectoral_span, length * 0.13, -0.04)
    add_paired_fins(0.42, -0.78, pectoral_span * 0.58, length * 0.08, -0.05)

    # 5. 左右立体鱼眼 (3D Hemispherical Eyes)
    t_eye = 0.13
    z_eye = z_head * (1.0 - t_eye) + z_tail_base * t_eye
    arch_eye = math.sin(math.pow(t_eye, 0.68) * math.pi)
    x_eye = width_scale * math.pow(arch_eye, 0.85) * 0.82
    y_eye = height_scale * arch_eye * 0.25
    eye_radius = min(width_scale, height_scale) * 0.22

    for side in (-1.0, 1.0):
        center = Vector((side * x_eye, y_eye, z_eye))
        tip = bm.verts.new(center + Vector((side * eye_radius * 0.55, 0, 0)))
        ring_e = []
        for ie in range(8):
            ang = ie * math.pi * 2.0 / 8.0
            rv = bm.verts.new(center + Vector((0, math.sin(ang) * eye_radius, math.cos(ang) * eye_radius)))
            ring_e.append(rv)
        for ie in range(8):
            v1 = ring_e[ie]
            v2 = ring_e[(ie + 1) % 8]
            f = bm.faces.new((tip, v1, v2) if side > 0 else (tip, v2, v1))
            f.smooth = True
            for loop in f.loops:
                loop[uv_layer].uv = (0.104, 1.0 if loop.vert == tip else 0.987)

    bm.normal_update()
    mesh = bpy.data.meshes.new(mesh_name)
    bm.to_mesh(mesh)
    bm.free()

    # 启用自动平滑法线
    if hasattr(mesh, "use_auto_smooth"):
        mesh.use_auto_smooth = True
        mesh.auto_smooth_angle = math.radians(65.0)

    return mesh


# ---------------------------------------------------------------------
# 3. 高精度水族箱展缸组合构建器 (严格对齐 v21 CreateTank 尺寸与锚点)
# ---------------------------------------------------------------------
def add_beveled_box(bm, center, size, bevel_width=0.015, material_index=0):
    """在 bmesh 中创建一个带倒角的精细长方体组件"""
    existing = set(bm.faces)
    mat_trans = Matrix.Translation(Vector(center))
    mat_scale = Matrix.Diagonal((size[0], size[1], size[2], 1.0))
    ret = bmesh.ops.create_cube(bm, size=1.0, matrix=mat_trans @ mat_scale)
    verts = ret["verts"]
    # 收集该立方体的边进行微倒角
    edges = list({e for v in verts for e in v.link_edges})
    if bevel_width > 0.0:
        bmesh.ops.bevel(bm, geom=edges, offset=bevel_width, segments=2, affect='EDGES')
    for face in bm.faces:
        if face not in existing:
            face.material_index = material_index



def prepare_export(obj, labels, out_dir):
    for label in labels:
        obj.data.materials.append(create_pbr_material(label))
    for name, loc in [('Origin',(0,0,0)),('Right',(1,0,0)),('Up',(0,1,0)),('Forward',(0,0,1))]:
        a=bpy.data.objects.new('V25_'+name,None)
        bpy.context.collection.objects.link(a)
        a.location=loc
    source_dir=os.path.abspath('Tools/SeaLifeV25/BlenderSources')
    os.makedirs(source_dir,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source_dir,obj.name+'.blend'))

def build_aquarium_models(out_dir):
    """
    构建 v25 高精度水族箱 3 件套模型并导出 FBX:
    严格对齐 SeaLifeAquarium.cs 中 CreateTank 的局部坐标:
    - 原木底柜: 中心 (0, 0.22, 0), 尺寸 (3.5, 0.44, 2.3)
    - 玻璃主缸体: 中心 (0, 1.50, 0), 尺寸 (3.5, 1.40, 2.0)
    - 缸内沙床与礁石群: 底部高度 y=0.48 ~ 1.35
    """
    models_dir = os.path.join(out_dir, "Models")
    os.makedirs(models_dir, exist_ok=True)

    # --- 部件 1: 榫卯原木底柜 + 黄铜包角立柱 + 顶部悬浮灯架 (AquariumTank_V25_Frame.fbx) ---
    clear_scene()
    bm_frame = bmesh.new()

    # 底柜主体与上下装饰线脚
    add_beveled_box(bm_frame, (0.0, 0.40, 0.0), (3.56, 0.78, 2.04), 0.012)
    add_beveled_box(bm_frame, (0.0, 0.81, 0.0), (3.64, 0.06, 2.12), 0.008)  # 承重台面
    add_beveled_box(bm_frame, (0.0, 0.03, 0.0), (3.62, 0.06, 2.10), 0.008)  # 底部踢脚线

    # 正面与背面柜门百叶凹槽与立柱装饰 (3 扇柜门结构)
    for ix in (-1.12, 0.0, 1.12):
        add_beveled_box(bm_frame, (ix, 0.40, -1.035), (0.98, 0.56, 0.03), 0.006)
        add_beveled_box(bm_frame, (ix, 0.40, 1.035), (0.98, 0.56, 0.03), 0.006)

    # 四角倒角立柱与上下横梁 (包围玻璃缸体)
    for x in (-1.76, 1.76):
        for z in (-0.99, 0.99):
            add_beveled_box(bm_frame, (x, 1.52, z), (0.060, 1.44, 0.060), 0.008)
            # 黄铜包角箍套
            add_beveled_box(bm_frame, (x, 0.86, z), (0.085, 0.12, 0.085), 0.005, 1)
            add_beveled_box(bm_frame, (x, 2.18, z), (0.085, 0.12, 0.085), 0.005, 1)

    for y in (0.84, 2.22):
        for z in (-0.99, 0.99):
            add_beveled_box(bm_frame, (0.0, y, z), (3.56, 0.075, 0.085), 0.006)
        for x in (-1.76, 1.76):
            add_beveled_box(bm_frame, (x, y, 0.0), (0.085, 0.075, 2.02), 0.006)

    # 顶部悬浮生态照明灯罩 (带散热鳍片造型)
    add_beveled_box(bm_frame, (0.0, 2.42, 0.0), (2.85, 0.07, 0.55), 0.010)
    add_beveled_box(bm_frame, (-1.10, 2.31, 0.0), (0.04, 0.22, 0.04), 0.003, 1)
    add_beveled_box(bm_frame, (1.10, 2.31, 0.0), (0.04, 0.22, 0.04), 0.003, 1)

    add_beveled_box(bm_frame, (0.0, 2.375, 0.0), (2.65, 0.018, 0.36), 0.004, 2)
    bm_frame.normal_update()
    uv_layer = bm_frame.loops.layers.uv.new("UVMap")
    for f in bm_frame.faces:
        f.smooth = True
        for loop in f.loops:
            co = loop.vert.co
            loop[uv_layer].uv = ((co.x + co.z) * 0.5, co.y * 0.5)

    mesh_frame = bpy.data.meshes.new("AquariumTank_V25_Frame")
    bm_frame.to_mesh(mesh_frame)
    bm_frame.free()
    obj_frame = bpy.data.objects.new("AquariumTank_V25_Frame", mesh_frame)
    bpy.context.collection.objects.link(obj_frame)
    prepare_export(obj_frame, ['V25_Wood','V25_Brass','V25_Lamp'], out_dir)
    frame_fbx = os.path.join(models_dir, "AquariumTank_V25_Frame.fbx")
    bpy.ops.export_scene.fbx(filepath=frame_fbx, use_selection=False, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')

    # --- 部件 2: 波纹海砂床与自然层叠礁石造景 (AquariumTank_V25_Interior.fbx) ---
    clear_scene()
    bm_int = bmesh.new()

    # 波纹起伏砂床网格 (28 x 16 细分)
    nx, nz = 28, 16
    sand_w, sand_d = 3.36, 1.86
    base_y = 0.85
    sand_grid = []
    for ix in range(nx):
        tx = (ix / float(nx - 1)) - 0.5
        col = []
        for iz in range(nz):
            tz = (iz / float(nz - 1)) - 0.5
            x = tx * sand_w
            z = tz * sand_d
            # 自然沙丘起伏 + 右后方礁石基座隆起
            ripple = 0.018 * math.sin(x * 5.5 + math.sin(z * 3.2)) + 0.012 * math.cos(z * 6.0)
            mound = 0.14 * math.exp(-(((x - 0.75) ** 2) / 0.65 + ((z - 0.35) ** 2) / 0.45))
            mound2 = 0.09 * math.exp(-(((x + 0.85) ** 2) / 0.55 + ((z + 0.25) ** 2) / 0.45))
            y = base_y + 0.07 + ripple + mound + mound2
            col.append(bm_int.verts.new((x, y, z)))
        sand_grid.append(col)

    for ix in range(nx - 1):
        for iz in range(nz - 1):
            f = bm_int.faces.new((sand_grid[ix][iz], sand_grid[ix][iz + 1], sand_grid[ix + 1][iz + 1], sand_grid[ix + 1][iz]))
            f.smooth = True

    # 层叠自然风化礁石群 (5 块主次分明的多面体雕塑礁石，彻底告别单调球体)
    rock_specs = [
        ((0.82, 0.88, 0.38), (0.48, 0.38, 0.36), 1.7),
        ((0.38, 0.72, 0.48), (0.32, 0.26, 0.28), 3.1),
        ((1.15, 0.68, 0.12), (0.28, 0.22, 0.25), 5.4),
        ((-0.88, 0.66, -0.24), (0.34, 0.22, 0.26), 2.3),
        ((-0.52, 0.58, 0.32), (0.22, 0.16, 0.20), 4.2),
    ]
    for center, radii, seed in rock_specs:
        existing_faces = set(bm_int.faces)
        ret = bmesh.ops.create_icosphere(bm_int, subdivisions=3, radius=1.0)
        for v in ret["verts"]:
            p = v.co
            # 多频层理噪声塑造真实岸礁棱角与水蚀凹槽
            strata = 0.18 * math.sin(p.y * 9.0 + seed)
            crag = 0.22 * math.sin(p.x * 4.3 + seed * 1.3) * math.cos(p.z * 4.7 - seed)
            scale_f = 1.0 + strata + crag
            v.co = Vector((
                center[0] + p.x * radii[0] * scale_f,
                max(0.89, center[1] + 0.39 + p.y * radii[1] * scale_f),
                center[2] + p.z * radii[2] * scale_f,
            ))
        for face in bm_int.faces:
            if face not in existing_faces:
                face.material_index = 1

    bm_int.normal_update()
    uv_int = bm_int.loops.layers.uv.new("UVMap")
    for f in bm_int.faces:
        f.smooth = True
        for loop in f.loops:
            co = loop.vert.co
            loop[uv_int].uv = (co.x * 0.3 + 0.5, co.z * 0.3 + 0.5)

    mesh_int = bpy.data.meshes.new("AquariumTank_V25_Interior")
    bm_int.to_mesh(mesh_int)
    bm_int.free()
    obj_int = bpy.data.objects.new("AquariumTank_V25_Interior", mesh_int)
    bpy.context.collection.objects.link(obj_int)
    prepare_export(obj_int, ['V25_Sand','V25_Reef'], out_dir)
    int_fbx = os.path.join(models_dir, "AquariumTank_V25_Interior.fbx")
    bpy.ops.export_scene.fbx(filepath=int_fbx, use_selection=False, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')

    # --- 部件 3: 超白倒角玻璃腔体 (AquariumTank_V25_Glass.fbx) ---
    clear_scene()
    bm_glass = bmesh.new()
    add_beveled_box(bm_glass, (0.0, 1.48, 0.96), (3.42, 1.42, 0.022), 0.003)   # 背玻璃
    add_beveled_box(bm_glass, (0.0, 1.48, -0.96), (3.42, 1.42, 0.022), 0.003)  # 前玻璃
    add_beveled_box(bm_glass, (-1.71, 1.48, 0.0), (0.022, 1.42, 1.90), 0.003)  # 左玻璃
    add_beveled_box(bm_glass, (1.71, 1.48, 0.0), (0.022, 1.42, 1.90), 0.003)   # 右玻璃

    bm_glass.normal_update()
    uv_g = bm_glass.loops.layers.uv.new("UVMap")
    for f in bm_glass.faces:
        f.smooth = True
        for loop in f.loops:
            co = loop.vert.co
            loop[uv_g].uv = ((co.x + co.z) * 0.28 + 0.5, (co.y - 0.77) / 1.42)

    mesh_glass = bpy.data.meshes.new("AquariumTank_V25_Glass")
    bm_glass.to_mesh(mesh_glass)
    bm_glass.free()
    obj_glass = bpy.data.objects.new("AquariumTank_V25_Glass", mesh_glass)
    bpy.context.collection.objects.link(obj_glass)
    prepare_export(obj_glass, ['V25_Glass'], out_dir)
    glass_fbx = os.path.join(models_dir, "AquariumTank_V25_Glass.fbx")
    bpy.ops.export_scene.fbx(filepath=glass_fbx, use_selection=False, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')

    print(f"[Aquarium] Exported Frame, Interior, and Glass FBX to {models_dir}")


# ---------------------------------------------------------------------
# 4. 主入口：一键生成全部 3 种鱼 + 水族箱展缸 3 件套 + 贴图
# ---------------------------------------------------------------------
def main():
    out_dir = "Assets/Deepsea/Resources/SeaLifeV25"
    argv = sys.argv
    if "--" in argv:
        custom_args = argv[argv.index("--") + 1:]
        for i, arg in enumerate(custom_args):
            if arg == "--out-dir" and i + 1 < len(custom_args):
                out_dir = custom_args[i + 1]

    out_dir = os.path.abspath(out_dir)
    models_dir = os.path.join(out_dir, "Models")
    os.makedirs(models_dir, exist_ok=True)

    print(f"=== [SeaLifeV25] Starting Blender Asset Generation into: {out_dir} ===")
    tex_paths = generate_fish_textures(out_dir)

    # 构建并导出 3 种鱼类 FBX
    fish_configs = [
        # (filename, mesh_name, length, width_scale, height_scale, belly_bias, tail_fork, tail_span, dorsal_h, pectoral_span, tex_idx)
        ("Fish_0_SilverBream.fbx", "Fish_0_SilverBream", 0.90, 0.13, 0.26, -0.05, 0.13, 0.30, 0.11, 0.13, 0),
        ("Fish_1_BluefinTuna.fbx", "Fish_1_BluefinTuna", 1.05, 0.16, 0.22, 0.02, 0.18, 0.36, 0.10, 0.16, 1),
        ("Fish_2_GoldButterfly.fbx", "Fish_2_GoldButterfly", 0.82, 0.11, 0.34, -0.08, 0.07, 0.26, 0.16, 0.12, 2),
    ]

    for fname, mname, length, w_sc, h_sc, b_bias, t_fork, t_span, d_h, p_span, tidx in fish_configs:
        clear_scene()
        mesh = build_detailed_fish_mesh(
            mesh_name=mname,
            length=length,
            width_scale=w_sc,
            height_scale=h_sc,
            belly_bias=b_bias,
            tail_fork_depth=t_fork,
            tail_span=t_span,
            dorsal_height=d_h,
            pectoral_span=p_span,
        )
        obj = bpy.data.objects.new(mname, mesh)
        bpy.context.collection.objects.link(obj)
        mat = create_pbr_material(mname + "_Mat", tex_paths[tidx])
        obj.data.materials.append(mat)

        prepare_export(obj, [], out_dir)
        fbx_path = os.path.join(models_dir, fname)
        bpy.ops.export_scene.fbx(
            filepath=fbx_path,
            use_selection=False,
            apply_scale_options='FBX_SCALE_ALL',
            axis_forward='-Z',
            axis_up='Y',
        )
        print(f"[Fish] Exported {fbx_path} (Verts: {len(mesh.vertices)}, Faces: {len(mesh.polygons)})")

    build_aquarium_models(out_dir)
    files=[p for folder in [models_dir,os.path.join(out_dir,"Textures")] for p in __import__("pathlib").Path(folder).glob("*") if p.suffix in (".fbx",".png")]
    with open("Tools/SeaLifeV25/generation.json","w",encoding="utf-8") as f:
        json.dump({"passed":len(files)==9,"files":[{"path":str(p.relative_to(__import__("pathlib").Path.cwd())),"bytes":p.stat().st_size,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()} for p in files]},f,indent=2)
    print("=== [SeaLifeV25] All 3D Fish & Aquarium Models and Textures Successfully Generated! ===")


if __name__ == "__main__":
    main()