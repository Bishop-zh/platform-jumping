from pathlib import Path
import tempfile
import shutil,json
from PIL import Image,ImageEnhance
ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'Assets/Art/Pixel/World';ui=ROOT/'Assets/Art/UI/Gameplay';ui.mkdir(parents=True,exist_ok=True)
archive=ROOT/'ArtSource/World/Revision2';archive.mkdir(parents=True,exist_ok=True)
ids={'HUD':'4c07f9f0-4802-40ed-b537-3e2765290820','Terrain':'12709316-7cd2-4734-ab32-86f8ad988266','Props':'b37b313e-b3d6-4813-ba17-69c3c1375ef2'}
sheets={}
manifest=json.loads((ROOT/'ArtSource/World/slices.json').read_text(encoding='utf-8'))
def crop(sheet,rect):
 im=sheets[sheet];r=tuple(round(v*im.width/1280) for v in rect);im=im.crop(r);box=im.getchannel('A').point(lambda v:255 if v>80 else 0).getbbox();return im.crop(box)
def world(name,sheet,rect,size):
 im=crop(sheet,rect).resize(size,Image.Resampling.BOX)
 im.putalpha(im.getchannel('A').point(lambda v:255 if v>=100 else 0));im.save(out/(name+'.png'))
 manifest[name]={'revision':2,'sheet':sheet,'source_rect_1280':list(rect),'native_size':list(size),'ppu':32}
for name,key in ids.items():
 target=archive/(name+'.png');source=Path(tempfile.gettempdir())/('codex-clipboard-'+key+'.png')
 if source.exists():shutil.copy2(source,target)
 sheets[name]=Image.open(target).convert('RGBA');print(name,sheets[name].size)
for i,rect in enumerate([(43,77,263,306),(267,77,501,306),(505,77,738,306),(735,77,983,310),(988,77,1205,310)]):world('GroundModule'+str(i),'Terrain',rect,(64,64))
for name,rect,size in [('PlatformLeft',(48,518,392,645),(64,16)),('PlatformA',(390,518,698,645),(64,16)),('PlatformRight',(698,518,983,645),(64,16)),('MovingPlatform',(642,665,970,775),(80,16)),('SupportMiddle',(140,887,267,1080),(16,32)),('SupportBase',(80,1077,312,1189),(24,16))]:world(name,'Terrain',rect,size)
for name,rect,size in [('CrateA',(37,198,281,416),(32,32)),('CrateB',(297,246,490,416),(32,32)),('WeightPlateOff',(520,336,696,417),(32,12)),('WeightPlateOn',(717,336,910,417),(32,12)),('WallButtonOff',(47,625,219,789),(32,32)),('WallButtonOn',(254,625,420,789),(32,32)),('CheckpointOff',(923,202,1068,416),(32,64)),('CheckpointOn',(1086,202,1235,416),(32,64)),('ExitOff',(780,471,996,818),(64,96)),('ExitOn',(1010,451,1233,818),(64,96)),('Player0',(116,817,311,1060),(32,48)),('Patrol0',(380,818,532,1062),(32,32)),('SpikeLeft',(43,1072,262,1220),(32,16)),('SpikeMiddle',(269,1090,481,1220),(32,16)),('SpikeRight',(481,1072,701,1220),(32,16)),('Fragment0',(1146,1090,1227,1204),(16,16))]:world(name,'Props',rect,size)
# Reuse the original timing, with the revised character/collectible shapes.
for n in ['Player1']:
 shutil.copy2(out/'Player0.png',out/(n+'.png'));manifest[n]=dict(manifest['Player0'])
for n in ['Patrol1','Patrol2','Patrol3']:
 shutil.copy2(out/'Patrol0.png',out/(n+'.png'));manifest[n]=dict(manifest['Patrol0'])
for n in ['Fragment1','Fragment2','Fragment3']:
 shutil.copy2(out/'Fragment0.png',out/(n+'.png'));manifest[n]=dict(manifest['Fragment0'])
for name,rect in {'CollectFrame':(24,103,626,292),'LevelFrame':(619,103,1236,292),'TimeFrame':(23,319,478,475),'SettingsButton':(489,319,634,475),'PauseButton':(639,319,789,475),'Battery':(892,316,978,475),'Clock':(997,335,1116,472),'KeyA':(53,707,203,854),'KeyD':(213,707,366,854),'KeySpace':(373,698,707,854),'KeyC':(714,698,868,854),'Multiply':(56,1050,171,1170)}.items():
 crop('HUD',rect).save(ui/(name+'.png'))
# The new atlas digits are intentionally unused: retain the project's shared numeral atlas.
reference=Image.open((Path(tempfile.gettempdir()) / 'codex-clipboard-87daa104-feb2-4623-8eda-1cc81abb26ea.png')).convert('RGBA')
for name,rect in {'Name01':(432,43,572,76),'Name02':(432,43,572,76)}.items():reference.crop(rect).save(ui/(name+'.png'))
# Richer original skyline, without the old gray multiply and severe 16-color reduction.
bg=Image.open(ROOT/'Assets/Art/UI/MainMenu/Background.png').convert('RGBA').resize((640,360),Image.Resampling.BOX)
bg=ImageEnhance.Color(bg).enhance(1.18);bg=ImageEnhance.Contrast(bg).enhance(1.12);bg.putalpha(bg.getchannel('A').point(lambda v:255 if v>=96 else 0));bg.save(out/'Backdrop.png')
(ROOT/'ArtSource/World/slices.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Revision art prepared')
for stem in ['GroundModule0','GroundModule1','GroundModule2','GroundModule3','GroundModule4','PlatformLeft','PlatformA','PlatformRight','SpikeLeft','SpikeMiddle','SpikeRight']:
 im=Image.open(out/(stem+'.png'))
 for width in [8,16,24,32,40,48,56]:
  if width>=im.width:continue
  name=stem+'_'+str(width);im.crop((0,0,width,im.height)).save(out/(name+'.png'));manifest[name]={'derived_from':stem,'native_size':[width,im.height],'ppu':32}
column=Image.open(out/'SupportMiddle.png')
for height in [32,40,96]:
 im=Image.new('RGBA',(16,height))
 shaft=column.crop((0,4,16,28))
 for y in range(4,height-4,24):im.paste(shaft.crop((0,0,16,min(24,height-4-y))),(0,y))
 im.paste(column.crop((0,0,16,4)),(0,0));im.paste(column.crop((0,28,16,32)),(0,height-4))
 name='SupportHeight_'+str(height);im.save(out/(name+'.png'));manifest[name]={'derived_from':'SupportMiddle','native_size':[16,height],'ppu':32}
colon=Image.open(ROOT/'Assets/Art/UI/Modal/Colon.png').convert('RGBA');dot=colon.crop((0,colon.height//2,colon.width,colon.height));dot=dot.crop(dot.getchannel('A').getbbox());dot.save(ui/'Dot.png')
(ROOT/'ArtSource/World/slices.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
