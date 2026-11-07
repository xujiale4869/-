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
import json

try:
    import bpy
    import bmesh
    from mathutils import Vector, Matrix
except ImportError:
    print("请在 Blender 环境中运行此脚本: blender -b -P generate_village_and_biodiversity_v31.py")
    sys.exit(1)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


ROLE_NAMES = ['Timber', 'SlateRoof', 'SeaStone', 'WarmGlass', 'Brass', 'Rope']
MANIFEST = []

def export_bm_to_fbx(bm, name, filepath):
    """Bake identical source geometry to Unity-Y mesh JSON and Blender-Z FBX.
    All dimensions derive from the user's parametric brief, not a reference image.
    """
    bm.normal_update()
    architecture = name.startswith('Village_')
    multi = architecture or name.startswith('Shore_1')
    for f in bm.faces:
        f.smooth = not architecture
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.calc_loop_triangles()
    uv = mesh.uv_layers.active
    # Split per loop so UV seams / hard architectural edges survive Unity import.
    verts, normals, texcoords = [], [], []
    sub = [[] for _ in ROLE_NAMES]
    for tri in mesh.loop_triangles:
        ids = []
        for li in tri.loops:
            vi = mesh.loops[li].vertex_index
            v = mesh.vertices[vi]
            ids.append(len(verts))
            verts.append(list(v.co))
            normals.append(list(v.normal if not architecture else mesh.polygons[tri.polygon_index].normal))
            texcoords.append(list(uv.data[li].uv) if uv else [v.co.x*.4,v.co.y*.4])
        sub[tri.material_index if multi else 0].extend(ids)
    out = {'name':name,'vertices':[x for v in verts for x in v],'normals':[x for v in normals for x in v],'uv':[x for v in texcoords for x in v],
           'parts':[{'role':ROLE_NAMES[i], 'triangles':t} for i,t in enumerate(sub) if t],
           'coordinates':'Unity metres, Y-up; source mesh before FBX basis conversion'}
    root = os.path.dirname(os.path.dirname(filepath))
    os.makedirs(os.path.join(root,'SourceMeshes'),exist_ok=True)
    with open(os.path.join(root,'SourceMeshes',name+'.json'),'w',encoding='utf-8') as f:
        json.dump(out,f,separators=(',',':'))
    bounds = [[min(v[k] for v in verts),max(v[k] for v in verts)] for k in range(3)]
    # Author Y-up; convert exactly once to canonical Blender Z-up, -Y-forward.
    for v in mesh.vertices:
        x,y,z=v.co
        v.co=(x,-z,y)
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    colors=[(.36,.24,.14,1),(.12,.21,.24,1),(.43,.47,.41,1),(1,.67,.20,1),(.58,.41,.15,1),(.38,.32,.19,1)]
    for i,role in enumerate(ROLE_NAMES):
        mat=bpy.data.materials.new(role);mat.diffuse_color=colors[i];obj.data.materials.append(mat)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1.0
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    os.makedirs(os.path.dirname(filepath),exist_ok=True)
    master_dir=os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),'Tools','SeaVillageV31','Masters')
    os.makedirs(master_dir,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(master_dir,name+'.blend'))
    bpy.ops.export_scene.fbx(filepath=filepath,use_selection=True,object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    MANIFEST.append({'name':name,'triangles':len(mesh.loop_triangles),'boundsUnity':bounds,
        'materials':[x['role'] for x in out['parts']], 'anchor':'ground centre; metres',
        'animated':'existing vertex shader sway from UV.y for flora; static architecture'})
    print('[SeaVillageV31] Exported',name,'triangles',len(mesh.loop_triangles),flush=True)


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
        if twist_turns > .8:
            cx += math.sin(t*math.pi*2*twist_turns+seed)*.11*t
            cz += math.cos(t*math.pi*2*twist_turns+seed)*.11*t
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
        if tangent.length < 0.0001:
            tangent=(p-Vector(points[max(0,idx-1)])).normalized()
        if tangent.y < -0.2:tangent=-tangent
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
        add_Ribbon_blade(bm, uv, (ox, 0.0, oz), h, 0.075, 1.05, (bx, bz), i * 1.3)
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
        bladder=bmesh.ops.create_icosphere(bm,subdivisions=2,radius=.13)
        for v in bladder['verts']:v.co+=Vector((top_x,top_y-.025,top_z))
        for f in bm.faces:
            if all(v in bladder['verts'] for v in f.verts):
                for loop in f.loops:loop[uv].uv=(.8,.82)
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
    for j in range(10):
        a0=(j/10.0*2-1)*1.15;a1=((j+1)/10.0*2-1)*1.15
        for level in [.38,.62,.84]:
            def fanpt(a,t):return (math.sin(a)*t*.85,math.cos(a*.65)*t*.85*.92,math.sin(a/1.15*2.2)*.08*t)
            p0=fanpt(a0,level);p1=fanpt(a1,level+.045)
            mid=tuple((p0[k]+p1[k])*.5+(0.025 if k==1 else 0) for k in range(3))
            add_tapered_tube(bm,uv,[p0,mid,p1],[.014,.011,.009],4,.38)
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
        top=Vector(pts[-1]); outer=rads[-1]
        pts.extend([tuple(top),tuple(top-Vector((0,h*.27,0)))])
        rads.extend([outer*.66,outer*.48])
        before=set(bm.faces)
        add_tapered_tube(bm, uv, pts, rads, 10, 0.15)
        added=[f for f in bm.faces if f not in before]
        for f in added[-10:]:
            for loop in f.loops:loop[uv].uv=(.15,.08)
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
    for i in range(11):
        a=i*2.39996; r=.1+i*.035; h=.85+(i%5)*.18
        x,z=math.cos(a)*r,math.sin(a)*r
        add_tapered_tube(bm,uv,[(x,0,z),(x+.10,h,z+.06),(x+.15,h+.24,z+.10)],[.012,.016,.006],5,.5)
        for j in range(5):
            q=h+j*.043
            add_tapered_tube(bm,uv,[(x+.1,q,z+.06),(x+.1+(j%2*2-1)*.07,q+.055,z+.09)],[.016,.005],4,.7)
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
    for i,(x,z) in enumerate([(-.5,.5),(.5,-.35),(.85,.35)]):
        ret=bmesh.ops.create_icosphere(bm,subdivisions=2,radius=1)
        for v in ret['verts']:v.co=Vector((x+v.co.x*(.25+i*.03),.04+v.co.y*.12,z+v.co.z*.22))
        stones=set(ret['verts'])
        for f in bm.faces:
            if all(v in stones for v in f.verts):f.material_index=2
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
    # Inferred occupational architecture, using dimensions from supplied brief.
    # Material roles remain separate in one mesh; Unity uses these exact triangles.
    for kind,name in enumerate(['Village_0_BoatwrightHouse','Village_1_HarborTavernInn','Village_2_ScholarObservatory']):
        clear_scene();bm=bmesh.new();uv=bm.loops.layers.uv.new('UVMap')
        def box(p,sz,role=0,angle=0):
            old=set(bm.faces)
            mat=Matrix.Translation(Vector(p)) @ Matrix.Rotation(angle,4,'Z') @ Matrix.Diagonal((*sz,1.0))
            bmesh.ops.create_cube(bm,size=1,matrix=mat)
            for f in set(bm.faces)-old:f.material_index=role
        def tube(points,radii,role=0,segs=8):
            old=set(bm.faces);add_tapered_tube(bm,uv,points,radii,segs)
            for f in set(bm.faces)-old:f.material_index=role
        def house(cx,cz,w,h,d):
            box((cx,.25,cz),(w+.4,.5,d+.4),2)
            box((cx,.5+h*.5,cz),(w,h,d))
            for x in [-w/2,w/2]:
                for z in [-d/2,d/2]:box((cx+x,.5+h*.5,cz+z),(.17,h+.1,.17))
            # Broad alternating cladding bands visible at game scale.
            for j in range(5):box((cx,.65+j*h/5,cz+d/2+.025),(w,.045,.09))
            roofh=1.35;hw=w*.5+.42;ang=math.atan2(roofh,hw);sl=math.hypot(hw,roofh)
            for side in [-1,1]:
                box((cx+side*hw*.5,.5+h+roofh*.5,cz),(sl+.04,.16,d+.84),1,-side*ang)
                # Sparse roof battens, same merged material rather than extra renderers.
                for q in [-.32,0,.32]:
                    box((cx+side*hw*(.5+q),.5+h+roofh*(.5-q)+.08,cz),(.045,.05,d+.85),0,-side*ang)
            box((cx,.5+h+roofh,cz),(.20,.20,d+.98))
            # Close gable faces with actual triangles.
            for z in [-d/2,d/2]:
                f=bm.faces.new([bm.verts.new((cx-w/2,.5+h,cz+z)),bm.verts.new((cx+w/2,.5+h,cz+z)),bm.verts.new((cx,.5+h+roofh,cz+z))]);f.material_index=0
                if z<0:f.normal_flip()
            for x in [-w*.28,w*.28]:
                box((cx+x,1.95,cz+d/2+.065),(.82,.9,.06),3)
                for dx in [-.43,0,.43]:box((cx+x+dx,1.95,cz+d/2+.12),(.06,1.0,.08))
                box((cx+x,1.95,cz+d/2+.12),(.9,.06,.08))
            box((cx,1.40,cz+d/2+.08),(1.0,1.8,.12),1)
            box((cx,.48,cz+d/2+.65),(w+.35,.16,1.2))
            box((cx,.14,cz+d/2+1.15),(1.3,.25,.65),2)
        w,h,d=[(4.6,2.6,5.4),(5.6,3.2,6.2),(4.2,2.5,4.6)][kind]
        house(0,0,w,h,d)
        if kind==0:
            box((0,.42,4.4),(4.1,.20,3.1))
            for x in [-1.7,1.7]:
                for z in [3.2,5.5]:box((x,-.25,z),(.2,1.6,.2))
            box((0,.69,4.6),(.18,.22,2.6))
            # Open curved ribs distinguish a boat hull from a solid crate.
            for z in [3.55,4.1,4.65,5.2]:
                tube([(-.65,1.15,z),(-.48,.84,z),(0,.72,z),(.48,.84,z),(.65,1.15,z)],[.055]*5)
            for x in [-.85,.85]:box((x,.5,6.0),(.18,.16,1.8))
        elif kind==1:
            house(3.35,-.65,3.0,2.25,3.9)
            box((-2.1,2.7,-1.6),(.8,4.9,.8),2)
            box((-2.1,5.18,-1.6),(1.05,.25,1.05),2)
            box((0,2.50,4.1),(5.6,.11,1.6),1,.05)
            for x in [-2.5,2.5]:box((x,1.38,4.7),(.14,2.5,.14))
            # Barrels are deliberately low-sided, closed top.
            for x,z in [(-3.25,3.3),(-3.1,4.1)]:
                tube([(x,0,z),(x,.4,z),(x,.8,z)],[.30,.37,.30],0,10)
                box((x,.81,z),(.48,.08,.48))
            for x in [-2.15,-.95]:box((x,1.15,5.35),(.10,2.1,.10))
            box((-1.55,2.1,5.35),(1.45,.12,.10))
            for x in [-1.95,-1.6,-1.25]:tube([(x,2.03,5.35),(x,1.45,5.35)],[.035,.035],5,5)
        else:
            # Octagonal lookout and genuinely tapered cap.
            tube([(2.25,0,.6),(2.25,4.25,.6)],[1.15,1.15],2,8)
            tube([(2.25,4.15,.6),(2.25,5.65,.6)],[1.5,.02],1,8)
            for a in [0,math.pi/2,math.pi,3*math.pi/2]:
                x,z=2.25+math.cos(a)*1.16,.6+math.sin(a)*1.16
                box((x,3.6,z),(.65,.70,.09),3)
            box((0,.35,3.5),(4.4,.17,2.3))
            box((.8,.82,3.7),(.12,.9,.12),4)
            tube([(.8,1.4,3.5),(.8,1.65,4.15)],[.12,.18],4,10)
            box((-.95,.9,3.7),(1.15,.13,.65))
            for x in [-1.4,-.5]:box((x,.48,3.7),(.08,.85,.08))
        # A lantern cage with a visible warm core, no added realtime light.
        box((1.25,2.4,d/2+.45),(.20,.28,.20),3)
        for x in [1.12,1.38]:box((x,2.4,d/2+.45),(.035,.36,.27))
        bm.normal_update()
        for f in bm.faces:
            n=f.normal;dominant=max(range(3),key=lambda i:abs(n[i]));axes=[k for k in range(3) if k!=dominant]
            for loop in f.loops:loop[uv].uv=(loop.vert.co[axes[0]]*.5,loop.vert.co[axes[1]]*.5)
        export_bm_to_fbx(bm,name,os.path.join(models_dir,name+'.fbx'))


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
    with open(os.path.join(out_dir,'model-manifest.json'),'w',encoding='utf-8') as f:
        json.dump({'assets':MANIFEST,'basis':'Unity JSON Y-up; Blender master Z-up,-Y-forward; metres','scope':'procedural brief; no reference-image match claimed'},f,indent=2)
    print("=== [SeaVillageV31] All 6 Diverse Marine Flora, 2 Shoreline Ecotone Props & 3 Coastal NPC Village Houses Generated! ===")


if __name__ == "__main__":
    main()

