import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'AR_CoastLighthouse_master.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in list(bpy.data.collections['LOW'].objects):
 if o.type!='MESH':continue
 bpy.context.view_layer.objects.active=o;o.select_set(True)
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
 mod=o.modifiers.new('Delivery triangulation','TRIANGULATE');mod.keep_custom_normals=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'AR_CoastLighthouse_delivery.blend'))
print('LIGHTHOUSE_TRIANGULATED_DELIVERY_SAVED')
