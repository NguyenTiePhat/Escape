import bpy, os, random, math
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.open_mainfile(filepath=OUT+'/SmallCabinet.blend')
random.seed(47)
for name in ['Walnut vertical grain','Walnut horizontal grain']:
 m=bpy.data.materials[name]; n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
 ramp=next(x for x in n if x.type=='VALTORGB')
 ramp.color_ramp.elements[0].color=(.009,.006,.004,1)
 ramp.color_ramp.elements[1].color=(.066,.042,.023,1)
 p.inputs['Roughness'].default_value=.83
 noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=17; noise.inputs['Detail'].default_value=5
 coord=next(x for x in n if x.type=='TEX_COORD'); l.new(coord.outputs['Generated'],noise.inputs['Vector'])
 grime=n.new('ShaderNodeMixRGB'); grime.blend_type='MULTIPLY'; grime.inputs[0].default_value=.65
 l.new(ramp.outputs[0],grime.inputs[1]); l.new(noise.outputs['Fac'],grime.inputs[2])
 l.new(grime.outputs[0],p.inputs['Base Color'])
 rough=n.new('ShaderNodeMapRange'); rough.inputs['To Min'].default_value=.62; rough.inputs['To Max'].default_value=.94
 l.new(noise.outputs['Fac'],rough.inputs[0]); l.new(rough.outputs[0],p.inputs['Roughness'])
 # Curvature wear: brighter exposed edges broken up by grime.
 bevel=n.new('ShaderNodeBevel'); bevel.inputs['Radius'].default_value=.0015; bevel.samples=4
 geom=n.new('ShaderNodeNewGeometry'); dot=n.new('ShaderNodeVectorMath'); dot.operation='DOT_PRODUCT'
 l.new(geom.outputs['Normal'],dot.inputs[0]); l.new(bevel.outputs['Normal'],dot.inputs[1])
 edge=n.new('ShaderNodeMath'); edge.operation='SUBTRACT'; edge.inputs[0].default_value=1; l.new(dot.outputs['Value'],edge.inputs[1])
 boost=n.new('ShaderNodeMath'); boost.operation='MULTIPLY'; boost.inputs[1].default_value=5; boost.use_clamp=True; l.new(edge.outputs[0],boost.inputs[0])
 wear=n.new('ShaderNodeMixRGB'); wear.inputs[2].default_value=(.13,.092,.055,1); l.new(boost.outputs[0],wear.inputs[0]); l.new(grime.outputs[0],wear.inputs[1]); l.new(wear.outputs[0],p.inputs['Base Color'])
m=bpy.data.materials['Antique brass']; p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(.095,.07,.028,1); p.inputs['Roughness'].default_value=.7
scratch=bpy.data.materials.new('Exposed faded timber'); scratch.use_nodes=True; p=scratch.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(.105,.077,.049,1); p.inputs['Roughness'].default_value=.92
dark=bpy.data.materials.new('Deep scratches'); dark.use_nodes=True; p=dark.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(.012,.009,.006,1); p.inputs['Roughness'].default_value=.95
def mark(a,b,parent,i):
 c=bpy.data.curves.new('Surface abrasion','CURVE'); c.dimensions='3D'; c.bevel_depth=random.uniform(.00010,.00030); c.bevel_resolution=1
 s=c.splines.new('POLY'); s.points.add(2)
 for p,v,r in zip(s.points,[a,tuple((x+y)/2 for x,y in zip(a,b)),b],[.15,1,.05]):p.co=(*v,1); p.radius=r
 o=bpy.data.objects.new('Wear scratch',c); bpy.context.collection.objects.link(o); o.parent=parent; c.materials.append(scratch if i%4 else dark)
root=bpy.data.objects['SmallCabinet']; door=bpy.data.objects['Door_Hinge']; drawer=bpy.data.objects['Drawer_Slide']
for i in range(180):
 x=random.uniform(-.22,.22); y=random.uniform(-.19,.19); length=random.uniform(.003,.038)
 mark((x,y,.6443),(min(x+length,.23),y+random.uniform(-.002,.002),.6443),root,i)
for i in range(110):
 x=random.uniform(-.322,-.082); z=random.uniform(.07,.30)
 mark((x,-.0193,z),(x+random.uniform(-.002,.002),-.0193,min(z+random.uniform(.003,.025),.303)),door,i)
for i in range(70):
 x=random.uniform(-.15,.15); z=random.uniform(.518,.551)
 mark((x,-.2043,z),(x+random.uniform(.003,.025),-.2043,z+.0005),drawer,i)
scene=bpy.context.scene; scene.frame_set(1); scene.cycles.samples=48
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/SmallCabinet_Aged.blend')
scene.render.filepath=OUT+'/aged-closed.png'; bpy.ops.render.render(write_still=True)
scene.frame_set(100); scene.camera.data.ortho_scale=1.15; scene.camera.rotation_euler=(Vector((0,-.08,.32))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=OUT+'/aged-open.png'; bpy.ops.render.render(write_still=True)
print('AGED_DONE')
