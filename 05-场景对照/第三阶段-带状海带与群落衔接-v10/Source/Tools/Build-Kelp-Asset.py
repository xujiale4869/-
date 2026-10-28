"""Create the actual OBJ imported by Unity, not a gameplay illustration."""
from pathlib import Path
import math,json
import numpy as np
root=Path(__file__).resolve().parents[1]
vertices=[];uvs=[];triangles=[];ribbons=[]
def vertex(p,uv):
 vertices.append(np.asarray(p,float));uvs.append(tuple(uv));return len(vertices)-1
def tri(a,b,c):triangles.append((a,b,c))
def ribbon(index):
 phase=index*2.399963;height=.82+.14*math.sin(index*1.73);half_width=.022+.004*math.cos(index*2.1)
 az=index*2.399963;spread=np.array([math.cos(az),0,math.sin(az)])
 first=len(vertices);centres=[];rows=20
 for row in range(rows+1):
  t=row/rows
  centre=spread*(.024+.15*math.sin(t*math.pi)*(.65+index*.06))
  centre+=np.array([.29*t*t+.025*math.sin(t*6+phase)*t*t,height*(t-.13*t*t*t),.08*t*t])
  centres.append(centre)
  side=spread*math.cos(t*.42)+np.array([-spread[2],0,spread[0]])*math.sin(t*.42)
  transverse=np.array([side[0],-.10*math.sin(t*3+phase),side[2]])
  transverse/=np.linalg.norm(transverse)
  w=half_width*max(.025,math.sin(math.pi*t))**.50*(1+.07*math.sin(t*17+phase))
  for col in range(3):
   a=col-1;p=centre+transverse*w*a+np.array([0,1,0])*w*.24*(1-a*a)
   vertex(p,(col*.5,t))
 for row in range(rows):
  for col in range(2):
   a=first+row*3+col;tri(a,a+3,a+1);tri(a+1,a+3,a+4)
 arc=sum(float(np.linalg.norm(b-a)) for a,b in zip(centres,centres[1:]))
 ribbons.append({'ribbon':index,'length_to_width':round(arc/(2*half_width),2),'rows':rows})
for i in range(6):ribbon(i)
# Small holdfast roots, kept close to the substrate instead of a tall woody stalk.
for arm in range(7):
 angle=arm*math.tau/7;side=np.array([-math.sin(angle),0,math.cos(angle)]);first=len(vertices)
 for ring in range(4):
  t=ring/3;centre=np.array([math.cos(angle)*t*.11,.018*(1-t),math.sin(angle)*t*.11]);radius=.012*(1-t*.82)
  for j in range(5):
   a=j*math.tau/5;vertex(centre+side*math.cos(a)*radius+np.array([0,1,0])*math.sin(a)*radius,(j/5,t*.12))
 for ring in range(3):
  for j in range(5):
   a=first+ring*5+j;b=first+ring*5+(j+1)%5;tri(a,b,a+5);tri(b,b+5,a+5)
v=np.asarray(vertices);scale=1/v[:,1].max();v*=scale
normals=np.zeros_like(v);areas=[]
for a,b,c in triangles:
 n=np.cross(v[b]-v[a],v[c]-v[a]);areas.append(float(np.linalg.norm(n)*.5))
 for i in [a,b,c]:normals[i]+=n
length=np.linalg.norm(normals,axis=1);assert np.all(length>1e-10);normals/=length[:,None]
assert np.isfinite(v).all() and min(areas)>1e-10
assert len(set(tuple(sorted(t)) for t in triangles))==len(triangles)
assert min(r['length_to_width'] for r in ribbons)>8
path=root/'Assets/KelpRibbon.obj'
with path.open('w') as f:
 f.write('# Deepsea v10 original long-ribbon kelp mesh\no Deepsea_KelpRibbon\n')
 for p in v:f.write('v '+' '.join(f'{x:.8f}' for x in p)+'\n')
 for uv in uvs:f.write('vt '+' '.join(f'{x:.8f}' for x in uv)+'\n')
 for n in normals:f.write('vn '+' '.join(f'{x:.8f}' for x in n)+'\n')
 f.write('s 1\n')
 for t in triangles:f.write('f '+' '.join(f'{i+1}/{i+1}/{i+1}' for i in t)+'\n')
audit={'asset':path.name,'purpose':'Actual Unity model source; not gameplay screenshot','vertices':len(v),'triangles':len(triangles),'bounds_min':v.min(axis=0).tolist(),'bounds_max':v.max(axis=0).tolist(),'min_triangle_area':min(areas),'normals_unit':True,'uv_vertices':len(uvs),'ribbons':ribbons,'runtime_verification':'pending Unity device connection'}
(root/'Assets/kelp-mesh-audit.json').write_text(json.dumps(audit,indent=2))
print(json.dumps(audit,indent=2))
