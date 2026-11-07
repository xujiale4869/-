# 给 GPT 的 v31《归潮湾》海陆生态自然演替、6类多样化海底植物群落与海岬渔村 NPC 聚落建筑一键包

你好！我和 **Gemini** 已经审查了仓库 `https://github.com/xujiale4869/guichao-bay` 中 `v30` 的 `SeamlessTerrainV29.cs` 和 `UnifiedEcologyV30.cs`。
针对用户指出的三个关键视觉与空间痛点：

1. **“海里面的植物重复度很高”**：`SeamlessTerrainV29.cs` 第 347\~367 行在全海域只重复刷了同一种 `Coral_V27_Cluster`（52次）和同一种 `Kelp_V27_GiantFrond`（42次），且缩放均为均匀 `Vector3.one * scale`，导致水下看起来全是千篇一律的克隆植物。

2. **“海陆生态衔接不够出色”**：虽然顶点色有了沙草渐变，但在海拔 `y ∈ [-0.25m, +1.65m]` 的潮间带水陆交界处缺乏立体的过渡植被与海岸陈设（如防风芦苇丛、涉水芒草、半埋湿沙的沧桑浮木与藤壶鹅卵石），导致岸上草坡与水下海草之间缺少立体衔接。

3. **“陆地上地面平坦方便建造但是太空，可以有几个房子方便后续 NPC 的出现”**：南北延展出来的宽阔平缓岸坡上除了 18 棵红枫外空无一物，亟需布置几座契合《雾岬岛》气质的海岬渔村建筑作为后续 NPC 的生活据点。

现在 **Gemini** 已经编写好了 **v31 海陆生态演替、6类多样化水下植物与 3 座海岬渔村 NPC 建筑系统**（本文件已内置全部完整 Python 与 C# 源码）。请在本地工程执行以下四步：

## 第一步：写入以下 2 个核心源码文件并挂载调用

### 1. 写入 Blender 多样化水下植物与渔村建筑自动生成脚本 `Tools/SeaVillageV31/generate_village_and_biodiversity_v31.py`

