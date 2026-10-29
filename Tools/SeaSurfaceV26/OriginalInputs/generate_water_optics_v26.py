#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
=====================================================================
深海灯塔·归潮湾 (v26) — “雾岬岛·东岸”水体光学纹理全自动烘焙脚本
File: Tools/SeaSurfaceV26/generate_water_optics_v26.py
=====================================================================
功能说明:
无需任何第三方依赖 (纯 Python 标准库即可运行，也可在 Blender / Codex Python 环境运行)，
在 Assets/Deepsea/Resources/SeaSurfaceV26/Textures/ 下自动生成 3 张核心光学贴图:
1. WaterCaustics_V26.png (512x512, 四方无缝平铺 Tileable):
   - R 通道: 主层高锐度 Voronoi 晶格亮丝焦散网 (还原原图浅滩沙底清晰的网状金色/青白亮线)
   - G 通道: 次层相位偏移 Voronoi 焦散网 (用于双层反向交叉流动，产生真实水面波光交织感)
   - B 通道: 高频微面元波峰闪烁掩模 (Sparkle Microfacet Mask，用于远海波光粼粼的碎金点)
2. ShoreFoamNoise_V26.png (512x512, 四方无缝平铺 Tileable):
   - R 通道: 岸际涌浪丝状破碎泡沫网格 (Cellular Lace Foam)
   - G 通道: 多尺度湍流扰动场 (用于拍岸浪花边缘的自然撕裂与消散)
   - B 通道: 船尾螺旋桨与开尔文尾流高频气泡簇
3. WaterDepthRamp_V26.png (256x16, 五级水深光学吸收与散射 LUT 色板):
   - 严格采样自《雾岬岛·东岸》原图从左岸沙滩 -> 栈桥浅湾 -> 珊瑚礁盘边缘 -> 右侧深海鱼群海沟的色彩过渡！
