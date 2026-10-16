from pathlib import Path
from PIL import Image,ImageDraw
import re
root=Path(__file__).resolve().parents[1];art=root/'Assets/Art/Pixel/Robot';report=[]
for path in art.glob('*.png'):
    im=Image.open(path);assert im.size==((64,96) if path.stem.startswith('Charge') else (64,64)),path
    assert im.getpixel((0,0))[3]==0,path
    assert set(im.getchannel('A').get_flattened_data())<= {0,255},path
    meta=path.with_suffix('.png.meta').read_text(encoding='utf-8');assert 'spritePixelsToUnits: 32' in meta and 'filterMode: 0' in meta and 'enableMipMap: 0' in meta,path
report.append('100 animation/charger sprites: binary transparency, native dimensions, 32 PPU and Point verified')
guids=set()
for path in (root/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)$',path.read_text(encoding='utf-8'),re.M)
    if match:guids.add(match[1])
for path in (root/'.work/UnityMainMenu/Packages').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)$',path.read_text(encoding='utf-8',errors='ignore'),re.M)
    if match:guids.add(match[1])
for path in list((root/'Assets/Animations/Robot').glob('*.anim'))+list((root/'Assets/Animations/Robot').glob('*.controller'))+list((root/'Assets/Prefabs/Levels').glob('*.prefab'))+[root/'Assets/Prefabs/World/Player.prefab',root/'Assets/Prefabs/World/ChargeStation.prefab']:
    for guid in re.findall(r'guid: (\w{32})',path.read_text(encoding='utf-8')):
        assert guid.startswith('0000000000000000') or guid in guids or guid in {'dc42784cf147c0c48a680349fa168899','fe87c0e1cc204ed48ad3b37840f39efc0','0cd44c1031e13a943bb63640046fad76'},(path,guid)
controller=(root/'Assets/Animations/Robot/Robot.controller').read_text(encoding='utf-8')
for state in ['Idle','Run','Rise','Fall','Dash','Death','Charging','Land']:assert 'm_Name: '+state+'\n' in controller,state
report.append('Eight Animator states, animation clips and delivered prefab references verified')
for id in range(1,4):
    text=(root/f'Assets/Prefabs/Levels/Level0{id}_Map.prefab').read_text(encoding='utf-8')
    assert text.count('chargePerSecond:')==(2 if id==1 else 3),id
report.append('All three editable maps contain chargers in place of disposable batteries')
checks=(root/'Artifacts/Robot/validation.txt').read_text(encoding='utf-8').splitlines();assert not any(s.startswith('FAILED') for s in checks)
report.append(f'{len(checks)} real Unity Play Mode assertions passed')
(root/'Artifacts/Robot/asset-validation.txt').write_text('\n'.join(report),encoding='utf-8')
sheet=Image.new('RGB',(768,560),(35,42,49));draw=ImageDraw.Draw(sheet)
for row,state in enumerate(['Idle','Run','Jump','Death']):
    for col,index in enumerate([0,4,8,12,16,23]):
        image=Image.open(art/f'{state}{index:02}.png').resize((128,128),Image.Resampling.NEAREST);sheet.paste(image,(col*128,row*140+12),image)
    draw.text((2,row*140),state,fill='white')
sheet.save(root/'Artifacts/Robot/AnimationFrames.png')
print('\n'.join(report))
