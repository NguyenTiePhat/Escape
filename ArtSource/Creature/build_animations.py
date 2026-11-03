import bpy, math, os, json
from mathutils import Vector
from math import sin,cos,pi
P=r'D:\HK1_2026_2027\CNM\CKCNM'
OUT=os.path.join(P,'Animations');os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(P,'character_source.blend'))
mesh=bpy.data.objects['node_0'];bpy.context.view_layer.objects.active=mesh;mesh.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
# Anatomical joints fitted to the supplied crouched, asymmetric sculpt.
joints={'pelvis':((.10,.19,.66),(.07,.12,.75),None),'spine':((.07,.12,.75),(.0,-.04,.85),'pelvis'),'chest':((0,-.04,.85),(-.06,-.15,.88),'spine'),'neck':((-.06,-.15,.88),(-.10,-.22,.91),'chest'),'head':((-.10,-.22,.91),(-.10,-.26,1.01),'neck')}
for side,sh,el,wr,ha,hip,knee,ank,toe in [
 ('L',(.16,-.04,.86),(.19,-.03,.62),(.06,-.19,.36),(.01,-.25,.23),(.19,.20,.66),(.13,-.06,.40),(.30,.12,.10),(.26,-.02,.035)),
 ('R',(-.16,.02,.79),(-.18,.15,.56),(-.29,-.08,.49),(-.34,-.25,.37),(.02,.18,.62),(-.12,-.10,.45),(-.07,-.03,.10),(-.13,-.15,.035))]:
 joints['upper_arm.'+side]=(sh,el,'chest');joints['forearm.'+side]=(el,wr,'upper_arm.'+side);joints['hand.'+side]=(wr,ha,'forearm.'+side)
 joints['thigh.'+side]=(hip,knee,'pelvis');joints['shin.'+side]=(knee,ank,'thigh.'+side);joints['foot.'+side]=(ank,toe,'shin.'+side)
bpy.ops.object.select_all(action='DESELECT');bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='Creature_Rig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
b=rig.data.edit_bones.new('root');b.head=(0,0,0);b.tail=(0,0,.15);b.use_deform=False
for name,(h,t,parent) in joints.items():
 b=rig.data.edit_bones.new(name);b.head=h;b.tail=t;b.parent=rig.data.edit_bones[parent or 'root']
bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
# Fill any vertices missed by heat weighting using smooth nearest-segment weights.
def dist(v,a,b):
 d=b-a;return (v-(a+d*max(0,min(1,(v-a).dot(d)/d.length_squared)))).length
unweighted=0
for v in mesh.data.vertices:
 if sum(g.weight for g in v.groups)<.0001:
  unweighted+=1;ds=sorted((dist(v.co,Vector(h),Vector(t)),n) for n,(h,t,p) in joints.items())[:3];ws=[1/(d+.015)**4 for d,n in ds];total=sum(ws)
  for (d,n),w in zip(ds,ws):
   vg=mesh.vertex_groups.get(n) or mesh.vertex_groups.new(name=n);vg.add([v.index],w/total,'REPLACE')
print('WEIGHT_FALLBACK',unweighted)
# Smooth heat weights over surface adjacency to remove isolated bone influence.
verts=mesh.data.vertices;adj=[[] for v in verts]
for e in mesh.data.edges:
 a,b=e.vertices;adj[a].append(b);adj[b].append(a)
weights=[{g.group:g.weight for g in v.groups} for v in verts]
for iteration in range(18):
 new=[]
 for i,v in enumerate(verts):
  w={k:val*.5 for k,val in weights[i].items()};ne=adj[i]
  if ne:
   for j in ne:
    for k,val in weights[j].items():w[k]=w.get(k,0)+.5*val/len(ne)
  new.append(w)
 weights=new
for vg in mesh.vertex_groups:vg.remove(list(range(len(verts))))
for i,w in enumerate(weights):
 ws=sorted(w.items(),key=lambda kv:kv[1],reverse=True)[:4];total=sum(v for k,v in ws)
 for k,v in ws:mesh.vertex_groups[k].add([i],v/total,'REPLACE')
