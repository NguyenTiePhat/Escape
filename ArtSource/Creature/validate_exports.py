import bpy,os,json,math
p=r'D:\HK1_2026_2027\CNM\CKCNM\Animations';result=[]
for name,d in [('Idle',4),('Walk',2.4),('Alert',1),('Investigate',3.2),('Run',.8),('Lunge',1),('Search',4)]:
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=os.path.join(p,name+'.fbx'))
 rigs=[o for o in bpy.data.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.data.objects if o.type=='MESH'];assert len(rigs)==1 and meshes
 r=rigs[0];assert r.animation_data and r.animation_data.action
 a=r.animation_data.action;start,end=a.frame_range;assert abs((end-start)/30-d)<.04
 for f in [start,(start+end)/2,end]:
  bpy.context.scene.frame_set(int(f));assert all(math.isfinite(v) for b in r.pose.bones for row in b.matrix for v in row)
 result.append(dict(clip=name,seconds=round((end-start)/30,3),bones=len(r.data.bones),vertices=sum(len(o.data.vertices) for o in meshes),status='PASS'))
open(os.path.join(p,'validation.json'),'w').write(json.dumps(result,indent=2));print('VALIDATED',result)
