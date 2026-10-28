# Executed in the calibrated Blender scene created by 01_scene.py.
class G:
    def __init__(self):self.v=[];self.f=[];self.uv=[];self.col=[]
    def tube(self,pts,radii,sides=6):
        pts=[Vector(p) for p in pts];start=len(self.v)
        for j,p in enumerate(pts):
            axis=(pts[min(j+1,len(pts)-1)]-pts[max(0,j-1)]).normalized();u=axis.cross(Vector((0,1,0)))
            if u.length<.01:u=axis.cross(Vector((1,0,0)))
            u.normalize();v=axis.cross(u)
            for i in range(sides):
                a=i*math.tau/sides;q=p+(u*math.cos(a)+v*math.sin(a))*radii[j]
                self.v.append(tuple(q));self.uv.append((i/sides,j*.3));self.col.append((1,1,1,1))
        for j in range(len(pts)-1):
            for i in range(sides):a=start+j*sides+i;b=start+j*sides+(i+1)%sides;self.f.append((a,b,b+sides,a+sides))
        self.f.append(tuple(start+i for i in reversed(range(sides))));self.f.append(tuple(start+(len(pts)-1)*sides+i for i in range(sides)))
    def quad(self,pts,uvs=None,weight=1):
        start=len(self.v);self.v.extend(pts);self.f.append((start,start+3,start+2,start+1));self.uv.extend(uvs or [(0,0),(1,0),(1,1),(0,1)]);self.col.extend([(1,1,1,weight)]*4)
    def build(self,name,role,group):return mesh(name,self.v,self.f,role,group,self.uv,self.col)

