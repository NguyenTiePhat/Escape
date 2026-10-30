"""Run with Blender --background --python Tools/build_campus.py. No downloaded assets."""
import bpy, json, math, random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
random.seed(18)
parts, labels = [], []
wall=(.58,.60,.55); teal=(.15,.29,.32); concrete=(.29,.33,.32)
metal=(.10,.15,.16); wood=(.29,.21,.13); glass=(.12,.23,.26)
def box(name,p,s,c=wall,floor=-1,solid=True,shape='cube'):
    parts.append(dict(name=name,p=list(p),s=list(s),color=list(c),floor=floor,solid=solid,shape=shape))
def sign(text,x,y,z):
    box('Sign plate '+text,(x,y,z),(2.7,.42,.08),teal)
    labels.append(dict(text=text,p=[x,y,z-.055]))
def desk(x,y,z):
    box('Desk top',(x,y+.76,z),(1.35,.09,.65),wood)
    for dx in [-.53,.53]:
        for dz in [-.23,.23]: box('Desk steel leg',(x+dx,y+.36,z+dz),(.045,.72,.045),metal)
    box('Chair seat',(x,y+.44,z+.65),(.48,.07,.48),wood)
    box('Chair back',(x,y+.76,z+.87),(.48,.55,.06),wood)
    for dx in [-.18,.18]:
        for dz in [.48,.82]: box('Chair leg',(x+dx,y+.21,z+dz),(.04,.42,.04),metal)

box('Courtyard',(0,-.22,-9),(88,.4,72),(.25,.29,.28))
for x in range(-40,41,4): box('Paving joint',(x,-.013,-10),(.025,.012,64),(.18,.22,.21),solid=False)
for z in range(-40,24,4): box('Paving joint',(0,-.013,z),(80,.012,.025),(.18,.22,.21),solid=False)
rooms=[['A101 / GIANG DUONG','A102 / GIANG DUONG','A103 / Y TE','A104 / GIAO VU','A105 / KHO','A106 / VE SINH'],
       ['A201 / GIANG DUONG','A202 / THU VIEN','A203 / MAY TINH','A204 / THI NGHIEM','A205 / KY THUAT','A206 / VE SINH']]
for level in range(2):
    y=level*4.2
    start=len(parts)
    box('Floor slab',(0,y-.12,5),(57,.24,14.5),concrete)
    for x in [-28,28]: box('End wall',(x,y+1.9,7),(.25,3.8,10),wall)
    for i in range(6):
        x=-20+i*8
        box('Room partition',(x-4,y+1.9,7),(.18,3.8,10))
        # Wide door opening centered on room: clear from floor to 2.6m.
        for dx in [-2.5,2.5]: box('Gallery wall',(x+dx,y+1.9,2),(3,3.8,.2))
        box('Door lintel',(x,y+3.2,2),(2,1.2,.2))
        for dx in [-1.05,1.05]: box('Door jamb',(x+dx,y+1.3,1.86),(.12,2.6,.18),teal)
        sign(rooms[level][i],x,y+2.96,1.83)
        box('Window sill wall',(x,y+.65,12),(8,1.3,.25))
        box('Window header',(x,y+3.45,12),(8,.7,.25))
        for dx in [-3.8,0,3.8]: box('Window pier',(x+dx,y+2.2,12),(.2,1.8,.25),teal)
        for dx in [-1.9,1.9]:
            box('Dusty rear glazing',(x+dx,y+2.2,12),(3.6,1.7,.06),glass)
            box('Window crossbar',(x+dx,y+2.2,11.94),(3.6,.05,.08),metal)
        box('Gallery pillar',(x-4,y+1.95,-1.8),(.38,3.9,.38),teal)
        box('Light housing',(x,y+3.78,0),(1.5,.08,.24),metal)
        box('Light strip',(x,y+3.72,0),(1.35,.025,.15),(.72,.86,.76),solid=False)
        if level==1:
            box('Balcony cap',(x,y+1.1,-1.8),(8,.10,.18),teal)
            for dx in range(-4,4): box('Balcony baluster',(x+dx,y+.55,-1.8),(.06,1.1,.06),teal)
        if i in [0,1] and not (level==1 and i==1):
            box('Blackboard',(x,y+1.9,11.72),(5,1.5,.07),(.075,.15,.13))
            for dx in [-2.2,0,2.2]:
                for dz in [4.2,6.1,8]: desk(x+dx,y,dz)
        elif level==1 and i==1:
            for dx in [-2.7,0,2.7]:
                box('Library shelf',(x+dx,y+1.2,8),(.4,2.4,5),wood)
                for h in [.45,1.05,1.65,2.25]:
                    for dz in range(5): box('Old books',(x+dx,y+h,6+dz),(.5,.4,.65),(.22+random.random()*.12,.23,.19))
        elif i==5:
            for dx in [-2.3,0,2.3]:
                box('WC divider',(x+dx,y+1.1,8),(.12,2.2,3),teal)
                box('WC pedestal',(x+dx+.7,y+.35,9),(.6,.7,.8),(.65,.66,.6))
            box('Sink counter',(x,y+.85,4.3),(5,.18,.7),wall)
        elif level==0 and i==2:
            for dx in [-2,2]:
                box('Clinic bed',(x+dx,y+.55,8),(1.5,.35,3),(.49,.55,.49))
                box('Pillow',(x+dx,y+.8,9),(1,.18,.55),wall)
        else:
            for dx in [-2,1.5]:
                for dz in [5,8]:
                    desk(x+dx,y,dz)
                    if level==1 and i==2:
                        box('Computer monitor',(x+dx,y+1.18,dz),(.75,.65,.1),metal)
                        box('Dead screen',(x+dx,y+1.18,dz-.06),(.66,.54,.02),glass)
            for dx in [-2,0,2]: box('Storage cabinet',(x+dx,y+1.1,11.2),(1.4,2.2,.65),teal)
        # Weathering stays flush with walls, not a navigation obstacle.
        for j in range(12):
            xx=x+random.uniform(-3.7,3.7); zz=random.uniform(3,11)
            box('Plaster debris',(xx,y+.025,zz),(.1+random.random()*.22,.035,.12),(.48,.47,.40),solid=False)
        for j in range(5):
            box('Damp plaster stain',(x-3.85+random.random()*2,y+random.uniform(.15,1),1.88),(.2+random.random()*.5,.18+random.random()*.5,.012),(.31,.36,.29),solid=False)
    box('Last room partition',(24,y+1.9,7),(.18,3.8,10))
    for p in parts[start:]: p['floor']=level
