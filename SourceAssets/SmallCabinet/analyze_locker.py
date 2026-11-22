import bpy, json
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'D:/Rac/models loker/old-wooden-locker/source/Locker/Locker.fbx')
o=bpy.data.objects['Locker']
faces=[]
for p in o.data.polygons:
 pts=[o.data.vertices[i].co for i in p.vertices]
 faces.append({'i':p.index,'n':list(p.normal),'lo':[round(min(v[k] for v in pts),4) for k in range(3)],'hi':[round(max(v[k] for v in pts),4) for k in range(3)]})
open(r'C:/UnityProjects/theghost/SourceAssets/SmallCabinet/locker_faces.json','w').write(json.dumps(faces))
print('MATRIX',o.matrix_world)
print('FRONT',json.dumps([p for p in faces if p['hi'][0]<11.4]))
