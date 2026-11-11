import bpy, math, os
from mathutils import Vector
ROOT=r'C:/UnityProjects/theghost'
OUT=ROOT+'/RecoveredProjects/TheGhost_School_Unity6/Assets/Recovery/M6/Models'
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
colors={'Steel':(.19,.21,.20,1),'DarkSteel':(.055,.065,.06,1),'Paint':(.12,.20,.16,1),'Wood':(.19,.10,.045,1),'Brass':(.40,.29,.10,1),'Recess':(.025,.025,.02,1)}
mats={}
for name,c in colors.items():
 m=bpy.data.materials.new(name);m.diffuse_color=c;m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=c;p.inputs['Metallic'].default_value=0 if name=='Wood' else .7;p.inputs['Roughness'].default_value=.78;mats[name]=m
objects=[]
def finish(o,name,mat):
 o.name=name;o.data.materials.append(mats[mat]);objects.append(o);return o
def box(name,loc,size,mat='Steel',bevel=.003):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Worn rounded edges','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,mat)
def cyl(name,loc,r,depth,mat='Steel',rot=(0,0,0),verts=20):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc,rotation=rot);o=bpy.context.object
 mod=o.modifiers.new('Edge bevel','BEVEL');mod.width=min(.002,depth*.15);mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,mat)
def ring(name,loc,major,minor,mat='Steel',rot=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=28,minor_segments=8,location=loc,rotation=rot);return finish(bpy.context.object,name,mat)
def key(loc,scale=1,mat='Brass'):
 x,y,z=loc;ring('Key bow',(x,y+.04*scale,z),.024*scale,.005*scale,mat)
 box('Key shaft',(x,y-.025*scale,z),(.009*scale,.1*scale,.009*scale),mat,.001)
 for i,w in enumerate([.02,.013,.023]):box('Key tooth',(x+w/2,y-.065*scale+i*.014*scale,z),(w,.008*scale,.009*scale),mat,.001)
def export(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 # UVs retain bevel detail while providing consistent procedural grime.
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
 bpy.ops.export_scene.fbx(filepath=OUT+'/'+name+'.fbx',use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 for o in objects:o.hide_set(True)
 objects.clear()
# All pickups lie flat on XY; Blender Z becomes Unity Y.
box('Mold base',(0,0,.012),(.23,.16,.024),'DarkSteel')
for x in [-.108,.108]:box('Mold rim',(x,0,.031),(.014,.16,.022),'Steel')
for y in [-.072,.072]:box('Mold rim',(0,y,.031),(.20,.014,.022),'Steel')
key((-.015,0,.027),.85,'Recess')
for x in [-.085,.085]:
 for y in [-.05,.05]:cyl('Rivet',(x,y,.027),.006,.006,'Brass',verts=8)
export('KeyFrame')
box('Metal blank',(0,0,.009),(.06,.20,.018),'Steel',.004)
box('Stamped grip',(0,.065,.020),(.045,.04,.004),'DarkSteel',.001)
export('MetalBlank')
# Tapered saw plate with real serrated teeth.
outline=[(-.24,.055),(.22,.035),(.24,-.015)]
for i in range(33):outline.append((.23-i*.014,-.035-(.012 if i%2 else 0)))
outline.append((-.24,-.04));n=len(outline)
verts=[(x,y,z) for z in [.003,.007] for x,y in outline]
faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh=bpy.data.meshes.new('Serrated steel');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Saw blade',mesh);bpy.context.collection.objects.link(o);finish(o,'Saw blade','Steel')
for x in [-.22,-.18]:cyl('Fixing hole',(x,.015,.008),.007,.001,'Recess',verts=12)
export('SawBlade')
# Thick wooden D handle with an actual opening.
outer=[(-.085,-.065),(.06,-.065),(.09,-.025),(.09,.055),(.05,.08),(-.085,.055)]
inner=[(-.045,-.035),(.035,-.035),(.05,-.01),(.05,.035),(.025,.048),(-.045,.025)]
verts=[(x,y,z) for z in [.004,.036] for loop in [outer,inner] for x,y in loop];faces=[]
for i in range(6):
 j=(i+1)%6;faces += [(i,j,j+6,i+6),(12+i,18+i,18+j,12+j),(i,12+i,12+j,j),(6+i,6+j,18+j,18+i)]
mesh=bpy.data.meshes.new('Wood grip');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Saw handle',mesh);bpy.context.collection.objects.link(o);finish(o,'Saw handle','Wood')
bevel=o.modifiers.new('Rounded grip','BEVEL');bevel.width=.006;bevel.segments=3;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=bevel.name)
for y in [-.04,.035]:cyl('Handle fixing',(-.065,y,.038),.008,.005,'Brass',verts=10)
export('SawHandle')
for i in range(3):
 x=(i-1)*.035;y=(i%2)*.015;cyl('Screw shaft',(x,y,.035),.006,.06,'Steel',verts=12);cyl('Slotted screw head',(x,y,.067),.011,.008,'Steel',verts=12);box('Screw slot',(x,y,.0715),(.016,.003,.001),'Recess',.0001)
 for j in range(7):ring('Thread',(x,y,.012+j*.006),.006,.0013,'DarkSteel')
export('Screws')
key((0,0,.008),1.3);export('MainKey')
# Bench and old mechanical key cutting machine. Front faces Blender -Y.
for x in [-.47,.47]:
 for y in [-.26,.26]:box('Bench leg',(x,y,.40),(.075,.075,.8),'Wood',.006)
box('Worktop',(0,0,.82),(1.15,.72,.075),'Wood',.008)
for y in [-.26,.26]:box('Bench brace',(0,y,.24),(.95,.045,.07),'DarkSteel')
box('Cast base',(0,.07,.89),(.69,.44,.08),'Paint',.016)
box('Motor casing',(-.19,.12,1.04),(.22,.28,.25),'Paint',.025)
for i in range(6):box('Motor cooling fin',(-.19,.015+i*.035,1.175),(.20,.012,.018),'DarkSteel',.002)
cyl('Spindle',(.04,.12,1.075),.022,.30,'Steel',(0,math.pi/2,0))
cyl('Cutting disc',(.14,.12,1.075),.10,.012,'Steel',(0,math.pi/2,0),40)
box('Wheel guard',(.15,.19,1.16),(.10,.17,.08),'Paint',.015)
for x in [-.02,.20]:
 box('Vice carriage',(x,-.075,.96),(.12,.16,.07),'DarkSteel',.006)
 box('Vice jaw',(x,-.03,1.008),(.11,.035,.04),'Steel',.002)
 cyl('Vice screw',(x,-.14,.98),.012,.12,'Steel',(math.pi/2,0,0))
 cyl('Vice knob',(x,-.22,.98),.023,.025,'Wood',(math.pi/2,0,0))
box('Receiving tray',(.34,-.22,.87),(.30,.19,.015),'Steel')
for x in [.19,.49]:box('Tray edge',(x,-.22,.885),(.012,.19,.025),'DarkSteel')
box('Tray back',(.34,-.125,.885),(.30,.012,.025),'DarkSteel')
box('Maker plate',(-.20,-.032,1.06),(.12,.006,.05),'Brass',.002)
for i in range(3):box('Engraved label',(-.20,-.036,1.045+i*.013),(.085,.002,.003),'Recess',.0001)
cyl('Switch',(-.3,-.12,.93),.016,.015,'DarkSteel')
export('KeyMachine')
# Retain editable sources, unhide all before saving.
for o in bpy.data.objects:o.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/SourceAssets/M6/M6Props.blend')
print('M6 models exported')
