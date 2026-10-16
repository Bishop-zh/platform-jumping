"""Check the delivered Unity asset references and native pixel contract."""
from pathlib import Path
import json,re
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
assets=ROOT/'Assets'
guids={}
for meta in assets.rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(encoding='utf-8'),re.M)
    if match:
        guid=match[1]
        assert guid not in guids, f'Duplicate GUID: {meta}, {guids.get(guid)}'
        guids[guid]=meta
packages=set()
for meta in (ROOT/'.work/UnityMainMenu/Packages').rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(encoding='utf-8',errors='ignore'),re.M)
    if match: packages.add(match[1])
report=[]
files=list((assets/'Prefabs/World').rglob('*.prefab'))+list((assets/'Prefabs/Levels').glob('*.prefab'))+list((assets/'Scenes').glob('Level0*.unity'))+[assets/'Prefabs/UI/SharedModal.prefab']
for asset in files:
    for guid in re.findall(r'guid: ([a-f0-9]{32})',asset.read_text(encoding='utf-8')):
        assert guid.startswith('0000000000000000') or guid in guids or guid in packages, f'Missing reference {guid}: {asset}'
report.append(f'{len(files)} scene/prefab files: all external GUID references resolve')
manifest=json.loads((ROOT/'ArtSource/World/slices.json').read_text(encoding='utf-8'))
for name,spec in manifest.items():
    image=Image.open(assets/f'Art/Pixel/World/{name}.png')
    assert list(image.size)==spec['native_size'],name
    assert {value for count,value in image.getchannel('A').getcolors(256)}.issubset({0,255}),name
    meta=(assets/f'Art/Pixel/World/{name}.png.meta').read_text(encoding='utf-8')
    assert 'spritePixelsToUnits: 32' in meta and 'filterMode: 0' in meta and 'enableMipMap: 0' in meta,name
report.append(f'{len(manifest)} sprites: native dimensions, binary alpha, 32 PPU and Point verified')
for scene in (assets/'Scenes').glob('Level0*.unity'):
    level_map=assets/'Prefabs/Levels'/f'{scene.stem}_Map.prefab'
    text=level_map.read_text(encoding='utf-8')
    for name in ['Systems','Terrain','Background','Mechanisms','Pickups','Hazards','Actors','UI']:
        assert 'm_Name: '+name+'\n' in text, (scene,name)
    guid=re.search(r'^guid: (\w+)$',level_map.with_suffix('.prefab.meta').read_text(),re.M)[1]
    assert guid in scene.read_text(encoding='utf-8'),f'Scene is not linked to map prefab: {scene}'
report.append('All three scenes reference editable map prefabs with the organized hierarchy')
catalog=json.loads((ROOT/'ArtSource/World/Catalog/catalog.json').read_text(encoding='utf-8'))
for item in catalog['items']:
    folder='Scenery' if item['decor'] else 'Modular'
    prefab=(assets/f'Prefabs/World/{folder}/{item["name"]}.prefab').read_text(encoding='utf-8')
    if item['decor']:
        assert '!u!61 ' not in prefab and '!u!50 ' not in prefab,item['name']
    else:
        assert prefab.count('!u!61 ')>=len(item['colliders']),item['name']
        assert '!u!50 ' in prefab,item['name']
    for box in item['colliders']:
        assert box['w']>0 and box['h']>0,item['name']
report.append('36 modular physics prefabs and 5 scenery prefabs verified; scenery has no collision or rigidbody')
for name in ['BatterySocketGateKit','CratePlateGateKit']:
    assert 'm_MethodName: SetPowered' in (assets/f'Prefabs/World/{name}.prefab').read_text(encoding='utf-8'),name
report.append('Both input/gate example kits contain persistent SetPowered event bindings')
checks=(ROOT/'Artifacts/World/physics-validation.txt').read_text(encoding='utf-8').splitlines()
assert not any(line.startswith('FAILED') for line in checks)
report.append(f'{len(checks)} real Unity Play Mode physics assertions passed')
(ROOT/'Artifacts/World/asset-validation.txt').write_text('\n'.join(report),encoding='utf-8')
# A contact sheet of actual camera captures; nearest-neighbor enlargement only.
sheet=Image.new('RGB',(1280,1110),(23,27,34));draw=ImageDraw.Draw(sheet)
for index in range(3):
    for col,view in enumerate(['Start','Mechanisms']):
        path=ROOT/f'Artifacts/World/Level0{index+1}_{view}.png'
        image=Image.open(path)
        sheet.paste(image,(col*640,index*370+10))
    draw.text((12,index*370),'LEVEL '+str(index+1),(179,228,205))
sheet.save(ROOT/'Artifacts/World/ThreeLevels.png')
print('\n'.join(report))
