"""Slice supplied selection atlases without baking UI numbers into artwork."""
from pathlib import Path
import json, shutil
import numpy as np
from PIL import Image, ImageFilter

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/Art/UI/LevelSelect'
ARCHIVE=ROOT/'ArtSource/LevelSelect'
DEST.mkdir(parents=True,exist_ok=True)
ARCHIVE.mkdir(parents=True,exist_ok=True)
sources={
 'preview':'1f42b0f0-3c3c-4278-9b77-47e61182fd4a',
 'labels':'35770896-e8d4-4068-b999-d74862b5a858',
 'background':'43bf735a-d04f-447b-ac7b-cf81b0b52eb5',
 'foreground':'3668856a-b7f7-4db3-8c68-cf113a4b378a',
 'decorations':'cdadf902-354b-4903-a2c6-8f2d93208e3f',
 'header':'9a7845f2-58b4-4b44-afb4-7e01ea9fd881',
 'selected':'9a8aba2a-b5a6-478b-9c85-fda4d6c60197',
 'locked2':'87706cd2-00be-4721-95cf-9bb5a89ec501',
 'locked3':'1ea09416-29db-4e25-9916-80f7ac441628',
 'blank':'72e01573-750e-4903-9d77-c7802562ac53',
}
images={}
manifest={}
for name,key in sources.items():
 source=Path('C:/Users/USER/AppData/Local/Temp')/('codex-clipboard-'+key+'.png')
 saved=ARCHIVE/(name+'.png')
 if source.exists(): shutil.copy2(source,saved)
 images[name]=Image.open(saved).convert('RGBA')
 print(name,images[name].size)

def cut(name,source,region=None,cream=False,tight=True):
 image=images[source].copy() if region is None else images[source].crop(region)
 if cream:
  rgb=np.asarray(image)[:,:,:3].astype(float)
  mask=(rgb[:,:,0]>175)&(rgb[:,:,1]>150)&(rgb[:,:,2]>100)&(rgb[:,:,0]>rgb[:,:,2])
  image.putalpha(Image.fromarray((mask*255).astype('uint8')).filter(ImageFilter.MaxFilter(3)))
 if tight:
  box=image.getchannel('A').point(lambda v:255 if v>=32 else 0).getbbox()
  image=image.crop(box)
 image.save(DEST/(name+'.png'))
 manifest[name]={'source':source,'region':region,'size':list(image.size)}

cut('Background','background',tight=False)
cut('Foreground','foreground',tight=False)
cut('Header','header')
cut('CardUnlocked','blank')
# Reuse the blank card's shell; replace only its built-in lower plaques with its own blank metal.
# This prevents duplicate plaques in the two locked atlases from colliding with live UI labels.
locked=images['blank'].copy()
metal=locked.crop((400,790,800,850)).resize((845,460),Image.Resampling.NEAREST)
locked.paste(metal,(150,790))
images['clean_locked']=locked
cut('CardLocked','clean_locked')
cut('LockIcon','locked3',(510,765,645,905))
cut('LockedStatusPlate','locked3',(210,915,935,1047))
cut('Divider','decorations',(770,520,1510,570))
cut('CardLocked2','locked2')
cut('CardLocked3','locked3')
cut('Thumbnail1','selected',(142,300,1001,767),tight=False)
cut('Thumbnail2','locked2',(140,247,1008,747),tight=False)
cut('Thumbnail3','locked3',(145,275,1005,747),tight=False)
cut('SelectionFrame','decorations',(0,50,760,975))
cut('SelectionArrow','decorations',(1060,140,1220,260))
cut('EnterButton','labels',(820,110,1448,315))
# Back arrow only, matching the compact preview rather than the large worded atlas button.
cut('BackButton','preview',(31,22,155,110),tight=False)
for name,region in {
 'LevelName1':(100,525,445,610),'LevelName2':(565,525,915,610),'LevelName3':(1040,525,1390,610),
 'AvailableLabel':(575,360,800,440),'LockedLabel':(170,665,350,740),
 'UnlockHint':(100,795,435,870),'BestTimeLabel':(65,935,260,995),'BestCollectLabel':(310,935,490,995),
}.items():
 if name=='AvailableLabel' or name=='LockedLabel':
  # Extract dark/coral characters only; no second plaque on top of the card's built-in plaque.
  image=images['labels'].crop(region)
  rgb=np.asarray(image)[:,:,:3].astype(float)
  if name=='AvailableLabel': mask=(rgb.max(axis=2)<65)&(np.asarray(image)[:,:,3]>200)
  else: mask=(rgb[:,:,0]>170)&(rgb[:,:,0]>rgb[:,:,1]*1.8)&(rgb[:,:,0]>rgb[:,:,2]*1.8)
  image.putalpha(Image.fromarray((mask*255).astype('uint8')))
  image=image.crop(image.getchannel('A').getbbox())
  image.save(DEST/(name+'.png'))
  manifest[name]={'source':'labels','region':region,'size':list(image.size)}
 else: cut(name,'labels',region,cream=True)
(ARCHIVE/'slices.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared',len(manifest),'level selection sprites')
