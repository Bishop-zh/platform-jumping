"""Render a coordinate schematic, not game art or a gameplay screenshot."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'Artifacts/LevelDesign'
out.mkdir(parents=True,exist_ok=True)
image=Image.new('RGB',(1360,825),'#171b22')
draw=ImageDraw.Draw(image)
fontpath='C:/Windows/Fonts/msyh.ttc'
font=ImageFont.truetype(fontpath,17)
small=ImageFont.truetype(fontpath,13)
titlefont=ImageFont.truetype(fontpath,25)
draw.text((55,20),'《锈境：能源重启》三关灰盒布局设计',font=titlefont,fill='#e6ddc1')
draw.text((55,61),'同一坐标比例：1单位=32示意像素；方块与线条仅表示功能，不是生成素材或已搭建场景。',font=font,fill='#929d9f')
scale=32
def px(x): return 80+x*scale
def label(x,y,text,color='#c7cdc3',center=True):
    draw.text((px(x),y),text,font=small,fill=color,anchor='mt' if center else 'lt')
def ground(a,b,height,base):
    y=base-height*scale
    draw.rectangle((px(a),y,px(b),base+44),fill='#3d4650')
    draw.rectangle((px(a),y,px(b),y+4),fill='#c17a43')
def box(x,height,base):
    y=base-height*scale
    draw.rectangle((px(x),y-32,px(x+1),y),fill='#96533a',outline='#d6b85a',width=2)
    draw.line((px(x),y-32,px(x+1),y),fill='#171b22',width=2)
    draw.line((px(x+1),y-32,px(x),y),fill='#171b22',width=2)
    label(x+.5,y-49,'箱子')
def gate(x,height,base):
    y=base-height*scale
    draw.rectangle((px(x),y-96,px(x+1),y),fill='#606b73',outline='#929d9f',width=2)
    label(x+.5,y-113,'闸门')
def checkpoint(x,height,base):
    y=base-height*scale
    draw.rectangle((px(x)-4,y-42,px(x)+4,y),fill='#4fb8b0')
    label(x,y-61,'检查点','#b3e4cd')
def plate(x,height,base):
    y=base-height*scale
    draw.rectangle((px(x),y-5,px(x+1),y),fill='#4fb8b0')
    label(x+.5,y+9,'压板','#b3e4cd')
def switch(x,height,base,name):
    y=base-height*scale
    draw.rectangle((px(x)-6,y-27,px(x)+6,y-7),fill='#4fb8b0')
    label(x,y-49,name,'#b3e4cd')
def terminal(x,height,base):
    y=base-height*scale
    draw.rectangle((px(x),y-96,px(x+2),y),fill='#272d35',outline='#4fb8b0',width=3)
    draw.rectangle((px(x+.6),y-68,px(x+1.2),y-25),fill='#4fb8b0')
    label(x+1,y-113,'终点终端','#b3e4cd')
def gap(a,b,base,text):
    draw.line((px(a),base+62,px(b),base+62),fill='#d65b4c',width=2)
    draw.line((px(a),base+56,px(a),base+68),fill='#d65b4c',width=2)
    draw.line((px(b),base+56,px(b),base+68),fill='#d65b4c',width=2)
    label((a+b)/2,base+71,text,'#d65b4c')
def section(y,name,width):
    draw.text((55,y-145),name,font=font,fill='#e6ddc1')
    for x in range(0,int(width)+1):
        draw.line((px(x),y-8,px(x),y+44),fill='#272d35')
        if x%4==0: label(x,y+46,str(x),'#929d9f')
    draw.ellipse((px(1.5)-5,y-21,px(1.5)+5,y-11),fill='#c7cdc3')
    label(1.5,y-43,'起点')

y=245
section(y,'01 废料回收区 · 32 U · 普通跳 → 推箱压板 → 检查点 → 冲刺跳',32)
for a,b in [(0,6),(8,18),(21.25,32)]: ground(a,b,0,y)
ground(3,4,.5,y); box(10,0,y); plate(12,0,y); gate(14.5,0,y); checkpoint(16.5,0,y)
draw.rectangle((px(12.5),y-112,px(16),y-96),fill='#3d4650')
ground(25,27,1.5,y); terminal(29.5,0,y)
gap(6,8,y,'2 U'); gap(18,21.25,y,'3.25 U')

y=475
section(y,'02 废弃运输线 · 36 U · 拉杆 → 往返平台 → 检查点 → 冲刺 → 巡逻机',36)
for a,b in [(0,7),(13,19),(22.25,36)]: ground(a,b,0,y)
switch(4,0,y,'拉杆'); checkpoint(16,0,y)
draw.rectangle((px(7.5),y-4,px(10),y+9),fill='#4fb8b0')
draw.line((px(7.5),y-30,px(11.5),y-30),fill='#4fb8b0',width=2)
label(9.5,y-53,'平台往返','#b3e4cd')
draw.rectangle((px(25),y-30,px(26),y),fill='#d65b4c')
label(26.5,y-51,'巡逻区25～28','#d65b4c')
ground(29,31,1.5,y); terminal(33.5,0,y)
gap(7,13,y,'6 U：搭平台'); gap(19,22.25,y,'3.25 U')

y=705
section(y,'03 能源核心站 · 36.5 U · 推箱供电A → 检查点 → 上升冲刺 → 按钮供电B → 组合出口',36.5)
ground(0,14,0,y); ground(17,23.5,.5,y); ground(26.75,36.5,.5,y)
box(3,0,y); plate(5,0,y); gate(8,0,y); checkpoint(11,0,y)
draw.rectangle((px(6),y-112,px(10),y-96),fill='#3d4650')
switch(21,.5,y,'按钮B'); gate(32,.5,y); terminal(34,.5,y)
draw.rectangle((px(30.5),y-128,px(34),y-112),fill='#3d4650')
ground(18.5,20.5,2.25,y); label(19.5,y-95,'可选收集台')
gap(14,17,y,'3 U / 升0.5 U'); gap(23.5,26.75,y,'3.25 U')
image.save(out/'level_layouts.png')
print(out/'level_layouts.png')
