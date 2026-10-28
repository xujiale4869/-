import bpy,json
from pathlib import Path
R=Path(__file__).resolve().parents[1]
data=json.loads((R/'exports/ShelfMeshes.json').read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC'
for name in ['REF','HIGH','LOW','COLLISION','SOCKETS','EXPORT']:scene.collection.children.link(bpy.data.collections.new(name))
low=bpy.data.collections['LOW'];floor=min(p['y'] for r in data['meshes'] for p in r['vertices'])
for record in data['meshes']:
 v=[(p['x']-72,96-p['z'],p['y']-floor) for p in record['vertices']]
 tris=[tuple(record['triangles'][i:i+3]) for i in range(0,len(record['triangles']),3)]
 me=bpy.data.meshes.new(record['name']+'_Mesh');me.from_pydata(v,[],tris);me.update();uv=me.uv_layers.new(name='UVMap')
 colour=me.color_attributes.new(name='StoneAndCavity',type='FLOAT_COLOR',domain='POINT')
 for i,c in enumerate(record['colors']):colour.data[i].color=(c['r'],c['g'],c['b'],c['a'])
 for polygon in me.polygons:
  polygon.use_smooth=True
  for li in polygon.loop_indices:
   u=record['uv'][me.loops[li].vertex_index];uv.data[li].uv=(u['x'],u['y'])
 normals=[(n['x'],-n['z'],n['y']) for n in record['normals']];me.normals_split_custom_set_from_vertices(normals)
 mat=bpy.data.materials.get('ShelfGeometryPreview')
 if mat is None:
  mat=bpy.data.materials.new('ShelfGeometryPreview');bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.30,.32,.29,1);bs.inputs['Roughness'].default_value=.88
 me.materials.append(mat);ob=bpy.data.objects.new(record['name'],me);low.objects.link(ob)
scene['coordinateMapping']='Unity(x,y,z) -> Blender(x-72,96-z,y-floor); see exports/shelf-coordinate-contract.json'
scene['scope']='Exact render geometry archive, not collision data. Unity ground/stone material blending remains an engine shader.'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'EN_FracturedReef_master.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in low.objects:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(R/'exports/EN_FracturedReef.glb'),export_format='GLB',use_selection=True,export_apply=True)
(R/'exports/shelf-coordinate-contract.json').write_text(json.dumps({'unityOrigin':[72,floor,96],'mapping':'Blender=(Unity.x-72,96-Unity.z,Unity.y-floor)','scope':'render geometry; shader must be rebuilt in another engine; original collision terrain unchanged','triangles':sum(len(r['triangles'])//3 for r in data['meshes'])},indent=2),encoding='utf-8')
print('FRACTURED_REEF_ARCHIVED')
