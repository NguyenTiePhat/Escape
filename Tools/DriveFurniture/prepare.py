import bpy, os, json, math
from mathutils import Vector

ROOT = 'C:/UnityProjects/theghost'
SOURCE = ROOT + '/SourceAssets/DriveFurniture'
OUTPUT = ROOT + '/RecoveredProjects/TheGhost_School_Unity6/Assets/Recovery/DriveFurniture'
REPORTS = ROOT + '/RecoveredProjects/TheGhost_School_Unity6/RecoveryReports/DriveFurniture'
os.makedirs(OUTPUT + '/Models', exist_ok=True)
os.makedirs(OUTPUT + '/Textures', exist_ok=True)
os.makedirs(REPORTS, exist_ok=True)
report = []
for name in ['BanDanhBongBan', 'Tutrungbaydodungdiali', 'TuNhoo', 'BanGheHocSinh']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE + '/' + name + '.fbx')
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(p) for o in meshes for p in o.bound_box]
    lo = Vector([min(p[i] for p in points) for i in range(3)])
    hi = Vector([max(p[i] for p in points) for i in range(3)])
    original_tris = sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    original_dimensions = list(hi-lo)
    # Bake transforms, normalize the longest dimension and bottom-center the model.
    scale = 1.0/max(hi-lo)
    center = Vector(((lo.x+hi.x)/2, (lo.y+hi.y)/2, lo.z))
    for o in meshes:
        matrix = o.matrix_world.copy()
        o.parent = None
        o.matrix_world.identity()
        for v in o.data.vertices:
            v.co = (matrix @ v.co-center)*scale
        bpy.context.view_layer.objects.active=o
        o.select_set(True)
        if original_tris > 30000:
            mod=o.modifiers.new('Game mesh budget', 'DECIMATE')
            mod.ratio=30000/original_tris
            bpy.ops.object.modifier_apply(modifier=mod.name)
        o.select_set(False)
    textures=[]
    for i, mat in enumerate([m for m in bpy.data.materials if m.users]):
        mat.name = name + '_Material_' + str(i)
        if not mat.use_nodes: continue
        for node in mat.node_tree.nodes:
            if node.type=='TEX_IMAGE' and node.image:
                img=node.image
                path=OUTPUT+'/Textures/'+name+'_'+str(i)+'_'+str(len(textures))+'.png'
                img.filepath_raw=path
                img.file_format='PNG'
                img.save()
                textures.append({'material':mat.name,'path':os.path.basename(path),'size':list(img.size)})
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=OUTPUT+'/Models/'+name+'.fbx',use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',axis_forward='-Z',axis_up='Y')
    tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    report.append({'name':name,'source_triangles':original_tris,'game_triangles':tris,'original_dimensions':original_dimensions,'normalized_dimensions':list((hi-lo)*scale),'textures':textures})
    # A separate neutral preview; no scene assets or lighting are modified by this render.
    scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.samples=20
    scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Preview world');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.18,.20,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
    bpy.ops.object.camera_add(location=(1.35,-1.65,1.15))
    cam=bpy.context.object;target=Vector((0,0,float((hi.z-lo.z)*scale)*.45))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=1.5;scene.camera=cam
    bpy.ops.object.light_add(type='AREA',location=(1,-2,3));bpy.context.object.data.energy=350;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=3
    bpy.ops.object.light_add(type='AREA',location=(-2,1,2));bpy.context.object.data.energy=200;bpy.context.object.data.size=2
    scene.render.filepath=REPORTS+'/'+name+'.png';bpy.ops.render.render(write_still=True)
    with open(REPORTS+'/models.json','w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('FURNITURE_PREPARATION_COMPLETE')