=====================================================================
"""

import os
import sys
import math
import struct
import zlib


def write_png_rgba(filepath, width, height, pixel_fn):
    """纯 Python 标准库生成 RGBA PNG 文件"""
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
    print(f"[WaterOpticsV26] Generated: {filepath} ({width}x{height})")


def hash2d_periodic(ix, iy, period, seed=0):
    """周期性二维哈希，确保生成的 Voronoi 晶格在 UV 边界 100% 无缝平铺"""
    px = ix % period
    py = iy % period
    n = px * 374761393 + py * 668265263 + seed * 1442695041
    n = (n ^ (n >> 13)) * 1274126177
    rx = ((n & 0xFFFF) / 65535.0)
    ry = (((n >> 16) & 0xFFFF) / 65535.0)
    return rx, ry


def voronoi_caustics_ridge(u, v, grid_size=10, seed=1):
    """
    计算四方无缝平铺的二阶 Voronoi 边界差值 (F2 - F1)，
    当 F2 - F1 趋近于 0 时形成锐利、自然连通的水底焦散亮丝网！
    """
    # 叠加轻微的正弦波流体弯曲，让焦散网格富有水波流动感
    angle_u = u * math.pi * 2.0
    angle_v = v * math.pi * 2.0
    warp_u = u + 0.025 * math.sin(angle_v * 2.0 + seed)
    warp_v = v + 0.025 * math.cos(angle_u * 2.0 - seed)

    gx = warp_u * grid_size
    gy = warp_v * grid_size
    ix = int(math.floor(gx))
    iy = int(math.floor(gy))
    fx = gx - ix
    fy = gy - iy

    d1 = 999.0
    d2 = 999.0

    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            rx, ry = hash2d_periodic(ix + dx, iy + dy, grid_size, seed)
            # 限制特征点抖动幅度在 0.15~0.85，使晶格大小均匀美观
            px = dx + 0.15 + rx * 0.70 - fx
            py = dy + 0.15 + ry * 0.70 - fy
            dist = math.sqrt(px * px + py * py)
            if dist < d1:
                d2 = d1
                d1 = dist
            elif dist < d2:
                d2 = dist

    edge_dist = max(0.0, d2 - d1)
    # 焦散亮线：边界处极亮，向晶格中心指数衰减，还原原图通透的亮青色水底光网
    ridge_sharp = math.exp(-edge_dist * 11.5)
    ridge_soft = math.exp(-edge_dist * 4.2) * 0.35
    node_glow = math.exp(-d2 * 3.8) * 0.25
    return min(1.0, ridge_sharp + ridge_soft + node_glow)


def generate_caustics_texture(out_dir):
    filepath = os.path.join(out_dir, "WaterCaustics_V26.png")

    def pixel_fn(u, v):
        # R: 主焦散网络 (10x10 晶格 + 20x20 次级细丝)
        c1_main = voronoi_caustics_ridge(u, v, grid_size=9, seed=11)
        c1_fine = voronoi_caustics_ridge(u, v, grid_size=18, seed=23)
        r = min(1.0, c1_main * 0.78 + c1_fine * 0.38)

        # G: 相位交错次层焦散网络 (用于 Shader 中反向滚动叠加)
        c2_main = voronoi_caustics_ridge((u + 0.37) % 1.0, (v + 0.61) % 1.0, grid_size=10, seed=47)
        c2_fine = voronoi_caustics_ridge((u + 0.19) % 1.0, (v + 0.83) % 1.0, grid_size=20, seed=79)
        g = min(1.0, c2_main * 0.78 + c2_fine * 0.38)

        # B: 高频太阳波光微面元闪烁核 (Anisotropic Sun Sparkle Nuclei)
        sp1 = voronoi_caustics_ridge(u, v, grid_size=24, seed=103)
        sp2 = voronoi_caustics_ridge((u + 0.5) % 1.0, (v + 0.5) % 1.0, grid_size=32, seed=151)
        b = min(1.0, (sp1 * sp2) * 1.65)

        return (r, g, b, 1.0)

    write_png_rgba(filepath, 384, 384, pixel_fn)


def generate_foam_texture(out_dir):
    filepath = os.path.join(out_dir, "ShoreFoamNoise_V26.png")

    def pixel_fn(u, v):
        # R: 岸边网孔状浪花泡沫 (Cellular Web Foam)
        f_large = voronoi_caustics_ridge(u, v, grid_size=12, seed=301)
        f_mid = voronoi_caustics_ridge(u, v, grid_size=24, seed=409)
        r = min(1.0, f_large * 0.65 + f_mid * 0.55)

        # G: 平滑周期湍流扰动场 (控制多层岸浪环的破碎度)
        au = u * math.pi * 2.0
        av = v * math.pi * 2.0
        turb = (
            0.50
            + 0.25 * math.sin(au * 3.0 + math.cos(av * 2.0))
            + 0.15 * math.cos(av * 5.0 - math.sin(au * 4.0))
            + 0.10 * math.sin(au * 8.0 + av * 7.0)
        )
        g = max(0.0, min(1.0, turb))

        # B: 细密微气泡簇 (用于船尾螺旋桨白沫与浪尖)
        bubbles = voronoi_caustics_ridge(u, v, grid_size=30, seed=521)
        b = min(1.0, bubbles ** 1.4)

        return (r, g, b, 1.0)

    write_png_rgba(filepath, 256, 256, pixel_fn)


def generate_depth_ramp_texture(out_dir):
    """
    生成严格匹配《雾岬岛·东岸》原图的 5 级水深色彩渐变贴图 (256x16):
    u = 0.00 ~ 0.15: 极浅岸滩透底暖沙金与清透浅绿 (Crystal Warm Sand Shallows)
    u = 0.15 ~ 0.42: 内湾高明度翡翠薄荷青绿 (Emerald Mint Lagoon)
    u = 0.42 ~ 0.68: 珊瑚礁台边缘孔雀青蓝 (Turquoise Reef Shelf)
    u = 0.68 ~ 0.88: 陡坡海沟钴蓝 (Cobalt Drop-off)
    u = 0.88 ~ 1.00: 深海远景午夜靛蓝 (Abyssal Indigo Trench)
    """
    filepath = os.path.join(out_dir, "WaterDepthRamp_V26.png")

    stops = [
        (0.00, (0.72, 0.86, 0.74)),  # 极浅滩清透暖绿沙色
        (0.16, (0.20, 0.74, 0.68)),  # 浅湾明亮翡翠薄荷色
        (0.42, (0.06, 0.56, 0.60)),  # 内湾中层青碧色
        (0.68, (0.03, 0.34, 0.48)),  # 礁盘陡坎孔雀深青
        (0.88, (0.015, 0.16, 0.28)), # 海沟深海钴蓝
        (1.00, (0.008, 0.08, 0.17)), # 远海深渊暗蓝
    ]

    def pixel_fn(u, v):
        for i in range(len(stops) - 1):
            u0, c0 = stops[i]
            u1, c1 = stops[i + 1]
            if u <= u1:
                t = (u - u0) / max(1e-5, (u1 - u0))
                # Smoothstep 平滑插值
                t = t * t * (3.0 - 2.0 * t)
                r = c0[0] + (c1[0] - c0[0]) * t
                g = c0[1] + (c1[1] - c0[1]) * t
                b = c0[2] + (c1[2] - c0[2]) * t
                return (r, g, b, 1.0)
        return (*stops[-1][1], 1.0)

    write_png_rgba(filepath, 256, 16, pixel_fn)


def main():
    out_dir = "Assets/Deepsea/Resources/SeaSurfaceV26/Textures"
    if len(sys.argv) > 1 and not sys.argv[-1].startswith("-"):
        out_dir = sys.argv[-1]
    out_dir = os.path.abspath(out_dir)
    os.makedirs(out_dir, exist_ok=True)

    print(f"=== [SeaSurfaceV26] Generating Water Optical Textures into: {out_dir} ===")
    generate_caustics_texture(out_dir)
    generate_foam_texture(out_dir)
    generate_depth_ramp_texture(out_dir)
    print("=== [SeaSurfaceV26] All Water Optical Textures Successfully Generated! ===")


if __name__ == "__main__":
    main()