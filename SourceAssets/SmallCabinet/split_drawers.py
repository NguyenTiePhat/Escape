import bpy,bmesh,os
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r'D:/Rac/models loker/old-wooden-locker/source/Locker/Locker.fbx')
src=bpy.data.objects['Locker']; out=r'C:/UnityProjects/theghost/RecoveredProjects/TheGhost_School_Unity6/Assets/Recovery/WoodenLocker'
groups=[[],[],[]]
for p in src.data.polygons:
 pts=[src.data.vertices[i].co for i in p.vertices]; y0=min(v.y for v in pts);y1=max(v.y for v in pts)
 front=all(v.x<11.4 and 2.016<v.z<4.358 for v in pts)
 g=1 if front and y0>=.8423 and y1<=2.9034 else 2 if front and y0>=2.9759 and y1<=4.999 else 0
 groups[g].append(p.index)
for g,name in enumerate(['Carcass','DrawerLower','DrawerUpper']):
 mesh=src.data.copy();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 bm=bmesh.new();bm.from_mesh(mesh);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index not in groups[g]],context='FACES');bm.to_mesh(mesh);bm.free()
 for v in mesh.vertices:
  x,y,z=v.co;v.co=Vector(((z-3.18)*.2,(x-11.2)*.2,y*.2))
 if g==0:
  # The original closed prop only has outward-facing shell surfaces.
  # Give its back, sides and shelves real inner faces and capped edges.
  mod=o.modifiers.new('Cabinet wall thickness','SOLIDIFY')
  mod.thickness=.008
  mod.offset=-1
  mod.use_rim=True
 if g:
  mod=o.modifiers.new('Panel thickness','SOLIDIFY');mod.thickness=.006
  # Lined drawer tray extends behind front panel, following drawer transform.
  for suffix,loc,dim in [('Base',(0,.38,(.87 if g==1 else 3.0)*.2),(.43,.75,.012)),('Left',(-.21,.38,(1.55 if g==1 else 3.69)*.2),(.012,.75,.27)),('Right',(.21,.38,(1.55 if g==1 else 3.69)*.2),(.012,.75,.27)),('Back',(0,.75,(1.55 if g==1 else 3.69)*.2),(.43,.012,.27))]:
   bpy.ops.mesh.primitive_cube_add(size=1,location=loc);b=bpy.context.object;b.name=name+suffix;b.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);b.parent=o
bpy.data.objects.remove(src,do_unlink=True)
for o in list(bpy.context.scene.objects):
 if o.type=='EMPTY':bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=out+'/LockerDrawers.fbx',use_selection=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
print('FACES',list(map(len,groups)))
