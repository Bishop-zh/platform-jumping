"""Normalize supplied irregular atlases into exact native PPU=32 sprite canvases."""
from pathlib import Path
import tempfile
import json, shutil
from PIL import Image
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Pixel/World'
ARCHIVE=ROOT/'ArtSource/World'
OUT.mkdir(parents=True,exist_ok=True); ARCHIVE.mkdir(parents=True,exist_ok=True)
keys={'A':'b29bd0d1-9ba7-4a96-98c3-6f2a27b1be5d','B':'0309343a-ce79-44fd-a078-3a0f92811552',
      'C':'a923d11b-5977-4901-a6a9-b419eb5a8828','D':'fe65d6da-9078-4e37-b377-168a1991982d'}
sources={}; manifest={}
for name,key in keys.items():
    source=Path(tempfile.gettempdir())/('codex-clipboard-'+key+'.png')
    saved=ARCHIVE/(name+'.png')
    if source.exists(): shutil.copy2(source,saved)
    sources[name]=Image.open(saved).convert('RGBA')
    print(name,sources[name].size,sources[name].getchannel('A').getextrema())
palette=['171B22','272D35','3D4650','606B73','929D9F','C7CDC3','633B32','96533A','C17A43','6D6652','9B9472','C9BD91','D65B4C','D6B85A','4FB8B0','B3E4CD']
colors=np.array([[int(c[i:i+2],16) for i in (0,2,4)] for c in palette],dtype=np.int32)

