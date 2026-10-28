import bpy, bmesh, math, json, random, os
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
PROJECT=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:\Users\许嘉乐\Desktop\深海灯塔-Unity海陆-v0.3'))
ART=PROJECT/'Assets/Deepsea/ThirdParty/CoastArtV4'
random.seed(241001)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
 if c.name!='Collection':bpy.data.collections.remove(c)
low=bpy.data.collections.get('Collection');low.name='LOW'
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
for name in ['REF','HIGH','COLLISION','SOCKETS','EXPORT']:
 c=bpy.data.collections.new(name);scene.collection.children.link(c)

def material(name,colour,rough=.8,metal=0,texture=None,emission=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*colour,1);m.use_nodes=True
 n=m.node_tree.nodes;links=m.node_tree.links;p=next(a for a in n if a.type=='BSDF_PRINCIPLED')
 p.inputs['Base Color'].default_value=(*colour,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 if emission:
  p.inputs['Emission Color'].default_value=(*colour,1);p.inputs['Emission Strength'].default_value=emission
 if texture:
  diff=n.new('ShaderNodeTexImage');diff.image=bpy.data.images.load(str(ART/(texture+'_diff_2k.jpg')),check_existing=True);links.new(diff.outputs['Color'],p.inputs['Base Color'])
  normal=n.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(ART/(texture+'_nor_gl_2k.jpg')),check_existing=True);normal.image.colorspace_settings.name='Non-Color'
  nm=n.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.38;links.new(normal.outputs['Color'],nm.inputs['Color']);links.new(nm.outputs['Normal'],p.inputs['Normal'])
  mask=n.new('ShaderNodeTexImage');mask.image=bpy.data.images.load(str(ART/(texture+'_arm_2k.jpg')),check_existing=True);mask.image.colorspace_settings.name='Non-Color'
  sep=n.new('ShaderNodeSeparateColor');links.new(mask.outputs['Color'],sep.inputs['Color']);links.new(sep.outputs['Green'],p.inputs['Roughness'])
 return m

M={
 'Plaster':material('Plaster',(.77,.73,.61),texture='worn_plaster_wall'),
 'Stone':material('Stone',(.42,.40,.34),texture='seaside_rock'),
 'Trim':material('Trim',(.56,.53,.44),.88),
 'Iron':material('Iron',(.075,.085,.073),.61,.45),
 'Roof':material('Roof',(.17,.115,.065),.78,.35),
 'Wood':material('Wood',(.20,.135,.075),.86),
 'Glass':material('Glass',(.025,.065,.068),.23,.15),
 'Lens':material('Lens',(.92,.48,.12),.24,0,emission=1.8)
}
glass_bsdf=next(n for n in M['Glass'].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
glass_bsdf.inputs['Alpha'].default_value=.24
if hasattr(M['Glass'],'surface_render_method'):M['Glass'].surface_render_method='DITHERED'

def finish(o,name,key,smooth=False,bevel=0):
 o.name='AR_CoastLighthouse_'+name+'_LOD0';o.data.name=o.name+'_Mesh';o.data.materials.clear();o.data.materials.append(M[key]);o['role']=key
 for p in o.data.polygons:p.use_smooth=smooth
 if bevel:
  mod=o.modifiers.new('Constructed edges','BEVEL');mod.width=bevel;mod.segments=2
  mod=o.modifiers.new('Corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=30
 return o

def mesh(name,key,verts,faces,smooth=False):
 d=bpy.data.meshes.new(name);d.from_pydata(verts,[],faces);d.update()
 bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 o=bpy.data.objects.new(name,d);low.objects.link(o);return finish(o,name,key,smooth)

def lathe(name,key,profile,segments=72,rough=0):
 v=[]
 for j,(z,r) in enumerate(profile):
  for i in range(segments):
   a=i*math.tau/segments
   delta=rough*(.55*math.sin(a*7+z*.6)+.30*math.sin(a*13-z*.7)+.15*math.sin(a*23+z*3.1))
   v.append(((r+delta)*math.cos(a),(r+delta)*math.sin(a),z))
 f=[]
 for j in range(len(profile)-1):
  for i in range(segments):
   k=j*segments+i;b=j*segments+(i+1)%segments;f.append((k,b,b+segments,k+segments))
 f.append(tuple(reversed(range(segments))));f.append(tuple((len(profile)-1)*segments+i for i in range(segments)))
 return mesh(name,key,v,f,True)

def box(name,key,center,size,bevel=.015):
 bpy.ops.mesh.primitive_cube_add(size=1,location=center);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,key,False,bevel)

def tube(name,key,a,b,r,sides=10):
 a=Vector(a);b=Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=r,depth=(b-a).length,location=(a+b)/2)
 o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,key,True)