def canopy(name,p,w,d,variant,yaw=0,group='Pine'):
    verts=[];uv=[];faces=[];cx,cy,cz=p;nx=6;nz=4
    u0=(variant%2)*.5;v0=(1-variant//2)*.5
    for j in range(nz+1):
        v=j/nz
        for i in range(nx+1):
            u=i/nx;lx=(u-.5)*w;lz=(v-.5)*d
            x=cx+math.cos(yaw)*lx+math.sin(yaw)*lz;z=cz-math.sin(yaw)*lx+math.cos(yaw)*lz
            y=cy+.36*math.sin(u*math.pi)*math.sin(v*math.pi)+.14*math.sin(lx*1.3+cz)
            verts.append((x,y,z));uv.append((u0+.003+u*.494,v0+.003+v*.494))
    for j in range(nz):
        for i in range(nx):k=j*(nx+1)+i;faces.append((k,k+nx+1,k+nx+2,k+1))
    return mesh(name,verts,faces,'Foliage',group,uv)

def pine(index,p,h,spread,lean=1,foreground=False):
    p=Vector(p);group='ForegroundPine' if foreground else 'Pine'
    stem=[p,p+Vector((-.15,h*.28,.1)),p+Vector((lean*.45,h*.56,-.15)),p+Vector((lean,h*.84,.1)),p+Vector((lean*1.12,h,0))]
    tube(f'EN_Pine{index}_Trunk_LOD0',[tuple(q) for q in stem],[h*.06,h*.048,h*.036,h*.023,.015],'Bark',12,group)
    for j,(fraction,ang,reach) in enumerate([(.32,3.4,.62),(.47,1.0,.70),(.58,5.7,1.0),(.72,2.8,.77),(.86,.25,.83),(1,4.3,.57)]):
        a=p+Vector((lean*fraction,h*fraction,0));delta=Vector((math.cos(ang)*spread*reach,.2,math.sin(ang)*spread*reach*.60))
        end=a+delta;mid=a+delta*.55+Vector((0,-.22,0));tube(f'EN_Pine{index}_Bough{j}_LOD0',[tuple(a),tuple(mid),tuple(end)],[h*.037,h*.022,.012],'Bark',8,group)
        for k in range(3):
            q=mid.lerp(end,.3+k*.3)+Vector((random.uniform(-.4,.4),.10+k*.04,random.uniform(-.5,.5)))
            width=spread*random.uniform(.72,1.08);canopy(f'EN_Pine{index}_Spray{j}_{k}_LOD0',tuple(q),width,width*.72,(j+k+index)%4,ang*.3+k*.51,group)

def coral(index,p,size,kind,role):
    rng=random.Random(3100+index);g=G();root=Vector(p);yaw=rng.random()*math.tau
    def W(v):x,y,z=v;return tuple(root+Vector((math.cos(yaw)*x+math.sin(yaw)*z,y,-math.sin(yaw)*x+math.cos(yaw)*z))*size)
    if kind in [0,3]:
        g.tube([W((0,0,0)),W((0,.25,0)),W((.06,.52,0))],[size*.10,size*.08,size*.04],7)
        for j in range(6):
            a=j*math.tau/6+rng.random()*.3;vx,vz=math.cos(a),math.sin(a)
            g.tube([W((0,.18,0)),W((vx*.25,.40,vz*.25)),W((vx*.44,.68,vz*.44))],[size*.065,size*.045,size*.018],6)
            for k in [-1,1]:
                q=a+k*.45
                g.tube([W((vx*.23,.4,vz*.23)),W((math.cos(q)*.45,.62,math.sin(q)*.45)),W((math.cos(q)*.53,.84+rng.random()*.18,math.sin(q)*.53))],[size*.035,size*.025,size*.008],5)
    elif kind==1:
        g.tube([W((0,0,0)),W((0,.55,0))],[size*.11,size*.06],7)
        for level in range(3):
            count=22;r=.62-level*.14;start=len(g.v)
            for ring in [0,1,2]:
                radius=[r*.12,r,r*.98][ring]
                for j in range(count):
                    a=j*math.tau/count;rr=radius*(1+.09*math.sin(a*7+level));y=.2+level*.18+(.075*math.cos(a*5+level) if ring else 0)+(.04 if ring==1 else 0)
                    g.v.append(W((rr*math.cos(a),y,rr*math.sin(a))));g.uv.append((.5+math.cos(a)*radius,.5+math.sin(a)*radius));g.col.append((1,1,1,1))
            for j in range(count):
                k=(j+1)%count;g.f.append((start+j,start+count+j,start+count+k,start+k));g.f.append((start+count+j,start+2*count+j,start+2*count+k,start+count+k))
    elif kind==2:
        rows=10;cols=20
        for j in range(rows+1):
            el=j/rows*math.pi*.52
            for i in range(cols):
                a=i*math.tau/cols;r=math.sin(el)*(.48+.025*math.sin(a*9+el*7));g.v.append(W((math.cos(a)*r,.06+math.cos(el)*.46,math.sin(a)*r)));g.uv.append((i/cols,j/rows));g.col.append((1,1,1,1))
        for j in range(rows):
            for i in range(cols):a=j*cols+i;b=j*cols+(i+1)%cols;g.f.append((a,b,b+cols,a+cols))
    else:
        g.tube([W((0,0,0)),W((0,.28,0))],[size*.06,size*.04],6)
        for j in range(7):
            x=(j-3)*.17;g.tube([W((0,.17,0)),W((x*.60,.45,0)),W((x,.78-abs(x)*.2,.06))],[size*.035,size*.025,size*.008],5)
            for k in [-1,1]:g.tube([W((x*.6,.45,0)),W((x+k*.12,.62,.03)),W((x+k*.15,.75,.04))],[size*.017,size*.012,size*.005],5)
    return g.build(f'EN_Coral{index:03d}_LOD0',role,'Coral')

def kelp(index,p,h):
    ob=bpy.data.objects.new(f'EN_CleanKelp{index:03d}_LOD0',kelpTemplate.copy());LOW.objects.link(ob);ob.location=B(p);ob.scale=(h,h,h);ob.rotation_euler.z=index*2.399963
    own(ob,'Kelp','Kelp');layer=ob.data.color_attributes.new(name='ArtWeights',type='FLOAT_COLOR',domain='POINT')
    for i,v in enumerate(ob.data.vertices):
        w=max(0,min(1,v.co.z));layer.data[i].color=(.42+.5*w,.8,.7,w)
    return ob

def rejected_broad_leaf_kelp(index,p,h):
    rng=random.Random(4100+index);root=Vector(p);g=G()
    for stalk in range(3):
        a=stalk*2.3+rng.random();heightK=h*rng.uniform(.7,1);spread=rng.uniform(.13,.3)
        def C(t):return root+Vector((math.sin(t*2.5+a)*spread*t,heightK*t,math.cos(t*3+a)*spread*t))
        g.tube([tuple(C(i/7)) for i in range(8)],[.014*(1-i/9) for i in range(8)],5)
        for leaf in range(3,11):
            t=leaf/12;angle=a+leaf*2.2;base=C(t);length=rng.uniform(.75,1.35)*h/2;direction=Vector((math.cos(angle),.24,math.sin(angle)));side=Vector((-math.sin(angle),0,math.cos(angle)))
            start=len(g.v);steps=7
            for j in range(steps+1):
                f=j/steps;centre=base+direction*length*f+Vector((0,math.sin(f*math.pi)*.12,0));width=(math.sin(math.pi*f)**.65)*.048*(h/2)
                for sign in [-1,1]:g.v.append(tuple(centre+side*width*sign));g.uv.append(((sign+1)/2,f));g.col.append((.8,1,.8,t+f*.15))
            for j in range(steps):k=start+j*2;g.f.append((k,k+1,k+3,k+2))
    g.col=[(1,1,1,max(0,min(1,(v[1]-root.y)/h))) for v in g.v]
    return g.build(f'EN_CleanKelp{index:03d}_LOD0','Kelp','Kelp')

def fern(index,p,scale):
    root=Vector(p);g=G()
    for i in range(8):
        a=i*math.tau/8+.3;d=Vector((math.cos(a),0,math.sin(a)));side=Vector((-d.z,0,d.x))
        for j in range(1,11):
            t=j/11;c=root+d*t*.8*scale+Vector((0,math.sin(t*math.pi)*.48*scale+.05,0));w=math.sin(t*math.pi)*.22*scale
            for sign in [-1,1]:
                end=c+side*w*sign-d*.06*scale
                g.quad([tuple(c-d*.025*scale),tuple(end-d*.035*scale),tuple(end+d*.025*scale+Vector((0,.025,0))),tuple(c+d*.045*scale)])
    return g.build(f'EN_Fern{index:03d}_LOD0','Kelp','LandPlants')

pine(0,(-18.0,3.3,7.8),4.2,3.8,2.5)
pine(1,(-27.0,5.7,18.5),6.7,4.0,-1.2)
pine(2,(-30,5.6,5),6.5,3.7,1.5)
pine(3,(-27,5.6,-8),5.3,3.2,1)
pine(4,(-18,1,-24),5.4,4.5,4.0,True)
pine(5,(30,2.5,-17),7.0,4.5,-4.5,True)
pine(6,(-29,5.8,31),5.2,3.5,1.5)

coralIndex=0;kelpIndex=0;anchors=[]
for station,z in enumerate([-26,-20,-14,-8,-2,4.5,11,17,24,31,38]):
    for j in range(28):
        px=near(z)+random.uniform(-3.6,2.9);pz=z+random.uniform(-3.4,3.4);p,n=surface(base_tree,px,pz)
        if not(-6.3<p[1]<-.55) or n[1]<.38:continue
        available=-.48-p[1];size=min(random.uniform(.45,1.0),available*.8)
        if size<.25:continue
        kind=random.randrange(5);size*=1.45 if kind==1 else 1.2 if kind==2 else 1;role=['CoralLavender','CoralGold','CoralMint','CoralSalmon','CoralLavender'][kind]
        coral(coralIndex,p,size,kind,role);anchors.append({'kind':'coral','position':p,'height':size});coralIndex+=1
    for j in range(6):
        px=near(z)+random.choice([-1,1])*random.uniform(2.2,4.2);pz=z+random.uniform(-3,3);p,n=surface(base_tree,px,pz)
        if not(-8<p[1]<-1.1) or n[1]<.35:continue
        h=min(random.uniform(1.6,3.4),-.45-p[1]);kelp(kelpIndex,p,h);kelpIndex+=1
for station,z in enumerate([-19,-6,7,20,33]):
    for j in range(13):
        px=far(z)+random.uniform(-3.8,2.5);pz=z+random.uniform(-3,3);p,n=surface(base_tree,px,pz)
        if -6<p[1]<-.7 and n[1]>.38:coral(coralIndex,p,min(random.uniform(.6,.9),(-.45-p[1])*.7),random.randrange(3),random.choice(['CoralLavender','CoralGold','CoralMint']));coralIndex+=1
for i in range(68):
    z=random.uniform(-25,33);x=shore(z)-random.uniform(.8,8);p,n=surface(base_tree,x,z)
    if n[1]>.5 and not(-24<x<-16 and 0<z<7):fern(i,p,random.uniform(.55,1.0))

# Foliage texture belongs to real bent, alpha-tested 3D cards, never a background plate.
fol=M['Foliage'];nt=fol.node_tree;bs=nt.nodes.get('Principled BSDF');tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'textures/CoastalPine-Atlas.png'));nt.links.new(tex.outputs['Color'],bs.inputs['Base Color']);nt.links.new(tex.outputs['Alpha'],bs.inputs['Alpha']);fol.surface_render_method='DITHERED'