def normalize(im,size,fit=False):
    bounds=im.getchannel('A').point(lambda v:255 if v>=96 else 0).getbbox()
    if not bounds: raise ValueError('Empty sprite')
    im=im.crop(bounds)
    if fit:
        factor=min(size[0]/im.width,size[1]/im.height)
        resized=im.resize((max(1,round(im.width*factor)),max(1,round(im.height*factor))),Image.Resampling.BOX)
        result=Image.new('RGBA',size)
        result.paste(resized,((size[0]-resized.width)//2,size[1]-resized.height))
        im=result
    else: im=im.resize(size,Image.Resampling.BOX)
    pixels=np.array(im)
    rgb=pixels[:,:,:3].astype(np.int32)
    distances=((rgb[:,:,None,:]-colors[None,None,:,:])**2).sum(axis=3)
    pixels[:,:,:3]=colors[distances.argmin(axis=2)]
    pixels[:,:,3]=np.where(pixels[:,:,3]>=96,255,0)
    return Image.fromarray(pixels)

def cut(name,sheet,region,size,fit=False):
    im=sources[sheet]; sx,sy=im.width/1280,im.height/1280
    rect=tuple(round(v*(sx if i%2==0 else sy)) for i,v in enumerate(region))
    native=normalize(im.crop(rect),size,fit)
    native.save(OUT/(name+'.png'))
    manifest[name]={'sheet':sheet,'source_rect':list(rect),'native_size':list(size),'ppu':32,'fit':fit}

xs=[(35,166),(179,315),(329,469),(484,624),(633,775),(785,924),(939,1077),(1088,1224)]
top=['GroundTopLeft','GroundTopA','GroundTopB','GroundTopC','GroundTopRight','GroundSingle','GroundFillA','GroundFillB']
for name,(left,right) in zip(top,xs): cut(name,'A',(left,40,right,180),(32,32))
for name,rect in zip(['GroundSideLeft','GroundSideRight','GroundBottomLeft','GroundBottomA','GroundBottomRight','GroundCornerInnerLeft','GroundCornerInnerRight','GroundFillC'],
 [(39,185,147,331),(200,185,306,331),(330,185,470,332),(483,242,624,332),(634,185,775,332),(785,211,924,333),(940,214,1080,334),(1088,185,1224,333)]): cut(name,'A',rect,(32,32))
for name,rect in zip(['PlatformLeft','PlatformA','PlatformB','PlatformC','PlatformRight','PlatformSingle'],
 [(16,55,170,152),(178,55,335,152),(343,55,501,152),(503,55,663,152),(668,55,810,153),(860,55,1074,153)]): cut(name,'B',rect,(32,16))
for name,rect in zip(['RailLeft','RailMiddle','RailRight'],[(18,185,170,258),(180,185,335,258),(345,185,502,258)]): cut(name,'B',rect,(32,16))
cut('SupportCap','B',(20,300,169,460),(32,32))
cut('SupportMiddle','B',(194,300,314,698),(32,96))
cut('SupportBase','B',(333,300,504,459),(32,32))
cut('MovingPlatform','B',(10,890,614,1033),(80,16))
for name,rect in zip(['CableHorizontal','CableVertical','CableCornerSW','CableCornerSE','CableCornerNE','CableCornerNW','CableTerminalOff','CableTerminalOn'],
 [(13,722,160,796),(196,701,247,839),(350,709,450,825),(510,709,605,825),(645,716,744,825),(798,716,903,825),(970,716,1051,795),(1149,715,1234,795)]): cut(name,'B',rect,(16,16))
for name,rect,size in [
 ('CrateA',(21,132,160,265),(32,32)),('CrateB',(177,132,317,265),(32,32)),
 ('WeightPlateOff',(330,185,480,263),(32,16)),('WeightPlateOn',(490,185,638,263),(32,16)),
 ('LeverOff',(630,130,790,265),(32,32)),('LeverOn',(790,130,937,265),(32,32)),
 ('WallButtonOff',(950,138,1085,255),(32,32)),('WallButtonOn',(1110,138,1245,255),(32,32)),
 ('GateClosed',(17,300,166,700),(32,96)),('GateOpen',(174,300,351,700),(32,96)),
 ('CheckpointOff',(348,396,497,700),(32,64)),('CheckpointOn',(498,396,638,700),(32,64)),
 ('ExitOff',(650,305,944,700),(64,96)),('ExitOn',(960,305,1246,700),(64,96))]: cut(name,'C',rect,size)
for i in range(4): cut('Fragment'+str(i),'D',(35+i*156,30,123+i*156,145),(16,16),True)
for name,rect in [('Shield0',(667,27,748,147)),('Shield1',(823,27,905,147)),('JumpBoost0',(965,27,1075,147)),('JumpBoost1',(1120,27,1230,147))]: cut(name,'D',rect,(16,16),True)
for name,rect in zip(['SpikeLeft','SpikeMiddle','SpikeRight'],[(1,193,159,286),(154,193,312,286),(309,193,467,286)]): cut(name,'D',rect,(32,16))
cut('HazardBarrel','D',(475,170,624,451),(32,64))
for i in range(4): cut('Patrol'+str(i),'D',(638+i*156,191,773+i*156,349),(32,32),True)
for i,rect in enumerate([(1,503,132,604),(148,503,308,604),(310,503,481,604),(487,503,619,604)]): cut('DashFX'+str(i),'D',rect,(32,32),True)
# World stand-in uses supplied home robot, normalized to the agreed fixed world canvas.
for i,frame in enumerate([0,2]):
    im=Image.open(ROOT/f'Assets/Art/UI/MainMenu/Robot_{frame}.png').convert('RGBA')
    im=normalize(im,(28,42),True)
    canvas=Image.new('RGBA',(32,48)); canvas.paste(im,(2,6))
    name='Player'+str(i); canvas.save(OUT/(name+'.png'))
    manifest[name]={'sheet':'existing supplied menu robot','native_size':[32,48],'ppu':32,'temporary_standing_pose':True}
# Clean common surface strip and joins, without changing internal rust variants.
reference=Image.open(OUT/'GroundTopA.png')
for name in ['GroundTopA','GroundTopB','GroundTopC']:
    im=Image.open(OUT/(name+'.png')); im.paste(reference.crop((0,0,32,2)),(0,0))
    im.paste(reference.crop((0,0,2,32)),(0,0)); im.paste(reference.crop((30,0,32,32)),(30,0))
    im.save(OUT/(name+'.png'))
for width in [8,16,24]:
    for name in ['GroundTopA','GroundFillA','PlatformA']:
        im=Image.open(OUT/(name+'.png')).crop((0,0,width,32 if name.startswith('Ground') else 16))
        short=name+'_'+str(width); im.save(OUT/(short+'.png'))
        manifest[short]={'derived_from':name,'native_size':list(im.size),'ppu':32}
backdrop=Image.open(ROOT/'Assets/Art/UI/MainMenu/Background.png').convert('RGBA')
backdrop=normalize(backdrop,(640,360))
backdrop.save(OUT/'Backdrop.png')
manifest['Backdrop']={'derived_from':'supplied menu background','native_size':[640,360],'ppu':32}
(ARCHIVE/'slices.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared',len(manifest),'native world sprites; all alpha binary and PPU 32')
