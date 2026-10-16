"""Check transferred Unity references and the two actual offscreen captures."""
from pathlib import Path
import re
from PIL import Image, ImageChops

root = Path(__file__).resolve().parents[1]
guids = set()
for directory in [root / 'Assets', root / 'Library/PackageCache/com.unity.ugui@1.0.0']:
    for meta in directory.rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if match:
            guids.add(match.group(1))

assets = ['Assets/Prefabs/UI/MainMenu.prefab', 'Assets/Scenes/MainMenu.unity',
          'Assets/Scenes/Preview/MainMenuPreview.unity']
for relative in assets:
    file = root / relative
    assert file.exists(), relative
    refs = set(re.findall(r'guid: ([0-9a-f]{32})', file.read_text(encoding='utf-8')))
    missing = {guid for guid in refs if guid not in guids and not guid.startswith('0000000000000000')}
    assert not missing, (relative, missing)
    print('PASS references:', relative)

captures = root / 'Artifacts/MainMenu'
a = Image.open(captures / 'main_menu_frame_0.png').convert('RGB')
b = Image.open(captures / 'main_menu_frame_3.png').convert('RGB')
assert a.size == b.size == (1672, 941)
box = ImageChops.difference(a, b).getbbox()
assert box and box[0] >= 1200 and box[1] >= 400, box
print('PASS rendered animation difference:', box)
assert 'MAIN_MENU_CAPTURE_COMPLETE' in (captures / 'unity-batch.log').read_text(encoding='utf-8')
print((captures / 'validation.txt').read_text(encoding='utf-8').strip())
