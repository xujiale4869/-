"""Periodic refracted light density, splatted from a wave surface; no Voronoi cell lines."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter
ROOT=Path(__file__).resolve().parents[1]
N=1536;S=512
y,x=np.mgrid[0:N,0:N]/N
sx=np.zeros_like(x);sy=np.zeros_like(y)
waves=[(3,2,1,.24),(5,-3,.62,1.6),(2,7,.45,3.2),(8,3,.28,4.1),(7,-6,.22,2.0),(11,5,.12,5.2)]
for fx,fy,amp,phase in waves:
    p=2*np.pi*(fx*x+fy*y)+phase
    sx+=np.cos(p)*fx*amp;sy+=np.cos(p)*fy*amp
# A ray crossing the wave normals concentrates into curved fold caustics.
u=np.mod((x+sx*.0088)*S,S);v=np.mod((y+sy*.0088)*S,S)
ix=u.astype(int);iy=v.astype(int);dx=u-ix;dy=v-iy;density=np.zeros((S,S))
for ox,oy,weights in [(0,0,(1-dx)*(1-dy)),(1,0,dx*(1-dy)),(0,1,(1-dx)*dy),(1,1,dx*dy)]:
    np.add.at(density,((iy+oy)%S,(ix+ox)%S),weights)
density/=density.mean()
bright=np.maximum(0,density-.95)
bright=np.clip(bright/np.percentile(bright,99.4),0,1)**.72
im=Image.fromarray((bright*255).astype('uint8')).filter(ImageFilter.GaussianBlur(.6)).convert('RGB')
out=ROOT/'exports/CausticRefracted.png';im.save(out)
(ROOT/'exports/caustic-audit.json').write_text(json.dumps({'method':'periodic wave normals, forward light-density splatting','raySamples':N*N,'resolution':[S,S],'tileable':True,'waves':waves},indent=2))
print('CAUSTIC_DENSITY_READY',out)
