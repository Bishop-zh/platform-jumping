"""Slice supplied modal atlases; only extract labels/icons from previews when absent."""
from pathlib import Path
import tempfile
import json, shutil
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SOURCES = {
 'victory_preview':'f141bb7c-bb38-4183-89de-ba5004f5f148',
 'failure_preview':'f693e3e4-e0d5-4f43-b92f-7e7e08ba7e23',
 'headers':'6d2cc9eb-a885-4b7e-bfce-4f37626166c0',
 'buttons':'b1871906-504a-41f3-a407-2d1f7ea619a2',
 'victory_labels':'e260342a-41a9-404a-93a3-d7558d7bb903',
 'digits':'1a96fe8b-ae53-427a-b87b-2a106507e415',
 'panels_a':'7f912171-87f8-47af-9ed7-581b28fe2b1d',
 'panels_b':'8cf49830-e9a4-45fe-b482-1fc910b3d657',
 'settings':'39817610-07c9-4996-95ba-5a0d5a20998d',
 'settings_preview':'0146e86b-1cb6-4f7b-a00f-b6d5b0921a52',
}
DEST = ROOT/'Assets/Art/UI/Modal'
ARCHIVE = ROOT/'ArtSource/Modal'
DEST.mkdir(parents=True, exist_ok=True)
ARCHIVE.mkdir(parents=True, exist_ok=True)
manifest = {}
images = {}
for name, key in SOURCES.items():
    source = Path(tempfile.gettempdir())/('codex-clipboard-'+key+'.png')
    archive = ARCHIVE/(name+'.png')
    if source.exists(): shutil.copy2(source, archive)
    images[name] = Image.open(archive).convert('RGBA')
    print(name, images[name].size, images[name].getchannel('A').getextrema())

def cut(name, source, region, extract=None):
    image = images[source].crop(region)
    if extract:
        array = np.asarray(image)
        rgb = array[:,:,:3].astype(float)
        if extract == 'cream': mask = (rgb[:,:,0]>175)&(rgb[:,:,1]>150)&(rgb[:,:,2]>100)&(rgb[:,:,0]>rgb[:,:,2])
        elif extract == 'cyan': mask = (rgb[:,:,1]>150)&(rgb[:,:,2]>150)&(rgb[:,:,0]<130)
        elif extract == 'red': mask = (rgb[:,:,0]>140)&(rgb[:,:,0]>rgb[:,:,1]*1.7)&(rgb[:,:,0]>rgb[:,:,2]*1.5)
        else: mask = (rgb.max(axis=2)>115)&(rgb.max(axis=2)-rgb.min(axis=2)<65)
        alpha = Image.fromarray((mask*255).astype('uint8')).filter(ImageFilter.MaxFilter(3))
        image.putalpha(alpha)
    bounds = image.getchannel('A').point(lambda v: 255 if v>=32 else 0).getbbox()
    if not bounds: raise ValueError((name,region))
    image = image.crop(bounds)
    image.save(DEST/(name+'.png'))
    manifest[name] = dict(source=source, region=list(region), tight=list(bounds), size=list(image.size))

if __name__ == '__main__':
    # One common blank chassis and one common blank hanging header for all three modes.
    # The common header already owns the top lugs: do not duplicate them on the body.
    cut('CommonBody','panels_b',(0,170,855,570))
    cut('CommonHeader','headers',(0,80,1448,400))
    cut('InnerPanel','panels_a',(850,360,1448,680))
    cut('InfoStrip','panels_a',(850,680,1448,800))
    cut('ButtonSelected','buttons',(20,300,740,550))
    cut('ButtonNormal','buttons',(740,300,1420,550))
    cut('Rail','victory_preview',(484,0,518,138))
    cut('VictoryIcon','victory_labels',(745,885,895,1035))
    cut('VictoryTitle','victory_labels',(245,135,1210,405))
    cut('NextLabel','panels_b',(125,975,330,1070))
    cut('ReplayLabel','victory_labels',(495,475,940,625))
    cut('LevelsLabel','victory_labels',(975,475,1410,625))
    cut('CollectIcon','victory_labels',(25,690,170,845))
    cut('TimeIcon','victory_labels',(490,690,650,845))
    cut('DeathIcon','victory_labels',(975,690,1125,845))
    cut('CollectLabel','victory_labels',(165,700,485,845))
    cut('TimeLabel','victory_labels',(645,700,950,845))
    cut('DeathLabel','victory_labels',(1115,700,1430,845))
    cut('UnlockedLabel','victory_labels',(35,890,715,1030))
    cut('FailureTitle','failure_preview',(723,173,1064,255),'cream')
    cut('FailureIcon','failure_preview',(613,167,715,265),'red')
    cut('FailureSubtitle','failure_preview',(697,266,982,305),'gray')
    cut('RetryLabel','failure_preview',(740,349,955,405),'cyan')
    cut('RestartLabel','failure_preview',(715,447,965,507),'cream')
    cut('MainMenuLabel','failure_preview',(712,647,964,706),'cream')
    cut('SettingsTitle','settings',(1220,75,1430,190))
    cut('MusicLabel','settings',(775,225,1000,310))
    cut('SfxLabel','settings',(775,330,1000,420))
    cut('FullscreenLabel','settings',(775,455,1000,545))
    cut('Track','settings',(1020,238,1350,301))
    cut('Fill','settings',(1020,300,1280,351))
    cut('Handle','settings',(1350,280,1448,360))
    cut('Checkbox','settings',(1015,485,1125,590))
    cut('Checkmark','settings',(1125,490,1225,585))
    cut('ControlsTitle','settings',(870,620,1070,690),'cream')
    cut('MoveHint','settings',(1145,675,1448,740))
    cut('JumpHint','settings',(1145,735,1340,800))
    cut('DashHint','settings',(1145,790,1365,850))
    cut('PauseHint','settings',(1145,842,1340,905))
    cut('DefaultsLabel','settings',(920,925,1135,1015))
    cut('BackLabel','settings',(1180,925,1310,1015))
    # Numeric font is genuinely dynamic; glyphs are reusable sprites, not baked sample values.
    im = images['digits']
    sx, sy = im.width/1280, im.height/1280
    for i in range(10):
        col,row=i%5,i//5
        region=(int((col*240+45)*sx),int((125+row*320)*sy),int((col*240+270)*sx),int((385+row*320)*sy))
        if col==4: region=(int(1000*sx),region[1],int(1220*sx),region[3])
        cut('Digit'+str(i),'digits',region)
    cut('Colon','digits',(int(60*sx),int(760*sy),int(195*sx),int(990*sy)))
    cut('Slash','digits',(int(260*sx),int(755*sy),int(420*sx),int(990*sy)))
    cut('Percent','settings_preview',(1124,240,1157,276),'gray')
    cut('Minus','digits',(int(435*sx),int(830*sy),int(640*sx),int(940*sy)))
    (ARCHIVE/'slices.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Prepared',len(manifest),'modal sprites')
