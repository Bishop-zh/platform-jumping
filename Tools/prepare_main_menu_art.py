"""Deterministically slice the supplied menu atlases; preserve the originals."""
from pathlib import Path
import json
import shutil
from PIL import Image
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('C:/Users/USER/AppData/Local/Temp')
FILES = {
    'preview': '3b7ad84d-aa88-4ef3-9374-0c56cd918a8f',
    'title': 'd21632e6-c004-4ad7-9bd0-6e2c31084e57',
    'buttons': '37c1fb0e-2a07-45cb-a93b-e33200f68810',
    'labels': '5502e851-8d77-4cf7-827e-0c46b2ffafeb',
    'background': '7713f1a6-46c5-4a8c-a489-28d329ac8012',
    'foreground': 'a37cfe26-f71b-4463-9b7e-d32894d7bee7',
    'station': '175a21a3-acff-4e11-a9d7-8091f0d04a0e',
    'robot': '9d78be24-ea6c-41ca-b8fd-a37923ad7910',
}
DEST = ROOT / 'Assets/Art/UI/MainMenu'
ORIGINALS = ROOT / 'ArtSource/MainMenu'
DEST.mkdir(parents=True, exist_ok=True)
ORIGINALS.mkdir(parents=True, exist_ok=True)
manifest = {}

def load(name):
    path = SOURCE / ('codex-clipboard-' + FILES[name] + '.png')
    saved = ORIGINALS / (name + '.png')
    if path.exists():
        shutil.copy2(path, saved)
    return Image.open(saved).convert('RGBA')

def save(name, image, crop=None, source=None):
    if crop is not None:
        image = image.crop(crop)
    image.save(DEST / (name + '.png'))
    manifest[name] = dict(size=list(image.size), source=source, crop=crop)

def tight(image):
    # Ignore isolated almost-transparent pixels while retaining the actual art.
    alpha = image.getchannel('A').point(lambda x: 255 if x >= 32 else 0)
    return alpha.getbbox()

def bands(image):
    alpha = np.asarray(image.getchannel('A'))
    rows = np.where((alpha >= 32).sum(axis=1) > 10)[0]
    runs = []
    for y in rows:
        if not runs or y > runs[-1][1] + 1:
            runs.append([int(y), int(y)])
        else:
            runs[-1][1] = int(y)
    return runs

if __name__ == '__main__':
    images = {key: load(key) for key in FILES}
    for name, im in images.items():
        print(name, im.size, tight(im), bands(im))
    save('Title', images['title'], tight(images['title']), 'title')
    save('Background', images['background'], source='background')
    save('Foreground', images['foreground'], source='foreground')
    # Fade the transparent title area without a hard rectangular panel edge.
    shade = Image.new('RGBA', (1672, 1))
    for x in range(1672):
        amount = max(0.0, min(1.0, (880 - x) / 440))
        shade.putpixel((x, 0), (21, 31, 40, round(235 * amount)))
    save('LeftShadeMask', shade, source='procedural transparency matte')
    im = images['buttons']
    for name, region in [('ButtonSelected', (90,220,1220,520)),
                         ('ButtonNormal', (90,570,1220,875)),
                         ('SelectionArrow', (1260,460,1400,620))]:
        section = im.crop(region)
        box = tight(section)
        absolute = (region[0]+box[0], region[1]+box[1], region[0]+box[2], region[1]+box[3])
        save(name, im, absolute, 'buttons')
    im = images['labels']
    for name, region in [('LabelStart',(280,70,1160,310)),
                         ('LabelLevels',(360,330,1100,540)),
                         ('LabelSettings',(530,555,925,765)),
                         ('LabelQuit',(360,765,1100,950)),
                         ('ControlHint',(90,950,1360,1080))]:
        section = im.crop(region)
        box = tight(section)
        absolute = (region[0]+box[0], region[1]+box[1], region[0]+box[2], region[1]+box[3])
        save(name, im, absolute, 'labels')
    normal_label = Image.open(DEST / 'LabelStart.png').convert('RGBA')
    pixels = np.asarray(normal_label).copy()
    cyan = (pixels[:,:,1] > pixels[:,:,0] * 1.35) & (pixels[:,:,2] > pixels[:,:,0] * 1.35) & (pixels[:,:,3] > 0)
    bright = np.max(pixels[:,:,:3], axis=2) > 110
    cyan &= bright
    intensity = np.max(pixels[:,:,:3], axis=2).astype(float) / 255
    pixels[cyan, :3] = (intensity[cyan, None] * np.array([211, 212, 197])).astype('uint8')
    save('LabelStartNormal', Image.fromarray(pixels), source='LabelStart state color normalization')
    for source, prefix in [('station','Station'),('robot','Robot')]:
        im = images[source]
        assert im.width % 4 == 0
        cell_width = im.width // 4
        boxes = [tight(im.crop((i*cell_width,0,(i+1)*cell_width,im.height))) for i in range(4)]
        # One common crop across frames: stable pivot, no frame-by-frame resizing.
        union = (min(b[0] for b in boxes), min(b[1] for b in boxes),
                 max(b[2] for b in boxes), max(b[3] for b in boxes))
        for i in range(4):
            box = (i*cell_width+union[0], union[1], i*cell_width+union[2], union[3])
            save(prefix + '_' + str(i), im, box, source)
        print(source, 'frame boxes', boxes, 'common crop', union)
    (ORIGINALS / 'slices.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    print('Prepared', len(manifest), 'sprites')
