from pathlib import Path
import tempfile
from PIL import Image,ImageDraw,ImageFont
import shutil,json
ROOT=Path(__file__).resolve().parents[1];out=ROOT/'Assets/Art/Pixel/World';archive=ROOT/'ArtSource/World/Catalog';archive.mkdir(parents=True,exist_ok=True)
keys=['19a011cd-199e-45aa-9b48-a1136879e26b','e3f58349-8ac8-4e1b-a3f1-01965e7d35a7','6f46993d-953e-4c14-bec4-90cbf2968005','db2c98ad-6c9a-4157-a84c-7700e0534bd5','27b9d9c6-1b23-4158-aef1-ccdd1e41ac63']
sheets=[];items=[];manifest=json.loads((ROOT/'ArtSource/World/slices.json').read_text(encoding='utf-8'))
for i,key in enumerate(keys):
 saved=archive/(str(i)+'.png');src=Path(tempfile.gettempdir())/('codex-clipboard-'+key+'.png')
 if src.exists():shutil.copy2(src,saved)
 sheets.append(Image.open(saved).convert('RGBA'))
SURFACES={
 'GroundRaised':[(30,450,359),(450,603,286)],
 'GroundSteps':[(35,167,799),(167,316,828),(316,415,855)],
 'GroundLowStep':[(463,790,905),(790,920,800)],
 'BridgeBrokenLeft':[(39,237,419),(237,287,441),(287,333,464)],
 'BridgeBrokenRight':[(443,490,467),(490,552,439),(552,672,419)],
 'BridgeStairs':[(676,1090,1120),(1090,1133,1148),(1133,1170,1175),(1170,1203,1200),(1203,1228,1223)]}
INTERACTIONS={'CheckpointPlatform':.8,'BridgeCheckpointBase':2.05,'ExitPlatform':0,'TerminalPlatform':.9,'GroundExitBase':1.8}
def asset(name,sheet,rect,size,surface=None,steps=None,decor=False,front=False):
 source=sheets[sheet];rect=tuple(round(v*source.width/1280) for v in rect);im=source.crop(rect);box=im.getchannel('A').point(lambda v:255 if v>90 else 0).getbbox();im=im.crop(box)
 im=im.resize(size,Image.Resampling.BOX);im.putalpha(im.getchannel('A').point(lambda v:255 if v>=100 else 0));im.save(out/(name+'.png'))
 colliders=[]
 if surface is not None:
  y=(rect[1]+box[3]-surface*source.width/1280)/(box[3]-box[1])*size[1]/32
  colliders=[{'x':0,'y':y-.125,'w':size[0]/32,'h':.25}]
 if steps:colliders=steps
 if name in SURFACES:
  colliders=[];scale=source.width/1280;nativew=size[0]/32;nativeh=size[1]/32
  for a,b,top in SURFACES[name]:
   left=(a*scale-rect[0]-box[0])/(box[2]-box[0])*nativew-nativew/2
   right=(b*scale-rect[0]-box[0])/(box[2]-box[0])*nativew-nativew/2
   y=(rect[1]+box[3]-top*scale)/(box[3]-box[1])*nativeh
   colliders.append({'x':(left+right)/2,'y':y-.125,'w':right-left,'h':.25})
 items.append({'name':name,'width':size[0]/32,'height':size[1]/32,'decor':decor,'front':front,'interactionX':INTERACTIONS.get(name,0),'colliders':colliders})
 manifest[name]={'sheet':'Catalog/'+str(sheet),'native_size':list(size),'ppu':32,'source_rect':list(rect)}
