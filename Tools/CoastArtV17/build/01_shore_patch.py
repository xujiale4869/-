import bpy,bmesh,math,json,random,os
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R=Path(__file__).resolve().parents[1];W=Path(os.environ.get('COAST_WORKSPACE_ROOT',r'C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔'));P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
bpy.ops.wm.open_mainfile(filepath=str(W/'art-v16/EN_MistCapeShowcase_master.blend'))
def U(p):return (-p.x,p.z,-p.y)
def B(p):return Vector((-p[0],-p[2],p[1]))
def smooth(a,b,t):q=max(0,min(1,(t-a)/(b-a)));return q*q*(3-2*q)
def bvh(obs):
    vs=[];fs=[]
    for ob in obs:
        start=len(vs);vs.extend(ob.matrix_world@v.co for v in ob.data.vertices);fs.extend(tuple(start+i for i in f.vertices) for f in ob.data.polygons)
    return BVHTree.FromPolygons(vs,fs)
original=list(bpy.data.collections['LOW'].objects);ground=bvh([o for o in original if o.get('group')=='Ground'])
def floor(x,z):
    p,n,i,d=ground.ray_cast(B((x,40,z)),Vector((0,0,-1)),100)
    return p.z if p is not None else -20
focusNames=['EN_Headland02_LOD0','EN_Headland03_LOD0','EN_Headland04_LOD0','EN_InlandOutcrop0_LOD0']
selected=[o for o in original if o.name in focusNames];land=[o for o in original if o.get('group')=='Land'];changed=0;maxMove=0
for ob in selected:
    centre=Vector(U(ob.matrix_world.translation));inv=ob.matrix_world.inverted()
    for v in ob.data.vertices:
        wp=ob.matrix_world@v.co;up=Vector(U(wp));h=floor(up.x,up.z);gap=up.y-h
        if -.10<gap<.35:
            delta=Vector((up.x-centre.x,0,up.z-centre.z));delta.normalize();strength=(1-smooth(-.10,.35,gap))*.075;new=up+delta*strength;new.y-=strength*.55
            maxMove=max(maxMove,(new-up).length);v.co=inv@B(new);changed+=1
    ob.data.update();ob['role']='FocusRock';ob['group']='ShoreFocus'
    col=ob.data.color_attributes.get('ArtWeights') or ob.data.color_attributes.new(name='ArtWeights',type='FLOAT_COLOR',domain='POINT')
    for i,v in enumerate(ob.data.vertices):
        x,y,z=U(ob.matrix_world@v.co);contact=1-smooth(.02,.62,y-floor(x,z));col.data[i].color=(contact,.82,1,1)
    ob.data.name=ob.name+'_Mesh'
# Low continuous mineral/soil transition at each selected rock's actual ground intersection.
def apron(ob,index):
    tree=bvh([ob]);points=[Vector(U(ob.matrix_world@v.co)) for v in ob.data.vertices];cx=sum(p.x for p in points)/len(points);cz=sum(p.z for p in points)/len(points);radius=max(math.hypot(p.x-cx,p.z-cz) for p in points)+.2
    v=[];f=[];uv=[];cols=[];count=64
    for i in range(count):
        a=i*math.tau/count;dx,dz=math.cos(a),math.sin(a);last=None
        for step in range(42):
            rr=radius*step/41;x,z=cx+dx*rr,cz+dz*rr;p,n,ix,d=tree.ray_cast(B((x,40,z)),Vector((0,0,-1)),100)
            if p is not None and p.z>floor(x,z)-.03:last=rr
        rr=last if last is not None else 0
        for ring,offset in enumerate([-.07,.15,.43]):
            x,z=cx+dx*max(0,rr+offset),cz+dz*max(0,rr+offset);y=floor(x,z)+[.026,.034,.012][ring]
            v.append(B((x,y,z)));uv.append((x*.31,z*.31));cols.append(([.90,.48,0][ring],1,1,1))
    for i in range(count):
        j=(i+1)%count
        for k in range(2):
            q=(i*3+k,j*3+k,j*3+k+1,i*3+k+1)
            if min(v[t].z for t in q)>.08:f.append(q)
    me=bpy.data.meshes.new(f'EN_ContactApron{index}_Mesh');me.from_pydata(v,[],[tuple(reversed(q)) for q in f]);me.update();layer=me.uv_layers.new(name='UVMap')
    for face in me.polygons:
        face.use_smooth=True
        for li in face.loop_indices:layer.data[li].uv=uv[me.loops[li].vertex_index]
    col=me.color_attributes.new(name='ArtWeights',type='FLOAT_COLOR',domain='POINT')
    for i,c in enumerate(cols):col.data[i].color=c
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.0000001)
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bm.to_mesh(me);bm.free();me.update()
    soil=bpy.data.materials.get('ContactSoil')
    if soil is None:soil=bpy.data.materials.new('ContactSoil');soil.diffuse_color=(.15,.18,.08,1)
    me.materials.append(soil)
    o=bpy.data.objects.new(f'EN_ContactApron{index}_LOD0',me);bpy.data.collections['LOW'].objects.link(o);o['role']='ContactSoil';o['group']='ShoreContact';return o
aprons=[apron(o,i) for i,o in enumerate(selected)]
for ob in list(aprons):
    if not ob.data.polygons:aprons.remove(ob);bpy.data.objects.remove(ob,do_unlink=True)
