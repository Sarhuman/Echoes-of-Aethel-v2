"""Author the v2 wolf FK rig, skin and six initial clips in Blender 4.5 LTS.

Run explicitly from repository root: blender --background --factory-startup
--python tools/art/rig_wolf.py. Never invoked by Unity import/compilation/build.
Input FBX remains untouched. Output includes editable authoring data in .blend.zip.
Leg targets are solved offline and baked into the same 24-bone Generic rig.
There is no runtime IK, physics change or animation-driven damage.
"""
import bpy, math, pathlib, hashlib, json, zipfile, runpy
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
    # The supplied Meshy mesh is an asymmetric stance, not a mirrored bind pose.
    # Fit each chain to its actual limb instead of assigning sides by x sign.
    fore = [(-.09,-.35,.06),(-.09,-.40,-.16),(-.09,-.48,-.29),(-.09,-.58,-.36)] if side=='L' else [(.10,-.38,.06),(.10,-.48,-.16),(.10,-.61,-.29),(.10,-.73,-.35)]
    hind = [(-.09,.28,-.035),(-.03,.42,-.17),(.02,.32,-.28),(.02,.18,-.36)] if side=='L' else [(.13,.24,-.035),(.17,.30,-.17),(.18,.13,-.28),(.19,-.02,-.36)]
    for chain, joints, parent in [('Fore',fore,'Chest'),('Hind',hind,'Pelvis')]:
        for i,part in enumerate(['Upper','Lower','Paw']):
            bone(chain+part+'.'+side,joints[i],joints[i+1],parent if i==0 else chain+['Upper','Lower'][i-1]+'.'+side)
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
    # Classify the head before limbs: the lower muzzle must never follow a foreleg.
    if y<-.64 and z>-.17:
        weights=nearest(p,['Neck','Head'])
        if z>.235 and abs(x)>.035:
            t=smooth(.235,.30,z)
            weights={n:v*(1-t) for n,v in weights.items()}; weights['Ear.'+side]=t
        elif z<.025:
            t=(1-smooth(-.015,.025,z))*smooth(-.64,-.73,y)
            weights={n:v*(1-t) for n,v in weights.items()}; weights['Jaw']=t
    elif y>.40:
        t=smooth(.40,.55,y)
        weights={n:v*(1-t) for n,v in torso.items()}
        for n,v in nearest(p,['TailBase','TailMid','TailTip']).items():
            weights[n]=weights.get(n,0)+v*t
    elif z<-.10 and (y<-.24 or y>-.06):
        chain='Fore' if y<-.24 else 'Hind'
        side=min(['L','R'],key=lambda s:min(distance(p,*spec[chain+part+'.'+s]) for part in ['Upper','Lower','Paw']))
        limb=nearest(p,[chain+part+'.'+side for part in ['Upper','Lower','Paw']])
        # A rigid sole patch prevents toe/ankle mixtures from shearing on support.
        sole=1-smooth(-.31,-.245,z)
        limb={n:v*(1-sole) for n,v in limb.items()}
        limb[chain+'Paw.'+side]=limb.get(chain+'Paw.'+side,0)+sole
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
# Smooth joint transitions over actual topology; preserve low sole vertices.
# This also removes a hard left/right assignment through the asymmetric haunch.
neighbors=[set() for v in mesh.data.vertices]
for edge in mesh.data.edges:
    a,b=edge.vertices; neighbors[a].add(b); neighbors[b].add(a)
neighbor_weights=[]
for i,adjacent in enumerate(neighbors):
    values={j:1/((mesh.data.vertices[i].co-mesh.data.vertices[j].co).length+.005)**2 for j in adjacent}
    total=sum(values.values())
    neighbor_weights.append({j:.5*v/total+.5/len(adjacent) for j,v in values.items()})
