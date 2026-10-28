import bpy,bmesh,math,random,json,os
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils import noise

R=Path(__file__).resolve().parents[1]
P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
random.seed(160324);noise.seed_set(160324)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC'
for name in ['REF','HIGH','LOW','COLLISION','SOCKETS','EXPORT']:
    scene.collection.children.link(bpy.data.collections.new(name))
LOW=bpy.data.collections['LOW'];M={};objects=[]
palette={'Rock':(.46,.45,.39),'RockCliff':(.45,.46,.40),'Sand':(.65,.62,.47),'Ground':(.23,.28,.14),'Wood':(.34,.25,.15),'Bark':(.22,.18,.12),'Foliage':(.28,.35,.13),'CoralLavender':(.68,.43,.69),'CoralGold':(.78,.59,.26),'CoralMint':(.34,.60,.45),'CoralSalmon':(.77,.43,.34),'Kelp':(.25,.47,.20),'Iron':(.065,.075,.061),'Rope':(.45,.37,.23),'Glow':(1,.58,.22),'Bird':(.86,.87,.79),'Foam':(.75,.85,.8)}
for name,c in palette.items():
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Roughness'].default_value=.78;M[name]=m

def B(p):return Vector((-p[0],-p[2],p[1]))
def U(p):return (-p.x,p.z,-p.y)
def S(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def lerp(a,b,t):return a+(b-a)*t
def bank(points,z):
    for i in range(1,len(points)):
        if z<=points[i][0]:return lerp(points[i-1][1],points[i][1],S(points[i-1][0],points[i][0],z))
    return points[-1][1]
def shore(z):return bank([(-40,-24),(-25,-20),(-13,-20),(-6,-18),(2,-17),(8,-17.5),(16,-21),(28,-22),(45,-26)],z)+.45*math.sin(z*.48)
def near(z):return bank([(-40,3),(-25,1.5),(-14,4),(-5,8),(4,12),(11,6),(19,-1.5),(29,3),(45,10)],z)
def far(z):return bank([(-40,29),(-20,28),(-6,24),(7,25),(20,25),(45,34)],z)
def height(x,z):
    d=shore(z)-x
    if d>=0:
        h=.12+d*.14+S(2.0,7.0,d)*4.8
        tower=math.hypot(x+24.4,z-4)
        h=lerp(h,3.80,1-S(2.7,5.4,tower))
        path=[(-17.3,.74,2.6),(-18.7,1.4,4.3),(-20.2,2.45,5.5),(-23.6,3.77,6.1)]
        best=100;ph=0
        for a,b in zip(path,path[1:]):
            dx,dz=b[0]-a[0],b[2]-a[2];t=max(0,min(1,((x-a[0])*dx+(z-a[2])*dz)/(dx*dx+dz*dz)));dist=math.hypot(x-a[0]-t*dx,z-a[2]-t*dz)
            if dist<best:best=dist;ph=lerp(a[1],b[1],t)-.20
        h=lerp(h,ph,1-S(.95,2.0,best))
        return h+.18*noise.noise_vector(Vector((x*.19,z*.19,2))).x
    water=-d;h=-.24-water*.16+.18*math.sin(x*.54+math.sin(z*.3))*math.sin(z*.33)
    h=lerp(h,-19,S(near(z)+1.0,near(z)+12,x))
    ridge=-4.8-13*S(2.0,10.5,abs(x-near(z)))
    h=max(h,ridge)
    opposite=-8.5-10*S(1.0,7.0,abs(x-far(z)))
    return max(h,opposite)+.10*noise.noise_vector(Vector((x*.3,z*.3,3))).z

def own(ob,role,group):
    for c in list(ob.users_collection):c.objects.unlink(ob)
    LOW.objects.link(ob);ob.data.materials.clear();ob.data.materials.append(M[role]);ob['role']=role;ob['group']=group;objects.append(ob);return ob
def mesh(name,verts,faces,role,group,uvs=None,colors=None,smooth=True):
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata([B(v) for v in verts],[],[tuple(reversed(f)) for f in faces]);me.update();ob=bpy.data.objects.new(name,me);LOW.objects.link(ob);ob['role']=role;ob['group']=group;me.materials.append(M[role]);objects.append(ob)
    uv=me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        poly.use_smooth=smooth
        for li in poly.loop_indices:
            i=me.loops[li].vertex_index;p=verts[i]
            uv.data[li].uv=uvs[i] if uvs is not None else (p[0]*.25,p[2]*.25)
    if colors:
        col=me.color_attributes.new(name='ArtWeights',type='FLOAT_COLOR',domain='POINT')
        for i,c in enumerate(colors):col.data[i].color=c
    if group=='Coral':
        bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.0000001);bm.to_mesh(me);bm.free();me.update()
    return ob
