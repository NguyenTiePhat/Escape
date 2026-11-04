import bpy, math, os, json
from mathutils import Vector, Quaternion

ROOT = 'D:/UnityProjects/theghost/'
OUT = ROOT + 'RecoveredProjects/TheGhost_School_Unity6/Assets/Recovery/Boss/Rigged/'
bpy.ops.wm.open_mainfile(filepath=ROOT+'Preview/BossRig/normalized.blend')
mesh = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
mesh.name = 'Boss_LOD0'
for obj in list(bpy.context.scene.objects):
    if obj != mesh: bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
arm_data=bpy.data.armatures.new('BossSkeleton'); arm=bpy.data.objects.new('BossRig',arm_data)
bpy.context.collection.objects.link(arm); arm.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='EDIT')
landmarks={}
def bone(name,head,tail,parent=None,deform=True):
    b=arm_data.edit_bones.new(name);b.head=head;b.tail=tail;b.use_deform=deform
    if parent: b.parent=arm_data.edit_bones[parent]
    landmarks[name]={'head':list(head),'tail':list(tail)}
    return b
bone('Root',(0,0,0),(0,0,.2))
bone('Hips',(.31,.46,1.35),(.23,.38,1.55),'Root')
bone('Spine',(.23,.38,1.55),(.12,.19,1.77),'Hips')
bone('Chest',(.12,.19,1.77),(-.04,-.08,1.92),'Spine')
bone('Neck',(-.04,-.08,1.92),(-.18,-.30,1.94),'Chest')
bone('Head',(-.18,-.30,1.94),(-.24,-.40,2.13),'Neck')
limbs={
 'L':dict(shoulder=(.27,-.02,1.94),elbow=(.37,-.20,1.32),wrist=(.10,-.37,.70),hand=(.04,-.40,.47),hip=(.48,.46,1.37),knee=(.24,.02,.88),ankle=(.68,.42,.20),toe=(.49,.12,.06)),
 'R':dict(shoulder=(-.25,-.03,1.80),elbow=(-.36,-.14,1.20),wrist=(-.63,-.38,1.05),hand=(-.72,-.46,.87),hip=(.22,.45,1.36),knee=(-.23,-.08,1.04),ankle=(-.10,.02,.17),toe=(-.37,-.18,.05))
}
for side,p in limbs.items():
    bone('Clavicle.'+side,landmarks['Chest']['tail'],p['shoulder'],'Chest')
    bone('UpperArm.'+side,p['shoulder'],p['elbow'],'Clavicle.'+side)
    bone('Forearm.'+side,p['elbow'],p['wrist'],'UpperArm.'+side)
    bone('Hand.'+side,p['wrist'],p['hand'],'Forearm.'+side)
    bone('Thigh.'+side,p['hip'],p['knee'],'Hips')
    bone('Shin.'+side,p['knee'],p['ankle'],'Thigh.'+side)
    bone('Foot.'+side,p['ankle'],p['toe'],'Shin.'+side)
    bone('FootIK.'+side,p['ankle'],p['toe'],'Root',False)
bpy.ops.object.mode_set(mode='OBJECT')
mesh.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
# Normalize and limit influences for Unity's four-weight skinning.
bpy.context.view_layer.objects.active=mesh;arm.select_set(False)
bpy.ops.object.vertex_group_limit_total(limit=4)
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
unweighted=[v.index for v in mesh.data.vertices if sum(g.weight for g in v.groups)<.99]
if unweighted: raise RuntimeError('Unweighted or unnormalized vertices: '+str(len(unweighted)))
for s in limbs:
    ik=arm.pose.bones['Shin.'+s].constraints.new('IK');ik.target=arm;ik.subtarget='FootIK.'+s;ik.chain_count=2
    c=arm.pose.bones['Foot.'+s].constraints.new('COPY_ROTATION');c.target=arm;c.subtarget='FootIK.'+s;c.target_space='WORLD';c.owner_space='WORLD'

