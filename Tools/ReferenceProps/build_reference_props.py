"""Rebuild the eight reference props. Run with Blender --background --python this_file."""
import bpy, math, json, sys
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'RecoveredProjects/TheGhost_School_Unity6/Assets/Recovery/ReferenceProps'
SOURCE = ROOT / 'SourceAssets/ReferenceProps'
PREVIEW = SOURCE / 'Previews'
for p in [OUT/'Models', OUT/'Textures', SOURCE, PREVIEW]: p.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials): bpy.data.materials.remove(m)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.render.resolution_x=512
scene.render.resolution_y=512
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=True
scene.view_settings.view_transform='Standard'
scene.world.color=(.23,.23,.23)

# Tileable, deterministic multi-scale colour textures, shared by Blender and Unity.
N=512
rng=np.random.default_rng(4129)
def noise(freq):
    grid=rng.random((freq,freq))
    a=np.arange(N)*freq/N
    i=a.astype(int); t=a-i; t=t*t*(3-2*t)
    return ((1-t[:,None])*((1-t[None,:])*grid[i[:,None],i[None,:]]+t[None,:]*grid[i[:,None],(i[None,:]+1)%freq])+
            t[:,None]*((1-t[None,:])*grid[(i[:,None]+1)%freq,i[None,:]]+t[None,:]*grid[(i[:,None]+1)%freq,(i[None,:]+1)%freq]))
coarse,medium,fine,grain=noise(7),noise(26),noise(110),rng.random((N,N))
yy,xx=np.mgrid[:N,:N]/N
palette={
 'RP_Steel':((.36,.39,.38),.60,.69),
 'RP_Silver':((.49,.51,.47),.68,.60),
 'RP_Gold':((.63,.40,.105),.64,.57),
 'RP_Wood':((.40,.285,.20),0,.88),
 'RP_Dark':((.105,.125,.12),.32,.83),
 'RP_Brass':((.48,.37,.16),.55,.66)}
mats={}
for name,(base,metal,rough) in palette.items():
    variation=.68+.38*coarse+.20*medium+.12*fine+.09*grain
    rgb=np.array(base)[None,None,:]*variation[:,:,None]
    if name=='RP_Wood':
        lines=np.sin(yy*720+np.sin(xx*12)*2+medium*4)*.025
        rgb+=lines[:,:,None]
        rgb*=1-(np.maximum(0,np.sin(yy*1350+coarse*9)-.93)*.7)[:,:,None]
    else:
        rust=np.clip((coarse-.61)*3,0,.38)*np.clip((medium-.32)*2,0,1)
        if name=='RP_Gold' or name=='RP_Brass': rust*=.5
        rgb=rgb*(1-rust[:,:,None])+np.array([.24,.145,.085])[None,None,:]*rust[:,:,None]
        scratches=(np.sin(xx*1600+yy*50+fine*2)>.997)*(grain>.75)
        rgb+=scratches[:,:,None]*.10
    rgba=np.ones((N,N,4),dtype=np.float32); rgba[:,:,:3]=np.clip(rgb,0,1)
    im=bpy.data.images.new(name+'_Albedo',N,N)
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures'/(name+'_Albedo.png'));im.file_format='PNG';im.save()
    mat=bpy.data.materials.new(name);mat.use_nodes=True;mat.diffuse_color=(*base,1)
    nt=mat.node_tree;p=nt.nodes.get('Principled BSDF');p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=im;nt.links.new(tex.outputs['Color'],p.inputs['Base Color'])
    mats[name]=mat

groups={}; current=[]
def finish(o,name,mat,bevel=0):
    o.name=name;o.data.materials.append(mats['RP_'+mat]);current.append(o)
    bpy.context.view_layer.objects.active=o
    if bevel:
        mod=o.modifiers.new('Soft worn edges','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o
def box(name,loc,size,mat='Steel',bevel=.001):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,bevel)
def cylinder(name,loc,r,depth,mat='Steel',vertices=16,bevel=.0005):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc)
    return finish(bpy.context.object,name,mat,bevel)
def extrude(name,poly,z0,z1,mat='Steel',bevel=0):
    # Counterclockwise XY outline, solid thickness along Z.
    if sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly)))<0:poly=list(reversed(poly))
    n=len(poly);verts=[(x,y,z) for z in [z0,z1] for x,y in poly]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    return finish(o,name,mat,bevel)
