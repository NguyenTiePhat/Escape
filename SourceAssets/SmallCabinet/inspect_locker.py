import bpy
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'D:/Rac/models loker/old-wooden-locker/source/Locker/Locker.fbx')
for o in bpy.context.scene.objects:
 print('OBJECT',o.name,'TYPE',o.type,'PARENT',o.parent.name if o.parent else None,'ORIGIN',tuple(o.location),'SIZE',tuple(o.dimensions))
 if o.type=='MESH':
  mesh=o.data; adj=[[] for v in mesh.vertices]
  for e in mesh.edges:
   a,b=e.vertices;adj[a].append(b);adj[b].append(a)
  seen=set();parts=[]
  for v in mesh.vertices:
   if v.index in seen:continue
   stack=[v.index];seen.add(v.index);ids=[]
   while stack:
    i=stack.pop();ids.append(i)
    for j in adj[i]:
     if j not in seen:seen.add(j);stack.append(j)
   pts=[mesh.vertices[i].co for i in ids]
   parts.append((len(ids),tuple(round(min(p[k] for p in pts),4) for k in range(3)),tuple(round(max(p[k] for p in pts),4) for k in range(3))))
  print('CONNECTED_PARTS',sorted(parts,reverse=True))
print('ACTIONS',[a.name for a in bpy.data.actions])