for name,rect,size,y in [('GroundWide_A',(25,65,630,265),(192,64),111),('GroundWide_B',(650,65,1224,265),(192,64),111),('GroundRaised',(25,268,625,510),(192,80),359),('GroundPlateBase',(645,300,1232,512),(192,64),368),('GroundCantileverLeft',(25,519,429,747),(128,72),571),('GroundCenterPier',(471,519,834,750),(128,72),571),('GroundCantileverRight',(888,530,1241,750),(128,72),571),('GroundSteps',(24,757,425,1010),(128,80),819),('GroundLowStep',(447,762,934,1010),(160,80),905),('GroundPier',(1013,778,1225,977),(64,64),819),('GroundLong',(22,1046,740,1238),(224,64),1107),('GroundExitBase',(771,967,1234,1238),(160,96),1107)]:asset(name,0,rect,size,y)
for name,rect,size,y in [('BridgeLong_A',(30,10,1230,195),(320,48),68),('BridgeLong_B',(29,190,1230,368),(320,48),243),('BridgeBrokenLeft',(28,366,389,549),(96,48),419),('BridgeBrokenRight',(389,366,683,549),(96,48),419),('BridgeMovingDeck',(685,370,1235,535),(160,40),442),('BridgeTwinSupports',(28,551,551,825),(160,80),609),('BridgeSmall',(542,619,812,769),(80,40),681),('BridgeCenterSupport',(805,543,1244,831),(128,80),584),('BridgeCheckpointBase',(24,785,631,1058),(192,80),906),('BridgeCrateBase',(646,778,1238,1032),(192,80),920),('BridgeThin',(49,1055,608,1213),(160,40),1103),('BridgeStairs',(657,1038,1244,1234),(192,64),1120)]:asset(name,1,rect,size,y)
for name,rect,size,y in [('ColumnNarrow',(20,89,179,481),(32,128),116),('ColumnWide',(190,89,429,481),(64,128),116),('SupportArch',(422,103,899,484),(160,128),175),('TerminalPlatform',(884,104,1242,478),(128,128),350),('ColumnTall',(1058,487,1243,922),(48,144),550),('CheckpointPlatform',(385,520,663,873),(96,112),748),('ExitPlatform',(655,507,1080,873),(128,112),748),('CratePlatePlatform',(27,637,375,873),(128,80),748),('PierTall',(20,875,370,1225),(96,112),927),('PierDouble',(340,949,624,1225),(96,80),980),('PierSingle',(611,957,860,1225),(80,80),1005),('EnergyPlatform',(882,940,1240,1225),(128,96),1081)]:asset(name,2,rect,size,y)
for name,rect,size in [('NearScrapHeap',(0,0,880,558),(256,160)),('NearBrokenTank',(674,183,1251,447),(192,80)),('NearCrane',(15,569,465,1230),(128,192)),('NearCoreFactory',(505,570,1260,1230),(224,192))]:asset(name,3,rect,size,decor=True,front=True)
asset('FarCity',4,(0,0,1280,1280),(512,320),decor=True)
# Battery sprites share the HUD artwork, normalized to half a world unit in width.
im=Image.open(ROOT/'Assets/Art/UI/Gameplay/Battery.png').convert('RGBA').resize((16,24),Image.Resampling.BOX);im.putalpha(im.getchannel('A').point(lambda v:255 if v>=100 else 0));im.save(out/'BatteryWorld.png');manifest['BatteryWorld']={'native_size':[16,24],'ppu':32}
# Compact Chinese labels, retaining the existing image atlas for every numeral.
ui=ROOT/'Assets/Art/UI/Gameplay';font=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',14)
for name,text in [('BatteryLabel','电池'),('CrystalLabel','水晶'),('BagLabel','背包'),('MoveLabel','移动'),('JumpLabel','跳跃'),('InteractLabel','交互')]:
 im=Image.new('RGBA',(40,20));d=ImageDraw.Draw(im);d.text((0,-1),text,font=font,fill=(235,225,201,255));im=im.crop(im.getchannel('A').getbbox());im.putalpha(im.getchannel('A').point(lambda v:255 if v>120 else 0));im.save(ui/(name+'.png'))
(archive/'catalog.json').write_text(json.dumps({'items':items},ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'ArtSource/World/slices.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared',len(items),'modular collision/decor assets')
