"""Check transferred Unity references, screenshots, and validation evidence."""
from pathlib import Path
import re
from PIL import Image, ImageChops

root=Path(__file__).resolve().parents[1]
guids=set()
for folder in [root/'Assets',root/'Library/PackageCache/com.unity.ugui@1.0.0']:
 for file in folder.rglob('*.meta'):
  match=re.search(r'^guid: ([0-9a-f]{32})',file.read_text(encoding='utf-8'),re.M)
  if match: guids.add(match.group(1))
for relative in ['Assets/Prefabs/UI/LevelSelect.prefab','Assets/Prefabs/UI/SharedModal.prefab',
 'Assets/Scenes/MainMenu.unity','Assets/Scenes/Preview/LevelSelectPreview.unity']:
 text=(root/relative).read_text(encoding='utf-8')
 refs=set(re.findall(r'guid: ([0-9a-f]{32})',text))
 missing={guid for guid in refs if guid not in guids and not guid.startswith('0000000000000000')}
 assert not missing,(relative,missing)
 print('PASS references:',relative)
shots=[Image.open(root/'Artifacts/LevelSelect'/name).convert('RGB')
 for name in ['initial.png','level2_unlocked.png','level3_unlocked.png']]
for shot in shots: assert shot.size==(1672,941)
for region in [(0,0,1672,165),(0,800,1672,941),(0,165,130,800),(1545,165,1672,800)]:
 for shot in shots[1:]:
  assert ImageChops.difference(shots[0].crop(region),shot.crop(region)).getbbox() is None,region
print('PASS stable background, title and navigation across unlock states')
report=(root/'Artifacts/LevelSelect/validation.txt').read_text(encoding='utf-8')
assert report.startswith('PASS')
print(report.strip())
