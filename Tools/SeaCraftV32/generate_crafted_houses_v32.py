"""Build three metre-scale village buildings and the slipway as Blender masters and Unity mesh data.

Run in Blender with -- --project PROJECT_ROOT. The v31 footprints, room heights and
occupational props provide the placement contract. Only the render meshes are replaced.
"""
import argparse
import importlib.util
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROLES = ("Timber", "SlateRoof", "SeaStone", "WarmGlass", "Brass", "Rope", "Beam")
TINTS = (
    (.55, .36, .23, 1), (.21, .31, .34, 1), (.50, .51, .45, 1),
    (1, .67, .20, 1), (.58, .41, .15, 1), (.38, .32, .19, 1), (.38, .25, .16, 1)
)
HOUSES = ("Village_V32_Boatwright", "Village_V32_Tavern", "Village_V32_Observatory")


def load_v31(project):
    script = project / "Tools/SeaVillageV31/generate_village_and_biodiversity_v31.py"
    spec = importlib.util.spec_from_file_location("village_v31", script)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class CraftMesh:
    def __init__(self, base, seed):
        self.base = base
        self.rng = random.Random(seed)
        self.mesh = bmesh.new()
        self.uv = self.mesh.loops.layers.uv.new("UVMap")
        self.color = self.mesh.loops.layers.color.new("PieceTintAO")
        self.counts = {role: 0 for role in ROLES}

    def finish_piece(self, faces, role, shade, grain_axis=None):
        self.counts[ROLES[role]] += 1
        for face in faces:
            face.material_index = role
            face.smooth = False
            face.normal_update()
            n = face.normal
            ao = .58 if n.y < -.55 else (.85 if n.y < .20 else 1.)
            heat = self.rng.uniform(.96, 1.04)
            axes = [a for a in range(3) if a != max(range(3), key=lambda a: abs(n[a]))]
            if grain_axis in axes:
                axes.remove(grain_axis)
                axes.insert(0, grain_axis)
            for loop in face.loops:
                p = loop.vert.co
                loop[self.uv].uv = (p[axes[0]] * .75, p[axes[1]] * .75)
                loop[self.color] = (
                    min(1, shade * heat), min(1, shade), min(1, shade / heat), ao
                )

    def box(self, point, size, role=0, bevel=.04, rotation=0, shade=None, grain_axis=None, axis="Z"):
        # Closed octagonal rings, with a short bevel band on both ends.
        sx, sy, sz = (q * .5 for q in size)
        bx, by, bz = min(bevel, sx*.30), min(bevel, sy*.45), min(bevel, sz*.30)
        rotate = Matrix.Rotation(rotation, 3, axis)
        origin = Vector(point)
        rings = []
        for height, inset in [(-sy, 1), (-sy+by, 0), (sy-by, 0), (sy, 1)]:
            hx, hz = sx-bx*.48*inset, sz-bz*.48*inset
            ring = [
                (-hx+bx, -hz), (hx-bx, -hz), (hx, -hz+bz), (hx, hz-bz),
                (hx-bx, hz), (-hx+bx, hz), (-hx, hz-bz), (-hx, -hz+bz)
            ]
            rings.append([self.mesh.verts.new(origin + rotate @ Vector((x, height, z))) for x, z in ring])
        faces = [self.mesh.faces.new(rings[0]), self.mesh.faces.new(list(reversed(rings[-1])))]
        for lower, upper in zip(rings, rings[1:]):
            for i in range(8):
                j = (i + 1) % 8
                faces.append(self.mesh.faces.new((lower[i], upper[i], upper[j], lower[j])))
        self.finish_piece(faces, role, self.rng.uniform(.78,.97) if shade is None else shade, grain_axis)

    def tube(self, points, radii, role=0, sides=8):
        old = set(self.mesh.faces)
        self.base.add_tapered_tube(self.mesh, self.uv, points, radii, sides)
        self.finish_piece((face for face in self.mesh.faces if face not in old), role, .86)

    def deck(self, cx, cy, cz, width, length, thick=.18):
        count = max(3, round(length/.21))
        stride = length/count
        for index in range(count):
            z = cz-length*.5+stride*(index+.5)
            self.box((cx,cy,z), (width-self.rng.uniform(0,.032),thick,stride-.012),
                     bevel=.025, grain_axis=0)
        for side in (-1,1):
            self.box((cx+side*width*.37,cy-thick*.62,cz), (.13,.13,length-.02), 6, .018, grain_axis=2)

    def stone_course(self, cx, cz, width, depth, height=.50, y=0):
        # Staggered courses on the perimeter. A recessed core keeps the bed closed.
        self.box((cx,y+height*.5,cz), (width-.20,height,depth-.20), 2, .055, shade=.76)
        rows = max(2, round(height/.29))
        for level in range(rows):
            base_y = y+(level+.5)*height/rows
            for face in range(4):
                span = width if face < 2 else depth-.44
                count = max(1, round(span/(.71 if level % 2 == 0 else .60)))
                size = span/count
                for j in range(count):
                    u = -span*.5+(j+.5)*size
                    a = .23 + self.rng.uniform(-.025,.028)
                    position = ((cx+u,base_y,cz+(-1 if face == 0 else 1)*(depth-a)*.5)
                                if face < 2 else
                                (cx+(-1 if face == 2 else 1)*(width-a)*.5,base_y,cz+u))
                    dimensions = (size-.012,height/rows-.012,a) if face < 2 else (a,height/rows-.012,size-.012)
                    self.box(position, dimensions, 2, .041)

    def slope(self, center, slope_length, depth, angle, eave_side):
        self.box(center, (slope_length,.13,depth), 1,.035,angle,shade=.67)
        rot = Matrix.Rotation(angle,3,'Z')
        origin = Vector(center)
        rows = max(5, round(slope_length/.42))
        tile_depth = depth/max(5,round(depth/.53))
        run = slope_length/rows
        for row in range(rows):
            along = eave_side*(slope_length*.5-(row+.52)*run)
            cursor = -depth*.5
            while cursor < depth*.5-.018:
                width = min(tile_depth*(.50 if row%2 and cursor == -depth*.5 else 1), depth*.5-cursor)
                if width < .04:
                    break
                tile_center = origin + rot @ Vector((along, .085+row*.003, cursor+width*.5))
                self.box(tile_center,(run*1.12,.074,width-.014),1,.04,angle,
                         shade=self.rng.uniform(.77,.99),grain_axis=0)
                cursor += width
        for z in (-depth*.5-.008,depth*.5+.008):
            trim = origin + rot @ Vector((0,-.065,z))
            self.box(trim,(slope_length+.08,.20,.085),6,.023,angle,grain_axis=0)

    def cottage(self, cx, cz, width, height, depth):
        self.stone_course(cx,cz,width+.40,depth+.40)
        self.box((cx,.5+height*.5,cz), (width-.06,height,depth-.06), bevel=.035,shade=.65)
        bands = max(7,round(height/.30))
        pitch = height/bands
        for index in range(bands):
            y = .50+(index+.5)*pitch
            for side in (-1,1):
                self.box((cx,y,cz+side*depth*.5),(width-.08,pitch*1.03,.118),
                         bevel=.019,rotation=-side*.12,grain_axis=0,axis="X")
                self.box((cx+side*width*.5,y,cz),(.118,pitch*1.03,depth-.08),
                         bevel=.019,rotation=side*.12,grain_axis=2)
        for x in (-width*.5,width*.5):
            for z in (-depth*.5,depth*.5):
                self.box((cx+x,.5+height*.5,cz+z),(.19,height+.08,.19),6,.030,grain_axis=1)
        roof_height = 1.35
        overhang = width*.5+.42
        slant = math.hypot(overhang,roof_height)
        angle = math.atan2(roof_height,overhang)
        for side in (-1,1):
            self.slope((cx+side*overhang*.5,.50+height+roof_height*.5,cz),
                       slant+.04,depth+.84,-side*angle,side)
        self.box((cx,.5+height+roof_height+.08,cz),(.24,.16,depth+.98),6,.045,grain_axis=2)
        for side in (-1,1):
            z = cz+side*depth*.5
            face = self.mesh.faces.new([self.mesh.verts.new(p) for p in
                [(cx-width*.5,.5+height,z),(cx+width*.5,.5+height,z),(cx,.5+height+roof_height,z)]])
            if side < 0:
                face.normal_flip()
            self.finish_piece([face],0,.76,0)
        for x in (-width*.28,width*.28):
            self.box((cx+x,1.95,cz+depth*.5+.12),(.80,.90,.055),3,.014)
            for dx in (-.43,0,.43):
                self.box((cx+x+dx,1.95,cz+depth*.5+.155),(.066,1.,.073),6,.015,grain_axis=1)
            for dy in (-.46,0,.46):
                self.box((cx+x,1.95+dy,cz+depth*.5+.155),(.90,.066,.073),6,.015,grain_axis=0)
        self.box((cx,1.40,cz+depth*.5+.13),(1.,1.80,.12),6,.035,grain_axis=1)
        self.deck(cx,.48,cz+depth*.5+.65,width+.35,1.20,.16)
        self.box((cx,.14,cz+depth*.5+1.15),(1.3,.25,.65),2,.055)

    def slipway(self):
        self.deck(0,.42,4.40,4.10,3.10,.20)
        for x in (-1.7,1.7):
            for z in (3.2,5.5):
                self.box((x,-.25,z),(.20,1.60,.20),6,.033,grain_axis=1)
        self.box((0,.69,4.60),(.18,.22,2.60),0,.026,shade=.98,grain_axis=2)
        for z in (3.55,4.1,4.65,5.2):
            self.tube([(-.65,1.15,z),(-.48,.84,z),(0,.72,z),(.48,.84,z),(.65,1.15,z)],[.055]*5)
        for x in (-.85,.85):
            self.box((x,.50,6.),(.18,.16,1.80),6,.025,grain_axis=2)

    def export(self, project, name):
        self.mesh.normal_update()
        mesh = bpy.data.meshes.new(name)
        self.mesh.to_mesh(mesh)
        self.mesh.free()
        mesh.calc_loop_triangles()
        vertices, normals, uv, rgba = [], [], [], []
        submeshes = [[] for _ in ROLES]
        colors = mesh.color_attributes["PieceTintAO"].data
        loop_indices = {}
        for triangle in mesh.loop_triangles:
            normal = mesh.polygons[triangle.polygon_index].normal
            for i in triangle.loops:
                if i not in loop_indices:
                    loop_indices[i] = len(vertices)//3
                    vertices.extend(round(value, 5) for value in mesh.vertices[mesh.loops[i].vertex_index].co)
                    normals.extend(round(value, 5) for value in normal)
                    uv.extend(round(value, 5) for value in mesh.uv_layers.active.data[i].uv)
                    rgba.extend(round(value, 4) for value in colors[i].color_srgb)
                submeshes[triangle.material_index].append(loop_indices[i])
        parts = [{"role": ROLES[i], "triangles": t} for i,t in enumerate(submeshes) if t]
        data = {"name":name, "vertices":vertices, "normals":normals, "uv":uv, "rgba":rgba, "parts":parts}
        root = project / "Tools/SeaCraftV32/SourceMeshes"
        root.mkdir(parents=True,exist_ok=True)
        (root/(name+".json")).write_text(json.dumps(data,separators=(",",":")),encoding="utf-8")
        roles = project / "Assets/Deepsea/Resources/SeaCraftV32/Roles"
        roles.mkdir(parents=True,exist_ok=True)
        (roles/(name+".json")).write_text(json.dumps({"parts":[{"role": part["role"]} for part in parts]}),
                                           encoding="utf-8")

        # Author in Unity metres. The edit master uses Blender's Z-up basis.
        for vertex in mesh.vertices:
            x,y,z = vertex.co
            vertex.co = (x,-z,y)
        obj = bpy.data.objects.new(name,mesh)
        bpy.context.collection.objects.link(obj)
        for role,tint in zip(ROLES,TINTS):
            material = bpy.data.materials.new(role)
            material.diffuse_color = tint
            obj.data.materials.append(material)
        bpy.context.scene.unit_settings.system = 'METRIC'
        bpy.context.scene.unit_settings.scale_length = 1.0
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        masters = project / "Tools/SeaCraftV32/Masters"
        masters.mkdir(parents=True,exist_ok=True)
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(masters/(name+".blend")))
        return {"name":name, "triangles":len(mesh.loop_triangles), "pieces":self.counts,
                "roles":[p["role"] for p in parts]}