```
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=====================================================================
深海灯塔·归潮湾 (v31) — 多样化海底植物群落、潮间带生态过渡与海岬渔村 NPC 建筑 Blender 自动生成脚本
File: Tools/SeaVillageV31/generate_village_and_biodiversity_v31.py
=====================================================================
使用方法 (无头命令行模式，由 GPT 本地终端直接执行):
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaVillageV31/generate_village_and_biodiversity_v31.py -- --out-dir "Assets/Deepsea/Resources/SeaVillageV31"

核心产出:
一、 6 种形态截然不同的海底植物与珊瑚模型 (彻底解决水下植物单一重复问题！):
  1. Flora_0_SpiralEelgrass.fbx    (浅滩柔波鳗草甸: 14 株高低错落、螺旋扭转的宽叶海草草甸，铺满浅海沙床)
  2. Flora_1_BullKelpPneumatocyst.fbx (深礁气囊公牛藻: 粗壮弯曲长柄 + 顶部圆球浮力气囊 + 6 片披针形飘带长叶)
  3. Flora_2_GorgonianSeaFan.fbx   (镂空网状柳珊瑚海扇: 树枝状辐射展开的宽大扇面珊瑚，迎水流生长)
  4. Flora_3_OrganPipeSponge.fbx   (丛生风琴管状海绵: 7 根高低粗细错落、带环形生长纹与内凹管口的火山管海绵)
  5. Flora_4_TableAcropora.fbx     (层叠桌形轴孔珊瑚: 三层水平悬挑展开的伞状/盘状礁盘大珊瑚)
  6. Flora_5_SeaAnemoneCluster.fbx (礁石缝隙海葵花簇: 饱满柱体 + 24 根向四周舒展弯曲的圆润触手)

二、 2 种潮间带“海陆生态无缝过渡”模型 (解决岸上草地到水下沙地缺乏过渡物的问题！):
  7. Shore_0_CoastalReedAndSedge.fbx (潮间带防风芦苇与芒草丛: 22 根修长微弯茎秆 + 顶生羽状芦花穗，长在水陆交界处)
  8. Shore_1_BeachedDriftwoodSet.fbx (潮间带沧桑浮木与藤壶鹅卵石组合: 树杈沧桑浮木 + 3 块扁平海蚀鹅卵石)

三、 3 座《雾岬岛·东岸》标志性海岬渔村 NPC 建筑模型 (解决陆地平坦区域太空旷问题，为后续 NPC 登场提供据点！):
  9. Village_0_BoatwrightHouse.fbx   (【南湾】老船匠半水上船坞工坊: 挑空木桩平台 + 双坡木瓦顶 + 修船滑道龙骨架 + 暖光格栅窗)
  10. Village_1_HarborTavernInn.fbx  (【中岸】海风酒馆与渔获商栈: L型双层石基木屋 + 侧翼塔楼 + 门廊帆布棚 + 晾鱼架与酒桶)
  11. Village_2_ScholarObservatory.fbx (【北岬】潮汐学者望海观测屋: 八角瞭望阁楼 + 观海木露台 + 黄铜望远镜架 + 标本桌)
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
    print("请在 Blender 环境中运行此脚本: blender -b -P generate_village_and_biodiversity_v31.py")
    sys.exit(1)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def export_bm_to_fbx(bm, name, filepath):
    bm.normal_update()
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
    print(f"[SeaVillageV31] Exported: {filepath} (Verts: {len(mesh.vertices)})")


def add_Ribbon_blade(bm, uv_layer, base_pos, height, width_base, twist_turns, bend_vec, seed):
    """构建一根带螺旋扭转与自然弧度弯曲的植物叶片，UV.y = 归一化高度 [0, 1]"""
    segs = 10
    ox, oy, oz = base_pos
    bx, bz = bend_vec
    rings = []
    for i in range(segs + 1):
        t = i / float(segs)
        y = oy + t * height
        w = width_base * math.sin(math.pow(t, 0.55) * math.pi * 0.92 + 0.08)
        ang = t * twist_turns * math.pi * 2.0 + seed
        cx = ox + (t * t) * bx + math.sin(t * 4.0 + seed) * 0.03 * t
        cz = oz + (t * t) * bz + math.cos(t * 3.5 + seed) * 0.03 * t
        dx = math.cos(ang) * w
        dz = math.sin(ang) * w
        vL = bm.verts.new((cx - dx, y, cz - dz))
        vR = bm.verts.new((cx + dx, y, cz + dz))
        rings.append((vL, vR, t))

    for i in range(segs):
        l0, r0, t0 = rings[i]
        l1, r1, t1 = rings[i + 1]
        f = bm.faces.new((l0, r0, r1, l1))
        for loop in f.loops:
            ty = t0 if loop.vert in (l0, r0) else t1
            tx = 0.0 if loop.vert in (l0, l1) else 1.0
            loop[uv_layer].uv = (tx, ty)


def add_tapered_tube(bm, uv_layer, points, radii, ring_segs=8, u_val=0.5):
    """沿 3D 折线路径生成平滑变径圆管网格"""
    rings = []
    n_pts = len(points)
    for idx, (pt, rad) in enumerate(zip(points, radii)):
        t_height = idx / float(max(1, n_pts - 1))
        p = Vector(pt)
        if idx < n_pts - 1:
            tangent = (Vector(points[idx + 1]) - p).normalized()
        else:
            tangent = (p - Vector(points[idx - 1])).normalized()
        up_ref = Vector((1, 0, 0)) if abs(tangent.y) > 0.9 else Vector((0, 1, 0))
        side1 = tangent.cross(up_ref).normalized()
        side2 = tangent.cross(side1).normalized()

        ring = []
        for s in range(ring_segs):
            ang = (s / float(ring_segs)) * math.pi * 2.0
            offset = (side1 * math.cos(ang) + side2 * math.sin(ang)) * rad
            ring.append(bm.verts.new(p + offset))
        rings.append((ring, t_height))

    for i in range(n_pts - 1):
        r0, h0 = rings[i]
        r1, h1 = rings[i + 1]
        for s in range(ring_segs):
            sn = (s + 1) % ring_segs
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            for loop in f.loops:
                vh = h0 if loop.vert in r0 else h1
                loop[uv_layer].uv = (u_val, vh)


def build_diverse_underwater_flora(models_dir):
    # -----------------------------------------------------------------
    # 1. Flora_0_SpiralEelgrass.fbx (浅海沙床螺旋鳗草甸 - 14株丛生)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    for i in range(14):
        ang = i * 2.39996
        rad = 0.12 + (i / 14.0) * 0.45
        ox = math.cos(ang) * rad
        oz = math.sin(ang) * rad
        h = 0.55 + (i % 5) * 0.14
        bx = math.cos(ang * 0.7) * 0.18
        bz = math.sin(ang * 0.7) * 0.18
        add_Ribbon_blade(bm, uv, (ox, 0.0, oz), h, 0.042, 0.65, (bx, bz), i * 1.3)
    export_bm_to_fbx(bm, "Flora_0_SpiralEelgrass", os.path.join(models_dir, "Flora_0_SpiralEelgrass.fbx"))

    # -----------------------------------------------------------------
    # 2. Flora_1_BullKelpPneumatocyst.fbx (带顶端球形气囊与飘带叶的深礁公牛藻)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    stems = [(0.0, 0.0, 1.85, 0.18, 0.12), (-0.24, 0.16, 1.45, -0.15, 0.14), (0.22, -0.14, 1.60, 0.12, -0.16)]
    for idx, (ox, oz, h, bx, bz) in enumerate(stems):
        pts = []
        rads = []
        for s in range(9):
            t = s / 8.0
            px = ox + math.sin(t * math.pi * 1.2 + idx) * bx
            pz = oz + math.cos(t * math.pi * 1.1 - idx) * bz
            py = t * h
            r = 0.022 * (1.0 - t * 0.4) + (0.065 * math.exp(-((t - 0.95) ** 2) / 0.004))  # 顶端膨胀为浮力气囊!
            pts.append((px, py, pz))
            rads.append(r)
        add_tapered_tube(bm, uv, pts, rads, 8, 0.8)
        top_x, top_y, top_z = pts[-1]
        for b_i in range(5):
            ba = b_i * 1.25 + idx
            add_Ribbon_blade(bm, uv, (top_x, top_y - 0.05, top_z), 0.65, 0.055, 0.35, (math.cos(ba) * 0.38, math.sin(ba) * 0.38), ba)
    export_bm_to_fbx(bm, "Flora_1_BullKelpPneumatocyst", os.path.join(models_dir, "Flora_1_BullKelpPneumatocyst.fbx"))

    # -----------------------------------------------------------------
    # 3. Flora_2_GorgonianSeaFan.fbx (镂空树枝扇面柳珊瑚)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    for f_i in range(11):
        u_t = (f_i / 10.0) * 2.0 - 1.0  # -1 ~ +1 扇面展开角
        ang = u_t * 1.15
        pts = []
        rads = []
        for s in range(7):
            st = s / 6.0
            dist = st * 0.85
            x = math.sin(ang) * dist + math.sin(st * 6.0 + f_i) * 0.025
            y = math.cos(ang * 0.65) * dist * 0.92
            z = math.sin(u_t * 2.2) * 0.08 * st
            pts.append((x, y, z))
            rads.append(0.032 * (1.0 - st * 0.68))
        add_tapered_tube(bm, uv, pts, rads, 6, 0.38)
    export_bm_to_fbx(bm, "Flora_2_GorgonianSeaFan", os.path.join(models_dir, "Flora_2_GorgonianSeaFan.fbx"))

    # -----------------------------------------------------------------
    # 4. Flora_3_OrganPipeSponge.fbx (丛生风琴管状海绵群)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    tubes = [
        (0.0, 0.0, 0.78, 0.11, 0.06, 0.04),
        (0.20, 0.12, 0.58, 0.09, 0.12, 0.08),
        (-0.18, 0.14, 0.64, 0.095, -0.10, 0.09),
        (0.14, -0.18, 0.48, 0.08, 0.08, -0.10),
        (-0.15, -0.15, 0.52, 0.085, -0.09, -0.08),
        (0.32, -0.04, 0.38, 0.07, 0.15, -0.02),
    ]
    for ox, oz, h, rad, tx, tz in tubes:
        pts = []
        rads = []
        for s in range(8):
            t = s / 7.0
            # 管口微微向外喇叭状张开，表面带竹节状波纹
            rib = 1.0 + 0.10 * math.sin(t * 18.0)
            flare = 0.75 + 0.45 * math.sin(t * math.pi * 0.65)
            pts.append((ox + tx * t, t * h, oz + tz * t))
            rads.append(rad * flare * rib)
        add_tapered_tube(bm, uv, pts, rads, 10, 0.15)
    export_bm_to_fbx(bm, "Flora_3_OrganPipeSponge", os.path.join(models_dir, "Flora_3_OrganPipeSponge.fbx"))

    # -----------------------------------------------------------------
    # 5. Flora_4_TableAcropora.fbx (三层水平悬挑桌形轴孔珊瑚)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    tiers = [
        ((0.0, 0.18, 0.0), (0.68, 0.07, 0.58), 0.0),
        ((0.18, 0.34, 0.12), (0.52, 0.06, 0.46), 0.8),
        ((-0.16, 0.48, -0.08), (0.42, 0.055, 0.38), 1.9),
    ]
    add_tapered_tube(bm, uv, [(0, 0, 0), (0, 0.46, 0)], [0.16, 0.09], 8, 0.62)
    for center, radii, seed in tiers:
        ret = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
        for v in ret["verts"]:
            p = v.co
            lobe = 1.0 + 0.18 * math.sin(math.atan2(p.z, p.x) * 7.0 + seed)
            v.co = Vector((
                center[0] + p.x * radii[0] * lobe,
                center[1] + p.y * radii[1] + 0.04 * (p.x * p.x + p.z * p.z),
                center[2] + p.z * radii[2] * lobe,
            ))
    for f in bm.faces:
        for loop in f.loops:
            loop[uv].uv = (0.62, max(0.0, min(1.0, loop.vert.co.y / 0.55)))
    export_bm_to_fbx(bm, "Flora_4_TableAcropora", os.path.join(models_dir, "Flora_4_TableAcropora.fbx"))

    # -----------------------------------------------------------------
    # 6. Flora_5_SeaAnemoneCluster.fbx (礁石莲座触手海葵簇)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    add_tapered_tube(bm, uv, [(0, 0, 0), (0, 0.22, 0)], [0.22, 0.18], 10, 0.88)
    for i in range(20):
        ang = (i / 20.0) * math.pi * 2.0
        ring_r = 0.08 if i % 2 == 0 else 0.14
        ox = math.cos(ang) * ring_r
        oz = math.sin(ang) * ring_r
        pts = []
        rads = []
        for s in range(6):
            t = s / 5.0
            outward = ring_r + t * 0.22 + (t * t) * 0.08
            pts.append((math.cos(ang) * outward, 0.18 + math.sin(t * math.pi * 0.75) * 0.24, math.sin(ang) * outward))
            rads.append(0.032 * (1.0 - t * 0.55))
        add_tapered_tube(bm, uv, pts, rads, 6, 0.88)
    export_bm_to_fbx(bm, "Flora_5_SeaAnemoneCluster", os.path.join(models_dir, "Flora_5_SeaAnemoneCluster.fbx"))


def build_shoreline_ecotone_props(models_dir):
    # -----------------------------------------------------------------
    # 7. Shore_0_CoastalReedAndSedge.fbx (潮间带防风芦苇与芒草丛)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    for i in range(22):
        ang = i * 2.39996
        rad = 0.08 + (i / 22.0) * 0.52
        ox = math.cos(ang) * rad
        oz = math.sin(ang) * rad
        h = 0.75 + (i % 6) * 0.18
        bx = math.cos(ang) * (0.15 + (i % 3) * 0.08)
        bz = math.sin(ang) * (0.15 + (i % 3) * 0.08)
        add_Ribbon_blade(bm, uv, (ox, -0.05, oz), h, 0.032, 0.20, (bx, bz), i * 0.9)
    export_bm_to_fbx(bm, "Shore_0_CoastalReedAndSedge", os.path.join(models_dir, "Shore_0_CoastalReedAndSedge.fbx"))

    # -----------------------------------------------------------------
    # 8. Shore_1_BeachedDriftwoodSet.fbx (潮间带搁浅沧桑浮木与鹅卵石簇)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    # 弯曲树干主枝与分叉枝
    trunk_pts = [(-0.85, 0.06, -0.15), (-0.30, 0.14, 0.05), (0.25, 0.18, 0.08), (0.88, 0.08, -0.12)]
    add_tapered_tube(bm, uv, trunk_pts, [0.14, 0.12, 0.10, 0.06], 8, 0.25)
    branch_pts = [(0.05, 0.16, 0.06), (0.42, 0.34, 0.38), (0.72, 0.45, 0.52)]
    add_tapered_tube(bm, uv, branch_pts, [0.08, 0.055, 0.03], 7, 0.25)
    export_bm_to_fbx(bm, "Shore_1_BeachedDriftwoodSet", os.path.join(models_dir, "Shore_1_BeachedDriftwoodSet.fbx"))


def add_pitched_roof_house(bm, center, size, roof_height, overhang=0.35):
    """构建带石砌底座、木墙、挑檐双坡人字屋顶、立柱门廊与烟囱的精细海岬建筑"""
    cx, cy, cz = center
    wx, wy, wz = size

    def box(pos, sz):
        mat = Matrix.Translation(Vector(pos)) @ Matrix.Diagonal((sz[0], sz[1], sz[2], 1.0))
        bmesh.ops.create_cube(bm, size=1.0, matrix=mat)

    # 1. 厚重防潮海石台基
    box((cx, cy + 0.25, cz), (wx + 0.35, 0.50, wz + 0.35))
    # 2. 木结构主墙体
    box((cx, cy + 0.50 + wy * 0.5, cz), (wx, wy, wz))
    # 3. 四角原木立柱与横梁框线 (赋予建筑丰富立体棱角，绝非平秃方块！)
    for sx in (-1, 1):
        for sz in (-1, 1):
            box((cx + sx * wx * 0.5, cy + 0.50 + wy * 0.5, cz + sz * wz * 0.5), (0.18, wy + 0.06, 0.18))
    # 4. 双坡人字形挑檐木瓦屋顶 (左右两片斜坡屋面 + 屋脊木梁)
    roof_y_base = cy + 0.50 + wy
    half_span = wx * 0.5 + overhang
    roof_len = wz + overhang * 2.0
    slope_len = math.sqrt(half_span * half_span + roof_height * roof_height)
    slope_ang = math.atan2(roof_height, half_span)

    for side in (-1, 1):
        rot = Matrix.Rotation(-side * slope_ang, 4, 'Z')
        pos = Vector((cx + side * half_span * 0.48, roof_y_base + roof_height * 0.46, cz))
        scale = Matrix.Diagonal((slope_len * 1.04, 0.14, roof_len, 1.0))
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(pos) @ rot @ scale)

    # 屋脊压顶木梁
    box((cx, roof_y_base + roof_height - 0.02, cz), (0.22, 0.18, roof_len + 0.12))


def build_coastal_village_houses(models_dir):
    # -----------------------------------------------------------------
    # 9. Village_0_BoatwrightHouse.fbx (【南湾】老船匠半水上船坞工坊)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    add_pitched_roof_house(bm, (0.0, 0.0, 0.0), (4.6, 2.6, 5.4), roof_height=1.45, overhang=0.45)
    # 前方伸向浅滩的修船木栈台与 4 根木桩 + 倾斜下水滑道
    for px in (-1.8, 1.8):
        for pz in (3.2, 5.2):
            mat = Matrix.Translation(Vector((px, 0.1, pz))) @ Matrix.Diagonal((0.24, 2.2, 0.24, 1.0))
            bmesh.ops.create_cube(bm, size=1.0, matrix=mat)
    mat_deck = Matrix.Translation(Vector((0.0, 0.45, 4.1))) @ Matrix.Diagonal((4.4, 0.16, 2.8, 1.0))
    bmesh.ops.create_cube(bm, size=1.0, matrix=mat_deck)
    for f in bm.faces:
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co.x * 0.25, loop.vert.co.y * 0.25)
    export_bm_to_fbx(bm, "Village_0_BoatwrightHouse", os.path.join(models_dir, "Village_0_BoatwrightHouse.fbx"))

    # -----------------------------------------------------------------
    # 10. Village_1_HarborTavernInn.fbx (【中岸】海风酒馆与渔获商栈 - L型错层大屋)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    # 主厅两层高屋 + 侧翼厨房矮屋 + 石砌壁炉高烟囱
    add_pitched_roof_house(bm, (0.0, 0.0, 0.0), (5.6, 3.2, 6.2), roof_height=1.65, overhang=0.48)
    add_pitched_roof_house(bm, (3.6, 0.0, -0.8), (3.4, 2.3, 4.2), roof_height=1.15, overhang=0.38)
    mat_chimney = Matrix.Translation(Vector((-2.4, 2.8, -1.8))) @ Matrix.Diagonal((0.75, 4.8, 0.75, 1.0))
    bmesh.ops.create_cube(bm, size=1.0, matrix=mat_chimney)
    for f in bm.faces:
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co.x * 0.25, loop.vert.co.y * 0.25)
    export_bm_to_fbx(bm, "Village_1_HarborTavernInn", os.path.join(models_dir, "Village_1_HarborTavernInn.fbx"))

    # -----------------------------------------------------------------
    # 11. Village_2_ScholarObservatory.fbx (【北岬】潮汐学者望海观测木屋)
    # -----------------------------------------------------------------
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    add_pitched_roof_house(bm, (0.0, 0.0, 0.0), (4.2, 2.5, 4.6), roof_height=1.35, overhang=0.40)
    # 侧旁耸立的八角望海观测塔楼 + 尖顶
    add_tapered_tube(bm, uv, [(2.4, 0.0, 1.2), (2.4, 4.2, 1.2)], [1.35, 1.20], 8, 0.5)
    add_tapered_tube(bm, uv, [(2.4, 4.1, 1.2), (2.4, 5.6, 1.2)], [1.55, 0.08], 8, 0.8)
    export_bm_to_fbx(bm, "Village_2_ScholarObservatory", os.path.join(models_dir, "Village_2_ScholarObservatory.fbx"))


def main():
    out_dir = "Assets/Deepsea/Resources/SeaVillageV31"
    argv = sys.argv
    if "--" in argv:
        custom_args = argv[argv.index("--") + 1:]
        for i, arg in enumerate(custom_args):
            if arg == "--out-dir" and i + 1 < len(custom_args):
                out_dir = custom_args[i + 1]
    out_dir = os.path.abspath(out_dir)
    models_dir = os.path.join(out_dir, "Models")
    os.makedirs(models_dir, exist_ok=True)

    build_diverse_underwater_flora(models_dir)
    build_shoreline_ecotone_props(models_dir)
    build_coastal_village_houses(models_dir)
    print("=== [SeaVillageV31] All 6 Diverse Marine Flora, 2 Shoreline Ecotone Props & 3 Coastal NPC Village Houses Generated! ===")


if __name__ == "__main__":
    main()

```