weights=[{mesh.vertex_groups[g.group].name:g.weight for g in v.groups} for v in mesh.data.vertices]
for iteration in range(24):
    revised=[]
    for i,w in enumerate(weights):
        if points[i].z<-.31 or not neighbors[i]: revised.append(w); continue
        blended={n:v*.4 for n,v in w.items()}
        # Narrow fur triangles need coherence across their short edges. Uniform
        # diffusion treated a long strand edge like its millimetre-wide base.
        for j,factor in neighbor_weights[i].items():
            for n,v in weights[j].items(): blended[n]=blended.get(n,0)+.6*v*factor
        best=sorted(blended.items(),key=lambda t:-t[1])[:4]; total=sum(v for n,v in best)
        revised.append({n:v/total for n,v in best})
    weights=revised
# Keep distal joints from pulling fur on the haunch/mane. Nearest-segment
# weights and unconstrained diffusion can leak a paw across the upper-leg fur.
# Transfer that influence up the *same* chain through smooth anatomical bands;
# soles, jaw, ears, topology, UVs and the authored motion remain untouched.
for i,w in enumerate(weights):
    z=points[i].z
    for chain in ['Fore','Hind']:
        for side in ['L','R']:
            for part,parent,low,high in [('Paw','Lower',-.30,-.24),('Lower','Upper',-.23,-.13)]:
                name=chain+part+'.'+side
                transfer=w.get(name,0)*smooth(low,high,z)
                if transfer:
                    w[name]-=transfer
                    target=chain+parent+'.'+side
                    w[target]=w.get(target,0)+transfer
    best=sorted(w.items(),key=lambda t:-t[1])[:4];total=sum(v for n,v in best)
    weights[i]={n:v/total for n,v in best if v>1e-8}
for group in groups.values(): group.remove(list(range(len(points))))
for i,w in enumerate(weights):
    for n,v in w.items(): groups[n].add([i],v,'REPLACE')
modifier=mesh.modifiers.new('Wolf skin','ARMATURE'); modifier.object=rig
mesh.parent=rig
mesh.matrix_parent_inverse=Matrix.Identity(4)
rig.animation_data_create()
fps=60; bpy.context.scene.render.fps=fps
clips={'Idle':2.0,'Run':.4,'Windup':14/30,'Bite':.20,'Recovery':26/30,'Death':1.0}
def rot(pose,rx=0,ry=0,rz=0):
    basis=pose.bone.matrix_local.to_quaternion()
    world=Quaternion((1,0,0),math.radians(rx))@Quaternion((0,1,0),math.radians(ry))@Quaternion((0,0,1),math.radians(rz))
    pose.rotation_quaternion=basis.inverted()@world@basis
def offset(pose,v): pose.location=pose.bone.matrix_local.to_3x3().inverted()@Vector(v)
def ease(x): return x*x*(3-2*x)

