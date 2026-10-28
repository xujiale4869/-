# Shallow shore integration V1
Actual upstream reuse:
- Unity-Technologies/boat-attack-water, revision 45d6e8c52a9f615fd6b83a91cdd0e9fda9ea86d3 (existing pinned source cache).
- Runtime/Textures/WaterSurface_single.tif copied as Resources/Water/WaterCaustics.tif.
- Runtime/Shaders/Caustics.shader preserved as Caustics.shader.txt.
- BayCaustics.hlsl adapts the packed-map sampling/driver for direct application on submerged ground.
- License: Unity Companion License; see LICENSE.md.
Original work:
- CoastalGround.shader: height-based sand/grass/wet-shore palette and seabed shading.
- Seagrass.shader and ShoreHabitat.cs: tapered leaf meshes, bounded current sway, continuous refined sand bed and submerged stones.
- Ocean.shader: controlled seabed transmission, sun highlights, east-shore surf fronts and retained local fish ripples.
No Descent or third-party grass package was imported.
No new collision geometry or navigation blockers are added.
