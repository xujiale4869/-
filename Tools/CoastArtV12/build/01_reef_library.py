"""Reproducible original reef meshes, using Blender's GPL Rock Generator as a tool.
The scene reference is a perspective concept; all metre dimensions/unseen faces are inferred.
"""
import bpy, bmesh, json, math, random, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
import numpy as np
from add_mesh_rocks import rockgen

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
for name in ['REF','HIGH','LOW','RIG_CTRL','RIG_DEF','COLLISION','SOCKETS','EXPORT']:
    if name not in bpy.data.collections:scene.collection.children.link(bpy.data.collections.new(name))
low=bpy.data.collections['LOW'];records=[]
for index in range(6):
    seed=231200+index*137;random.seed(seed);np.random.seed(seed)
    # Metres inferred: boulders 2 x 2 x 1.6, cliff modules 2 x 2 x 4 before normalization.
    height=1.6 if index<3 else 4.0
    objects=rockgen.generateRocks(bpy.context,[1.3,2.4],0,[1.4,2.5],0,[height*.75,height*1.2],0,
        [1,1,1],3,3,18 if index<3 else 12,28,.12,1,1,seed,False,False,False)
    obj=objects[0];bpy.context.view_layer.objects.active=obj
    for mod in list(obj.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    # The legacy tool makes folded/intersecting sheets. Keep its primary mass as a clean hull,
    # subdivide that mass and add bounded erosion instead of shipping those intersections.
    hull=bmesh.new()
    for vertex in obj.data.vertices:hull.verts.new(vertex.co)
    bmesh.ops.convex_hull(hull,input=list(hull.verts),use_existing_faces=False)
    unused=[v for v in hull.verts if not v.link_faces]
    if unused:bmesh.ops.delete(hull,geom=unused,context='VERTS')
    hull.to_mesh(obj.data);hull.free()
    refine=obj.modifiers.new('Erosion support grid','SUBSURF');refine.subdivision_type='SIMPLE';refine.levels=2
    bpy.ops.object.modifier_apply(modifier=refine.name)
    from mathutils import noise
    obj.data.update()
    for vertex in obj.data.vertices:
        value=noise.noise_vector(vertex.co*3.8+Vector((index*2.7,0,0))).x
        vertex.co+=vertex.normal*(value*.035)
    mesh=obj.data;mins=Vector(tuple(min(v.co[k] for v in mesh.vertices) for k in range(3)))
    maxs=Vector(tuple(max(v.co[k] for v in mesh.vertices) for k in range(3)));size=maxs-mins
    for v in mesh.vertices:
        x=(v.co.x-mins.x)/size.x*2-1;y=(v.co.y-mins.y)/size.y*2-1;z=(v.co.z-mins.z)/size.z
        # A broad worn shoulder instead of a needle; retain the generated asymmetry.
        if index>=3:
            z=1-max(0,1-min(1,max(0,z)))**1.15
            ledge=.018*math.sin(z*23+index)+.009*math.sin(z*43+index*1.7)
            x*=1+ledge;y*=1+ledge
        v.co=(x,y,z)
    # Normalize exact envelope after shaping: used to map onto existing visual/physics bounds.
    lo=Vector(tuple(min(v.co[k] for v in mesh.vertices) for k in range(3)))
    hi=Vector(tuple(max(v.co[k] for v in mesh.vertices) for k in range(3)))
    for v in mesh.vertices:v.co=((v.co.x-lo.x)/(hi.x-lo.x)*2-1,(v.co.y-lo.y)/(hi.y-lo.y)*2-1,(v.co.z-lo.z)/(hi.z-lo.z))
    dec=obj.modifiers.new('Runtime triangle budget','DECIMATE');dec.ratio=min(1,1600/max(1,sum(len(p.vertices)-2 for p in mesh.polygons)))
    bpy.ops.object.modifier_apply(modifier=dec.name)
    mesh=obj.data;bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.00001)
    zero=[f for f in bm.faces if f.calc_area()<1e-9]
    if zero:bmesh.ops.delete(bm,geom=zero,context='FACES')
    # Separate volumes which touch at a shared edge; they are render meshes, never collision.
    junctions=[e for e in bm.edges if len(e.link_faces)>2]
    if junctions:bmesh.ops.split_edges(bm,edges=junctions)
    # Remove tiny disconnected chips created by displacement/remeshing.
    remaining=set(bm.faces);islands=[]
    while remaining:
        todo=[remaining.pop()];island=[]
        while todo:
            face=todo.pop();island.append(face)
            for edge in face.edges:
                for neighbour in edge.link_faces:
                    if neighbour in remaining:remaining.remove(neighbour);todo.append(neighbour)
        islands.append(island)
    largest=max(sum(f.calc_area() for f in island) for island in islands)
    chips=[f for island in islands if sum(f.calc_area() for f in island)<largest*.008 for f in island]
    if chips:bmesh.ops.delete(bm,geom=chips,context='FACES')
    bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    # Canonical floor after reduction; preserve UVs even though Unity uses world triplanar mapping.
    floor=min(v.co.z for v in mesh.vertices)
    for v in mesh.vertices:v.co.z-=floor
    uv=mesh.uv_layers.new(name='UVMap')
    for face in mesh.polygons:
        face.use_smooth=True
        for loop in face.loop_indices:
            p=mesh.vertices[mesh.loops[loop].vertex_index].co;uv.data[loop].uv=(p.x*.5+.5,p.y*.5+.5)
    mesh.update();mesh.calc_loop_triangles()
    obj.name=('EN_ReefBoulder' if index<3 else 'EN_ReefCliff')+str(index%3+1)+'_LOD0';mesh.name=obj.name+'_Mesh'
    mat=bpy.data.materials.new(obj.name+'_Stone');mat.diffuse_color=(.32,.35,.32,1)
    bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.32,.35,.32,1);bs.inputs['Roughness'].default_value=.88;mesh.materials.append(mat)
    for coll in list(obj.users_collection):coll.objects.unlink(obj)
    low.objects.link(obj)
    def vec(v):return {'x':round(v[0],6),'y':round(v[2],6),'z':round(-v[1],6)}
    record={'name':obj.name,'vertices':[vec(v.co) for v in mesh.vertices],
      'normals':[vec(v.normal) for v in mesh.vertices],
      'uv':[{'x':round(v.co.x*.5+.5,6),'y':round(v.co.y*.5+.5,6)} for v in mesh.vertices],
      'triangles':[i for tri in mesh.loop_triangles for i in tri.vertices]}
    records.append(record);obj.location=(index%3*3.2,index//3*3.2,0)
    obj['source_tool']='blender/blender-addons add_mesh_rocks b1fbf73a08cfb68b14bb328590a9bb6602d96916'
    obj['seed']=seed;obj['inferred']='Dimensions, underside, reef erosion profile inferred from perspective concept'
    print('REEF_MESH',obj.name,len(record['triangles'])//3)
(ROOT/'exports'/'ReefLibrary.json').write_text(json.dumps({'meshes':records},separators=(',',':')),encoding='utf-8')
(ROOT/'exports'/'mesh-audit.json').write_text(json.dumps({'objects':[{'name':r['name'],'vertices':len(r['vertices']),'triangles':len(r['triangles'])//3} for r in records]},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'EN_ReefLibrary_master.blend'))
