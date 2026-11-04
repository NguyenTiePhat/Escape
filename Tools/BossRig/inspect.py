import bpy, json, os
from mathutils import Vector
os.makedirs(r'D:/UnityProjects/theghost/Preview/BossRig', exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=r'D:/UnityProjects/theghost/RecoveredProjects/TheGhost_School_Unity6/Assets/Recovery/Boss/M4Boss.fbx')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
info=[]
for o in meshes:
    vs=[o.matrix_world@v.co for v in o.data.vertices]
    info.append(dict(name=o.name,vertices=len(vs),bounds=[[min(v[i] for v in vs),max(v[i] for v in vs)] for i in range(3)],matrix=[list(r) for r in o.matrix_world]))
    with open(r'D:/UnityProjects/theghost/Preview/BossRig/vertices.json','w') as f: json.dump([list(v) for v in vs],f)
with open(r'D:/UnityProjects/theghost/Preview/BossRig/import.json','w') as f: json.dump(info,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=r'D:/UnityProjects/theghost/Preview/BossRig/import.blend')
