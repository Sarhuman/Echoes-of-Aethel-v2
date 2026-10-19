"""Offline skin/sole measurements in Blender. No Unity scenes or gameplay writes.

blender --background .bootstrap/LoboEtereo_Rig.blend --python
tools/art/audit_wolf_motion.py -- docs/validation/wolf-polish/baseline.json
"""
import bpy, json, math, pathlib, sys
from mathutils import Vector

mesh = bpy.data.objects['WolfSkin']
rig = bpy.data.objects['WolfRig']
rest = [v.co.copy() for v in mesh.data.vertices]
# Anatomical sole patches, independent of the weights being tested.
soles = {}
for side, sign in [('L', -1), ('R', 1)]:
    for chain, fore in [('Fore', True), ('Hind', False)]:
        candidates = [i for i, p in enumerate(rest) if
                      (p.x*sign > .035 and p.y < -.48 if fore else
                       (p.y > .21 if side=='L' else -.02 < p.y < .17))
                      and p.z < .06]
        low = min(rest[i].z for i in candidates)
        soles[chain+'.'+side] = [i for i in candidates if rest[i].z < low+.018]

report = {'units': 'metres; sole patches selected geometrically, not by skin weights',
          'sole_vertex_counts': {n: len(v) for n, v in soles.items()}, 'clips': {}}
for name in ['Idle', 'Run', 'Windup', 'Bite', 'Recovery']:
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    start, end = map(int, action.frame_range)
    frames, edge_ratios = [], []
    worst = (0, None)
    # Compare both sources at the same 30 Hz regardless of export sampling rate.
    for frame in range(start, end+1, round(bpy.context.scene.render.fps/30)):
        bpy.context.scene.frame_set(frame); bpy.context.view_layer.update()
        evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        baked = evaluated.to_mesh()
        verts = [v.co.copy() for v in baked.vertices]
        feet = {n: sum((verts[i] for i in ids), Vector())/len(ids) for n, ids in soles.items()}
        frames.append(feet)
        for edge in mesh.data.edges:
            a, b = edge.vertices
            length = (rest[a]-rest[b]).length
            if length > .001:
                ratio=(verts[a]-verts[b]).length/length
                edge_ratios.append(ratio)
                if ratio>worst[0]:worst=(ratio,{'frame':frame,'vertices':[a,b],'rest':[list(rest[a]),list(rest[b])]})
        evaluated.to_mesh_clear()
    heights = [p.z for feet in frames for p in feet.values()]
    data = {'sole_centroid_min_height': min(heights), 'sole_centroid_max_height': max(heights),
            'edge_stretch_p99': sorted(edge_ratios)[int(len(edge_ratios)*.99)],
            'edge_stretch_max': max(edge_ratios)}
    data['worst_edge']=worst[1]
    if name == 'Run':
        errors = []
        for foot in soles:
            phase = 0 if foot in ('Fore.L', 'Hind.R') else .5
            for i in range(len(frames)-1):
                a=(i/(len(frames)-1)+phase)%1
                b=((i+1)/(len(frames)-1)+phase)%1
                # Interior of the designed stance; omit contact/lift interpolation.
                if .04 < a < b < .30:
                    delta=frames[i+1][foot]-frames[i][foot]
                    delta.y -= 3/30  # AI forward is Blender -Y, 3 m/s.
                    errors.append(delta.length)
        data['stance_world_delta_max_per_30hz_frame'] = max(errors)
        data['stance_world_delta_mean_per_30hz_frame'] = sum(errors)/len(errors)
    report['clips'][name] = data

out=pathlib.Path(sys.argv[sys.argv.index('--')+1]);out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(report,indent=2)+'\n')
print('AETHEL_WOLF_MOTION_AUDIT', json.dumps(report), flush=True)
