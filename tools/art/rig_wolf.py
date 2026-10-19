"""Author the v2 wolf FK rig, skin and six initial clips in Blender 4.5 LTS.

Run explicitly from repository root: blender --background --factory-startup
--python tools/art/rig_wolf.py. Never invoked by Unity import/compilation/build.
Input FBX remains untouched. Output includes editable authoring data in .blend.zip.
These are initial authored FK poses, still subject to artist/foot-contact review.
"""
import bpy, math, pathlib, hashlib, json, zipfile
from mathutils import Vector, Quaternion, Matrix

root = pathlib.Path.cwd()
source = root / 'Assets/Art/Models/Exported/MD_LoboEtereo.fbx'
assert hashlib.sha256(source.read_bytes()).hexdigest() == 'e4fea772ba883f9d5dd5ddff416caa2e278eaa6689f17cb43707567be7219044'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source), use_image_search=False)
mesh = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True); bpy.context.view_layer.objects.active = mesh
# Meshy FBX includes a scaled parent. Bake the complete world matrix before
# unparenting; applying only the mesh's local transform silently changes units.
world=mesh.matrix_world.copy()
points = [world @ v.co for v in mesh.data.vertices]
mesh.parent=None; mesh.matrix_parent_inverse=Matrix.Identity(4); mesh.matrix_world=Matrix.Identity(4)
for v,p in zip(mesh.data.vertices,points): v.co=p
for o in list(bpy.context.scene.objects):
    if o!=mesh: bpy.data.objects.remove(o,do_unlink=True)
minimum = [min(v[i] for v in points) for i in range(3)]
maximum = [max(v[i] for v in points) for i in range(3)]
cx = (minimum[0]+maximum[0])/2; cy = (minimum[1]+maximum[1])/2
scale = .9/(maximum[2]-minimum[2])
def point(p): return Vector(((p[0]-cx)*scale, (p[1]-cy)*scale, (p[2]-minimum[2])*scale))
for v in mesh.data.vertices: v.co = point(v.co)
mesh.name = 'WolfSkin'; mesh.data.name = 'WolfSkin_5164tris'
mesh.data.materials.clear()  # Shared Unity material is assigned in the prefab.
for image in list(bpy.data.images): bpy.data.images.remove(image)

data = bpy.data.armatures.new('WolfSkeleton')
rig = bpy.data.objects.new('WolfRig', data)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig; mesh.select_set(False); rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
spec = {}
def bone(name, head, tail, parent=None):
    b=data.edit_bones.new(name); b.head=point(head); b.tail=point(tail)
    if parent: b.parent=data.edit_bones[parent]
    spec[name]=(Vector(head), Vector(tail))
bone('Root',(cx,cy,minimum[2]),(cx,cy,minimum[2]+.12))
bone('Pelvis',(0,.25,-.04),(0,.12,.035),'Root')
bone('Spine',(0,.12,.035),(0,-.14,.06),'Pelvis')
bone('Chest',(0,-.14,.06),(0,-.39,.09),'Spine')
bone('Neck',(0,-.39,.09),(0,-.55,.16),'Chest')
bone('Head',(0,-.55,.16),(0,-.82,.12),'Neck')
bone('Jaw',(0,-.62,.025),(0,-.89,-.015),'Head')
for sign,side in [(-1,'L'),(1,'R')]:
    bone('Ear.'+side,(sign*.09,-.64,.23),(sign*.10,-.66,.36),'Head')
    bone('ForeUpper.'+side,(sign*.13,-.38,.06),(sign*.14,-.41,-.16),'Chest')
    bone('ForeLower.'+side,(sign*.14,-.41,-.16),(sign*.14,-.58,-.29),'ForeUpper.'+side)
    bone('ForePaw.'+side,(sign*.14,-.58,-.29),(sign*.14,-.70,-.35),'ForeLower.'+side)
    bone('HindUpper.'+side,(sign*.13,.24,-.035),(sign*.15,.38,-.17),'Pelvis')
    bone('HindLower.'+side,(sign*.15,.38,-.17),(sign*.15,.25,-.28),'HindUpper.'+side)
    bone('HindPaw.'+side,(sign*.15,.25,-.28),(sign*.15,.08,-.36),'HindLower.'+side)