def loopmesh(name,outer,inner,z0,z1,mat,bevel=0):
    n=len(outer);verts=[(x,y,z) for z in [z0,z1] for poly in [outer,inner] for x,y in poly]
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i+n,j+n,j,i),(i+2*n,j+2*n,j+3*n,i+3*n),(i,j,j+2*n,i+2*n),(j+n,i+n,i+3*n,j+3*n)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    return finish(o,name,mat,bevel)
def annulus(name,center,ro,ri,z0,z1,mat,bevel=.0006,n=32):
    x,y=center
    return loopmesh(name,[(x+ro*math.cos(i*2*math.pi/n),y+ro*math.sin(i*2*math.pi/n)) for i in range(n)],[(x+ri*math.cos(i*2*math.pi/n),y+ri*math.sin(i*2*math.pi/n)) for i in range(n)],z0,z1,mat,bevel)
def subtract(target,cutter):
    bpy.context.view_layer.objects.active=target
    mod=target.modifiers.new('Machined recess','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    current.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)
def key(mat='Gold',z=0,scale=1):
    start=len(current)
    annulus('Faceted circular key bow',(0,-.057),.032,.023,z+.002,z+.008,mat)
    box('Rectangular key shank',(0,.023,z+.005),(.009,.109,.006),mat,.0006)
    # Two separated teeth and a small end nib, matching the reference silhouette.
    box('Lower key tooth',(-.009,.046,z+.005),(.022,.008,.006),mat,.0005)
    box('Upper key tooth',(-.010,.074,z+.005),(.025,.009,.006),mat,.0005)
    box('Tip step',(-.003,.081,z+.005),(.011,.006,.006),mat,.0004)
    box('Bow collar',(0,-.025,z+.005),(.012,.014,.008),mat,.0006)
    # Union the pieces: coincident top faces otherwise cause black seams at teeth.
    parts=list(current[start:]);base=parts[0]
    for other in parts[1:]:
        bpy.context.view_layer.objects.active=base
        mod=base.modifiers.new('Joined key metal','BOOLEAN');mod.operation='UNION';mod.solver='EXACT';mod.object=other
        bpy.ops.object.modifier_apply(modifier=mod.name)
        current.remove(other);bpy.data.objects.remove(other,do_unlink=True)
    for o in current[start:]:
        o.location*=scale
        for v in o.data.vertices:v.co*=scale
    return current[start:]
def blade():
    poly=[(-.20,-.033),(.23,-.033),(.23,.026)]
    for i in range(21):
        x=.23-i*.0205
        poly.extend([(x-.007,.026),(x-.012,.043),(x-.0205,.026)])
    poly.extend([(-.21,.029),(-.219,.016),(-.219,-.022)])
    o=extrude('Steel blade with 21 cut teeth',poly,.009,.012,'Steel',.00035)
    for x in [-.195,-.164]:
        subtract(o,cylinder('Mounting hole cutter',(x,-.012,.011),.0033,.02,'Dark',12,0))
    return o
def handle(x=0):
    outer=[(-.115,-.015),(-.100,-.058),(-.056,-.079),(.078,-.069),(.091,.057),(-.051,.077),(-.101,.042)]
    inner=[(-.073,-.012),(-.060,-.031),(-.033,-.042),(.038,-.037),(.043,.026),(-.034,.039),(-.064,.020)]
    outer=[(a+x,b) for a,b in outer];inner=[(a+x,b) for a,b in inner]
    o=loopmesh('Solid walnut D grip',outer,inner,.001,.025,'Wood',.004)
    for y in [-.045,.038]:
        cylinder('Brass handle pin',(x+.066,y,.025),.0068,.002,'Brass',12,.0004)
    return o
def screw(x,y,height):
    cylinder('Screw core',(x,y,height+.023),.0042,.043,'Steel',12,.0003)
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=.0025,radius2=.0042,depth=.007,location=(x,y,height+.002))
    finish(bpy.context.object,'Blunt screw tip','Steel')
    # Actual continuous helical thread, not stacked discs.
    verts=[];faces=[];steps=160;turns=9
    for j in range(steps+1):
        a=j/steps*turns*2*math.pi;z=height+.005+j/steps*.035
        for r,dz in [(.0041,-.0013),(.00565,0),(.0041,.0013)]:verts.append((x+r*math.cos(a),y+r*math.sin(a),z+dz))
    for j in range(steps):
        for k in range(2):faces.append((j*3+k,(j+1)*3+k,(j+1)*3+k+1,j*3+k+1))
    faces.extend([(0,1,2),(steps*3+2,steps*3+1,steps*3)])
    mesh=bpy.data.meshes.new('Helical thread');mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new('Nine-turn helical thread',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,'Steel')
    head=cylinder('Slotted screw head',(x,y,height+.048),.010,.008,'Steel',16,.0007)
    subtract(head,box('Slot cutter',(x,y,height+.052),(.026,.0027,.005),'Dark',0))
    box('Dark bottom of slot',(x,y,height+.0496),(.017,.0025,.0001),'Dark',0)