# Calibration camera uses the same projection direction as v15; world layout is rebuilt.
bpy.ops.object.camera_add(location=B((0,0,0)));cam=bpy.context.object;cam.name='REF_GameCamera';scene.camera=cam
focus=Vector((0,-.8,0));pitch=math.radians(42);yaw=math.radians(-18);forward=Vector((math.sin(yaw)*math.cos(pitch),-math.sin(pitch),math.cos(yaw)*math.cos(pitch)))
pos=focus-forward*104;cam.location=B(pos);cam.rotation_euler=(B(focus)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=32.0
for c in list(cam.users_collection):c.objects.unlink(cam)
bpy.data.collections['REF'].objects.link(cam)
scene.render.resolution_x=1920;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
cam.data.ortho_scale=32.0*16/9
scene.world=bpy.data.worlds.new('World') if scene.world is None else scene.world
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH';scene.world.color=(.10,.12,.13)
scene['artScope']='Independent Unity 3D environment sample; original game and v15 physics kept. Single-view concept dimensions inferred.'
scene['mapping']='Unity(x,y,z) -> Blender(-x,-z,y), triangle winding reversed at both boundaries. Original Unity world coordinates retained in JSON.'
# Freeze delivery transforms and explicit data names; retain the scan/kelp UV seam splits.
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:
    ob.data.name=ob.name+'_Mesh';bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);ob.select_set(False)
    if ob.name.startswith('AR_LanternRoof') or ob.name.startswith('AR_LanternPole'):
        bm=bmesh.new();bm.from_mesh(ob.data)
        if 'Roof' in ob.name:bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.0000001)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free();ob.data.update()