def ring(name,key,z,r,tube_radius,steps=64):
 bpy.ops.mesh.primitive_torus_add(major_radius=r,minor_radius=tube_radius,major_segments=steps,minor_segments=6,location=(0,0,z));return finish(bpy.context.object,name,key,True)

def arch_outline(width,height,n=14):
 r=width/2;spring=height-r
 return [(-r,0),(r,0)]+[(math.cos(i*math.pi/n)*r,spring+math.sin(i*math.pi/n)*r) for i in range(n+1)]

def arch_solid(name,key,width,height,bottom,y0,y1):
 outline=arch_outline(width,height);v=[(x,y,z+bottom) for y in [y0,y1] for x,z in outline];n=len(outline)
 f=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]
 f += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,key,v,f)

def arch_frame(name,width,height,bottom,y,thick=.10):
 outer=arch_outline(width+thick*2,height+thick*2);inner=arch_outline(width,height);n=len(outer)
 v=[]
 for depth in [y-.055,y+.055]:
  v.extend((x,depth,z+bottom-thick) for x,z in outer)
  v.extend((x,depth,z+bottom) for x,z in inner)
 f=[]
 for i in range(n):
  j=(i+1)%n;f.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
 o=mesh(name,'Trim',v,f);mod=o.modifiers.new('Stone edge rounding','BEVEL');mod.width=.018;mod.segments=2;return o

# Inferred taper from reference; existing world envelope is retained at runtime.
profile=[(.42,1.53)]+[(.48+j*6.20/40,1.51-(j/40)*.39) for j in range(41)]+[(6.77,1.12)]
shaft=lathe('TaperedShaft','Plaster',profile,96,.016)
for index,(bottom,width,height) in enumerate([(1.95,.44,.91),(4.12,.43,.88),(.36,.73,1.62)]):
 top=bottom+height;radius=1.51-(min(top,6.68)-.48)/6.20*.39
 front=-(1.51-(bottom-.48)/6.20*.39)
 cut=arch_solid('CUT_'+str(index),'Glass',width,height,bottom,-1.9,-.72)
 bpy.context.view_layer.objects.active=shaft;mod=shaft.modifiers.new('Recess '+str(index),'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cut
 bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
 arch_frame('Reveal'+str(index),width,height,bottom,front-.012,.09 if index<2 else .14)
 arch_solid('Inset'+str(index),'Lens' if index==1 else 'Glass' if index==0 else 'Wood',width*.95,height*.98,bottom+.012,front+.21,front+.24)
 if index<2:
  box('WindowMullion'+str(index),'Wood',(0,front+.19,bottom+height*.42),(.026,.04,height*.82),.004)
  box('WindowSill'+str(index),'Trim',(0,front-.10,bottom-.045),(width+.27,.25,.09),.018)
 else:
  for k in range(5):box('DoorPlank'+str(k),'Wood',((k-2)*.126,front+.17,bottom+.63),(.116,.055,1.19),.007)
  for strap,z in enumerate([bottom+.32,bottom+1.10]):box('DoorStrap'+str(strap),'Iron',(0,front+.125,z),(.59,.035,.055),.007)
  tube('DoorHandle','Iron',(.20,front+.10,bottom+.61),(.20,front+.10,bottom+.76),.019,8)

lathe('Footing','Stone',[(0,1.69),(.08,1.71),(.26,1.63),(.41,1.59),(.49,1.54)],72,.035)
lathe('BaseCourse','Trim',[(.39,1.57),(.47,1.57),(.55,1.52)],72,.012)
lathe('Cornice','Trim',[(6.48,1.14),(6.54,1.20),(6.64,1.22),(6.72,1.30),(6.80,1.31)],72)
lathe('GalleryDeck','Iron',[(6.76,1.67),(6.89,1.67),(6.92,1.60)],72)
lathe('ChamberFoot','Roof',[(6.91,1.16),(7.02,1.17)],48)
for i in range(12):
 a=i*math.tau/12;r=Vector((math.cos(a),math.sin(a),0))
 tube('BalconyPost'+str(i),'Iron',r*1.59+Vector((0,0,6.90)),r*1.59+Vector((0,0,7.59)),.026,8)
 if i%2==0:tube('GalleryBrace'+str(i),'Iron',r*1.09+Vector((0,0,6.13)),r*1.57+Vector((0,0,6.79)),.047,8)
ring('BalconyUpper','Iron',7.59,1.59,.032);ring('BalconyLower','Iron',7.22,1.59,.022)
for i in range(8):
 a=i*math.tau/8;b=(i+1)*math.tau/8;r=1.10
 tube('ChamberPost'+str(i),'Roof',(math.cos(a)*r,math.sin(a)*r,7.0),(math.cos(a)*r,math.sin(a)*r,8.05),.043,8)
 v=[(math.cos(q)*1.075,math.sin(q)*1.075,z) for z in [7.05,8.0] for q in [a+.035,b-.035]]
 pane=mesh('LanternPane'+str(i),'Glass',v,[(0,1,3,2)]);solidify=pane.modifiers.new('Glazing thickness','SOLIDIFY');solidify.thickness=.012
ring('LanternHeader','Roof',8.04,1.12,.055)
lathe('Roof','Roof',[(8.02,1.38),(8.10,1.42),(8.16,1.23),(8.50,.14),(8.57,.06)],72,.007)
tube('Finial','Iron',(0,0,8.53),(0,0,8.86),.034,12)
lathe('LensCore','Lens',[(7.18,.22),(7.27,.28),(7.70,.28),(7.80,.20)],32)
for i in range(8):ring('LensRing'+str(i),'Lens',7.25+i*.066,.29,.012,16)

# Explicit metres-per-tile UVs; cylindrical unwrap on round architecture, projected elsewhere.
bpy.ops.object.select_all(action='DESELECT')
for o in list(low.objects):
 if o.type!='MESH':continue
 bpy.context.view_layer.objects.active=o;o.select_set(True)
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);o.select_set(False)
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.00001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 uv=o.data.uv_layers.new(name='UVMap') if not o.data.uv_layers else o.data.uv_layers[0]
 cylindrical=any(s in o.name for s in ['Shaft','Footing','Course','Cornice','GalleryDeck','ChamberFoot','Roof_LOD'])
 for face in o.data.polygons:
  values=[]
  for li in face.loop_indices:
   p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
   if cylindrical:values.append((li,math.atan2(p.y,p.x)/math.tau,p.z/2.4))
   else:
    n=face.normal;k=max(range(3),key=lambda j:abs(n[j]));xy=(p.y,p.z) if k==0 else (p.x,p.z) if k==1 else (p.x,p.y);values.append((li,xy[0]/1.6,xy[1]/1.6))
  seam=cylindrical and max(x[1] for x in values)-min(x[1] for x in values)>.5
  for li,u,v in values:uv.data[li].uv=((u+1 if seam and u<0 else u)*3.4 if cylindrical else u,v)