# Resolve finger/leg proximity using surface distance, preserving body heat weights.
import heapq
fields={}
for name in ['hand.L','shin.R','forearm.L']:
 h,t,p=joints[name];mid=(Vector(h)+Vector(t))*.5;seed=min(range(len(verts)),key=lambda i:(verts[i].co-mid).length_squared)
 ds=[1e9]*len(verts);ds[seed]=0;heap=[(0,seed)]
 while heap:
  d,i=heapq.heappop(heap)
  if d>ds[i]:continue
  for j in adj[i]:
   nd=d+(verts[j].co-verts[i].co).length
   if nd<ds[j]:ds[j]=nd;heapq.heappush(heap,(nd,j))
 fields[name]=ds
for v in verts:
 i=v.index
 if v.co.z<.40 and fields['hand.L'][i]<.34:
  for vg in mesh.vertex_groups:vg.remove([i])
  mesh.vertex_groups['hand.L'].add([i],1,'REPLACE')
# IK controllers follow the root. The deform skeleton is baked on FBX export.
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for side in ['L','R']:
 b=rig.data.edit_bones.new('foot_ctl.'+side);b.head=joints['foot.'+side][0];b.tail=Vector(b.head)+Vector((0,0,.08));b.parent=rig.data.edit_bones['root'];b.use_deform=False
bpy.ops.object.mode_set(mode='POSE')
for side in ['L','R']:
 c=rig.pose.bones['shin.'+side].constraints.new('IK');c.target=rig;c.subtarget='foot_ctl.'+side;c.chain_count=2;c.use_tail=True
for b in rig.pose.bones:b.rotation_mode='XYZ'
bpy.ops.object.mode_set(mode='OBJECT')
s=bpy.context.scene;s.render.fps=30
clips=[('Idle',4,True),('Walk',2.4,True),('Alert',1,False),('Investigate',3.2,True),('Run',.8,True),('Lunge',1,False),('Search',4,True)]
def rot(name,x=0,y=0,z=0):rig.pose.bones[name].rotation_euler=(x,y,z)
def loc(name,x=0,y=0,z=0):
 v=Vector((x,y,z));rig.pose.bones[name].location=rig.data.bones[name].matrix_local.to_3x3().inverted()@v if name in ['root','pelvis'] else v
