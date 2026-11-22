import bpy
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
root=r'C:/UnityProjects/theghost/RecoveredProjects/TheGhost_School_Unity6'
bpy.ops.import_scene.fbx(filepath=root+'/Assets/Recovery/WoodenLocker/LockerDrawers.fbx')
body=bpy.data.objects['Carcass'];print('BODY_POLYGONS',len(body.data.polygons))
# Rays from empty upper cabinet interior must meet a surface facing inward.
from mathutils.bvhtree import BVHTree
dg=bpy.context.evaluated_depsgraph_get(); tree=BVHTree.FromObject(body,dg)
for label,start,direction in [('back',(0,.2,.95),(0,1,0)),('left',(0,.2,.95),(-1,0,0)),('right',(0,.2,.95),(1,0,0))]:
 hit,n,idx,dist=tree.ray_cast(Vector(start),Vector(direction))
 print(label,hit,n,'faces_inside',n.dot(Vector(direction))<0 if n else False)
 assert hit is not None and n.dot(Vector(direction))<0
mat=bpy.data.materials.new('Wood');mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.8
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(root+'/Assets/Recovery/WoodenLocker/Textures/Locker_Locker_BaseColor.png');mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);mat.use_backface_culling=True
lining=bpy.data.materials.new('Inner wood');lining.diffuse_color=(.12,.085,.055,1)
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.data.materials.clear();o.data.materials.append(mat if o.name in ['Carcass','DrawerLower','DrawerUpper'] else lining)
for name in ['DrawerLower','DrawerUpper']:bpy.data.objects[name].location.y-=.5
bpy.ops.object.camera_add(location=(.9,-1.8,1.7));c=bpy.context.object;c.rotation_euler=(Vector((0,.15,.55))-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=1.9;c.data.clip_start=.01
bpy.ops.object.light_add(type='AREA',location=(0,-1,2));l=bpy.context.object;l.data.energy=100;l.data.size=2
s=bpy.context.scene;s.camera=c;s.render.engine='CYCLES';s.cycles.samples=16;s.world=bpy.data.worlds.new('Studio');s.world.color=(.15,.15,.15);s.render.resolution_x=800;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.filepath=root+'/RecoveryReports/drawers-interior-fixed.png';bpy.ops.render.render(write_still=True)