def plant(chain,side,travel=0,lift=0,running=False):
    """Analytic two-link solve in armature space, baked as FK keys.

    Preserve a stable rearward knee pole and level paw orientation; solve against a
    fixed ground target after torso motion. No extra bones or constraints export.
    """
    upper=rig.pose.bones[chain+'Upper.'+side]
    lower=rig.pose.bones[chain+'Lower.'+side]
    paw=rig.pose.bones[chain+'Paw.'+side]
    bpy.context.view_layer.update()
    hip=upper.head.copy()
    ankle=paw.bone.head_local+Vector((0,travel,lift))
    if running:
        ankle.y=upper.bone.head_local.y+travel+(-.08 if chain=='Fore' else -.04)
        ankle.x+=(-1 if side=='L' else 1)*(.045 if chain=='Hind' else .025)
    delta=ankle-hip; distance_to=delta.length; direction=delta.normalized()
    a=upper.bone.length; b=lower.bone.length
    reach=min(distance_to,a+b-.00001)
    # Clamp unreachable poses instead of stretching the skin or changing bones.
    ankle=hip+direction*reach
    along=(a*a-b*b+reach*reach)/(2*reach)
    height=math.sqrt(max(0,a*a-along*along))
    # A fixed pole avoids a nearly straight foreleg flipping its knee plane
    # during the contact push. The old rest-vector projection could cross zero.
    pole=Vector((0,1,0)) if chain=='Fore' else lower.bone.head_local-upper.bone.head_local
    bend=pole-direction*pole.dot(direction)
    bend.normalize()
    knee=hip+direction*along+bend*height
    for pose,head,tail in [(upper,hip,knee),(lower,knee,ankle)]:
        rest_dir=pose.bone.tail_local-pose.bone.head_local
        rotation=rest_dir.rotation_difference(tail-head)@pose.bone.matrix_local.to_quaternion()
        pose.matrix=Matrix.LocRotScale(head,rotation,Vector((1,1,1)))
        bpy.context.view_layer.update()
    paw.matrix=Matrix.LocRotScale(ankle,paw.bone.matrix_local.to_quaternion(),Vector((1,1,1)))
    bpy.context.view_layer.update()

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
            offset(rig.pose.bones['Root'],(0,0,-.06+.006*(1-math.cos(t*math.tau*2))))
            rot(rig.pose.bones['Spine'],rx=1.5*wave); rot(rig.pose.bones['Head'],rx=-1.5*wave)
            rot(rig.pose.bones['TailBase'],rx=3,rz=5*wave)
        elif name in ('Windup','Bite','Recovery'):
            if name=='Windup':
                charge=ease(min(1,t/.60)); contact=ease(max(0,(t-.68)/.32))
            elif name=='Bite': charge=1-ease(t); contact=1-ease(t)
            else: charge=0; contact=0  # Bite ends at rest; no second crouch/pop.
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
        if name!='Death':
            for chain in ['Fore','Hind']:
                for side in ['L','R']:
                    travel=lift=0
                    if name=='Run':
                        phase=(t+(0 if (chain,side) in [('Fore','L'),('Hind','R')] else .5))%1
                        stance=.32
                        stride=3*duration*stance
                        if phase<stance:
                            travel=stride*(phase/stance-.5)
                        else:
                            u=(phase-stance)/(1-stance)
                            # Hermite swing matches stance velocity at both contacts.
                            slope=3*duration*(1-stance)
                            travel=(2*u**3-3*u*u+1)*stride/2+(u**3-2*u*u+u)*slope+(-2*u**3+3*u*u)*(-stride/2)+(u**3-u*u)*slope
                            lift=.07*math.sin(math.pi*u)**2
                    plant(chain,side,travel,lift,name=='Run')
        for pose in rig.pose.bones:
            pose.keyframe_insert('rotation_quaternion',frame=frame,group=pose.name)
            pose.keyframe_insert('location',frame=frame,group=pose.name)
    print('AETHEL_WOLF_CLIP',name,'frames',end,'seconds',(end-1)/fps,flush=True)

# Reviewed local mesh offsets are applied after weight/pose authoring, so UVs,
# skeleton, weights and every animation key remain independent of these edits.
corrections=runpy.run_path(str(root/'tools/art/apply_wolf_fur_corrections.py'))['apply'](
    mesh,root/'tools/art/wolf_fur_corrections.json')

rig.animation_data.action=bpy.data.actions['Idle']
rig.animation_data.action_slot=rig.animation_data.action.slots[0]
bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); rig.select_set(True)
bpy.context.view_layer.objects.active=rig
output=root/'Assets/Art/Models/Exported/MD_LoboEtereo_Rigged.fbx'
staged=root/'.bootstrap/LoboEtereo_Rig.pending.fbx'
bpy.ops.export_scene.fbx(filepath=str(staged),use_selection=True,object_types={'MESH','ARMATURE'},
    add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',
    path_mode='STRIP',embed_textures=False)
staged.replace(output)
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
    note='Offline two-link planted stance, diagonal gait at 3 m/s; baked FK, no runtime IK. Artist approval remains pending.')
folder=root/'docs/validation/wolf-local-fur'; folder.mkdir(parents=True,exist_ok=True)
(folder/'rig-generation.json').write_text(json.dumps(report,indent=2)+'\n')
(folder/'correction-statistics.json').write_text(json.dumps(corrections,indent=2)+'\n')
print('AETHEL_WOLF_RIG_EXPORTED',report,flush=True)