lods=[mesh]
for index,ratio in [(1,.40),(2,.14)]:
    obj=mesh.copy();obj.data=mesh.data.copy();bpy.context.collection.objects.link(obj);obj.name='Boss_LOD'+str(index)
    bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
    mod=obj.modifiers.new('LOD reduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True
    bpy.ops.object.modifier_move_up(modifier=mod.name);bpy.ops.object.modifier_apply(modifier=mod.name)
    lods.append(obj);obj.hide_render=True

def rotate(name,angles):
    pb=arm.pose.bones[name];rest=pb.bone.matrix_local.to_quaternion()
    q=Quaternion((1,0,0),math.radians(angles[0])) @ Quaternion((0,1,0),math.radians(angles[1])) @ Quaternion((0,0,1),math.radians(angles[2]))
    pb.rotation_mode='QUATERNION';pb.rotation_quaternion=rest.inverted() @ q @ rest
def shift(name,delta):
    pb=arm.pose.bones[name];pb.location=pb.bone.matrix_local.to_3x3().inverted() @ Vector(delta)
def pulse(t,a,b):
    if t<=a or t>=b:return 0
    return math.sin(math.pi*(t-a)/(b-a))
specs=[('Idle',3.6,True),('Walk',2.0,True),('Alert',1.0,False),('Investigate',1.6,True),('Run',.9,True),('Lunge',1.0,False),('Search',4.0,True),('Roar',1.8,False)]
sc=bpy.context.scene;sc.render.fps=30
arm.animation_data_create();actions={}
for name,duration,loop in specs:
    action=bpy.data.actions.new(name);action.use_fake_user=True;arm.animation_data.action=action;actions[name]=action
    count=round(duration*30)
    for f in range(count+1):
        t=f/30;u=t/duration;phase=u*2*math.pi
        for pb in arm.pose.bones: pb.location=(0,0,0);pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion();pb.scale=(1,1,1)
        rotate('Chest',(1.2*math.sin(phase),0,0));rotate('Head',(0,1.7*math.sin(phase),0));shift('Hips',(0,0,.009*math.sin(phase)))
        if name in ['Walk','Investigate','Run']:
            stride={'Walk':.15,'Investigate':.19,'Run':.29}[name]
            lift={'Walk':.055,'Investigate':.065,'Run':.13}[name]
            # Investigate pauses briefly at the ends of each half-cycle.
            p=phase if name!='Investigate' else phase-.24*math.sin(2*phase)
            for s,offset in [('L',0),('R',math.pi)]:
                q=p+offset;shift('FootIK.'+s,(0,-stride*math.cos(q),lift*max(0,math.sin(q))))
                swing=math.sin(q)*({'Walk':9,'Investigate':13,'Run':28}[name])
                rotate('UpperArm.'+s,(swing,0,(-1 if s=='L' else 1)*2))
                rotate('Forearm.'+s,(-abs(swing)*.35,0,0))
            shift('Hips',(0,0,.018*(1-math.cos(2*p))-(.045 if name=='Investigate' else 0)))
            rotate('Spine',({'Walk':2,'Investigate':9,'Run':12}[name],0,1.5*math.sin(p)))
            rotate('Head',(-3,0,(5 if name!='Run' else 9)*math.sin(p*.999)))
        elif name=='Alert':
            snap=min(1,t/.12);settle=1-.15*max(0,(t-.25)/.75)
            rotate('Head',(3*snap,0,25*snap*settle));rotate('Chest',(-2*snap,0,4*snap))
        elif name=='Lunge':
            crouch=min(t/.3,1) if t<.3 else max(0,1-(t-.3)/.14)
            strike=pulse(t,.3,.75);recover=max(0,(t-.7)/.3)
            shift('Hips',(0,-.10*strike,-.10*crouch+.055*strike))
            rotate('Spine',(-5*crouch+18*strike,0,0));rotate('Head',(-8*strike,0,0))
            for s in limbs:
                rotate('UpperArm.'+s,(-12*crouch-62*strike,0,(-1 if s=='L' else 1)*8*strike))
                rotate('Forearm.'+s,(-15*strike,0,0));rotate('Hand.'+s,(10*strike,0,0))
                shift('FootIK.'+s,(0,-.06*strike,.06*strike))
        elif name=='Search':
            scan=20*math.tanh(2*math.sin(phase))
            rotate('Head',(3,0,scan));rotate('Chest',(4,0,scan*.17))
            for s,offset in [('L',0),('R',math.pi)]:
                rotate('UpperArm.'+s,(-25-7*math.sin(phase+offset),0,5*math.cos(phase+offset)))
                rotate('Forearm.'+s,(-14,0,8*math.sin(phase+offset)));rotate('Hand.'+s,(0,8*math.sin(phase+offset),0))
        elif name=='Roar':
            e=math.sin(math.pi*u);rotate('Chest',(-9*e,0,.8*math.sin(t*24)*e));rotate('Head',(-12*e,0,0))
        for pb in arm.pose.bones:
            pb.keyframe_insert('location',frame=f+1,group=pb.name);pb.keyframe_insert('rotation_quaternion',frame=f+1,group=pb.name)
    # Linear samples avoid overshoot between baked frames; endpoints match on loops.
    if action.fcurves:
        for curve in action.fcurves:
            for k in curve.keyframe_points:k.interpolation='LINEAR'

arm.animation_data.action=actions['Idle'];sc.frame_set(1);sc.frame_start=1;sc.frame_end=109
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True)
for o in lods:o.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'SourceAssets/BossRig/M4Boss.blend')
bpy.ops.export_scene.fbx(filepath=OUT+'M4BossRigged.fbx',use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
with open(ROOT+'Preview/BossRig/build-report.json','w') as f:json.dump({'bones':landmarks,'clips':specs,'unweighted':len(unweighted),'lodTriangles':[sum(len(p.vertices)-2 for p in o.data.polygons) for o in lods]},f,indent=2)
# Controlled preview of poses. The saved source and FBX are free of cameras/lights.
sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=12;sc.render.resolution_x=650;sc.render.resolution_y=750;sc.render.resolution_percentage=100;sc.world.color=(.25,.25,.25)
for loc,power in [((2,-3,4),650),((-3,0,3),400)]:
    d=bpy.data.lights.new('Preview','AREA');d.energy=power;d.size=4;l=bpy.data.objects.new('Preview',d);sc.collection.objects.link(l);l.location=loc;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Preview');c=bpy.data.objects.new('Preview',d);sc.collection.objects.link(c);sc.camera=c;d.type='ORTHO';d.ortho_scale=2.8;c.location=(.3,-5,1.2);c.rotation_euler=(Vector((0,0,1.1))-c.location).to_track_quat('-Z','Y').to_euler()
for name,frame in [('Idle',1),('Walk',16),('Alert',10),('Investigate',13),('Run',8),('Lunge',16),('Search',31)]:
    arm.animation_data.action=actions[name];sc.frame_set(frame);sc.render.filepath=ROOT+'Preview/BossRig/'+name+'.png';bpy.ops.render.render(write_still=True)
