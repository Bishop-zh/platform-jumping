from pathlib import Path
import tempfile
from PIL import Image,ImageOps,ImageDraw,ImageFont,ImageFilter
import shutil,json,sys
from collections import deque
ROOT=Path(__file__).resolve().parents[1]
archive=ROOT/'ArtSource/Robot';archive.mkdir(parents=True,exist_ok=True)
out=ROOT/'Assets/Art/Pixel/Robot';out.mkdir(parents=True,exist_ok=True)
ids={'Charge':'cf2cdec2-8138-44dc-83e0-df50e770595c','Run':'c74906ea-2a46-4394-9378-85f8062abe87','Jump':'f1e78387-aa47-44f1-9d82-0b94140b33b8','Death':'518ef23f-e063-438e-8f5b-c2b13829406b','Idle':'16c29eeb-6a8c-495d-91eb-a0888733e371'}
robot_only='--robot-only' in sys.argv
report={'ppu':32,'canvas':[64,64],'mirrored_idle_indices':[12,14,16,22],'outline':'Restore one native pixel dark silhouette after opaque-background extraction; preserve authored alpha when available','frames':{}}
for state,key in ids.items():
    if robot_only and state=='Charge':report['frames'][state]=4;continue
    source=Path(tempfile.gettempdir())/f'codex-clipboard-{key}.png';saved=archive/f'{state}.png'
    if not saved.exists() and source.exists():shutil.copy2(source,saved)
    im=Image.open(saved).convert('RGBA');cols,rows=(4,1) if state=='Charge' else (6,4)
    # These new sheets use opaque black backgrounds. Remove only black connected
    # to the outside, preserving enclosed dark metal and screen pixels.
    # Genuine transparent sheets already distinguish black artwork from background.
    # Never remove dark pixels by RGB on those sheets (charger, for example).
    authored_alpha=im.getchannel('A').getextrema()[0]<128
    pixels=im.load();visited=set();queue=deque()
    for x in range(im.width):queue.append((x,0));queue.append((x,im.height-1))
    for y in range(im.height):queue.append((0,y));queue.append((im.width-1,y))
    while queue and not authored_alpha:
        x,y=queue.popleft()
        if (x,y) in visited:continue
        visited.add((x,y));r,g,b,a=pixels[x,y]
        if a>0 and max(r,g,b)>12:continue
        pixels[x,y]=(r,g,b,0)
        for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if 0<=nx<im.width and 0<=ny<im.height and (nx,ny) not in visited:queue.append((nx,ny))
    for i in range(cols*rows):
        x,y=i%cols,i//cols
        cell=im.crop((round(x*im.width/cols),round(y*im.height/rows),round((x+1)*im.width/cols),round((y+1)*im.height/rows)))
        if state=='Charge':
            box=cell.getchannel('A').point(lambda v:255 if v>90 else 0).getbbox();cell=cell.crop(box).resize((64,96),Image.Resampling.BOX)
        else:
            if state=='Idle' and i in report['mirrored_idle_indices']:cell=ImageOps.mirror(cell)
            if state=='Death':
                box=cell.getchannel('A').getbbox()
                if box is not None:
                    grounded=Image.new('RGBA',cell.size);grounded.alpha_composite(cell,(0,cell.height-box[3]-3));cell=grounded
            # One scale for all poses; keep source-cell positions and floor baseline.
            cell=cell.resize((56,56),Image.Resampling.BOX)
            canvas=Image.new('RGBA',(64,64));canvas.alpha_composite(cell,(4,0));cell=canvas
        cell.putalpha(cell.getchannel('A').point(lambda v:255 if v>=100 else 0))
        if state!='Charge' and not authored_alpha:
            # Background and outer ink share the same black in the source. Reconstruct
            # a consistent one-pixel silhouette at native resolution, not RGB keying.
            # Closing repairs small pinholes in black seams; dilation restores outer ink.
            mask=cell.getchannel('A').filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
            mask=mask.filter(ImageFilter.MaxFilter(3))
            ink=Image.new('RGBA',cell.size,(4,6,8,0));ink.putalpha(mask)
            ink.alpha_composite(cell);cell=ink
        cell.save(out/f'{state}{i:02}.png')
    report['frames'][state]=cols*rows
(archive/'frames.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
if robot_only:
    print('Prepared 96 robot frames with preserved native black outlines; charger, UI and meta unchanged')
    sys.exit(0)
ui=ROOT/'Assets/Art/UI/Gameplay';font=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',14)
for name,text in [('EnergyLabel','电量'),('ChargerLabel','充电器')]:
    im=Image.new('RGBA',(64,24));ImageDraw.Draw(im).text((0,-1),text,font=font,fill=(235,225,201,255));im=im.crop(im.getchannel('A').getbbox());im.putalpha(im.getchannel('A').point(lambda v:255 if v>120 else 0));im.save(ui/f'{name}.png')
print('Prepared 96 robot frames and 4 charger frames at 32 PPU')
im=Image.open(ui/'KeyC.png').convert('RGBA');w,h=im.size;draw=ImageDraw.Draw(im);draw.rectangle((w*.25,h*.22,w*.76,h*.79),fill=(27,34,41,255));keyfont=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',int(h*.55));draw.text((w*.5,h*.47),'E',font=keyfont,anchor='mm',fill=(235,225,201,255));im.save(ui/'KeyE.png')