def ease(a,b,t):t=max(0,min(1,t));return a+(b-a)*t*t*(3-2*t)
for name,duration,loop in clips:
 rig.animation_data_clear();act=bpy.data.actions.new(name);act.use_fake_user=True;rig.animation_data_create();rig.animation_data.action=act;n=round(duration*30);s.frame_start=1;s.frame_end=n+1
 for frame in range(n+1):
  t=frame/30;u=frame/n;p=2*pi*u
  for b in rig.pose.bones:b.location=(0,0,0);b.rotation_euler=(0,0,0);b.scale=(1,1,1)
  if name=='Idle':
   loc('pelvis',0,0,.004*sin(p));rot('chest',.018*sin(p),0,.01*sin(p));rot('head',0,.045+.025*sin(p),.045*sin(p));rot('upper_arm.L',.018*sin(p));rot('upper_arm.R',-.018*sin(p))
  elif name in ['Walk','Run','Investigate']:
   fast=name=='Run';invest=name=='Investigate';phase=p*(2 if invest else 1);amp=.16 if fast else (.08 if invest else .065)
   if invest:phase=2*pi*(u*2-.11*sin(4*pi*u))
   loc('pelvis',0,0,(.013 if fast else .006)*(1-cos(2*phase))-(.025 if invest else 0));rot('spine',-.13 if fast else (-.07 if invest else 0),0,.025*sin(phase));rot('head',0,0,.13+.04*sin(phase) if fast else .09*sin(p))
   for side,offset,sgn in [('L',0,1),('R',pi,-1)]:
    q=phase+offset
    # Controller local Y points upward, local Z points backward.
    loc('foot_ctl.'+side,0,.045*max(0,sin(q)) if fast else .018*max(0,sin(q)),amp*cos(q))
    rot('upper_arm.'+side,(.30 if fast else .10)*sin(q),.035*sgn, .035*sgn*sin(q));rot('forearm.'+side,.08*max(0,-sin(q)));rot('hand.'+side,0,.04*sin(q),0)
  elif name=='Alert':
   snap=ease(0,1,u/.16);rot('head',-.04*snap,.06*snap,-.60*snap);rot('chest',.025*snap,0,-.08*snap);rot('upper_arm.L',-.05*snap);rot('upper_arm.R',-.05*snap)
  elif name=='Lunge':
   if t<=.3:c=ease(0,1,t/.3);travel=0;air=0;reach=0
   elif t<=.7:c=ease(1,-.3,(t-.3)/.4);travel=ease(0,.75,(t-.3)/.4);air=.14*sin(pi*(t-.3)/.4);reach=ease(0,1,(t-.3)/.25)
   else:c=ease(.6,0,(t-.7)/.3);travel=.75;air=0;reach=ease(1,.4,(t-.7)/.3)
   loc('root',0,-travel,air);loc('pelvis',0,0,-.055*c);rot('spine',-.10*c-.20*reach);rot('head',.12*reach)
   for side in ['L','R']:rot('upper_arm.'+side,-.95*reach);rot('forearm.'+side,.18*reach);rot('hand.'+side,-.20*reach)
  elif name=='Search':
   # Pauses between each slow directional scan; seamless loop.
   points=[(0,0),(.12,.48),(.24,.48),(.42,-.5),(.55,-.5),(.73,.28),(.85,.28),(1,0)]
   for (a,x),(b,y) in zip(points,points[1:]):
    if a<=u<=b:scan=ease(x,y,(u-a)/(b-a));break
   rot('head',.04,0,scan);rot('chest',0,0,.12*scan)
   for side,sgn in [('L',1),('R',-1)]:rot('upper_arm.'+side,-.32+.12*sin(p+sgn*.6),sgn*.06,sgn*.10+.09*sin(p));rot('forearm.'+side,.12+.09*cos(p+sgn));rot('hand.'+side,.12*sin(2*p),0,.14*sin(p+sgn))
  for b in rig.pose.bones:
   b.keyframe_insert(data_path='location',frame=frame+1,group=b.name);b.keyframe_insert(data_path='rotation_euler',frame=frame+1,group=b.name)
 act['loop']=loop;act['duration_seconds']=duration
 s.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,name+'.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
# Retain one NLA strip per action for easy selection in Blender.
rig.animation_data.action=None
for name,duration,loop in clips:
 tr=rig.animation_data.nla_tracks.new();tr.name=name;tr.strips.new(name,1,bpy.data.actions[name]);tr.mute=True
rig.animation_data.action=bpy.data.actions['Idle'];s.frame_start=1;s.frame_end=121;s.frame_set(1)
# Preview camera and studio setup.
center=Vector((0,0,.5));bpy.ops.object.camera_add(location=(1.55,-2.3,1.15));cam=bpy.context.object;cam.name='Preview_Camera';cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=1.55;s.camera=cam
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.world=bpy.data.worlds.new('Studio');s.world.color=(.07,.07,.07);s.render.resolution_x=600;s.render.resolution_y=600;s.render.resolution_percentage=100
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Creature_Animations.blend'))
# Contact sheets: start, anticipation/midpoint and end of each motion.
for name,duration,loop in clips:
 rig.animation_data.action=bpy.data.actions[name]
 for frac in [.0,.3,.6,.9]:
  s.frame_set(1+round(duration*30*frac));cam.location=Vector((1.55,-2.3,1.15))+Vector((0,-.75*frac,0)) if name=='Lunge' else Vector((1.55,-2.3,1.15));s.render.filepath=os.path.join(OUT,name+'_'+str(int(frac*100))+'.png');bpy.ops.render.render(write_still=True)
print('COMPLETE',json.dumps(clips))
