import bpy
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'D:/Rac/models loker/old-wooden-locker/source/Locker/Locker.fbx')
o=bpy.data.objects['Locker']; corners=[o.matrix_world@Vector(v) for v in o.bound_box]; center=sum(corners,Vector())/8
bpy.ops.object.camera_add(location=center+Vector((-.1,-.08,.06)));c=bpy.context.object;c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=.09;c.data.clip_start=.0001
s=bpy.context.scene;s.camera=c;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='SINGLE';s.display.shading.show_cavity=True;s.render.resolution_x=800;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.filepath=r'C:/UnityProjects/theghost/SourceAssets/SmallCabinet/locker-inspect.png';bpy.ops.render.render(write_still=True)
