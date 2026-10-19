"""Offline triangle crossing and regional stretch audit; no scene writes.

blender --background <wolf.blend> --python-exit-code 1 --python
tools/art/audit_wolf_surface.py -- <report.json>
Triangle pairs sharing a vertex are excluded; rest crossings are reported
separately. This detects crossings, not close contact, thickness or containment.
"""
import bpy, json, pathlib, sys
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri

mesh=bpy.data.objects['WolfSkin'];rig=bpy.data.objects['WolfRig']
rest=[v.co.copy() for v in mesh.data.vertices]
mesh.data.calc_loop_triangles()
triangles=[tuple(t.vertices) for t in mesh.data.loop_triangles]
sets=[set(t) for t in triangles]

def crossings(vertices):
    tree=BVHTree.FromPolygons(vertices,triangles,all_triangles=True)
    pairs=set()
    for a,b in tree.overlap(tree):
        if a>=b or sets[a]&sets[b]:continue
        # Confirm an interior segment/triangle crossing instead of relying on
        # broad-phase overlap. Segment endpoints/coplanar cases are omitted;
        # triangle-edge touches can remain, so counts are a diagnostic proxy.
        hit=False
        for first,second in [(triangles[a],triangles[b]),(triangles[b],triangles[a])]:
            v0,v1,v2=[vertices[i] for i in second]
            for i in range(3):
                origin=vertices[first[i]];delta=vertices[first[(i+1)%3]]-origin
                if delta.length<1e-7:continue
                point=intersect_ray_tri(v0,v1,v2,delta.normalized(),origin,True)
                if point is not None and 1e-5<(point-origin).length<delta.length-1e-5:
                    hit=True;break
            if hit:break
        if hit:pairs.add((a,b))
    return pairs

def region(p):
    if .15<p.y<.80 and p.z>.10:return 'haunch'
    if p.y>.65:return 'tail'
    if p.y<-.35 and p.z>.18:return 'mane'
    return 'other'

rest_pairs=crossings(rest)
report={'method':'Confirmed non-coplanar triangle crossings; shared-vertex pairs excluded; 9 uniform samples/clip',
        'rest_crossings':len(rest_pairs),'clips':{}}
for name in ['Idle','Run','Windup','Bite','Recovery','Death']:
    action=bpy.data.actions[name];rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0]
    start,end=action.frame_range;counts=[];added=[];ratios={n:[] for n in ['haunch','tail','mane','other']};worst={}
    for sample in range(9):
        frame=start+(end-start)*sample/8
        bpy.context.scene.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update()
        evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());baked=evaluated.to_mesh()
        vertices=[v.co.copy() for v in baked.vertices];evaluated.to_mesh_clear()
        pairs=crossings(vertices);counts.append(len(pairs));added.append(len(pairs-rest_pairs))
        for edge in mesh.data.edges:
            a,b=edge.vertices;length=(rest[a]-rest[b]).length
            if length<.001:continue
            group=region((rest[a]+rest[b])*.5)
            ratio=(vertices[a]-vertices[b]).length/length;ratios[group].append(ratio)
            if ratio>worst.get(group,{}).get('ratio',0):
                worst[group]={'ratio':ratio,'sample':sample,'vertices':[a,b],
                    'weights':[{mesh.vertex_groups[g.group].name:round(g.weight,4) for g in mesh.data.vertices[i].groups} for i in [a,b]]}
    report['clips'][name]={'crossings_per_sample':counts,'crossings_absent_in_rest_per_sample':added,
        'regions':{n:{'p99':sorted(v)[int(len(v)*.99)],'max':max(v),'worst':worst[n]} for n,v in ratios.items() if v}}
out=pathlib.Path(sys.argv[sys.argv.index('--')+1]);out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(report,indent=2)+'\n')
print('AETHEL_WOLF_SURFACE_AUDIT',out,'rest crossings',len(rest_pairs),flush=True)
