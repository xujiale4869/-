"""Deterministic original environment meshes; dimensions are inferred in asset-brief.md."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'exports'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
low=bpy.data.collections.new('LOW'); scene.collection.children.link(low)

class Shape:
    def __init__(self): self.v=[]; self.f=[]; self.c=[]; self.uv=[]
    def vertex(self,p,c,uv=(0,0)):
        self.v.append(tuple(p));self.c.append(tuple(c));self.uv.append(uv);return len(self.v)-1
    def tube(self,points,radii,base,tip,sides=6):
        first=len(self.v)
        for j,p in enumerate(points):
            axis=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(j-1,0)])).normalized()
            u=axis.cross(Vector((0,1,0)))
            if u.length<.1:u=axis.cross(Vector((1,0,0)))
            u.normalize();v=axis.cross(u).normalized();t=j/(len(points)-1)
            col=[base[k]*(1-t)+tip[k]*t for k in range(3)]+[1]
            for i in range(sides):
                a=2*math.pi*i/sides
                self.vertex(Vector(p)+(u*math.cos(a)+v*math.sin(a))*radii[j],col,(i/sides,t))
        for j in range(len(points)-1):
            for i in range(sides):
                a=first+j*sides+i;b=first+j*sides+(i+1)%sides
                self.f.extend([(a,b,a+sides),(b,b+sides,a+sides)])
        self.f.append(tuple(first+i for i in range(sides-1,-1,-1)))
        self.f.append(tuple(first+(len(points)-1)*sides+i for i in range(sides)))
    def mesh(self,name):
        mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(self.v,[],self.f);mesh.update()
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
        floor=min(v.co.z for v in mesh.vertices)
        for v in mesh.vertices:v.co.z-=floor
        mesh.update()
        uv=mesh.uv_layers.new(name='UVMap');colour=mesh.color_attributes.new(name='CoralColor',type='FLOAT_COLOR',domain='POINT')
        for i,c in enumerate(self.c):colour.data[i].color=c
        for polygon in mesh.polygons:
            polygon.use_smooth=True
            for loop in polygon.loop_indices:uv.data[loop].uv=self.uv[mesh.loops[loop].vertex_index]
        obj=bpy.data.objects.new(name,mesh);low.objects.link(obj)
        mat=bpy.data.materials.new(name+'_Color')
        nt=mat.node_tree;bsdf=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
        attr=nt.nodes.new('ShaderNodeVertexColor');attr.layer_name='CoralColor';nt.links.new(attr.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.8
        mesh.materials.append(mat)
        return obj

