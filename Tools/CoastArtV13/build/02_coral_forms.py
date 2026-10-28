"""Original mesh forms plus an attributed space-colonization growth port. Dimensions inferred."""
import sys,random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from coral_growth import grow
# Shared primitive writer is copied into this build folder; it owns only this v13 project.
from coral_shape import *
objects=[]
for name,seed,fan in [('EN_CoralBranch_LOD0',231311,False),('EN_CoralFan_LOD0',231312,True)]:
    s=Shape()
    for a,b,r0,r1 in grow(seed,fan):
        height=(a.z+b.z)*.5;t=min(1,height/1.1)
        base=(.35+.18*t,.17+.16*t,.36+.18*t) if fan else (.32+.23*t,.19+.15*t,.31+.18*t)
        tip=(.73,.46,.72) if fan else (.71,.47,.62)
        s.tube([a,b],[r0,r1],base,tip,sides=6)
    objects.append(s.mesh(name))

# Thin, ruffled foliate plates with irregular lobes rather than circular dinner plates.
s=Shape();N=24;rings=4
for layer in range(3):
    radius=.36+layer*.035;z=.10+layer*.18;cx=.14*math.sin(layer*2.1);cy=.11*math.cos(layer*1.7);first=len(s.v)
    for face in range(2):
        for ring in range(rings):
            t=ring/(rings-1)
            for j in range(N):
                a=2*math.pi*j/N;r=radius*(.06+.94*t)*(1+.17*math.sin(a*3+layer)+.055*math.sin(a*7+layer))
                p=(cx+math.cos(a)*r,cy+math.sin(a)*r,z+.18*t*t+.04*math.sin(a*6+layer)*t**3-face*.018)
                c=(.52+.23*t,.27+.19*t,.15+.10*t,1)
                if face:c=(c[0]*.58,c[1]*.58,c[2]*.58,1)
                s.vertex(p,c,(j/N,t))
        for ring in range(rings-1):
            for j in range(N):
                a=first+face*rings*N+ring*N+j;b=first+face*rings*N+ring*N+(j+1)%N
                f=[(a,b,a+N),(b,b+N,a+N)];s.f.extend([tuple(reversed(q)) if face==0 else q for q in f])
    for j in range(N):
        a=first+(rings-1)*N+j;b=first+(rings-1)*N+(j+1)%N
        s.f.extend([(a,b,a+rings*N),(b,b+rings*N,a+rings*N)])
objects.append(s.mesh('EN_CoralPlate_LOD0'))

def dome(shape,centre,scale,colour,seed,N=24,rows=10):
    first=len(shape.v);cx,cy,cz=centre;rx,ry,rz=scale
    shape.vertex((cx,cy,cz+rz),(*colour,1),(0,0))
    for row in range(1,rows+1):
        theta=row/rows*math.pi*.5
        for j in range(N):
            a=2*math.pi*j/N;groove=math.sin(a*11+2*math.sin(theta*5+seed)+math.sin(a*3))
            lobe=1+.10*math.sin(a*4+seed)*math.sin(theta);ridge=1+.035*groove
            p=(cx+rx*math.sin(theta)*math.cos(a)*lobe*ridge,cy+ry*math.sin(theta)*math.sin(a)*lobe*ridge,cz+rz*math.cos(theta))
            value=.75+.25*(.5+.5*groove)
            shape.vertex(p,tuple(c*value for c in colour)+(1,),(j/N,row/rows))
    for j in range(N):shape.f.append((first,first+1+j,first+1+(j+1)%N))
    for row in range(rows-1):
        for j in range(N):
            a=first+1+row*N+j;b=first+1+row*N+(j+1)%N;shape.f.extend([(a,a+N,b),(b,a+N,b+N)])
    shape.f.append(tuple(first+1+(rows-1)*N+j for j in range(N-1,-1,-1)))

s=Shape();dome(s,(0,0,0),(.49,.43,.36),(.37,.56,.47),4)
objects.append(s.mesh('EN_CoralBrain_LOD0'))
s=Shape()
for i in range(7):
    a=i*2.399;r=.23*math.sqrt(i/6);z=.17+.12*(1-i/7)
    dome(s,(math.cos(a)*r,math.sin(a)*r,z),(.17,.16,.20),(.66,.59,.42),i,N=12,rows=5)
s.tube([(0,0,0),(0,0,.24)],[.075,.13],(.30,.31,.23),(.50,.48,.32),sides=8)
objects.append(s.mesh('EN_CoralFlower_LOD0'))

records=[]
order=['EN_CoralBranch_LOD0','EN_CoralPlate_LOD0','EN_CoralBrain_LOD0','EN_CoralFan_LOD0','EN_CoralFlower_LOD0']
objects.sort(key=lambda obj:order.index(obj.name))
for index,obj in enumerate(objects):
    mesh=obj.data
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update();mesh.calc_loop_triangles()
    # Authored colour attributes are sRGB; decode in both Blender preview and the Unity shader.
    nt=mesh.materials[0].node_tree;attr=next(n for n in nt.nodes if n.type=='VERTEX_COLOR');bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');gamma=nt.nodes.new('ShaderNodeGamma');gamma.inputs['Gamma'].default_value=2.2;nt.links.new(attr.outputs['Color'],gamma.inputs['Color']);nt.links.new(gamma.outputs['Color'],bs.inputs['Base Color'])
    def vec(v):return {'x':round(v[0],6),'y':round(v[2],6),'z':round(-v[1],6)}
    record={'name':obj.name,'vertices':[vec(v.co) for v in mesh.vertices],'normals':[vec(v.normal) for v in mesh.vertices],
      'uv':[{'x':round(v.co.x,6),'y':round(v.co.y,6)} for v in mesh.vertices],
      'colors':[{'r':round(c.color[0],6),'g':round(c.color[1],6),'b':round(c.color[2],6),'a':1} for c in mesh.color_attributes['CoralColor'].data],
      'triangles':[i for tri in mesh.loop_triangles for i in tri.vertices]}
    assert len(record['triangles'])//3<=2400,obj.name
    records.append(record);obj.location.x=index*1.5
(OUT/'CoralLibrary.json').write_text(json.dumps({'meshes':records},separators=(',',':')),encoding='utf-8')
(OUT/'coral-mesh-audit.json').write_text(json.dumps({'growthSource':'NejcHirci/procedural-corals @25e89f97c2f9639b7a3de60406d2d293058857d2, MIT, algorithm adapted in coral_growth.py','objects':[{'name':r['name'],'triangles':len(r['triangles'])//3,'vertices':len(r['vertices'])} for r in records]},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'EN_CoralHabitat_master.blend'))
print('V13_CORAL_HABITAT_READY',[(r['name'],len(r['triangles'])//3) for r in records])