# Neutral review environment, editable source and explicit runtime axis conversion.
scene.world.color=(.13,.13,.13);scene.view_settings.view_transform='Standard'
for name,power,loc,size in [('Key',1250,(7,-8,12),7),('Fill',500,(-5,-3,7),6)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,4))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('ReferenceCamera');cam=bpy.data.objects.new('ReferenceCamera',d);scene.collection.objects.link(cam);cam.location=(12,-21,17);cam.rotation_euler=(Vector((0,0,4.2))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=11;scene.camera=cam
scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100
try:scene.render.engine='BLENDER_EEVEE'
except:scene.render.engine='BLENDER_EEVEE_NEXT'
scene.render.image_settings.file_format='PNG'
ROOT.joinpath('review').mkdir(exist_ok=True);ROOT.joinpath('exports').mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'AR_CoastLighthouse_master.blend'))
scene.render.filepath=str(ROOT/'review/lighthouse-material.png');bpy.ops.render.render(write_still=True)

groups={};deps=bpy.context.evaluated_depsgraph_get()
for o in low.objects:
 if o.type!='MESH':continue
 key=o['role'];rec=groups.setdefault(key,{'name':'AR_CoastLighthouse_'+key+'_LOD0','material':key,'vertices':[],'normals':[],'uv':[],'triangles':[]})
 ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();uv=me.uv_layers.active.data;normal_matrix=o.matrix_world.to_3x3().inverted().transposed()
 for tri in me.loop_triangles:
  for li in tri.loops:
   k=len(rec['vertices']);v=o.matrix_world@me.vertices[me.loops[li].vertex_index].co;n=normal_matrix@me.corner_normals[li].vector;n.normalize();u=uv[li].uv
   rec['vertices'].append({'x':v.x,'y':v.z,'z':-v.y});rec['normals'].append({'x':n.x,'y':n.z,'z':-n.y});rec['uv'].append({'x':u.x,'y':u.y});rec['triangles'].append(k)
 ev.to_mesh_clear()
data={'meshes':list(groups.values()),'units':'metres','axes':'Unity vertices=(Blender x,z,-y); runtime root yaw 180 and scale .9','inferred':'Single perspective concept; hidden faces and dimensions inferred.'}
(ROOT/'exports/LighthouseMeshes.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
audit={k:len(v['triangles'])//3 for k,v in groups.items()};assert sum(audit.values())<=30000,audit
(ROOT/'review/lighthouse-build-audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='DESELECT')
for o in low.objects:
 if o.type=='MESH':o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'exports/AR_CoastLighthouse.glb'),export_format='GLB',use_selection=True,export_apply=True)
print('LIGHTHOUSE_BUILD_OK',audit)