bone('TailBase',(0,.30,-.025),(0,.52,-.015),'Pelvis')
bone('TailMid',(0,.52,-.015),(0,.72,.10),'TailBase')
bone('TailTip',(0,.72,.10),(0,.90,.22),'TailMid')
bpy.ops.object.mode_set(mode='OBJECT')

groups={n:mesh.vertex_groups.new(name=n) for n in spec}
def distance(p,a,b):
    ab=b-a; t=max(0,min(1,(p-a).dot(ab)/ab.length_squared))
    return (p-(a+t*ab)).length
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a))); return t*t*(3-2*t)
def nearest(p,names):
    values=sorted([(n,1/(distance(p,*spec[n])+.025)**4) for n in names],key=lambda t:-t[1])[:3]
    total=sum(v for n,v in values)
    return {n:v/total for n,v in values}
for index,p in enumerate(points):
    x,y,z=p; side='L' if x<0 else 'R'
    torso=nearest(p,['Pelvis','Spine','Chest','Neck','Head'])
    if y>.43:
        weights=nearest(p,['TailBase','TailMid','TailTip'])
    elif z<-.10 and (y<-.24 or y>-.06):
        chain='Fore' if y<-.24 else 'Hind'
        limb=nearest(p,[chain+part+'.'+side for part in ['Upper','Lower','Paw']])
        blend=1-smooth(-.19,-.08,z)
        weights={n:v*(1-blend) for n,v in torso.items()}
        for n,v in limb.items(): weights[n]=weights.get(n,0)+v*blend
    elif y<-.54:
        weights=nearest(p,['Neck','Head'])
        if z>.235 and y<-.57 and abs(x)>.035:
            t=smooth(.235,.30,z)
            weights={n:v*(1-t) for n,v in weights.items()}; weights['Ear.'+side]=t
        elif -.16<z<.025 and y<-.64:
            t=(1-smooth(-.015,.025,z))*smooth(-.64,-.73,y)
            weights={n:v*(1-t) for n,v in weights.items()}; weights['Jaw']=t
    else: weights=torso
    weights=sorted(weights.items(),key=lambda t:-t[1])[:4]
    total=sum(v for n,v in weights)
    for name,value in weights:
        if value>0: groups[name].add([index],value/total,'REPLACE')
modifier=mesh.modifiers.new('Wolf skin','ARMATURE'); modifier.object=rig
mesh.parent=rig
mesh.matrix_parent_inverse=Matrix.Identity(4)
rig.animation_data_create()
fps=30; bpy.context.scene.render.fps=fps
clips={'Idle':2.0,'Run':14/30,'Windup':14/30,'Bite':.20,'Recovery':26/30,'Death':1.0}
def rot(pose,rx=0,ry=0,rz=0):
    basis=pose.bone.matrix_local.to_quaternion()
    world=Quaternion((1,0,0),math.radians(rx))@Quaternion((0,1,0),math.radians(ry))@Quaternion((0,0,1),math.radians(rz))
    pose.rotation_quaternion=basis.inverted()@world@basis