bpy.ops.wm.save_as_mainfile(filepath=str(R/'EN_MistCapeShowcase_master.blend'))
scene.render.filepath=str(R/'review/forms.png');bpy.ops.render.render(write_still=True)

# Build engine batches from evaluated geometry with exact corner normals and UV splits.
dg=bpy.context.evaluated_depsgraph_get();batches={};triTotal=0
for ob in objects:
    eo=ob.evaluated_get(dg);me=eo.to_mesh();me.calc_loop_triangles();uv=me.uv_layers.active
    role=ob['role'];group=ob['group'];z=U(ob.matrix_world.translation)[2]
    if group in ['Pine','ForegroundPine','Pier']:zone=0
    else:
        centre=sum((eo.matrix_world@v.co for v in me.vertices),Vector())/max(1,len(me.vertices));zone=int(U(centre)[2]//14)
    key=(group,role,zone)
    if key not in batches:batches[key]={'name':f'EN_{group}_{role}_{zone+4:02d}_LOD0','material':role,'group':group,'vertices':[],'normals':[],'uv':[],'colors':[],'triangles':[]}
    out=batches[key];normalMatrix=eo.matrix_world.to_3x3().inverted().transposed();col=me.color_attributes.get('ArtWeights')
    for tri in me.loop_triangles:
        for li in reversed(tri.loops):
            vi=me.loops[li].vertex_index;v=U(eo.matrix_world@me.vertices[vi].co);n=U((normalMatrix@me.corner_normals[li].vector).normalized());u=uv.data[li].uv if uv else (v[0]*.25,v[2]*.25)
            c=tuple(col.data[vi].color) if col and col.domain=='POINT' else (1,1,1,1)
            out['triangles'].append(len(out['vertices']));out['vertices'].append(dict(zip('xyz',v)));out['normals'].append(dict(zip('xyz',n)));out['uv'].append(dict(zip('xy',u)));out['colors'].append(dict(zip('rgba',c)))
        triTotal+=1
    eo.to_mesh_clear()

fishPath=[];fish_tree=substrate_bvh(True)
for i in range(160):
    a=i*math.tau/160;z=4+13*math.cos(a);x=lerp(near(z),far(z),.62)+1.3*math.sin(a)
    envelope=max(surface(fish_tree,x+dx,z+dz)[0][1] for dx in [-.85,0,.85] for dz in [-.85,0,.85])
    y=max(-5.3-1.4*math.sin(a),envelope+1.25);y=min(-.55,y);fishPath.append({'x':x,'y':y,'z':z})
data={'meshes':list(batches.values()),'fishPath':fishPath,'corals':coralIndex,'kelp':kelpIndex,'source':'Original locally authored scene with inferred forms. Existing v14 lighthouse / hero boat / v12 water reused. No gameplay integration asserted.'}
(R/'exports/CoastalShowcase.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
dest=P/'Assets/Deepsea/ThirdParty/CoastArtV16';dest.mkdir(parents=True,exist_ok=True)
(dest/'CoastalShowcase.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
(R/'exports/scene-manifest.json').write_text(json.dumps({'triangles':triTotal,'batches':len(batches),'corals':coralIndex,'kelp':kelpIndex,'mapping':'Unity(x,y,z) -> Blender(-x,-z,y), winding reversed at each boundary','source':'01_scene.py + 02_life.py; original hand-directed layout and shapes; reference unseen geometry inferred','texture':'CoastalPine-Atlas.png, generated via native imagegen, RGBA; sRGB colour + alpha clip; not a scene background','gameplay':'Independent visual sample; old v15/gameplay/physics not changed'},indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(R/'exports/EN_MistCapeShowcase.glb'),export_format='GLB',use_selection=True,export_apply=True)
print('SHOWCASE_MESH_COMPLETE',triTotal,'tris',len(batches),'batches',coralIndex,'coral',kelpIndex,'kelp')