def save_group(name):
    global current
    # Stable object names, applied triangulation and metre-based UVs on every mesh.
    for index,o in enumerate(current):
        o.name=f'{name}_{index:02d}_{o.name.split(".")[0]}'
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('Game triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
        uv=o.data.uv_layers.new(name='UVMap') if not o.data.uv_layers else o.data.uv_layers.active
        for face in o.data.polygons:
            axis=max(range(3),key=lambda k:abs(face.normal[k]));axes=[k for k in range(3) if k!=axis]
            for li in face.loop_indices:
                p=o.matrix_world @ o.data.vertices[o.data.loops[li].vertex_index].co
                uv.data[li].uv=(p[axes[0]]*6+.31,p[axes[1]]*6+.47)
    bpy.ops.object.select_all(action='DESELECT')
    for o in current:o.select_set(True)
    bpy.context.view_layer.objects.active=current[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
    collection=bpy.data.collections.new(name);scene.collection.children.link(collection)
    for o in current:
        for c in list(o.users_collection):c.objects.unlink(o)
        collection.objects.link(o)
        o.hide_render=True;o.hide_set(True)
    groups[name]=list(current);current=[]

box('Beveled metal blank',(0,0,.008),(.065,.235,.016),'Steel',.004)
box('Inset dark rectangular stamp',(0,-.073,.0161),(.048,.047,.0007),'Dark',.0008)
save_group('MetalBlank')
key('Gold');save_group('GoldKey')
key('Silver');save_group('SilverKey')
blade();save_group('SawBlade')
handle();save_group('SawHandle')
for x,y,h in [(-.029,.016,.015),(0,-.012,0),(.033,.004,0)]:screw(x,y,h)
save_group('Screws')
blade();handle(-.255);save_group('AssembledSaw')

# Shallow key-shaped engraving cut into the mold plate, surrounded by raised rails.
box('Mold steel base',(0,0,.009),(.250,.205,.018),'Steel',.003)
plate=box('Dark mold face',(0,0,.019),(.225,.178,.013),'Dark',.001)
cutters=key('Steel',z=.023,scale=.85)
for cutter in list(cutters):subtract(plate,cutter)
for face in plate.data.polygons:face.material_index=0
for x in [-.119,.119]:box('Side frame rail',(x,0,.024),(.012,.205,.022),'Steel',.0015)
for y in [-.096,.096]:box('End frame rail',(0,y,.024),(.226,.012,.022),'Steel',.0015)
for x in [-.093,.093]:
    for y in [-.071,.071]:
        head=cylinder('Corner brass fastener',(x,y,.027),.0058,.003,'Brass',12,.0004)
        subtract(head,box('Fastener slot',(x,y,.0285),(.014,.0014,.0015),'Dark',0))
save_group('KeyMold')

def bounds(obs):
    points=[o.matrix_world@Vector(c) for o in obs for c in o.bound_box]
    lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
    return lo,hi
report={}
for name,obs in groups.items():
    lo,hi=bounds(obs)
    report[name]={'triangles':sum(len(o.data.polygons) for o in obs),'objects':len(obs),'dimensions_m':list(hi-lo),'uv_complete':all(len(o.data.uv_layers)>0 for o in obs)}
(SOURCE/'mesh-report.json').write_text(json.dumps(report,indent=2))

bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='Preview camera';scene.camera=cam;cam.data.type='ORTHO'
lights=[]
for name,pos,energy,size in [('Large softbox',(-1,-1,2),90,2),('Cool edge light',(1,.5,1.5),55,1.2)]:
    bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.name=name;l.data.energy=energy;l.data.shape='DISK';l.data.size=size
    l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler();lights.append(l)
for name,obs in groups.items():
    for o in obs:o.hide_render=False;o.hide_set(False)
    lo,hi=bounds(obs);center=(lo+hi)/2
    direction=Vector((.9,-1.45,2.5)) if name not in ['GoldKey','SilverKey','MetalBlank'] else Vector((.7,-1.25,2.8))
    cam.location=center+direction;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    inv=cam.rotation_euler.to_matrix().transposed()
    ps=[inv@(o.matrix_world@Vector(c)-center) for o in obs for c in o.bound_box]
    extent=max(max(p[i] for p in ps)-min(p[i] for p in ps) for i in [0,1])
    cam.data.ortho_scale=extent*1.24
    scene.render.filepath=str(PREVIEW/(name+'.png'));bpy.ops.render.render(write_still=True)
    for o in obs:o.hide_render=True;o.hide_set(True)

# Lay editable collections out on a workbench in the source file.
for index,(name,obs) in enumerate(groups.items()):
    for o in obs:o.location+=Vector(((index%4)*.72,-(index//4)*.46,0));o.hide_render=False;o.hide_set(False)
for im in bpy.data.images:
    if im.source in {'FILE','GENERATED'}:im.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ReferenceProps.blend'))
print('REFERENCE_PROPS_COMPLETE '+json.dumps(report))