def offset(pose,v): pose.location=pose.bone.matrix_local.to_3x3().inverted()@Vector(v)
def ease(x): return x*x*(3-2*x)
for name,duration in clips.items():
    action=bpy.data.actions.new(name); action.use_fake_user=True
    rig.animation_data.action=action
    end=round(duration*fps)+1
    for frame in range(1,end+1):
        t=(frame-1)/(end-1); wave=math.sin(t*math.tau)
        for pose in rig.pose.bones:
            pose.rotation_mode='QUATERNION'; pose.rotation_quaternion=(1,0,0,0); pose.location=(0,0,0); pose.scale=(1,1,1)
        if name=='Idle':
            offset(rig.pose.bones['Chest'],(0,0,.006*wave)); rot(rig.pose.bones['Neck'],rx=1.5*wave)
            rot(rig.pose.bones['TailMid'],rz=4*wave)
        elif name=='Run':
            offset(rig.pose.bones['Root'],(0,0,.026*(1-math.cos(t*math.tau*2))))
            rot(rig.pose.bones['Spine'],rx=4*wave); rot(rig.pose.bones['Head'],rx=-4*wave)
            for sign,side in [(-1,'L'),(1,'R')]:
                w=math.sin(t*math.tau+(0 if sign<0 else math.pi))
                rot(rig.pose.bones['ForeUpper.'+side],rx=32*w)
                rot(rig.pose.bones['ForeLower.'+side],rx=18*max(0,-w))
                rot(rig.pose.bones['ForePaw.'+side],rx=-12*w)
                rot(rig.pose.bones['HindUpper.'+side],rx=-30*w)
                rot(rig.pose.bones['HindLower.'+side],rx=-20*max(0,w))
                rot(rig.pose.bones['HindPaw.'+side],rx=12*w)
            rot(rig.pose.bones['TailBase'],rx=5,rz=7*wave)
        elif name in ('Windup','Bite','Recovery'):
            if name=='Windup':
                charge=ease(min(1,t/.72)); contact=ease(max(0,(t-.82)/.18))
            elif name=='Bite': charge=1-ease(t); contact=1-ease(t)
            else: charge=.15*(1-ease(t)); contact=0
            offset(rig.pose.bones['Root'],(0,-.085*contact,-.055*charge+.035*contact))
            rot(rig.pose.bones['Chest'],rx=-5*charge+10*contact)
            rot(rig.pose.bones['Neck'],rx=-12*charge+26*contact)
            rot(rig.pose.bones['Head'],rx=-6*charge+16*contact)
            rot(rig.pose.bones['Jaw'],rx=23*charge*(1-contact))
            for side in ['L','R']:
                rot(rig.pose.bones['ForeUpper.'+side],rx=-9*charge+18*contact)
                rot(rig.pose.bones['ForeLower.'+side],rx=14*charge)
                rot(rig.pose.bones['HindUpper.'+side],rx=12*charge)
                rot(rig.pose.bones['Ear.'+side],rx=-12*charge)
            rot(rig.pose.bones['TailBase'],rx=-12*charge)
        elif name=='Death':
            collapse=ease(min(1,t/.7))
            offset(rig.pose.bones['Root'],(0,0,.14*collapse))
            rot(rig.pose.bones['Root'],ry=78*collapse)
            rot(rig.pose.bones['Head'],rx=23*collapse)
            for side in ['L','R']:
                rot(rig.pose.bones['ForeUpper.'+side],rx=30*collapse)
                rot(rig.pose.bones['HindUpper.'+side],rx=-25*collapse)
            rot(rig.pose.bones['TailBase'],rx=25*collapse)
        for pose in rig.pose.bones:
            pose.keyframe_insert('rotation_quaternion',frame=frame,group=pose.name)
            pose.keyframe_insert('location',frame=frame,group=pose.name)
    print('AETHEL_WOLF_CLIP',name,'frames',end,'seconds',(end-1)/fps,flush=True)

rig.animation_data.action=bpy.data.actions['Idle']
rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); rig.select_set(True)
bpy.context.view_layer.objects.active=rig
output=root/'Assets/Art/Models/Exported/MD_LoboEtereo_Rigged.fbx'
bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'MESH','ARMATURE'},
    add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',
    path_mode='STRIP',embed_textures=False)
rig.animation_data.action=bpy.data.actions['Idle']
rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.context.scene.frame_set(1); bpy.context.view_layer.update()
authoring=root/'.bootstrap/LoboEtereo_Rig.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(authoring))
archive=root/'Assets/Art/Models/Source/LoboEtereo_Rig.blend.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z: z.write(authoring,authoring.name)
report=dict(source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),output_sha256=hashlib.sha256(output.read_bytes()).hexdigest(),
    blender=bpy.app.version_string,fbx_scale_mode='FBX_SCALE_ALL',bones=len(data.bones),vertices=len(mesh.data.vertices),triangles=sum(len(p.vertices)-2 for p in mesh.data.polygons),
    unweighted_vertices=sum(not v.groups for v in mesh.data.vertices),max_influences=max(len(v.groups) for v in mesh.data.vertices),clips=clips,
    note='Initial authored FK rig/skin; visual polish and planted-foot review remain separate acceptance gates.')
folder=root/'docs/validation/wolf'; folder.mkdir(parents=True,exist_ok=True)
(folder/'rig-generation.json').write_text(json.dumps(report,indent=2)+'\n')
print('AETHEL_WOLF_RIG_EXPORTED',report,flush=True)