box('Roof slab',(0,8.35,5),(58,.28,15),concrete,floor=2)
box('Fascia',(0,8.63,-2.4),(58,.55,.2),teal,floor=2)
sign('IUH   |   KHU GIANG DUONG A',0,8.64,-2.55)
for x in [-26,26]:
    for n in range(21):
        height=(n+1)*.2
        box('Stair tread',(x,height/2,-12+(n+.5)*.5),(3.2,height,.5),concrete)
        for side in [-1,1]:
            box('Stair railing post',(x+side*1.55,height+.5,-12+(n+.5)*.5),(.06,1,.06),teal)
            box('Stair rail segment',(x+side*1.55,height+1,-12+(n+.5)*.5),(.09,.09,.55),teal)
    box('Upper stair landing',(x,4.08,-.9),(3.2,.24,1.3),concrete)
    sign('TANG 2',x,5.9,1.8)
# Perimeter with opening toward the main approach.
for x in [-42,42]: box('Boundary wall',(x,1,0),(.4,2,65),concrete)
box('Rear boundary',(0,1,24),(84,2,.4),concrete)
for x in [-26,26]: box('Front boundary',(x,.6,-37),(32,1.2,.4),concrete)
for x in list(range(-41,-9,2))+list(range(10,42,2)):
    box('Fence bar',(x,1.55,-37),(.08,1.8,.08),metal)
for x in [-9,9]: box('Gate pier',(x,1.7,-37),(.8,3.4,.8),teal)
box('Gate sign',(0,3.4,-37),(19,.65,.3),teal)
sign('IUH / CONG PHU',0,3.42,-37.2)
for x,z in [(-34,-10),(34,-10),(-32,-26),(32,-26),(-35,18),(35,18)]:
    box('Tree planter',(x,.2,z),(4,.4,4),concrete)
    box('Soil',(x,.42,z),(3.6,.08,3.6),(.15,.19,.12))
    box('Tree trunk',(x,2.4,z),(.45,4.5,.45),wood)
    for j in range(7):
        box('Tree canopy',(x+random.uniform(-2,2),4.5+random.uniform(0,2.4),z+random.uniform(-2,2)),(3.2,2.7,3),(.13+random.random()*.05,.22,.16),shape='sphere')
    box('Concrete bench',(x+3,.5,z),(1.1,.18,3),wall)
    for dz in [-1,1]: box('Bench base',(x+3,.23,z+dz),(.8,.46,.35),concrete)