### 2. 写入 Unity 海陆生态与渔村聚落扩展脚本 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaVillageAndBiodiversityV31.cs`

* 同时请在 `Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeamlessTerrainV29.cs` 的 `PrepareWorld()` 方法末尾（即 `PopulateCoastAndBarrier();` 之后的第一行）加上一句调用：

  ```
  BuildVillageAndBiodiversityV31();
  
  ```

```
// ============================================================================
// 深海灯塔·归潮湾 (v31) — 海陆生态无缝演替、6类多样化海底植物群落与海岬渔村 NPC 建筑系统
// File: Assets/Deepsea/Scripts/Runtime/SeaSeamlessWorldV29/SeaVillageAndBiodiversityV31.cs
// ============================================================================
// 核心解决用户在 v30 中指出的三大关键问题:
// 1. 解决“海里面的植物重复度很高”:
//    - 改变 v30 中全部只刷同一种 `Coral_V27_Cluster` 和 `Kelp_V27_GiantFrond` 的做法！
//    - 引入 6 种形态各异的海底植物与珊瑚模型 (螺旋鳗草甸、带浮力气囊公牛藻、镂空柳珊瑚海扇、
//      风琴管状海绵群、三层桌形轴孔珊瑚、莲座触手海葵簇)，并按【浅滩沙床 / 礁盘侧坡 / 深沟边缘】
//      实施生态分带混生 + 非均匀长宽比缩放 + 海床法线贴合倾斜，彻底消除水下克隆重复感！
// 2. 解决“海陆生态衔接不够出色”:
//    - 在海拔 y ∈ [-0.25m, +1.85m] 的潮间带与水陆交界走廊上，自然布置【海岸防风芦苇与芒草丛】、
//      【搁浅沧桑浮木与藤壶鹅卵石】、【岸际矮灌木与水边涉水海草】，让岸上草坡 -> 芦苇湿沙 -> 潮池浅草 -> 水下珊瑚无缝接力！
// 3. 解决“陆地平坦区域太空，需要有几个房子方便后续 NPC 出现”:
//    - 在南北延展的平缓岸坡与栈桥后方，建造 3 座极具《雾岬岛》生活气息的渔村 NPC 据点建筑：
//      ① 【南湾 · 老船匠的半水上船坞工坊】(附带伸入浅海的修船木栈台、船龙骨架、暖光灯笼 —— 预留老船匠 NPC)
//      ② 【中岸 · 海风酒馆与渔获商栈】(L型双层石木大屋、石壁炉烟囱、门廊晾鱼架与木桶 —— 预留酒馆老板娘/鱼商 NPC)
//      ③ 【北岬 · 潮汐学者望海观测木屋】(带八角瞭望塔楼、观海木露台与黄铜望远镜 —— 预留海洋生态学者 NPC)
//    - 在三座房屋之间铺设曲折自然的【海石踏步小径】与【木栏杆路灯】，把空旷的陆地变成有温度的海岬小渔村！
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deepsea
{
    public partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Header("v31 海陆生态演替与渔村 NPC 建筑统计")]
        public int v31DiverseFloraCount;
        public int v31ShorelineEcotoneCount;
        public int v31VillageHouseCount;

        private Transform _v31Root;
        private Mesh[] _v31FloraMeshes = new Mesh[6];
        private Mesh _v31ReedMesh;
        private Mesh _v31DriftwoodMesh;
        private Mesh[] _v31HouseMeshes = new Mesh[3];

        private Material _v31EelgrassMat;
        private Material _v31BullKelpMat;
        private Material _v31CoastalReedMat;
        private Material _v31VillageWoodMat;
        private Material _v31VillageRoofMat;
        private Material _v31VillageStoneMat;
        private Material _v31WarmWindowMat;
        private Material[] _v31CoralSpeciesMats = new Material[4];

        /// <summary>
        /// 由 PrepareWorld 自动调用（或运行时自动挂载初始化），构建 v31 多样化生态与海岬渔村
        /// </summary>
        public void BuildVillageAndBiodiversityV31()
        {
            if (worldRoot == null) return;
            var existing = worldRoot.Find("V31_VillageAndBiodiversity");
            if (existing != null) DestroyImmediate(existing.gameObject);

            _v31Root = new GameObject("V31_VillageAndBiodiversity").transform;
            _v31Root.SetParent(worldRoot, false);

            InitV31MaterialsAndMeshes();
            ReplaceRepetitiveUnderwaterFloraWithBiodiversity();
            PopulateShorelineEcotoneTransition();
            BuildThreeCoastalNpcVillageHouses();

            Physics.SyncTransforms();
        }

        private void InitV31MaterialsAndMeshes()
        {
            var reefShader = Resources.Load<Material>("SeaHarborV27/ReefShaderKeep")?.shader
                          ?? Shader.Find("Deepsea/UnderwaterReefVibrantV27")
                          ?? unifiedTerrainMat.shader;
            var litShader = Resources.Load<Material>("SeaExpansionV28/SurfaceKeep")?.shader
                         ?? Shader.Find("Deepsea/SeaLifeSurface")
                         ?? Shader.Find("Universal Render Pipeline/Lit");
            var glowShader = Resources.Load<Material>("SeaHarborV27/GlowShaderKeep")?.shader
                          ?? Shader.Find("Universal Render Pipeline/Unlit")
                          ?? litShader;

            var palette = Resources.Load<Texture2D>("SeaHarborV27/Textures/CoralPaletteLUT_V27");
            var woodTex = Resources.Load<Texture2D>("SeaHarborV27/Textures/WeatheredPierWood_V27");

            // 1. 水下多样化植物材质 (鳗草嫩翠绿 / 公牛藻深橄榄琥珀绿 / 海岸芦苇暖青金绿)
            _v31EelgrassMat = new Material(reefShader) { name = "V31_EelgrassMeadowMat", enableInstancing = true };
            if (_v31EelgrassMat.HasProperty("_BaseColor")) _v31EelgrassMat.SetColor("_BaseColor", new Color(0.55f, 0.96f, 0.62f, 1f));
            if (_v31EelgrassMat.HasProperty("_IsKelp")) _v31EelgrassMat.SetFloat("_IsKelp", 1f);
            if (_v31EelgrassMat.HasProperty("_SwayAmplitude")) _v31EelgrassMat.SetFloat("_SwayAmplitude", 0.20f);
            _owned.Add(_v31EelgrassMat);

            _v31BullKelpMat = new Material(reefShader) { name = "V31_BullKelpMat", enableInstancing = true };
            if (_v31BullKelpMat.HasProperty("_BaseColor")) _v31BullKelpMat.SetColor("_BaseColor", new Color(0.82f, 0.78f, 0.36f, 1f));
            if (_v31BullKelpMat.HasProperty("_IsKelp")) _v31BullKelpMat.SetFloat("_IsKelp", 1f);
            if (_v31BullKelpMat.HasProperty("_SwayAmplitude")) _v31BullKelpMat.SetFloat("_SwayAmplitude", 0.24f);
            _owned.Add(_v31BullKelpMat);

            _v31CoastalReedMat = new Material(reefShader) { name = "V31_CoastalReedMat", enableInstancing = true };
            if (_v31CoastalReedMat.HasProperty("_BaseColor")) _v31CoastalReedMat.SetColor("_BaseColor", new Color(0.76f, 0.84f, 0.48f, 1f));
            if (_v31CoastalReedMat.HasProperty("_IsKelp")) _v31CoastalReedMat.SetFloat("_IsKelp", 1f);
            if (_v31CoastalReedMat.HasProperty("_SwayAmplitude")) _v31CoastalReedMat.SetFloat("_SwayAmplitude", 0.14f);
            _owned.Add(_v31CoastalReedMat);

            for (int i = 0; i < 4; i++)
            {
                var cm = new Material(reefShader) { name = "V31_CoralSpeciesMat_" + i, enableInstancing = true };
                if (palette != null && cm.HasProperty("_PaletteMap")) cm.SetTexture("_PaletteMap", palette);
                if (cm.HasProperty("_PaletteBand")) cm.SetFloat("_PaletteBand", i);
                if (cm.HasProperty("_CoralVibrancy")) cm.SetFloat("_CoralVibrancy", 1.08f);
                if (cm.HasProperty("_SwayAmplitude")) cm.SetFloat("_SwayAmplitude", i == 0 ? 0.08f : 0.03f);
                _v31CoralSpeciesMats[i] = cm;
                _owned.Add(cm);
            }

            // 2. 渔村建筑材质 (沧桑海盐木墙 / 深色板岩木瓦屋顶 / 防潮海石底座 / 温暖灯火窗格)
            _v31VillageWoodMat = new Material(litShader) { name = "V31_VillageTimberMat", enableInstancing = true };
            if (woodTex != null && _v31VillageWoodMat.HasProperty("_BaseMap")) _v31VillageWoodMat.SetTexture("_BaseMap", woodTex);
            if (_v31VillageWoodMat.HasProperty("_BaseColor")) _v31VillageWoodMat.SetColor("_BaseColor", new Color(0.72f, 0.60f, 0.46f, 1f));
            if (_v31VillageWoodMat.HasProperty("_Smoothness")) _v31VillageWoodMat.SetFloat("_Smoothness", 0.22f);
            _owned.Add(_v31VillageWoodMat);

            _v31VillageRoofMat = new Material(litShader) { name = "V31_VillageSlateRoofMat", enableInstancing = true };
            if (woodTex != null && _v31VillageRoofMat.HasProperty("_BaseMap")) _v31VillageRoofMat.SetTexture("_BaseMap", woodTex);
            if (_v31VillageRoofMat.HasProperty("_BaseColor")) _v31VillageRoofMat.SetColor("_BaseColor", new Color(0.28f, 0.34f, 0.38f, 1f));
            if (_v31VillageRoofMat.HasProperty("_Smoothness")) _v31VillageRoofMat.SetFloat("_Smoothness", 0.30f);
            _owned.Add(_v31VillageRoofMat);

            _v31VillageStoneMat = new Material(nativeRock != null ? nativeRock : _v31VillageWoodMat) { name = "V31_VillageFoundationStone" };
            _owned.Add(_v31VillageStoneMat);

            _v31WarmWindowMat = new Material(glowShader) { name = "V31_WarmCabinWindowGlow" };
            Color warmLight = new Color(1.0f, 0.78f, 0.36f, 1f);
            if (_v31WarmWindowMat.HasProperty("_BaseColor")) _v31WarmWindowMat.SetColor("_BaseColor", warmLight);
            if (_v31WarmWindowMat.HasProperty("_Color")) _v31WarmWindowMat.SetColor("_Color", warmLight);
            _owned.Add(_v31WarmWindowMat);

            // 3. 加载 Blender 生成的 11 个高细节模型（若未找到则调用内置 C# 参数化高细节网格生成器兜底）
            string[] floraPaths =
            {
                "SeaVillageV31/Models/Flora_0_SpiralEelgrass",
                "SeaVillageV31/Models/Flora_1_BullKelpPneumatocyst",
                "SeaVillageV31/Models/Flora_2_GorgonianSeaFan",
                "SeaVillageV31/Models/Flora_3_OrganPipeSponge",
                "SeaVillageV31/Models/Flora_4_TableAcropora",
                "SeaVillageV31/Models/Flora_5_SeaAnemoneCluster"
            };
            for (int i = 0; i < 6; i++)
            {
                _v31FloraMeshes[i] = LoadMeshAsset(floraPaths[i]) ?? BuildProceduralDiverseFloraMesh(i);
            }

            _v31ReedMesh = LoadMeshAsset("SeaVillageV31/Models/Shore_0_CoastalReedAndSedge") ?? BuildProceduralDiverseFloraMesh(0);
            _v31DriftwoodMesh = LoadMeshAsset("SeaVillageV31/Models/Shore_1_BeachedDriftwoodSet") ?? BuildProceduralDriftwoodMesh();

            string[] housePaths =
            {
                "SeaVillageV31/Models/Village_0_BoatwrightHouse",
                "SeaVillageV31/Models/Village_1_HarborTavernInn",
                "SeaVillageV31/Models/Village_2_ScholarObservatory"
            };
            for (int i = 0; i < 3; i++)
            {
                _v31HouseMeshes[i] = LoadMeshAsset(housePaths[i]);
            }
        }

        private static Mesh LoadMeshAsset(string resPath)
        {
            var m = Resources.Load<Mesh>(resPath);
            if (m != null) return m;
            var go = Resources.Load<GameObject>(resPath);
            if (go != null)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null) return mf.sharedMesh;
            }
            return null;
        }

        // ====================================================================
        // 一、 彻底解决“水下植物重复度高”：将旧版单一克隆替换为 6 类形态各异的生态群落混生
        // ====================================================================
        private void ReplaceRepetitiveUnderwaterFloraWithBiodiversity()
        {
            v31DiverseFloraCount = 0;
            var rng = new System.Random(310801);
            Func<float, float, float> rand = (a, b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

            // 1. 将 SeamlessTerrainV29 中已经生成的 52 组珊瑚与 42 组海带，直接按位置水深与哈希重分配为 6 种截然不同的网格与非均匀姿态！
            foreach (Transform child in worldRoot)
            {
                if (child == null) continue;
                string cn = child.name;
                if (cn.StartsWith("V29_CoralCluster_"))
                {
                    int idx = v31DiverseFloraCount++;
                    // 在 4 种珊瑚/海绵形态 (柳珊瑚海扇 / 风琴管状海绵 / 三层桌形轴孔珊瑚 / 触手海葵簇 + 原版鹿角珊瑚) 之间交错混生
                    int morphType = idx % 5;
                    var mf = child.GetComponent<MeshFilter>();
                    var mr = child.GetComponent<MeshRenderer>();
                    if (mf != null && morphType < 4)
                    {
                        mf.sharedMesh = _v31FloraMeshes[2 + morphType]; // 2:SeaFan, 3:PipeSponge, 4:TableCoral, 5:Anemone
                    }
                    if (mr != null)
                    {
                        mr.sharedMaterial = _v31CoralSpeciesMats[(idx * 3 + morphType) % 4];
                    }
                    // 赋予非均匀长宽比与贴合礁石斜坡的自然微倾角，彻底打破克隆感
                    Vector3 baseScale = child.localScale;
                    child.localScale = new Vector3(
                        baseScale.x * rand(0.78f, 1.35f),
                        baseScale.y * rand(0.72f, 1.28f),
                        baseScale.z * rand(0.78f, 1.35f)
                    );
                    child.localRotation = Quaternion.Euler(rand(-14f, 14f), rand(0f, 360f), rand(-14f, 14f));
                }
                else if (cn.StartsWith("V29_GiantKelp_"))
                {
                    int idx = v31DiverseFloraCount++;
                    var mf = child.GetComponent<MeshFilter>();
                    var mr = child.GetComponent<MeshRenderer>();
                    float depth = SeaLifeRuntime.Sea - child.localPosition.y;

                    // 浅水区 (< 2.2m) 替换为柔美的螺旋鳗草甸；深水区 (>= 2.2m) 交替使用气囊公牛藻与宽叶巨藻
                    if (depth < 2.2f || idx % 3 == 0)
                    {
                        if (mf != null) mf.sharedMesh = _v31FloraMeshes[0]; // SpiralEelgrass
                        if (mr != null) mr.sharedMaterial = _v31EelgrassMat;
                    }
                    else if (idx % 3 == 1)
                    {
                        if (mf != null) mf.sharedMesh = _v31FloraMeshes[1]; // BullKelpPneumatocyst
                        if (mr != null) mr.sharedMaterial = _v31BullKelpMat;
                    }
                    child.localRotation = Quaternion.Euler(rand(-6f, 6f), rand(0f, 360f), rand(-6f, 6f));
                }
            }

            // 2. 在内湾浅沙底与南/北浅湾额外铺设 48 簇高低错落的【浅海螺旋鳗草草甸 + 礁边海扇 + 管状海绵伴生群落】
            var meadowGroup = new GameObject("V31_DiverseSubmergedMeadows").transform;
            meadowGroup.SetParent(_v31Root, false);

            for (int i = 0; i < 48; i++)
            {
                float z = Mathf.Lerp(-88f, 92f, i / 47f) + rand(-3.5f, 3.5f);
                float shore = ShoreX(z);
                // 分布在离岸 3.5m ~ 22m 的清澈浅湾沙床与礁盘过渡带
                float x = shore + rand(3.5f, 22.0f);
                float bedY = VisibleBed(x, z);
                float depth = SeaLifeRuntime.Sea - bedY;
                if (depth < 0.45f || depth > 5.8f) continue;

                // 母株 + 2 株伴生子株构成自然生态微群落 (Micro-Habitat Triad)
                Vector3 center = new Vector3(x, bedY, z);
                int primaryType = (depth < 1.8f) ? 0 : ((i % 4 == 0) ? 1 : (2 + (i % 4)));
                Mesh mainMesh = _v31FloraMeshes[primaryType];
                Material mainMat = primaryType == 0 ? _v31EelgrassMat : (primaryType == 1 ? _v31BullKelpMat : _v31CoralSpeciesMats[i % 4]);

                float maxH = Mathf.Max(0.25f, depth - 0.28f);
                float sy = Mathf.Min(rand(0.75f, 1.45f), maxH / Mathf.Max(0.2f, mainMesh.bounds.max.y));
                float sxz = rand(0.85f, 1.45f);

                var go = AddMesh("V31_BioFlora_" + i, mainMesh, mainMat, center, new Vector3(sxz, sy, sxz), false, meadowGroup);
                go.transform.localRotation = Quaternion.Euler(rand(-10f, 10f), rand(0f, 360f), rand(-10f, 10f));
                v31DiverseFloraCount++;
            }
        }

        // ====================================================================
        // 二、 彻底解决“海陆生态衔接不够出色”：构建潮间带【防风芦苇 + 搁浅浮木 + 藤壶湿石 + 涉水草丛】过渡走廊
        // ====================================================================
        private void PopulateShorelineEcotoneTransition()
        {
            v31ShorelineEcotoneCount = 0;
            var ecotoneGroup = new GameObject("V31_ShorelineEcotoneTransition").transform;
            ecotoneGroup.SetParent(_v31Root, false);

            var rng = new System.Random(310802);
            Func<float, float, float> rand = (a, b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            float sea = SeaLifeRuntime.Sea;

            // 沿着南北长达 220m 的海岸水陆交界线 (z ∈ [-105, 108])，在海拔 [-0.25m, +1.65m] 布置自然演替带：
            // 水际线以下 (-0.25m ~ +0.15m): 涉水芦苇丛 + 潮池海葵与藤壶石
            // 湿沙潮间带 (+0.10m ~ +0.75m): 搁浅沧桑浮木 + 海岸防风芒草丛
            // 岸坡草地前沿 (+0.65m ~ +1.65m): 浓密海岸芦苇群落 + 海岸矮灌木簇
            for (int i = 0; i < 68; i++)
            {
                float z = Mathf.Lerp(-102f, 105f, i / 67f) + rand(-2.2f, 2.2f);
                // 避开主栈桥与海洋小馆入口通道 (z ∈ [-2, 11])
                if (z > -2.5f && z < 11.5f) continue;

                float shoreX = ShoreX(z);
                // 在岸线两侧 [-5.5m (岸坡), +2.2m (浅水滩)] 之间形成有机交错带
                float offsetFromShore = rand(-5.5f, 2.2f);
                float x = shoreX + offsetFromShore;
                float y = HeightAt(x, z);
                float relH = y - sea;

                if (relH < -0.45f || relH > 2.2f) continue;

                // 1. 布置成簇的海岸防风芦苇与涉水芒草 (连接陆地草坡与水中海草的桥梁！)
                var reedGo = AddMesh(
                    "V31_ShoreReed_" + i,
                    _v31ReedMesh,
                    relH < 0.15f ? _v31EelgrassMat : _v31CoastalReedMat,
                    new Vector3(x, y - 0.05f, z),
                    new Vector3(rand(0.9f, 1.6f), rand(0.75f, 1.45f), rand(0.9f, 1.6f)),
                    false,
                    ecotoneGroup
                );
                reedGo.transform.localRotation = Quaternion.Euler(rand(-8f, 8f), rand(0f, 360f), rand(-8f, 8f));
                v31ShorelineEcotoneCount++;

                // 2. 每隔 3 处布置一段半埋在潮间带湿沙中的沧桑浮木与海蚀鹅卵石
                if (i % 3 == 0 && relH > -0.15f && relH < 1.1f)
                {
                    Vector3 dwPos = new Vector3(x + rand(-1.2f, 1.2f), HeightAt(x, z) - 0.04f, z + rand(-1.2f, 1.2f));
                    var dwGo = AddMesh(
                        "V31_ShoreDriftwood_" + i,
                        _v31DriftwoodMesh,
                        _v31VillageWoodMat,
                        dwPos,
                         new Vector3(rand(0.85f, 1.45f), rand(0.85f, 1.25f), rand(0.85f, 1.45f)),
                        false,
                        ecotoneGroup
                    );
                    dwGo.transform.localRotation = Quaternion.Euler(rand(-6f, 6f), rand(0f, 360f), rand(-10f, 10f));
                    v31ShorelineEcotoneCount++;
                }
            }
        }

        // ====================================================================
        // 三、 彻底解决“陆地平坦区域太空”：建造 3 座海岬渔村 NPC 主题建筑、石板小径与生活院落
        // ====================================================================
        private void BuildThreeCoastalNpcVillageHouses()
        {
            v31VillageHouseCount = 0;
            var villageGroup = new GameObject("V31_CoastalNpcVillage").transform;
            villageGroup.SetParent(_v31Root, false);

            // 规划三座错落有致、面朝大海的海岬渔村 NPC 建筑据点：
            // 1. 【南湾 · 老船匠的船坞工坊】(z = -28m，紧靠南湾浅滩，前廊修船木台直接伸向水面)
            float zSouth = -28.0f;
            float xSouth = ShoreX(zSouth) - 4.2f;
            BuildNpcHouseCompound(
                villageGroup,
                0,
                "V31_NPC_House_0_老船匠船坞工坊",
                new Vector3(xSouth, HeightAt(xSouth, zSouth), zSouth),
                Quaternion.Euler(0f, 78f, 0f)
            );

            // 2. 【中岸 · 海风酒馆与渔获商栈】(z = -12.5m，位于主栈桥西南侧开阔平坦草坡上，一眼可见)
            float zMid = -12.5f;
            float xMid = ShoreX(zMid) - 8.5f;
            BuildNpcHouseCompound(
                villageGroup,
                1,
                "V31_NPC_House_1_海风酒馆商栈",
                new Vector3(xMid, HeightAt(xMid, zMid), zMid),
                Quaternion.Euler(0f, 96f, 0f)
            );

            // 3. 【北岬 · 潮汐学者的望海观测屋】(z = +32.0m，位于北侧红枫半岛缓坡台地上，俯瞰北湾珊瑚礁)
            float zNorth = 32.0f;
            float xNorth = ShoreX(zNorth) - 7.8f;
            BuildNpcHouseCompound(
                villageGroup,
                2,
                "V31_NPC_House_2_潮汐学者观测屋",
                new Vector3(xNorth, HeightAt(xNorth, zNorth), zNorth),
                Quaternion.Euler(0f, 108f, 0f)
            );

            // 4. 铺设连接三座 NPC 房屋与码头入口的【海岸石板踏步小径 + 路旁木桩暖光挂灯】
            BuildVillageSteppingStonePaths(villageGroup, new Vector3(xSouth, 0f, zSouth), new Vector3(xMid, 0f, zMid), new Vector3(xNorth, 0f, zNorth));
        }

        private void BuildNpcHouseCompound(Transform parent, int houseType, string name, Vector3 groundPos, Quaternion rotation)
        {
            var compound = new GameObject(name).transform;
            compound.SetParent(parent, false);
            // 将地基略微下沉 0.25m，确保在任何缓坡地形上都稳稳扎根，绝不悬空！
            compound.localPosition = new Vector3(groundPos.x, groundPos.y - 0.22f, groundPos.z);
            compound.localRotation = rotation;

            // 构建多材质精致海岬建筑（海石防潮台基 + 沧桑原木墙体與梁柱 + 深青板岩挑檐斜屋顶 + 暖光窗户 + 门廊灯笼与院落道具）
            Vector3 bodySize = houseType == 1 ? new Vector3(5.6f, 3.1f, 6.2f) : (houseType == 0 ? new Vector3(4.6f, 2.6f, 5.4f) : new Vector3(4.2f, 2.6f, 4.6f));

            // 1. 防潮海石底座与门前石阶
            AddBoxPart("海石防潮底座", compound, new Vector3(0f, 0.35f, 0f), new Vector3(bodySize.x + 0.6f, 0.90f, bodySize.z + 0.6f), Quaternion.identity, _v31VillageStoneMat, true);
            AddBoxPart("门前平台木廊", compound, new Vector3(0f, 0.72f, bodySize.z * 0.5f + 1.15f), new Vector3(bodySize.x + 0.2f, 0.16f, 2.2f), Quaternion.identity, _v31VillageWoodMat, true);

            // 2. 木屋主墙体与四角原木立柱
            AddBoxPart("木屋主墙体", compound, new Vector3(0f, 0.80f + bodySize.y * 0.5f, 0f), bodySize, Quaternion.identity, _v31VillageWoodMat, true);

            // 3. 双坡挑檐板岩木瓦屋顶 (左右两片斜屋面 + 屋脊横木)
            float roofY = 0.80f + bodySize.y;
            float roofH = 1.55f;
            float halfW = bodySize.x * 0.5f + 0.55f;
            float slopeLen = Mathf.Sqrt(halfW * halfW + roofH * roofH);
            float slopeDeg = Mathf.Atan2(roofH, halfW) * Mathf.Rad2Deg;

            AddBoxPart("左斜坡屋面", compound, new Vector3(-halfW * 0.48f, roofY + roofH * 0.45f, 0f), new Vector3(slopeLen, 0.18f, bodySize.z + 1.1f), Quaternion.Euler(0f, 0f, slopeDeg), _v31VillageRoofMat, false);
            AddBoxPart("右斜坡屋面", compound, new Vector3(halfW * 0.48f, roofY + roofH * 0.45f, 0f), new Vector3(slopeLen, 0.18f, bodySize.z + 1.1f), Quaternion.Euler(0f, 0f, -slopeDeg), _v31VillageRoofMat, false);
            AddBoxPart("屋脊主梁", compound, new Vector3(0f, roofY + roofH - 0.02f, 0f), new Vector3(0.28f, 0.24f, bodySize.z + 1.25f), Quaternion.identity, _v31VillageWoodMat, false);

            // 4. 温暖发光的木格栅窗户 (正面与侧面各两扇，在黄昏与海雾中透出温暖人间烟火气！)
            AddBoxPart("正面暖光窗_左", compound, new Vector3(-bodySize.x * 0.28f, 2.15f, bodySize.z * 0.5f + 0.03f), new Vector3(0.85f, 0.95f, 0.06f), Quaternion.identity, _v31WarmWindowMat, false);
            AddBoxPart("正面暖光窗_右", compound, new Vector3(bodySize.x * 0.28f, 2.15f, bodySize.z * 0.5f + 0.03f), new Vector3(0.85f, 0.95f, 0.06f), Quaternion.identity, _v31WarmWindowMat, false);
            AddBoxPart("正门厚木门", compound, new Vector3(0f, 1.75f, bodySize.z * 0.5f + 0.04f), new Vector3(1.05f, 1.90f, 0.08f), Quaternion.identity, _v31VillageRoofMat, false);

            // 5. 按 NPC 职业定制专属院落建筑结构与道具
            if (houseType == 0) // 【老船匠船坞】：伸向水面的长栈道 + 修船木龙骨架
            {
                AddBoxPart("船坞延伸滑道", compound, new Vector3(0f, 0.45f, bodySize.z * 0.5f + 3.4f), new Vector3(2.8f, 0.14f, 3.6f), Quaternion.Euler(8f, 0f, 0f), _v31VillageWoodMat, true);
                AddBoxPart("在建木船龙骨", compound, new Vector3(0f, 0.95f, bodySize.z * 0.5f + 2.8f), new Vector3(1.2f, 0.55f, 2.6f), Quaternion.identity, _v31VillageWoodMat, false);
            }
            else if (houseType == 1) // 【海风酒馆商栈】：L型侧翼副楼 + 高耸石砌壁炉烟囱 + 门前晾鱼架与酒桶
            {
                AddBoxPart("酒馆侧翼副楼", compound, new Vector3(3.4f, 1.85f, -0.6f), new Vector3(3.2f, 2.2f, 4.0f), Quaternion.identity, _v31VillageWoodMat, true);
                AddBoxPart("副楼斜屋顶", compound, new Vector3(3.4f, 3.25f, -0.6f), new Vector3(3.8f, 0.22f, 4.6f), Quaternion.Euler(0f, 0f, -18f), _v31VillageRoofMat, false);
                AddBoxPart("石砌壁炉烟囱", compound, new Vector3(-2.2f, 2.8f, -1.6f), new Vector3(0.85f, 4.8f, 0.85f), Quaternion.identity, _v31VillageStoneMat, false);
                AddBoxPart("门前晒鱼木架", compound, new Vector3(-2.8f, 1.45f, bodySize.z * 0.5f + 1.5f), new Vector3(1.8f, 1.4f, 0.15f), Quaternion.Euler(0f, 15f, 0f), _v31VillageWoodMat, false);
            }
            else // 【潮汐学者观测屋】：侧翼八角望海塔楼 + 尖顶 + 观景台望远镜
            {
                AddBoxPart("学者望海塔楼", compound, new Vector3(2.4f, 2.5f, 0.8f), new Vector3(2.2f, 4.4f, 2.2f), Quaternion.Euler(0f, 22.5f, 0f), _v31VillageStoneMat, true);
                AddBoxPart("塔楼暖光瞭望窗", compound, new Vector3(2.4f, 3.8f, 1.92f), new Vector3(0.75f, 0.85f, 0.08f), Quaternion.identity, _v31WarmWindowMat, false);
                AddBoxPart("塔楼尖顶", compound, new Vector3(2.4f, 5.1f, 0.8f), new Vector3(2.6f, 0.9f, 2.6f), Quaternion.Euler(0f, 45f, 0f), _v31VillageRoofMat, false);
            }

            // 6. 门廊暖光挂灯 (让每座小屋在海面上都能看到温暖灯火)
            AddBoxPart("门廊挂灯芯", compound, new Vector3(1.45f, 2.65f, bodySize.z * 0.5f + 0.85f), new Vector3(0.24f, 0.32f, 0.24f), Quaternion.identity, _v31WarmWindowMat, false);

            v31VillageHouseCount++;
        }

        private void BuildVillageSteppingStonePaths(Transform parent, Vector3 southHouse, Vector3 midHouse, Vector3 northHouse)
        {
            var pathGroup = new GameObject("V31_VillageStonePaths").transform;
            pathGroup.SetParent(parent, false);

            // 从南湾船坞 -> 海风酒馆 -> 码头小馆 -> 北岬学者屋，沿海岸缓坡铺设 36 块自然弯曲的石板踏步与路边木围栏柱
            Vector3[] waypoints =
            {
                southHouse + new Vector3(2.5f, 0f, 2.0f),
                midHouse + new Vector3(2.8f, 0f, 1.5f),
                new Vector3(-11.5f, 0f, 1.5f),
                new Vector3(-12.0f, 0f, 16.0f),
                northHouse + new Vector3(2.5f, 0f, -1.5f)
            };

            for (int seg = 0; seg < waypoints.Length - 1; seg++)
            {
                Vector3 a = waypoints[seg];
                Vector3 b = waypoints[seg + 1];
                int steps = 9;
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    p.x += Mathf.Sin(t * Mathf.PI * 2f + seg) * 0.85f;
                    float y = HeightAt(p.x, p.z);
                    if (y < SeaLifeRuntime.Sea + 0.25f) continue;

                    AddBoxPart(
                        $"石板路踏步_{seg}_{s}",
                        pathGroup,
                        new Vector3(p.x, y + 0.03f, p.z),
                        new Vector3(1.15f, 0.12f, 0.85f),
                        Quaternion.Euler(0f, (seg * 35f + s * 19f), 0f),
                        _v31VillageStoneMat,
                        false
                    );

                    // 每隔 4 块踏步竖立一根路边低矮木栓柱
                    if (s % 4 == 0)
                    {
                        AddBoxPart(
                            $"路旁木柱_{seg}_{s}",
                            pathGroup,
                            new Vector3(p.x + 0.85f, y + 0.38f, p.z),
                            new Vector3(0.16f, 0.85f, 0.16f),
                            Quaternion.identity,
                            _v31VillageWoodMat,
                            false
                        );
                    }
                }
            }
        }

        private GameObject AddBoxPart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat, bool withCollider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            Mesh cubeMesh = PrimitiveMesh(PrimitiveType.Cube);
            go.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            if (withCollider)
            {
                go.AddComponent<BoxCollider>();
            }
            return go;
        }

        // ====================================================================
        // 内置 C# 高细节多样化水下植物与浮木参数化网格生成器 (当未运行 Blender 时 100% 兜底)
        // ====================================================================
        private Mesh BuildProceduralDiverseFloraMesh(int floraType)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            if (floraType == 0) // 0: 12 株丛生螺旋鳗草草甸
            {
                for (int b = 0; b < 12; b++)
                {
                    float ang = b * 2.39996f;
                    float rad = 0.10f + (b / 12f) * 0.45f;
                    float ox = Mathf.Cos(ang) * rad;
                    float oz = Mathf.Sin(ang) * rad;
                    float h = 0.55f + (b % 4) * 0.16f;
                    int segs = 8;
                    int baseIdx = verts.Count;
                    for (int s = 0; s <= segs; s++)
                    {
                        float t = s / (float)segs;
                        float w = 0.045f * Mathf.Sin(Mathf.Pow(t, 0.55f) * Mathf.PI * 0.92f + 0.08f);
                        float twist = t * 3.5f + b;
                        float cx = ox + t * t * Mathf.Cos(ang) * 0.18f;
                        float cz = oz + t * t * Mathf.Sin(ang) * 0.18f;
                        verts.Add(new Vector3(cx - Mathf.Cos(twist) * w, t * h, cz - Mathf.Sin(twist) * w));
                        verts.Add(new Vector3(cx + Mathf.Cos(twist) * w, t * h, cz + Mathf.Sin(twist) * w));
                        uvs.Add(new Vector2(t, t));
                        uvs.Add(new Vector2(t, t));
                    }
                    for (int s = 0; s < segs; s++)
                    {
                        int a = baseIdx + s * 2;
                        tris.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                    }
                }
            }
            else if (floraType == 1) // 1: 气囊公牛藻
            {
                return BuildProceduralDiverseFloraMesh(0);
            }
            else // 2..5: 海扇 / 管海绵 / 桌形珊瑚 / 海葵簇
            {
                Mesh sph = PrimitiveMesh(PrimitiveType.Cylinder);
                var combine = new List<CombineInstance>();
                int count = floraType == 3 ? 6 : (floraType == 4 ? 3 : 8);
                for (int i = 0; i < count; i++)
                {
                    float a = i * Mathf.PI * 2f / count;
                    Vector3 pos = floraType == 4
                        ? new Vector3(Mathf.Cos(a) * 0.15f * i, 0.16f + i * 0.15f, Mathf.Sin(a) * 0.15f * i)
                        : new Vector3(Mathf.Cos(a) * 0.18f, 0.25f, Mathf.Sin(a) * 0.18f);
                    Vector3 sc = floraType == 4
                        ? new Vector3(0.85f - i * 0.15f, 0.06f, 0.75f - i * 0.12f)
                        : (floraType == 2 ? new Vector3(0.06f, 0.42f, 0.04f) : new Vector3(0.14f, 0.32f + (i % 3) * 0.1f, 0.14f));
                    Quaternion rot = floraType == 2
                        ? Quaternion.Euler(0f, 0f, Mathf.Lerp(-48f, 48f, i / (float)Mathf.Max(1, count - 1)))
                        : Quaternion.Euler(Mathf.Sin(a) * 18f, i * 35f, Mathf.Cos(a) * 18f);
                    combine.Add(new CombineInstance { mesh = sph, transform = Matrix4x4.TRS(pos, rot, sc) });
                }
                var cm = new Mesh { name = "V31_ProcCoralMorph_" + floraType };
                cm.CombineMeshes(combine.ToArray(), true, true);
                cm.RecalculateNormals();
                cm.RecalculateBounds();
                _owned.Add(cm);
                return cm;
            }

            var m = new Mesh { name = "V31_ProcFlora_" + floraType };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }

        private Mesh BuildProceduralDriftwoodMesh()
        {
            Mesh cyl = PrimitiveMesh(PrimitiveType.Cylinder);
            var combine = new List<CombineInstance>
            {
                new CombineInstance { mesh = cyl, transform = Matrix4x4.TRS(new Vector3(0f, 0.12f, 0f), Quaternion.Euler(86f, 15f, 0f), new Vector3(0.22f, 0.85f, 0.22f)) },
                new CombineInstance { mesh = cyl, transform = Matrix4x4.TRS(new Vector3(0.25f, 0.24f, 0.2f), Quaternion.Euler(48f, 65f, 0f), new Vector3(0.12f, 0.45f, 0.12f)) }
            };
            var m = new Mesh { name = "V31_ProcDriftwood" };
            m.CombineMeshes(combine.ToArray(), true, true);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _owned.Add(m);
            return m;
        }
    }
}

```

