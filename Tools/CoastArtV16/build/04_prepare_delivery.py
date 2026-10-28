"""Pack source textures; GLB is an environment geometry/material preview, not the Unity water renderer."""
import bpy,json,hashlib,os
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1]
P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
bpy.ops.wm.open_mainfile(filepath=str(R/'EN_MistCapeShowcase_master.blend'))
maps={
 'Rock':P/'Assets/Deepsea/ThirdParty/CoastSlice/boulder_01/boulder_01_diff_2k.jpg',
 'Wood':R/'textures/CoastalTimber-Albedo.png',
 'Bark':P/'Assets/Deepsea/ThirdParty/CoastSlice/island_tree_01/island_tree_01_diff_1k.jpg',
 'Foliage':R/'textures/CoastalPine-Atlas.png',
}
for role,path in maps.items():
    m=bpy.data.materials.get(role);nt=m.node_tree;bs=nt.nodes.get('Principled BSDF')
    for link in list(bs.inputs['Base Color'].links):nt.links.remove(link)
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path),check_existing=True)
    nt.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if role=='Foliage':nt.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);m.surface_render_method='DITHERED'
for role,cell in [('CoralLavender',(0,.5)),('CoralGold',(.5,.5)),('CoralMint',(0,0)),('CoralSalmon',(.5,0))]:
    m=bpy.data.materials[role];nt=m.node_tree;bs=nt.nodes.get('Principled BSDF');coord=nt.nodes.new('ShaderNodeTexCoord')
    mul=nt.nodes.new('ShaderNodeVectorMath');mul.operation='MULTIPLY';mul.inputs[1].default_value=(2,2,1);nt.links.new(coord.outputs['UV'],mul.inputs[0])
    frac=nt.nodes.new('ShaderNodeVectorMath');frac.operation='FRACTION';nt.links.new(mul.outputs[0],frac.inputs[0])
    scale=nt.nodes.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(.494,.494,1);nt.links.new(frac.outputs[0],scale.inputs[0])
    offset=nt.nodes.new('ShaderNodeVectorMath');offset.operation='ADD';offset.inputs[1].default_value=(*[x+.003 for x in cell],0);nt.links.new(scale.outputs[0],offset.inputs[0])
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'textures/CoralTissue-Atlas.png'),check_existing=True);nt.links.new(offset.outputs[0],tex.inputs['Vector']);nt.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
bpy.ops.file.pack_all()
bpy.context.scene['deliveryNote']='Packed editable environment. Tower, rowboat, fisherman, animated fish, original kelp material, water and exact lighting are authored in Unity CoastArtShowcase.unity; GLB is a geometry/material preview.'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'EN_MistCapeShowcase_master.blend'))
objects=[o for o in bpy.data.collections['LOW'].objects if o.type=='MESH']
points=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
rise=-lo[2]
# GLB ground is zero for the standard round-trip tool. Original master/Unity coordinates stay unchanged.
for o in objects:o.location.z+=rise
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(R/'exports/EN_MistCapeShowcase.glb'),export_format='GLB',use_selection=True,export_apply=True,export_cameras=False,export_lights=False,export_animations=False)
manifest=json.loads((R/'exports/scene-manifest.json').read_text())
manifest.update({
 'units':'metres','masterAxes':'Blender Z up, Unity(x,y,z) -> Blender(-x,-z,y)',
 'masterBounds':{'min':lo,'max':hi},'glbVerticalOffsetM':rise,
 'restoreGlb':'glTF Y up -> Unity world: X=-glTF.X, Y=glTF.Y-glbVerticalOffsetM, Z=glTF.Z; reverse triangle winding for the X handedness flip. Exact Unity JSON needs no conversion.',
 'materialScope':'GLB approximates base colour; Blender packs generated and scanned diffuse textures. Exact water, triplanar ground, coral atlas addressing, caustics, kelp and lighting are in Unity, not portable GLB shaders.',
 'textureContract':'Generated colour maps sRGB; pine RGBA alpha clip .4 in Unity; existing rock OpenGL normal Non-Color in source/Unity normal import; roughness constants .78-.90. No new normal/roughness bake claimed.',
 'texelDensity':'Source rock UV and 2K scan retained. All three generated source maps are 1254x1254; pine/coral quadrant627px before importer scaling. Repeating UV/card sizing, no uniform unique-UV texel density claimed.',
 'lodScope':'Single LOD environment visual prototype, grouped/cullable in Unity. No shipping LOD transition acceptance.',
 'inferred':'Unseen geometry, scale, back sides, underwater floor and tree branching inferred from one perspective concept.',
 'shaderBaseline':'CoastFoundationWater SHA256 4a7fdfa9164a208a8136c192cb3284ea335d2b60e5ec03f7fe7a652ac02c015c; _Refraction .0022'
})
(R/'exports/asset-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('DELIVERY_PACKED',len(objects),'objects',rise,'m GLB offset')