def apply(ob):
    bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.select_set(False)
def uv_box(ob,wood=False):
    me=ob.data
    if not me.uv_layers:me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        n=poly.normal;axis=max(range(3),key=lambda a:abs(n[a]))
        for li in poly.loop_indices:
            p=me.vertices[me.loops[li].vertex_index].co
            me.uv_layers.active.data[li].uv=(p.x*.35,p.y*.55) if axis==2 else ((p.x*.35,p.z*.55) if axis==1 else (p.y*.55,p.z*.35))
def box(name,p,size,role='Wood',yaw=0,bevel=.035,group='Pier'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=B(p));ob=bpy.context.object;ob.name=name;ob.scale=(size[0],size[2],size[1]);ob.rotation_euler.z=-yaw
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Worn rounded edges','BEVEL');mod.width=bevel;mod.segments=2
        apply(ob)
    uv_box(ob,True)
    if role=='Wood':
        seed=sum((i+1)*ord(c) for i,c in enumerate(name));offset=Vector(((seed*.173)%1,(seed*.317)%1))
        for loop in ob.data.uv_layers.active.data:loop.uv+=offset
    own(ob,role,group);return ob
def tube(name,path,radii,role='Bark',sides=8,group='Pine'):
    pts=[Vector(p) for p in path];verts=[];uv=[]
    for j,p in enumerate(pts):
        axis=(pts[min(j+1,len(pts)-1)]-pts[max(0,j-1)]).normalized();u=axis.cross(Vector((0,1,0)))
        if u.length<.01:u=axis.cross(Vector((1,0,0)))
        u.normalize();v=axis.cross(u)
        for i in range(sides):
            a=i*math.tau/sides;q=p+(u*math.cos(a)+v*math.sin(a))*radii[j];verts.append(tuple(q));uv.append((i/sides,j*.5))
    faces=[]
    for j in range(len(pts)-1):
        for i in range(sides):a=j*sides+i;b=j*sides+(i+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(reversed(range(sides))));faces.append(tuple((len(pts)-1)*sides+i for i in range(sides)))
    return mesh(name,verts,faces,role,group,uv)
def beam(name,a,b,width,role='Wood',group='Pier'):
    a,b=Vector(a),Vector(b);mid=(a+b)*.5
    ob=box(name,tuple(mid),(width,width,(b-a).length),role,0,.02,group)
    axis=B(b)-B(a);ob.rotation_euler=axis.to_track_quat('Y','Z').to_euler();return ob
templates={}
for templateName in ['boulder_01','coastal_cliff_01']:
    previous=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(P/f'Assets/Deepsea/ThirdParty/CoastSlice/{templateName}/{templateName}.fbx'))
    imported=[o for o in bpy.data.objects if o not in previous];candidates=[o for o in imported if o.type=='MESH'];chosen=min(candidates,key=lambda o:len(o.data.polygons));copy=chosen.copy();copy.data=chosen.data.copy();LOW.objects.link(copy)
    bpy.context.view_layer.objects.active=copy;copy.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    if len(copy.data.polygons)>3200:
        dec=copy.modifiers.new('Runtime shape budget','DECIMATE');dec.ratio=3200/len(copy.data.polygons);apply(copy)
    lo=Vector([min(v.co[i] for v in copy.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in copy.data.vertices) for i in range(3)]);centre=(lo+hi)*.5;extent=hi-lo
    for v in copy.data.vertices:v.co=Vector([(v.co[i]-centre[i])/max(.001,extent[i]) for i in range(3)])
    templates[templateName]=copy.data.copy()
    bpy.data.objects.remove(copy,do_unlink=True)
    for ob in imported:bpy.data.objects.remove(ob,do_unlink=True)