# Guardhouse and bike shelter.
box('Guardhouse',( -34,1.5,-33),(5,3,4),wall)
box('Guardhouse window',(-34,1.9,-35.03),(3.2,1.3,.05),glass)
box('Guardhouse roof',(-34,3.1,-33),(6,.2,5),teal)
for x in [29,39]:
    for z in [-20,-32]: box('Shelter post',(x,1.5,z),(.12,3,.12),metal)
box('Bike shelter canopy',(34,3,-26),(12,.18,14),teal)
for z in range(-30,-20,2):
    box('Abandoned bike frame',(34,.6,z),(1.6,.12,.12),metal)
    for x in [33.3,34.7]: box('Bike wheel',(x,.35,z),(.5,.55,.12),metal,shape='sphere')
box('Flagpole',(0,4,-16),(.10,8,.10),(.43,.47,.46))
box('Flagpole base',(0,.2,-16),(2,.4,2),concrete)
box('Faded flag',(.8,7.4,-16),(1.5,.8,.025),(.31,.13,.12),solid=False)

(ROOT/'Assets/Resources/campus.json').write_text(json.dumps(dict(parts=parts,labels=labels),ensure_ascii=False),encoding='utf-8')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
mats={}
for part in parts:
    if part['shape']=='sphere': bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.5)
    else: bpy.ops.mesh.primitive_cube_add(size=1)
    obj=bpy.context.object; obj.name=part['name']
    x,y,z=part['p']; sx,sy,sz=part['s']; obj.location=(x,-z,y); obj.scale=(sx,sz,sy)
    key=tuple(part['color'])
    if key not in mats:
        mat=bpy.data.materials.new('Campus material'); mat.diffuse_color=(*key,1); mat.use_nodes=True
        bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*key,1); bs.inputs['Roughness'].default_value=.87
        mats[key]=mat
    obj.data.materials.append(mats[key])
for label in labels:
    curve=bpy.data.curves.new('Sign','FONT'); curve.body=label['text']; curve.align_x='CENTER'; curve.align_y='CENTER'; curve.size=.19; curve.extrude=.001
    obj=bpy.data.objects.new('Sign text',curve); bpy.context.collection.objects.link(obj)
    x,y,z=label['p']; obj.location=(x,-z,y); obj.rotation_euler=(math.pi/2,0,math.pi)
    mat=bpy.data.materials.new('Lettering'); mat.diffuse_color=(.78,.85,.78,1); obj.data.materials.append(mat)
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.cycles.use_denoising=True; scene.render.resolution_x=1600; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.world.color=(.32,.36,.4)
bpy.ops.object.light_add(type='AREA',location=(0,4,35)); bpy.context.object.data.energy=16000; bpy.context.object.data.shape='DISK'; bpy.context.object.data.size=40
bpy.ops.object.light_add(type='SUN',location=(20,20,30)); bpy.context.object.rotation_euler=(.5,-.3,-.5); bpy.context.object.data.energy=1.8; bpy.context.object.data.angle=.4
for level in range(2):
    for x in [-20,-12,-4,4,12,20]:
        bpy.ops.object.light_add(type='AREA',location=(x,0,3.6+level*4.2)); bpy.context.object.data.energy=45; bpy.context.object.data.color=(.72,.85,.76); bpy.context.object.data.size=1.5
        bpy.ops.object.light_add(type='AREA',location=(x,-7,3.6+level*4.2)); bpy.context.object.data.energy=180; bpy.context.object.data.color=(.72,.80,.76); bpy.context.object.data.size=4
bpy.ops.object.camera_add(); cam=bpy.context.object; scene.camera=cam
def shot(name,position,target,lens):
    cam.location=position; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='PERSP'; cam.data.lens=lens
    scene.render.filepath=str(ROOT/'Preview'/name); bpy.ops.render.render(write_still=True)
shot('01-courtyard.png',(54,65,34),(0,0,2),43)
shot('02-gallery.png',(-22,0,5.85),(15,0,5.7),22)
shot('03-classroom.png',(-22,-3.4,1.7),(-19,-9,1.4),20)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Preview/IUH-campus.blend'))
print('CAMPUS_COMPLETE',len(parts),'parts')
