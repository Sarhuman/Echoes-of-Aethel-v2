"""Offline Blender preparation; never executed by Unity or a build.

Run from repository root after extracting the verified FBXs into .bootstrap/raw/<Asset>/.
Original ZIPs remain outside the project; only reduced runtime FBXs are exported.
Blender 4.5.9: --background --factory-startup --python tools/art/optimize_meshy.py
"""
import bpy,json,pathlib
root=pathlib.Path.cwd()
manifest=json.loads((root/'Assets/Art/Models/Source/Meshy_20261019_manifest.json').read_text(encoding='utf-8-sig'))
budgets={'Escudo':5000,'Arvore':15000,'Rocha':6000,'Pilar':8000,'Piso':4000}
jobs=[]
for entry in manifest:
    name=entry['asset']
    if name not in budgets: continue
    fbx=next(f for f in entry['files'] if f['kind']=='fbx')
    jobs.append(dict(asset=name,source=str(root/'.bootstrap/raw'/name/fbx['name']),
        output=str(root/'Assets/Art/Models/Exported'/('MD_'+name+'.fbx')),budget=budgets[name]))
report=[]
for job in jobs:
    if not job['budget']: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=job['source'],use_image_search=False)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    before=sum(len(o.data.polygons) for o in meshes)
    source_tri=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    for o in meshes:
        bpy.context.view_layer.objects.active=o
        modifier=o.modifiers.new('Runtime triangle budget','DECIMATE')
        modifier.ratio=min(1,job['budget']/source_tri)
        modifier.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        o.data.update()
        # Keep UVs and supplied shading; export textures separately, never embed 4K images.
    after=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    bpy.ops.export_scene.fbx(filepath=job['output'],use_selection=False,object_types={'MESH'},
        use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
    report.append(dict(asset=job['asset'],source_triangles=source_tri,runtime_triangles=after,
        budget=job['budget'],uv_layers=[len(o.data.uv_layers) for o in meshes],tool=bpy.app.version_string))
    (root/'docs/validation/assets/optimization.json').write_text(json.dumps(report,indent=2)+'\n')
    print('AETHEL_OPTIMIZED',job['asset'],source_tri,after,flush=True)
