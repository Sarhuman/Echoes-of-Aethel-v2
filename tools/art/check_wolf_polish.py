"""Verify the polish preserves every runtime script, prefab and saved scene."""
import hashlib, json, pathlib, subprocess

root=pathlib.Path.cwd()
baseline='9cd4a24f838bbd8200beb34eea4e8de72125c60e'
paths=subprocess.check_output(['git','ls-tree','-r','--name-only',baseline],text=True).splitlines()
protected=[p for p in paths if p.startswith(('Assets/Scripts/Runtime/','Assets/Prefabs/','Assets/Scenes/',
                                           'ProjectSettings/','Packages/'))]
checks=[]
for path in protected:
    before=subprocess.check_output(['git','show',baseline+':'+path]).replace(b'\r\n',b'\n')
    after=(root/path).read_bytes().replace(b'\r\n',b'\n')
    assert before==after,path
    checks.append(path)
source=root/'Assets/Art/Models/Exported/MD_LoboEtereo.fbx'
assert hashlib.sha256(source.read_bytes()).hexdigest()=='e4fea772ba883f9d5dd5ddff416caa2e278eaa6689f17cb43707567be7219044'
folder=root/'docs/validation/wolf-polish'
report={'baseline':baseline,'all_preserved':True,'paths':checks,'original_mesh_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'scene_sha256':hashlib.sha256((root/'Assets/Scenes/Aethel_FirstArena.unity').read_bytes()).hexdigest()}
(folder/'preserved-foundation.json').write_text(json.dumps(report,indent=2)+'\n')
print('AETHEL_WOLF_POLISH_FOUNDATION_PRESERVED',len(checks),'files')
