"""Apply reviewed sparse bind-mesh edits in Blender, never at Unity import time."""
import hashlib
import json
from mathutils import Vector


def digest(value):
    return hashlib.sha256(json.dumps(value).encode()).hexdigest()


def apply(mesh, path):
    contract=json.loads(path.read_text())
    data=mesh.data
    positions=[v.co.copy() for v in data.vertices]
    topology=[list(p.vertices) for p in data.polygons]
    uvs=[[list(v.uv) for v in layer.data] for layer in data.uv_layers]
    assert len(positions)==contract['vertex_count'], 'Correction vertex count mismatch'
    assert digest(topology)==contract['topology_sha256'], 'Correction topology mismatch'
    assert digest(uvs)==contract['uv_sha256'], 'Correction UV mismatch'
    changes={item['index']:item for item in contract['changes']}
    assert len(changes)==len(contract['changes']), 'Duplicate correction index'
    # Validate every edit before touching geometry. Source re-exports must never
    # silently apply an old vertex index to a different tuft.
    for index,item in changes.items():
        assert 0<=index<len(positions)
        assert (positions[index]-Vector(item['expected_position'])).length<.000001, 'Stale correction position'
        assert Vector(item['delta']).length<=contract['max_delta_metres']+.000001, 'Correction exceeds displacement budget'
        assert positions[index].z>.10, 'Soles and ground contact are protected'
    data.update()
    old_vertex_normals=[v.normal.copy() for v in data.vertices]
    old_corner_normals=[n.vector.copy() for n in data.corner_normals]
    for index,item in changes.items():data.vertices[index].co+=Vector(item['delta'])
    data.update()
    # Rotate existing split normals by the local geometric change. Preserve
    # authored creases and normals away from edited neighbourhoods.
    rotations=[old.rotation_difference(v.normal) if old.length>.00001 and v.normal.length>.00001 else None
               for old,v in zip(old_vertex_normals,data.vertices)]
    data.normals_split_custom_set([rotations[loop.vertex_index]@normal if rotations[loop.vertex_index] is not None else normal
                                  for loop,normal in zip(data.loops,old_corner_normals)])
    assert uvs==[[list(v.uv) for v in layer.data] for layer in data.uv_layers]
    assert topology==[list(p.vertices) for p in data.polygons]
    before=[[min(p[i] for p in positions),max(p[i] for p in positions)] for i in range(3)]
    after=[[min(v.co[i] for v in data.vertices),max(v.co[i] for v in data.vertices)] for i in range(3)]
    assert max(abs(a-b) for x,y in zip(before,after) for a,b in zip(x,y))<.000001, 'Visual fitting bounds changed'
    return {'vertices_edited':len(changes),'max_displacement_metres':max(Vector(v['delta']).length for v in changes.values()),
            'topology_sha256':digest(topology),'uv_sha256':digest(uvs),'bind_bounds_unchanged':True,
            'correction_file_sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
