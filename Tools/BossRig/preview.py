import bpy, math
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath=r'D:/UnityProjects/theghost/Preview/BossRig/import.blend')
o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=o;o.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for v in o.data.vertices: v.co*=2.2/1.019284963607788
m=bpy.data.materials.new('Preview');m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.4,.32,.25,1);bs.inputs['Roughness'].default_value=.75;o.data.materials.clear();o.data.materials.append(m)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=12;sc.render.resolution_x=700;sc.render.resolution_y=800;sc.render.resolution_percentage=100;sc.world.color=(.25,.25,.25)
for loc,power,size in [((2,-3,4),600,4),((-3,1,3),450,3)]:
    d=bpy.data.lights.new('Key','AREA');d.energy=power;d.shape='DISK';d.size=size;l=bpy.data.objects.new('Key',d);sc.collection.objects.link(l);l.location=loc;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Camera');c=bpy.data.objects.new('Camera',d);sc.collection.objects.link(c);sc.camera=c;d.type='ORTHO';d.ortho_scale=2.65
for name,pos in [('front',(0,-5,1.1)),('side',(5,0,1.1))]:
    c.location=pos;c.rotation_euler=(Vector((0,0,1.1))-c.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath='D:/UnityProjects/theghost/Preview/BossRig/'+name+'.png';bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=r'D:/UnityProjects/theghost/Preview/BossRig/normalized.blend')