# Preserve the previously accepted long clean kelp ribbons and their original UVs.
oldObjects=set(bpy.data.objects);bpy.ops.wm.obj_import(filepath=str(P/'Assets/Deepsea/Resources/CoastMarine/KelpRibbon.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
imported=[o for o in bpy.data.objects if o not in oldObjects];chosen=next(o for o in imported if o.type=='MESH');kelpTemplate=chosen.data.copy()
matrix=chosen.matrix_world;points=[matrix@v.co for v in kelpTemplate.vertices];bottom=min(p.z for p in points);top=max(p.z for p in points);unit=max(.001,top-bottom)
for v,p in zip(kelpTemplate.vertices,points):v.co=(p.x/unit,p.y/unit,(p.z-bottom)/unit)
for ob in imported:bpy.data.objects.remove(ob,do_unlink=True)

def rock(name,p,size,seed,yaw=0,group='Land'):
    rng=random.Random(seed);kind='boulder_01';ob=bpy.data.objects.new(name,templates[kind].copy());LOW.objects.link(ob)
    if group=='Reef':
        top=p[1]+size[1]*.32;bottom=height(p[0],p[2])-.4;high=max(1.0,top-bottom);p=(p[0],(top+bottom)*.5,p[2]);size=(size[0],high,size[2])
    phase=rng.random()*6;shear=rng.uniform(-.14,.14)
    for vert in ob.data.vertices:
        v=vert.co.copy();v.x+=v.z*shear;v.y+=math.sin(v.z*4+phase)*.025
        relief=noise.noise_vector(v*3+Vector((seed*.17,phase,3)))*.018;v+=relief;vert.co=(v.x*size[0],v.y*size[2],v.z*size[1])
    ob.location=B(p);ob.rotation_euler.z=-yaw
    for poly in ob.data.polygons:poly.use_smooth=True
    own(ob,'RockCliff' if kind=='coastal_cliff_01' else 'Rock',group);return ob

def rejected_round_rock(name,p,size,seed,yaw=0,group='Land'):
    rng=random.Random(seed);bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,radius=1)
    ob=bpy.context.object;ob.name=name;phase=rng.random()*7;exponent=rng.uniform(.82,1.05)
    planes=[]
    for j in range(4):
        direction=Vector((rng.uniform(-1,1),rng.uniform(-1,1),rng.uniform(-.3,1))).normalized();planes.append((direction,rng.uniform(.72,.99)))
    for vert in ob.data.vertices:
        v=vert.co.copy();v=Vector([math.copysign(abs(a)**exponent,a) for a in v])
        # Broad planes and a few directed fractures; no tall scaled scan extrusion.
        v*=1+.12*noise.noise_vector(v*2.1+Vector((seed*.13,phase,3))).x
        for normal,distance in planes:
            excess=v.dot(normal)-distance
            if excess>0:v-=normal*excess
        n=noise.noise_vector(v*3.2+Vector((seed*.73,phase,1)))
        relief=.045*n+.018*noise.noise_vector(v*10.0+Vector((phase,3,1)))
        v+=relief
        cut=math.exp(-((v.x+.38*v.y-.21*math.sin(v.z*4+phase))/.075)**2)
        v.z-=cut*.075*(.5+.5*v.z)
        vert.co=(v.x*size[0]*.5,v.y*size[2]*.5,v.z*size[1]*.5)
    ob.location=B(p);ob.rotation_euler.z=-yaw
    for poly in ob.data.polygons:poly.use_smooth=True
    own(ob,'Rock',group);uv_box(ob);return ob

def terrain():
    # Separate editable patches, not a repeated heightfield cliff wall.
    for z0 in range(-40,42,14):
        verts=[];faces=[];col=[];nx=88;nz=28
        for j in range(nz+1):
            z=z0+j*.5
            for i in range(nx+1):
                x=-40+i;h=height(x,z);verts.append((x,h,z));land=1-S(-.8,1.2,x-shore(z));col.append((land,1,1,1))
        for j in range(nz):
            for i in range(nx):
                k=j*(nx+1)+i;faces.extend([(k,k+nx+1,k+1),(k+1,k+nx+1,k+nx+2)])
        mesh(f'EN_CoveSubstrate{z0+40:02d}_LOD0',verts,faces,'Sand','Ground',colors=col)

def geology():
    # Authored headlands: each entry is an intentional primary mass, in metres.
    coast=[(-22,1.9,-8,(7.8,6.8,7.4),.20),(-18.1,1.35,-5.0,(4.6,5.8,5.7),-.45),(-23,3.1,-1,(6.5,5.5,5.8),.15),(-15.7,1.1,8.7,(4.6,5.5,5.2),.5),(-21.5,3.4,12.6,(7.5,6.8,6.3),-.27),(-24.5,2.5,18.2,(8.4,6.7,7.2),.10),(-23,1.3,24.5,(7.6,6.2,7.8),-.30),(-23,1.4,-17,(8.3,6.0,8.0),.45),(-30,3.0,-10,(9,6.6,8.2),.3)]
    for i,(x,y,z,size,a) in enumerate(coast):rock(f'EN_Headland{i:02d}_LOD0',(x,y,z),size,1640+i,a)
    # Boulder feet support these masses and create broken wet silhouettes.
    for i in range(37):
        z=-26+i*1.55;x=shore(z)+random.uniform(-.9,2.2)
        if abs(z-2.5)<3.0:continue
        r=random.uniform(.8,1.7);rock(f'EN_ShoreFoot{i:02d}_LOD0',(x,-.05,z),(r*1.6,r*1.1,r*1.7),1720+i,random.random()*6)
    # Near reef groups separated by genuine saddles; stones never stretched into 20m columns.
    stations=[(-29,6.0),(-23,6.7),(-16,7.4),(-9.5,6.8),(-3.5,5.5),(3.2,3.3),(9.4,4.8),(16.2,5.5),(23.5,4.9),(31,5.0),(38,4.5)]
    for i,(z,radius) in enumerate(stations):
        cx=near(z)
        for j in range(4):
            x=cx+[-2.0,1.8,-.9,4.4][j]+random.uniform(-.4,.4);pz=z+[-1.4,.5,2.2,-1.2][j]
            top=-1.1-[.3,.8,.5,3.9][j]-random.random()*.7
            high=4.0+random.random()*1.5
            size=(radius*random.uniform(.85,1.2),high,radius*random.uniform(.9,1.3))
            rock(f'EN_ReefMass{i:02d}_{j}_LOD0',(x,top-high*.32,pz),size,1820+i*9+j,random.uniform(-.7,.7),'Reef')
        for j in range(3):
            x=cx+4+j*1.5;pz=z+random.uniform(-2,3);h=height(x,pz)
            rock(f'EN_ReefTalus{i:02d}_{j}_LOD0',(x,h+.5,pz),(random.uniform(1.5,3),2.2,random.uniform(2,3.3)),2050+i*7+j,random.random()*5,'Reef')
    for i,z in enumerate([-27,-17,-8,3,14,23,35]):
        for j in range(3):
            x=far(z)+[-1,1.6,4.1][j];top=-2.3-j*.9;high=5.0
            rock(f'EN_FarReef{i:02d}_{j}_LOD0',(x,top-high*.34,z+j*.8),(5.4,high,6.2),2190+i*4+j,random.uniform(-.7,.7),'Reef')
    # Large close foreground rocks frame the water, with dark branches to follow.
    for i,(p,size) in enumerate([((-14,2.3,-23),(9,8,10)),((-7,.5,-28),(8,6,7)),((30,3,-10),(10,11,14)),((24,.3,-25),(8,7,8))]):
        rock(f'EN_Foreground{i}_LOD0',p,size,2390+i,.3*i,'Foreground')
    for i,(p,size) in enumerate([((-29,4.3,2),(6,5,7)),((-27,4.2,13),(5.6,4.8,6)),((-30,4,-9),(7,5,7))]):
        rock(f'EN_InlandOutcrop{i}_LOD0',p,size,2501+i,.45*i,'Land')

def substrate_bvh(includeCoral=False):
    v=[];faces=[];dg=bpy.context.evaluated_depsgraph_get()
    for ob in objects:
        if ob['group'] not in (['Ground','Land','Reef','Coral'] if includeCoral else ['Ground','Land','Reef']):continue
        eo=ob.evaluated_get(dg);me=eo.to_mesh();start=len(v);v.extend([eo.matrix_world@p.co for p in me.vertices]);faces.extend([tuple(start+i for i in p.vertices) for p in me.polygons]);eo.to_mesh_clear()
    return BVHTree.FromPolygons(v,faces)
def surface(tree,x,z):
    p,n,face,d=tree.ray_cast(Vector((-x,-z,30)),Vector((0,0,-1)),70)
    if p is None:return (x,height(x,z),z),(0,1,0)
    return U(p),U(n)

def shoreline_foam():
    gverts=[];gfaces=[];guv=[];seen=set();dg=bpy.context.evaluated_depsgraph_get()
    for ob in list(objects):
        if ob.get('group') not in ['Land','Foreground']:continue
        eo=ob.evaluated_get(dg);me=eo.to_mesh();me.calc_loop_triangles();v=[Vector(U(eo.matrix_world@p.co)) for p in me.vertices]
        for tri in me.loop_triangles:
            points=[v[i] for i in tri.vertices];hits=[]
            for a,b in zip(points,points[1:]+points[:1]):
                if (a.y+.22)*(b.y+.22)<0:hits.append(a.lerp(b,(-.22-a.y)/(b.y-a.y)))
            if len(hits)!=2 or (hits[0]-hits[1]).length<.025:continue
            a,b=hits;key=tuple(sorted(tuple(round(q[k],3) for k in [0,2]) for q in hits))
            if key in seen:continue
            seen.add(key);normal=Vector(U(eo.matrix_world.to_3x3()@tri.normal));normal.y=0
            if normal.length<.01:continue
            normal.normalize();width=.19+.10*math.sin(a.x*1.7+a.z*.8);base=len(gverts)
            a.y=-.205;b.y=-.205;gverts.extend([tuple(a),tuple(b),tuple(b+normal*width),tuple(a+normal*width)]);gfaces.append((base,base+1,base+2,base+3));guv.extend([(0,0),((b-a).length*3,0),((b-a).length*3,1),(0,1)])
        eo.to_mesh_clear()
    if gverts:mesh('EN_ShoreContactFoam_LOD0',gverts,gfaces,'Foam','Foam',guv)

def pier():
    for i in range(32):
        t=i/31;x=lerp(-17.1,-2.0,t);z=2.4+.48*math.sin(t*math.pi)
        plank=box(f'AR_PierTread{i:02d}_LOD0',(x,.63+math.sin(i*1.93)*.018,z+random.uniform(-.045,.045)),(.455,.21,3.05+random.uniform(-.19,.17)),'Wood',random.uniform(-.015,.015),.035)
        for v in plank.data.vertices:
            v.co.z+=math.sin(v.co.y*2.5+i)*.014
    for side in [-1,1]:
        beam(f'AR_Stringer{side}',(-17.1,.23,2.4+side*1.14),(-1.7,.23,2.4+side*1.14),.28)
        for i in range(7):
            x=-16.7+i*2.45;z=2.4+side*1.36+.35*math.sin(i/6*math.pi);bottom=height(x,z)-.5
            tube(f'AR_PierPost{side}_{i}',[(x,bottom,z),(x,1.18+random.uniform(-.08,.10),z)],[.16,.145],'Wood',10,'Pier')
            for q in [0,1]:
                y=1.01+q*.07
                bpy.ops.mesh.primitive_torus_add(major_radius=.161,minor_radius=.018,major_segments=16,minor_segments=5,location=B((x,y,z)))
                ob=bpy.context.object;ob.name=f'AR_PostBinding{side}_{i}_{q}';own(ob,'Rope','Pier');uv_box(ob)
            if i<6:beam(f'AR_PierBrace{side}_{i}',(x,.15,z),(x+2.4,bottom*.5,z),.11)
    path=[(-17.3,.74,2.6),(-18.7,1.4,4.3),(-20.2,2.45,5.5),(-23.6,3.77,6.1)]
    for segment in range(len(path)-1):
        a,b=Vector(path[segment]),Vector(path[segment+1]);direction=b-a;count=max(4,int(direction.length/.28))
        yaw=math.atan2(direction.x,direction.z)
        for i in range(count):
            p=a.lerp(b,(i+.5)/count);p.y-=.10;box(f'AR_HillWalk{segment}_{i}',tuple(p),(1.7,.18,direction.length/count+.035),'Wood',yaw,.025,'Pier')
        side=Vector((math.cos(yaw)*.98,0,-math.sin(yaw)*.98))
        for i in range(3):
            p=a.lerp(b,i/2)+side;tube(f'AR_RailPost{segment}_{i}',[tuple(p),tuple(p+Vector((0,1,0)))],[.052,.041],'Wood',8,'Pier')
        beam(f'AR_HillRail{segment}',tuple(a+side+Vector((0,.91,0))),tuple(b+side+Vector((0,.91,0))),.08)
    for i,(x,z) in enumerate([(-2.4,3.68),(-14.5,1.10)]):
        tube(f'AR_LanternPole{i}',[(x,.75,z),(x,2.8,z),(x+.5,2.65,z)],[.05,.04,.025],'Wood',8,'Pier')
        box(f'AR_LanternLight{i}',(x+.48,2.29,z),(.14,.25,.14),'Glow',0,.02)
        box(f'AR_LanternRoof{i}',(x+.48,2.45,z),(.25,.07,.25),'Iron')
        for sx in [-1,1]:
            for sz in [-1,1]:tube(f'AR_LanternFrame{i}{sx}{sz}',[(x+.48+sx*.09,2.12,z+sz*.09),(x+.48+sx*.09,2.45,z+sz*.09)],[.013,.013],'Iron',5,'Pier')

# Remaining authored vegetation and runtime export are appended by 02_life.py.
terrain();geology();base_tree=substrate_bvh();pier();shoreline_foam()
exec((R/'build/02_life.py').read_text(encoding='utf-8'))