def build(project):
    base = load_v31(project)
    manifests = []
    for kind,name in enumerate(HOUSES+("Village_V32_Slipway",)):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        art = CraftMesh(base, 32031+kind)
        if kind == 3:
            art.slipway()
        else:
            w,h,d = ((4.6,2.6,5.4),(5.6,3.2,6.2),(4.2,2.5,4.6))[kind]
            art.cottage(0,0,w,h,d)
            if kind == 0:
                art.slipway()
            elif kind == 1:
                art.cottage(3.35,-.65,3.0,2.25,3.9)
                art.stone_course(-2.1,-1.6,.80,.80,4.90,y=.25)
                art.box((-2.1,5.18,-1.6),(1.05,.25,1.05),2,.045)
                art.slope((0,2.50,4.10),5.60,1.60,.05,1)
                for x in (-2.5,2.5):
                    art.box((x,1.38,4.7),(.14,2.5,.14),6,.027,grain_axis=1)
                for x,z in ((-3.25,3.3),(-3.1,4.1)):
                    art.tube([(x,0,z),(x,.4,z),(x,.8,z)],[.30,.37,.30],6,10)
                    art.box((x,.81,z),(.48,.08,.48),6,.025)
            else:
                art.tube([(2.25,0,.6),(2.25,4.25,.6)],[1.10,1.10],2,8)
                for row in range(14):
                    for side in range(8):
                        a = math.tau * side/8
                        x,z = 2.25+math.cos(a)*1.06,.6+math.sin(a)*1.06
                        art.box((x,(row+.5)*.30,z),(.84,.286,.16),2,.038,
                                rotation=math.pi/2-a,axis="Y")
                art.tube([(2.25,4.15,.6),(2.25,5.65,.6)],[1.46,.02],1,8)
                for row in range(8):
                    y = 4.15+row*.184
                    radius = 1.50-row*.181
                    art.tube([(2.25,y,.6),(2.25,y+.21,.6)],[radius,max(.025,radius-.22)],1,8)
                for a in (0,math.pi/2,math.pi,3*math.pi/2):
                    x,z=2.25+math.cos(a)*1.16,.6+math.sin(a)*1.16
                    art.box((x,3.60,z),(.65,.70,.09),3,.02,rotation=math.pi/2-a,axis="Y")
                art.deck(0,.35,3.5,4.4,2.3,.17)
                art.box((.8,.82,3.7),(.12,.90,.12),4,.024)
                art.tube([(.8,1.4,3.5),(.8,1.65,4.15)],[.12,.18],4,10)
                art.box((-.95,.9,3.7),(1.15,.13,.65),0,.032)
                for x in (-1.4,-.5):
                    art.box((x,.48,3.7),(.08,.85,.08),6,.012)
            art.box((1.25,2.4,d*.5+.45),(.20,.28,.20),3,.026)
            for x in (1.12,1.38):
                art.box((x,2.4,d*.5+.45),(.035,.36,.27),6,.006)
        manifests.append(art.export(project,name))
    path = project / "Tools/SeaCraftV32/model-manifest.json"
    path.write_text(json.dumps({"models":manifests,"basis":"Y-up metre Unity source, Z-up Blender masters"},indent=2),encoding="utf-8")
    print("CRAFT_V32_MESHES",len(manifests),sum(x["triangles"] for x in manifests))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--project",required=True,type=Path)
    build(parser.parse_args(sys.argv[sys.argv.index("--")+1:]).project.resolve())
