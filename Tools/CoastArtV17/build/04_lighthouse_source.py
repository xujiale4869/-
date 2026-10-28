import bpy,json,os
from pathlib import Path
R=Path(__file__).resolve().parents[1]
W=Path(os.environ.get('COAST_WORKSPACE_ROOT',r'C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔'))
bpy.ops.wm.open_mainfile(filepath=str(W/'art-v14/AR_CoastLighthouse_master.blend'))
m=bpy.data.materials['Plaster'];nt=m.node_tree;bs=nt.nodes.get('Principled BSDF')
for link in list(bs.inputs['Base Color'].links):nt.links.remove(link)
tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'textures/Limewash-Albedo.png'));nt.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
m.diffuse_color=(.84,.86,.83,1);bs.inputs['Roughness'].default_value=.88
for link in list(bs.inputs['Roughness'].links):nt.links.remove(link)
bpy.ops.file.pack_all();bpy.context.scene['scope']='v17 editable lighthouse source: same v14 forms and new plaster albedo. Exact localized chips/rain/metal shading and added world-space footing are in CoastArtRefined.unity / scoped shore blend.'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'AR_LighthouseMaterial_master.blend'))
print('LIGHTHOUSE_SOURCE_PACKED')