# Two physically bevelled masonry courses at the existing tower footing. Front door remains open.
blocks=[];rng=random.Random(17027)
for row in range(2):
    for j in range(24):
        a=(j+(row%2)*.5)*math.tau/24;b=a+math.tau/24-.012
        mid=(a+b)/2
        if math.sin(mid)<-.90 and abs(math.cos(mid))<.30:continue
        inner=1.51-row*.025;outer=1.71-row*.045+rng.uniform(-.025,.025);z0=row*.215+.018;z1=z0+.195+rng.uniform(-.012,.012)
        # Source tower Blender x/y -> world(-.9*x,3.8+.9*z,4+.9*y).
        vs=[]
        for z in [z0,z1]:
            for radius,angle in [(inner,a),(outer,a),(outer,b),(inner,b)]:vs.append(B((-24.4-.9*radius*math.cos(angle),3.8+.9*z,4+.9*radius*math.sin(angle))))
        fs=[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
        me=bpy.data.meshes.new(f'AR_BaseMasonry{row}_{j}_Mesh');me.from_pydata(vs,[],fs);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();o=bpy.data.objects.new(f'AR_BaseMasonry{row}_{j}_LOD0',me);bpy.data.collections['LOW'].objects.link(o)
        bpy.context.view_layer.objects.active=o;o.select_set(True);mod=o.modifiers.new('Worn stone edges','BEVEL');mod.width=.014;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
        uv=o.data.uv_layers.new(name='UVMap')
        for face in o.data.polygons:
            for li in face.loop_indices:
                x,y,z=U(o.data.vertices[o.data.loops[li].vertex_index].co);uv.data[li].uv=(x*.6+y*.2,z*.6+y*.4)
        o['role']='FootingStone';o['group']='TowerFooting';blocks.append(o)
        o.data.materials.append(bpy.data.materials['Rock'])
owned=land+aprons+blocks
# Retain the rest only in the historical master; scoped source remains editable by individual object.
for ob in list(bpy.data.objects):
    if ob not in owned:bpy.data.objects.remove(ob,do_unlink=True)
dg=bpy.context.evaluated_depsgraph_get();records={};tris=0
for ob in owned:
    eo=ob.evaluated_get(dg);me=eo.to_mesh();me.calc_loop_triangles();uv=me.uv_layers.active;normal=eo.matrix_world.to_3x3().inverted().transposed();role=ob.get('role','Rock');group=ob.get('group','Land');centre=sum((eo.matrix_world@v.co for v in me.vertices),Vector())/len(me.vertices);zone=int(U(centre)[2]//14)
    key=(role,group,zone);out=records.setdefault(key,{'name':f'EN_V17_{group}_{role}_{zone+4:02d}_LOD0','material':role,'group':group,'vertices':[],'normals':[],'uv':[],'colors':[],'triangles':[]});col=me.color_attributes.get('ArtWeights')
    for tri in me.loop_triangles:
        for li in reversed(tri.loops):
            vi=me.loops[li].vertex_index;v=U(eo.matrix_world@me.vertices[vi].co);n=U((normal@me.corner_normals[li].vector).normalized());u=uv.data[li].uv if uv else (v[0]*.3,v[2]*.3);c=tuple(col.data[vi].color) if col else (0,1,1,1)
            out['vertices'].append(dict(zip('xyz',v)));out['normals'].append(dict(zip('xyz',n)));out['uv'].append(dict(zip('xy',u)));out['colors'].append(dict(zip('rgba',c)));out['triangles'].append(len(out['triangles']))
        tris+=1
    eo.to_mesh_clear()
data={'meshes':list(records.values()),'focusObjects':focusNames,'contactVertexChanges':changed,'maxVertexMoveM':maxMove,'triangles':tris,'masonryBlocks':len(blocks),'source':'v16 Land replaced with same evaluated objects;4 focus rocks get contact sampling and material. All other scene geometry from v16 unchanged.'}
for dest in [R/'exports/LandPatch.json',P/'Assets/Deepsea/ThirdParty/CoastArtV17/LandPatch.json']:dest.write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
(R/'review/patch-audit.json').write_text(json.dumps({k:v for k,v in data.items() if k!='meshes'},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(R/'EN_LighthouseShore_master.blend'))
bpy.ops.object.select_all(action='SELECT');shift=-min((o.matrix_world@v.co).z for o in owned for v in o.data.vertices)
for o in owned:o.location.z+=shift
bpy.ops.export_scene.gltf(filepath=str(R/'exports/EN_LighthouseShore.glb'),export_format='GLB',use_selection=True,export_apply=True,export_cameras=False,export_lights=False)
(R/'exports/asset-manifest.json').write_text(json.dumps({'triangles':tris,'batches':len(records),'glbVerticalOffsetM':shift,'sourceScope':'Land rocks,4 contact lips,43 foundation stones only; lighthouse full mesh and exact weathering remain in Unity scene.','mapping':'Master Blender(-UnityX,-UnityZ,UnityY); GLB Y up with vertical offset; JSON exact Unity vertices.','materialScope':'GLB geometry/partial material preview; new PBR material logic lives in isolated Unity shaders.'},indent=2),encoding='utf-8')
print('SHORE_PATCH_OK',tris,len(records),changed,maxMove)
