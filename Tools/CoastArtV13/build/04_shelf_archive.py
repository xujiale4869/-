"""Editable archive of the exact engine-generated field; origin offset is documented."""
import bpy,bmesh,json
from pathlib import Path
R=Path(__file__).resolve().parents[1]
data=json.loads((R/'exports/ShelfMeshes.json').read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC'
for name in ['REF','HIGH','LOW','RIG_CTRL','RIG_DEF','COLLISION','SOCKETS','EXPORT']:
    scene.collection.children.link(bpy.data.collections.new(name))
low=bpy.data.collections['LOW'];points=[v['y'] for m in data['meshes'] for v in m['vertices']];floor=min(points)
for record in data['meshes']:
    used=sorted(set(record['triangles']));mapping={old:new for new,old in enumerate(used)}
    vertices=[(record['vertices'][i]['x']-72,96-record['vertices'][i]['z'],record['vertices'][i]['y']-floor) for i in used]
    triangles=[tuple(mapping[i] for i in record['triangles'][k:k+3]) for k in range(0,len(record['triangles']),3)]
    mesh=bpy.data.meshes.new(record['name']+'_Mesh');mesh.from_pydata(vertices,[],triangles);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for polygon in mesh.polygons:
        polygon.use_smooth=True
        for loop in polygon.loop_indices:
            old=used[mesh.loops[loop].vertex_index];q=record['uv'][old];uv.data[loop].uv=(q['x'],q['y'])
    mat=bpy.data.materials.new(record['name']+'_Stone');mat.diffuse_color=(.24,.32,.33,1);bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.24,.32,.33,1);bs.inputs['Roughness'].default_value=.88;mesh.materials.append(mat)
    obj=bpy.data.objects.new(record['name'],mesh);low.objects.link(obj)
scene['engineOrigin']=(72,floor,96);scene['coordinateMapping']='Unity(x,y,z) -> Blender(x-72,96-z,y-floor); exact original data in ShelfMeshes.json'
scene['source']='FastNoiseLite 1.1.1; project-authored shelf profile, original physical terrain retained'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'EN_ContinuousShelf_master.blend'))
(R/'exports/shelf-coordinate-contract.json').write_text(json.dumps({'unityOrigin':[72,floor,96],'mapping':'blender=(unity.x-72,96-unity.z,unity.y-floor)','source':'exact engine mesh archive, not the collision terrain','inferred':data['inferred']},indent=2),encoding='utf-8')
print('SHELF_MASTER_SAVED',sum(len(m['triangles'])//3 for m in data['meshes']))
