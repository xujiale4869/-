"""Convert the verified CC0 GLB into a Unity-readable, baked mesh source.
Only standard library; preserve UVs including KHR_texture_transform.
"""
import json,struct,math,sys,hashlib
from pathlib import Path
src=Path(sys.argv[1]); out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
raw=src.read_bytes();assert raw[:4]==b'glTF' and struct.unpack_from('<I',raw,4)[0]==2
chunks={};at=12
while at<len(raw):
 n,t=struct.unpack_from('<II',raw,at);chunks[t]=raw[at+8:at+8+n];at+=8+n
j=json.loads(chunks[0x4e4f534a]);data=chunks[0x004e4942]
def accessor(index):
 a=j['accessors'][index];assert 'sparse' not in a
 v=j['bufferViews'][a['bufferView']];count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
 typ={5120:'b',5121:'B',5122:'h',5123:'H',5125:'I',5126:'f'}[a['componentType']]
 fmt='<'+typ*count;size=struct.calcsize(fmt);stride=v.get('byteStride',size);offset=v.get('byteOffset',0)+a.get('byteOffset',0)
 return [struct.unpack_from(fmt,data,offset+i*stride) for i in range(a['count'])]
identity=[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]
def multiply(a,b):return [sum(a[k*4+r]*b[c*4+k] for k in range(4)) for c in range(4) for r in range(4)]
def matrix(n):
 if 'matrix' in n:return n['matrix']
 x,y,z,w=n.get('rotation',[0,0,0,1]);sx,sy,sz=n.get('scale',[1,1,1]);tx,ty,tz=n.get('translation',[0,0,0])
 return [(1-2*(y*y+z*z))*sx,2*(x*y+z*w)*sx,2*(x*z-y*w)*sx,0,2*(x*y-z*w)*sy,(1-2*(x*x+z*z))*sy,2*(y*z+x*w)*sy,0,2*(x*z+y*w)*sz,2*(y*z-x*w)*sz,(1-2*(x*x+y*y))*sz,0,tx,ty,tz,1]
def point(m,p):return tuple(sum(m[c*4+r]*p[c] for c in range(3))+m[12+r] for r in range(3))
def normal(m,p):
 a,b,c=m[0],m[4],m[8];d,e,f=m[1],m[5],m[9];g,h,i=m[2],m[6],m[10]
 det=a*(e*i-f*h)-b*(d*i-f*g)+c*(d*h-e*g)
 cof=[e*i-f*h,f*g-d*i,d*h-e*g,c*h-b*i,a*i-c*g,b*g-a*h,b*f-c*e,c*d-a*f,a*e-b*d]
 return tuple(sum(cof[r*3+k]*p[k] for k in range(3))/det for r in range(3))
parts=[]
def visit(index,parent):
 n=j['nodes'][index];m=multiply(parent,matrix(n))
 if 'mesh' in n:
  for prim in j['meshes'][n['mesh']]['primitives']:
   assert prim.get('mode',4)==4
   attrs=prim['attributes'];positions=[point(m,p) for p in accessor(attrs['POSITION'])]
   normals=[normal(m,p) for p in accessor(attrs['NORMAL'])] if 'NORMAL' in attrs else [(0,1,0)]*len(positions)
   uv=accessor(attrs['TEXCOORD_0']) if 'TEXCOORD_0' in attrs else [(0,0)]*len(positions)
   inds=[p[0] for p in accessor(prim['indices'])] if 'indices' in prim else list(range(len(positions)))
   mat=prim.get('material',0);tex=j['materials'][mat].get('pbrMetallicRoughness',{}).get('baseColorTexture',{})
   tr=tex.get('extensions',{}).get('KHR_texture_transform',{});sc=tr.get('scale',[1,1]);off=tr.get('offset',[0,0]);rot=tr.get('rotation',0)
   uv=[(math.cos(rot)*u*sc[0]-math.sin(rot)*v*sc[1]+off[0],math.sin(rot)*u*sc[0]+math.cos(rot)*v*sc[1]+off[1]) for u,v in uv]
   parts.append((mat,positions,uv,inds,n.get('name',''),normals))
 for child in n.get('children',[]):visit(child,m)
for n in j['scenes'][j.get('scene',0)]['nodes']:visit(n,identity)
allp=[p for part in parts for p in part[1]];lo=[min(p[k] for p in allp) for k in range(3)];hi=[max(p[k] for p in allp) for k in range(3)]
# Source length is X. Swap X/Z, reversing handedness, then reverse triangles.
scale=[4.15/(hi[0]-lo[0]),.78/(hi[1]-lo[1]),1.65/(hi[2]-lo[2])]
result=[]
for mi in range(len(j['materials'])):
 batch={'positions':[],'normals':[],'uv':[],'indices':[],'material':mi};base=0
 for mat,pos,uv,inds,name,normals in parts:
  if mat!=mi:continue
  for x,y,z in pos:batch['positions'] += [(z-(lo[2]+hi[2])/2)*scale[2],(y-lo[1])*scale[1]-.10,(x-(lo[0]+hi[0])/2)*scale[0]]
  for x,y,z in normals:
   nx,ny,nz=z/scale[2],y/scale[1],x/scale[0];length=math.sqrt(nx*nx+ny*ny+nz*nz)
   batch['normals'] += [nx/length,ny/length,nz/length]
  for u,v in uv:batch['uv'] += [u,1-v]
  for k in range(0,len(inds),3):batch['indices'] += [base+inds[k],base+inds[k+2],base+inds[k+1]]
  base+=len(pos)
 result.append(batch)
materials=[]
for mat in j['materials']:
 pbr=mat.get('pbrMetallicRoughness',{});tex=pbr.get('baseColorTexture',{});name=''
 if tex:
  image=j['images'][j['textures'][tex['index']]['source']];view=j['bufferViews'][image['bufferView']];start=view.get('byteOffset',0)
  name=image['name']+'.png';(out/name).write_bytes(data[start:start+view['byteLength']])
 materials.append(dict(name=mat['name'],image=name,metallic=pbr.get('metallicFactor',0),roughness=pbr.get('roughnessFactor',1)))
manifest={'batches':result,'materials':materials}
(out/'BoatSource.json').write_text(json.dumps(manifest,separators=(',',':')))
report={'source':'https://www.innerscene.com/tools/library/3d-parts/rowing-boat-d14b7296','license':'CC0','sha256':hashlib.sha256(raw).hexdigest(),'sourceBytes':len(raw),'parts':len(parts),'vertices':len(allp),'triangles':sum(len(p[3])//3 for p in parts),'sourceBounds':[lo,hi],'targetBounds':[[-.825,-.10,-2.075],[.825,.68,2.075]],'materials':materials}
(out/'source-record.json').write_text(json.dumps(report,indent=2))
(out/'SOURCE-LICENSE.txt').write_text('Rowing boat, Innerscene original model. CC0 public domain.\n'+report['source']+'\nhttps://creativecommons.org/publicdomain/zero/1.0/\nOriginal UVs and embedded textures retained. Geometry fitted to the accepted boat visual envelope.\n')
print(json.dumps({k:v for k,v in report.items() if k!='materials'}))
