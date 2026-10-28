import bpy,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(R/'EN_MistCapeShowcase_master.blend'))
dg=bpy.context.evaluated_depsgraph_get();verts=[];faces=[]
for ob in bpy.data.collections['LOW'].objects:
    if ob.type!='MESH' or ob.get('group') not in ['Ground','Land','Reef','Coral']:continue
    eo=ob.evaluated_get(dg);me=eo.to_mesh();start=len(verts);verts.extend(eo.matrix_world@v.co for v in me.vertices);faces.extend(tuple(start+i for i in p.vertices) for p in me.polygons);eo.to_mesh_clear()
tree=BVHTree.FromPolygons(verts,faces)
data=json.loads((R/'exports/CoastalShowcase.json').read_text(encoding='utf-8'));path=[Vector((p['x'],p['y'],p['z'])) for p in data['fishPath']]
def floor(x,z):
    p,n,idx,d=tree.ray_cast(Vector((-x,-z,30)),Vector((0,0,-1)),70)
    return p.z if p is not None else -100
obstructions=[];samples=0;clear=100
for frame in range(160):
    phase=frame/160*22
    for i in range(144):
        t=((phase*.045+i/144)%1)*len(path);k=int(t);p=path[k%len(path)].lerp(path[(k+1)%len(path)],t-k);r=.14+(i%5)*.085
        p+=Vector((math.sin(i*2.3+phase*.3)*r,math.cos(i*1.7)*.16,math.cos(i*2.3+phase*.3)*r))
        for dx,dz in [(0,0),(.16,0),(-.16,0),(0,.16),(0,-.16)]:
            gap=p.y-.13-floor(p.x+dx,p.z+dz);clear=min(clear,gap)
            if gap<0 and len(obstructions)<20:obstructions.append({'fish':i,'frame':frame,'p':list(p),'clearance':gap})
        samples+=1
for frame in range(160):
    phase=frame/160*math.tau
    for i in range(34):
        a=phase+i*2.399963;p=Vector((-3+math.sin(a)*2.4,-1.4+math.sin(i)*.12,7+math.cos(a)*3.1))
        gap=p.y-.16-floor(p.x,p.z);clear=min(clear,gap)
        if gap<0 and len(obstructions)<20:obstructions.append({'goldFish':i,'frame':frame,'p':list(p),'clearance':gap})
        samples+=1
report={'scope':'Offline actual evaluated substrate/rock/coral mesh check of the independent sample fish centre plus body margin. No gameplay controller acceptance claimed.','samples':samples,'passed':not obstructions,'minimumVerticalClearanceM':clear,'firstObstructions':obstructions}
(R/'review/fish-route-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('ROUTE_AUDIT',report['passed'],samples,clear)
