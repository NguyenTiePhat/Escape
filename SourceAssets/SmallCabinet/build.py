import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def wood(name,scale):
 m=bpy.data.materials.new(name); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF'); p.inputs['Roughness'].default_value=.38
 tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=3.5; tex.inputs['Detail'].default_value=4; tex.inputs['Roughness'].default_value=.72
 coord=n.new('ShaderNodeTexCoord'); mapping=n.new('ShaderNodeVectorMath'); mapping.operation='MULTIPLY'; mapping.inputs[1].default_value=scale
 l.new(coord.outputs['Generated'],mapping.inputs[0]); l.new(mapping.outputs[0],tex.inputs['Vector'])
 ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.23; ramp.color_ramp.elements[0].color=(.018,.006,.002,1); ramp.color_ramp.elements[1].position=.78; ramp.color_ramp.elements[1].color=(.19,.073,.022,1)
 l.new(tex.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
 bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.18; bump.inputs['Distance'].default_value=.0012; l.new(tex.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],p.inputs['Normal'])
 return m
vertical=wood('Walnut vertical grain',(7,7,.32)); horizontal=wood('Walnut horizontal grain',(.35,8,6))
brass=bpy.data.materials.new('Antique brass'); brass.diffuse_color=(.32,.19,.055,1); brass.use_nodes=True
p=brass.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(.32,.19,.055,1); p.inputs['Metallic'].default_value=.8; p.inputs['Roughness'].default_value=.31
def empty(name,loc,parent=None):
 o=bpy.data.objects.new(name,None); bpy.context.collection.objects.link(o); o.location=loc; o.parent=parent; return o
root=empty('SmallCabinet',(0,0,0)); door=empty('Door_Hinge',(.202,-.191,.115),root); drawer=empty('Drawer_Slide',(0,0,0),root)
def box(name,loc,size,mat=vertical,parent=root,bevel=.003):
 bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0)); o=bpy.context.object; o.name=name; o.parent=parent; o.location=loc; o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(mat)
 mod=o.modifiers.new('Crafted rounded edges','BEVEL'); mod.width=bevel; mod.segments=3
 o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL'); return o
def sphere(name,loc,size,parent):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12); o=bpy.context.object; o.name=name; o.parent=parent; o.location=loc; o.scale=size; o.data.materials.append(brass)
 for f in o.data.polygons:f.use_smooth=True
def trim(name,cx,y,cz,w,h,parent):
 for x in [cx-w/2,cx+w/2]:box(name,(x,y,cz),(.012,.012,h),vertical,parent,.002)
 for z in [cz-h/2,cz+h/2]:box(name,(cx,y,z),(w+.012,.012,.012),horizontal,parent,.002)
for x in [-.199,.199]:
 box('Side carcass',(x,0,.355),(.026,.35,.49))
 box('Side inset panel',(x*1.06,0,.35),(.009,.265,.375))
 for y in [-.159,.159]:
  box('Corner stile',(x,y,.35),(.034,.035,.49))
  box('Foot',(x,y,.058),(.052,.058,.116),vertical,root,.009)
box('Back',(0,.166,.35),(.38,.018,.49))
for z in [.115,.305,.475]:box('Interior shelf',(0,0,z),(.38,.33,.018),horizontal)
for z,w,d,h in [(.115,.438,.388,.023),(.137,.425,.375,.012),(.602,.435,.382,.018),(.62,.465,.415,.024),(.637,.478,.427,.014)]:box('Profile molding',(0,0,z),(w,d,h),horizontal,root,.005)
box('Drawer face',(0,-.19,.535),(.375,.028,.094),horizontal,drawer)
trim('Drawer beading',0,-.207,.535,.337,.06,drawer)
box('Drawer bottom',(0,-.005,.492),(.35,.32,.012),horizontal,drawer)
for x in [-.17,.17]:box('Drawer side',(x,-.005,.533),(.014,.32,.078),horizontal,drawer)
box('Drawer back',(0,.148,.533),(.35,.014,.078),horizontal,drawer)
box('Door panel',(-.202,0,.185),(.374,.02,.346),vertical,door)
for x in [-.365,-.039]:box('Door stile',(x,-.012,.185),(.038,.022,.35),vertical,door)
for z in [.03,.34]:box('Door rail',(-.202,-.012,z),(.32,.022,.04),horizontal,door)
trim('Raised panel border',-.202,-.026,.185,.27,.263,door)
box('Raised center',(-.202,-.012,.185),(.25,.014,.243),vertical,door,.005)
sphere('Door knob',(-.35,-.045,.20),(.013,.019,.013),door)
for z in [.055,.31]:
 box('Hinge leaf',(-.006,-.008,z),(.024,.006,.035),brass,door,.002)
 sphere('Hinge barrel',(0,-.013,z),(.007,.007,.024),door)
for x in [-.055,.055]:sphere('Handle rosette',(x,-.211,.543),(.015,.005,.015),drawer)
curve=bpy.data.curves.new('Bail handle','CURVE'); curve.dimensions='3D'; curve.bevel_depth=.004; curve.bevel_resolution=4
sp=curve.splines.new('POLY'); sp.points.add(24)
for i,pt in enumerate(sp.points):
 a=math.pi*i/24; pt.co=(.055*math.cos(a),-.229,.543-.031*math.sin(a),1)
ob=bpy.data.objects.new('Brass bail handle',curve); bpy.context.collection.objects.link(ob); ob.parent=drawer; ob.data.materials.append(brass)
# Export the actual closed asset, with movable part origins preserved.
bpy.ops.object.select_all(action='DESELECT')
for ob in bpy.context.scene.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'SmallCabinet.fbx'),use_selection=True,add_leaf_bones=False,axis_forward='-Z',axis_up='Y')
# Demonstration motion in the source file.
for frame,angle,slide in [(1,0,0),(45,0,0),(100,105,-.19),(145,105,-.19),(200,0,0)]:
 door.rotation_euler.z=math.radians(angle); door.keyframe_insert(data_path='rotation_euler',frame=frame)
 drawer.location.y=slide; drawer.keyframe_insert(data_path='location',frame=frame)
scene=bpy.context.scene; scene.frame_end=200; scene.frame_set(1)
floor=bpy.data.materials.new('Studio warm gray'); floor.diffuse_color=(.12,.105,.09,1)
box('Studio floor',(0,0,-.025),(200,200,.04),floor,None,.001)
def aim(o,point):o.rotation_euler=(Vector(point)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.05,-1.55,1.00)); camera=bpy.context.object; aim(camera,(0,-.015,.32)); camera.data.type='ORTHO'; camera.data.ortho_scale=.97; scene.camera=camera
for name,pos,power,size in [('Key',(.1,-1.2,1.7),95,1.1),('Fill',(-1,-.4,.8),65,.9),('Rim',(.6,.7,1.2),110,.7)]:
 bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object; o.name=name; o.data.energy=power; o.data.shape='DISK'; o.data.size=size; aim(o,(0,0,.3))
scene.render.engine='CYCLES'; scene.cycles.samples=40; scene.cycles.use_denoising=True
scene.world.color=(.22,.22,.22); scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'SmallCabinet.blend'))
scene.render.filepath=os.path.join(OUT,'preview-closed.png'); bpy.ops.render.render(write_still=True)
scene.frame_set(100); camera.data.ortho_scale=1.15; aim(camera,(0,-.08,.32))
scene.render.filepath=os.path.join(OUT,'preview-open.png'); bpy.ops.render.render(write_still=True)
print('CABINET_COMPLETE')
