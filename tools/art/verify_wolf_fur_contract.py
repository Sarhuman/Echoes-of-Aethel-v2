"""Read-only Blender comparison: -- <baseline.blend> <revised.blend> <report.json>."""
import bpy, hashlib, json, pathlib, sys

def digest(value):return hashlib.sha256(json.dumps(value).encode()).hexdigest()

def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=str(path),use_scripts=False)
    mesh=bpy.data.objects['WolfSkin'];rig=bpy.data.objects['WolfRig']
    static={'polygons':[list(p.vertices) for p in mesh.data.polygons],
        'uvs':[[list(v.uv) for v in layer.data] for layer in mesh.data.uv_layers],
        'weights':[[[mesh.vertex_groups[g.group].name,g.weight] for g in v.groups] for v in mesh.data.vertices],
        'bones':[(b.name,b.parent.name if b.parent else None,[list(row) for row in b.matrix_local]) for b in rig.data.bones]}
    actions={}
    for action in sorted(bpy.data.actions,key=lambda a:a.name):
        curves=[]
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        curves.append([curve.data_path,curve.array_index,
                            [[list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation] for k in curve.keyframe_points]])
        actions[action.name]=digest(sorted(curves,key=lambda c:(c[0],c[1])))
    return {n:digest(v) for n,v in static.items()},actions,[list(v.co) for v in mesh.data.vertices]

args=sys.argv[sys.argv.index('--')+1:]
before,old_actions,old=snapshot(pathlib.Path(args[0]).resolve())
after,new_actions,new=snapshot(pathlib.Path(args[1]).resolve())
assert before==after,'UV, topology, skin weights or bind skeleton changed'
assert old_actions==new_actions,'Animation keys changed'
assert len(old)==len(new)
changed=[i for i,(a,b) in enumerate(zip(old,new)) if a!=b]
contract=json.loads(pathlib.Path('tools/art/wolf_fur_corrections.json').read_text())
assert set(changed)=={c['index'] for c in contract['changes']},'Unexpected geometry edits'
maximum=max(sum((a-b)**2 for a,b in zip(old[i],new[i]))**.5 for i in changed)
assert maximum<=contract['max_delta_metres']+.000001
report={'baseline':contract['baseline'],'preserved_signatures':before,'identical_animation_curve_signatures':old_actions,
    'vertices_changed':len(changed),'vertices_unchanged':len(old)-len(changed),'max_displacement_metres':maximum,
    'only_declared_vertex_indices_changed':True}
pathlib.Path(args[2]).write_text(json.dumps(report,indent=2)+'\n')
print('AETHEL_WOLF_LOCAL_FUR_CONTRACT_PRESERVED',len(changed),'vertices changed; UVs, skin, rig and keys unchanged',flush=True)
