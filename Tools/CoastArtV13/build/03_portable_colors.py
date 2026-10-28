import bpy
from pathlib import Path
R=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(R/'EN_CoralHabitat_master.blend'))
for obj in bpy.data.objects:
    if obj.type!='MESH':continue
    attr=obj.data.color_attributes.get('CoralColor')
    for item in attr.data:
        c=item.color;item.color=(max(c[0],.001)**2.2,max(c[1],.001)**2.2,max(c[2],.001)**2.2,c[3])
    nt=obj.data.materials[0].node_tree;vcol=next(n for n in nt.nodes if n.type=='VERTEX_COLOR');bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');nt.links.new(vcol.outputs['Color'],bs.inputs['Base Color'])
bpy.ops.wm.save_as_mainfile(filepath=str(R/'EN_CoralHabitat_delivery.blend'))
print('PORTABLE_LINEAR_VERTEX_COLORS_READY')
