import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Preview/IUH-campus.blend'))
for level in range(2):
    for x in [-20,-12,-4,4,12,20]:
        bpy.ops.object.light_add(type='AREA',location=(x,-7,3.6+level*4.2))
        bpy.context.object.data.energy=180
        bpy.context.object.data.color=(.72,.80,.76)
        bpy.context.object.data.size=4
bpy.context.scene.render.filepath=str(root/'Preview/03-classroom.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Preview/IUH-campus.blend'))
