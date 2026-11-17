import bpy,json,os
root='C:/UnityProjects/theghost'
result={}
for name in ['BanDanhBongBan','Tutrungbaydodungdiali','TuNhoo','BanGheHocSinh']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=root+'/SourceAssets/DriveFurniture/'+name+'.fbx')
 mats=[]
 for mat in bpy.data.materials:
  if not mat.use_nodes:continue
  nodes=[]
  for n in mat.node_tree.nodes:
   if n.type=='TEX_IMAGE':nodes.append({'name':n.name,'image':n.image.name if n.image else None,'connections':[(l.to_node.type,l.to_socket.name) for o in n.outputs for l in o.links]})
  mats.append(nodes)
 result[name]=mats
with open(root+'/RecoveredProjects/TheGhost_School_Unity6/RecoveryReports/DriveFurniture/texture-roles.json','w') as f:json.dump(result,f,indent=2)