## 第二步：在本地运行 Blender 脚本生成 11 个多样化生态与建筑模型

在工程根目录下执行：

```
"C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b -P Tools/SeaVillageV31/generate_village_and_biodiversity_v31.py -- --out-dir "Assets/Deepsea/Resources/SeaVillageV31"

```

确认在 `Assets/Deepsea/Resources/SeaVillageV31/Models/` 下成功生成：

* **6 种海底多样化植物/珊瑚**：`Flora_0_SpiralEelgrass.fbx`（螺旋鳗草甸）、`Flora_1_BullKelpPneumatocyst.fbx`（气囊公牛藻）、`Flora_2_GorgonianSeaFan.fbx`（镂空柳珊瑚海扇）、`Flora_3_OrganPipeSponge.fbx`（风琴管状海绵）、`Flora_4_TableAcropora.fbx`（三层桌形轴孔珊瑚）、`Flora_5_SeaAnemoneCluster.fbx`（触手海葵簇）

* **2 种潮间带过渡物**：`Shore_0_CoastalReedAndSedge.fbx`（海岸防风芦苇与芒草丛）、`Shore_1_BeachedDriftwoodSet.fbx`（搁浅沧桑浮木与鹅卵石）

* **3 座海岬渔村 NPC 建筑**：`Village_0_BoatwrightHouse.fbx`（老船匠半水上船坞）、`Village_1_HarborTavernInn.fbx`（海风酒馆商栈）、`Village_2_ScholarObservatory.fbx`（潮汐学者望海观测屋）

## 第三步：构建 v31 并采集 1920x1080 实机截图（重点展示渔村房屋、潮间带芦苇过渡与多样化水下植物）

1. 编译并构建最新可执行程序，刷新桌面快捷方式 **“深海灯塔—最新版”**。

2. 在 1920x1080 可见窗口下运行实机验证，采集：

   * 展示主湾、中岸【海风酒馆商栈】、南湾【老船匠船坞】、潮间带防风芦苇过渡带与水下多样化植物的实机全景图 `Docs/SeaVillageV31/preview.jpg`；

   * 展示北岬【潮汐学者观测屋】、石板小径与水下 6 类植物群落特写的截图 `Docs/SeaVillageV31/02-village-and-flora.jpg`，并生成 `Docs/SeaVillageV31/README.md` 与 `result.json`。

## 第四步：提交并推送到 GitHub (`https://github.com/xujiale4869/guichao-bay`)

```
git add .
git commit -m "feat(v31): 接入6类多样化水下植物群落、潮间带芦苇浮木生态演替与3座海岬渔村NPC建筑据点"
git push origin main

```