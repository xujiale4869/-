#!/usr/bin/env python3
"""Validate exported geometry contract independently of Unity import/render QA."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('directory');p.add_argument('--out',required=True);a=p.parse_args()
rows=[]
for f in sorted(Path(a.directory).glob('*.json')):
 d=json.loads(f.read_text());v=d['vertices'];n=d['normals'];uv=d['uv'];bad=[]
 if len(v)%3 or len(n)!=len(v) or len(uv)!=len(v)//3*2:bad.append('attribute lengths')
 if not all(math.isfinite(x) for x in v+n+uv):bad.append('non-finite attribute')
 verts=[v[i:i+3] for i in range(0,len(v),3)];deg=0;count=0
 for part in d['parts']:
  ts=part['triangles'];count+=len(ts)//3
  if len(ts)%3 or any(i<0 or i>=len(verts) for i in ts):bad.append('triangle index range');continue
  for j in range(0,len(ts),3):
   x,y,z=(verts[ts[j+k]] for k in range(3));u=[y[k]-x[k] for k in range(3)];w=[z[k]-x[k] for k in range(3)]
   cross=[u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]]
   if sum(c*c for c in cross)<1e-15:deg+=1
 if deg:bad.append('degenerate triangles')
 norms=[sum(x*x for x in n[i:i+3]) for i in range(0,len(n),3)]
 if any(x<.9 or x>1.1 for x in norms):bad.append('non-unit normals')
 height=max(x[1] for x in verts)-min(x[1] for x in verts)
 if height<.05 or height>15:bad.append('metre-height budget')
 if d['name'].startswith('Village_') and len(d['parts'])<4:bad.append('missing architectural material roles')
 rows.append({'asset':d['name'],'triangles':count,'degenerateTriangles':deg,'heightMetres':height,'materialRoles':[x['role'] for x in d['parts']],'failed':bad})
r={'passed':len(rows)==11 and all(not x['failed'] for x in rows),'models':len(rows),'triangles':sum(x['triangles'] for x in rows),'assets':rows,'scope':'geometry contract only; not Unity compilation, runtime, performance or visual acceptance'}
Path(a.out).write_text(json.dumps(r,indent=2));print(json.dumps(r,indent=2));raise SystemExit(0 if r['passed'] else 1)
