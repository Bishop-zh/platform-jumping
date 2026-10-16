"""Write exact atlas layouts and copy-ready prompts for user-run image generation."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/Prompts/Scenes'
OUT.mkdir(parents=True,exist_ok=True)

COMMON='''为《锈境：能源重启》制作废土机械风 2D 横向平台游戏素材。严格正侧视、正交、没有透视；锈橙金属与深钢灰为主，少量沙色，青蓝色只表示可用能源，危险用红色与尖刺形状表达。简洁连续色块，每种材质三档明暗，统一左上光源，轮廓清晰；不要细碎写实纹理、密集锈点、复杂反光。与提供的主页和选关参考一致，但本批是低分辨率游戏世界素材。

统一输出画布 2048×2048 PNG，真实透明 alpha 背景。它是 256×256 原生像素母版的 8 倍展示图，不是一个占 64 米的物体。全图使用统一 8×8 输出像素的逻辑像素方块，每一个逻辑像素方块大小完全一致，禁止出现有的物体用4像素方块、有的用16像素方块。物体大小由占格数决定，不通过改变像素方块大小决定。

画布概念上划分为 8 列×8 行、共64个格子；每格 256×256 输出像素，等于32×32原生像素，代表 Unity 世界1×1单位。行从上到下、列从左到右，编号1到8。不要画出网格线、行列编号、文字或尺寸标注。所有未分配格子保持透明，不自动补充对象。独立物体不要跨越分配边界，多格物体在指定连续矩形内一次性绘制，不能把每格画成不同物体。

目标世界像素密度固定为32原生像素/Unity单位。举例：1×1单位箱子占32×32原生像素，对应输出256×256；2×3单位终端占64×96原生像素，对应输出512×768；1×0.5单位薄板占32×16原生像素，对应输出256×128。所有组件的像素方块仍然同样大。

小于一格的对象放在该格左上角指定尺寸矩形内，余下区域透明；不要居中缩放到整格。独立物体在自身矩形内可留1～2个原生像素透明边，地块拼接边缘必须按说明接到矩形边界。不要为各格增加独立外边框或额外间距。输出是整数倍像素图，不使用抗锯齿，不画棋盘格假透明。

限色参考：#171B22 #272D35 #3D4650 #606B73 #929D9F #C7CDC3 #633B32 #96533A #C17A43 #6D6652 #9B9472 #C9BD91 #D65B4C #D6B85A #4FB8B0 #B3E4CD。普通组件优先4～8色，透明不计入颜色。
'''
NEGATIVE='''\n负面要求：写实、3D渲染、等距透视、斜视、摄影、柔焦、渐变光晕、粒子烟雾遮挡、半透明羽化边、抗锯齿、噪点、像素方块大小不一致、整体缩放后的不同像素密度、文字、水印、编号、网格线、棋盘格背景、自动填满空白格、物体超出指定矩形、紧裁整张画布、改变2048×2048输出尺寸。'''

sheets=[]
def sheet(id,title,description):
    value={'id':id,'title':title,'source_size':[2048,2048],'native_size':[256,256],
           'grid':[8,8],'native_cell':[32,32],'ppu':32,'preview_scale':8,
           'rect_origin':'top-left; x,y,width,height; Unity slice y=256-(y+height)',
           'assets':[], 'description':description}
    sheets.append(value)
    return value
def asset(s,name,row,col,width,height,description):
    s['assets'].append({'name':name,'row':row,'column':col,
        'native_rect':[(col-1)*32,(row-1)*32,width,height],
        'source_rect':[(col-1)*256,(row-1)*256,width*8,height*8],
        'world_canvas':[width/32,height/32],'description':description})

a=sheet('A','地形地块','所有组件32×32原生像素、1×1单位。每格一个独立Tile，顶面统一位于格子的第0行，最上方2像素高光一致。中段A/B/C左右2像素和顶面高度必须完全相同，只改变内部连续锈色块；填充块四边可重复。无草、无石砖、无透视。')
names=[('GroundTopLeft','厚钢铁地面左上边缘'),('GroundTopA','厚钢铁地面中段A'),('GroundTopB','中段B，少量不同锈块'),('GroundTopC','中段C，少量不同螺栓'),('GroundTopRight','厚钢铁地面右上边缘'),('GroundSingle','单格独立地面，两侧都有收边'),('GroundFillA','内部填充A，无亮顶边'),('GroundFillB','内部填充B，无亮顶边'),('GroundSideLeft','地面左侧边'),('GroundSideRight','地面右侧边'),('GroundBottomLeft','底部左角'),('GroundBottomA','底边中段A'),('GroundBottomRight','底部右角'),('GroundCornerInnerLeft','内凹左角'),('GroundCornerInnerRight','内凹右角'),('GroundFillC','第三种简洁内部填充')]
for i,(name,text) in enumerate(names): asset(a,name,1+i//8,1+i%8,32,32,text)

b=sheet('B','平台与结构','薄平台、护栏、支柱、电线为可拼接模块。薄平台顶面在本格第0行，实体厚度约8～12原生像素，画布32×16；剩余半格透明。所有平台中段左右2像素、顶部2像素完全一致。电线为2像素宽金属线/能源线，16×16的小格接头必须在对应边界中点接出。')
for i,(name,text) in enumerate([('PlatformLeft','薄平台左端'),('PlatformA','薄平台中段A'),('PlatformB','薄平台中段B'),('PlatformC','薄平台中段C'),('PlatformRight','薄平台右端'),('PlatformSingle','单格薄平台')]): asset(b,name,1,i+1,32,16,text)
for i,(name,text) in enumerate([('RailLeft','护栏左端，无地面碰撞'),('RailMiddle','护栏中段，无地面碰撞'),('RailRight','护栏右端，无地面碰撞')]): asset(b,name,2,i+1,32,16,text)
asset(b,'SupportCap',3,1,32,32,'支柱顶帽，顶面没有可站立的亮边')
asset(b,'SupportMiddle',3,2,32,64,'竖向重复钢梁，跨第3～4行第2列，一根连续支柱')
asset(b,'SupportBase',3,3,32,32,'支柱底座')
for i,(name,text) in enumerate([('CableHorizontal','水平直段'),('CableVertical','垂直直段'),('CableCornerNE','上右转角'),('CableCornerNW','上左转角'),('CableCornerSE','下右转角'),('CableCornerSW','下左转角'),('CableTerminalOff','断电端口'),('CableTerminalOn','青色通电端口')]): asset(b,name,5,i+1,16,16,text)
asset(b,'MovingPlatform',7,1,80,16,'一块完整起重移动平台，跨第7行第1～3列预留区，实际只占前80×16原生像素即2.5×0.5单位，不填满96像素；左右收边和中部简洁警示条')

c=sheet('C','交互机关','配对状态必须轮廓、尺寸、螺栓和脚底位置一致，只改变开关状态或门板位置。开启门保持完整32×96画布，门板收至上方，开口透明；不要将开门态紧裁成一条窄图。无数字、无文字。')
for col,name,text in [(1,'CrateA','1单位可推金属箱，简单十字加强筋'),(2,'CrateB','同尺寸箱子外观B，不改变箱体高度'),(3,'WeightPlateOff','重量压板未压下，绘制简洁箱子符号'),(4,'WeightPlateOn','同款压板压下，青色指示灯'),(5,'LeverOff','拉杆关闭，杆向左'),(6,'LeverOn','同款拉杆开启，杆向右，青灯'),(7,'WallButtonOff','墙面按钮未激活，暗红灯'),(8,'WallButtonOn','同款墙面按钮激活，青灯')]:
    asset(c,name,1,col,32,16 if 'Plate' in name else 32,text)
asset(c,'GateClosed',2,1,32,96,'1×3单位关闭闸门，跨第2～4行第1列')
asset(c,'GateOpen',2,2,32,96,'同款闸门打开，跨第2～4行第2列，门框和底基线保持一致')
asset(c,'CheckpointOff',2,3,32,64,'1×2单位检查点灯塔未激活，跨第2～3行第3列')
asset(c,'CheckpointOn',2,4,32,64,'同款灯塔激活，青色条灯，跨第2～3行第4列')
asset(c,'ExitOff',2,5,64,96,'2×3单位断电能源终端，跨第2～4行第5～6列')
asset(c,'ExitOn',2,7,64,96,'同款终端激活，青色能源槽，跨第2～4行第7～8列')

d=sheet('D','道具与危险','小道具16×16原生像素即0.5×0.5单位，放在分配格左上16×16矩形内；其他半格透明。相同动画保持固定轮廓、脚底与画布，亮灭不使用模糊光晕。危险造型靠形状识别。')
for i in range(4): asset(d,'Fragment'+str(i),1,i+1,16,16,'同一能源碎片轻微亮灭第'+str(i+1)+'帧，不改变外形或中心')
for col,name,text in [(5,'Shield0','护盾胶囊常态'),(6,'Shield1','同款护盾胶囊亮态'),(7,'JumpBoost0','弹跳能源模块常态，向上箭头符号'),(8,'JumpBoost1','同款弹跳模块亮态')]: asset(d,name,1,col,16,16,text)
for col,name,text in [(1,'SpikeLeft','尖锐废铁危险块左端'),(2,'SpikeMiddle','尖锐废铁危险块中段，横向可重复'),(3,'SpikeRight','尖锐废铁危险块右端')]: asset(d,name,2,col,32,16,text)
asset(d,'HazardBarrel',2,4,32,64,'1×2单位危险废料桶装饰，跨第2～3行第4列；首版不设计爆炸')
for i in range(4): asset(d,'Patrol'+str(i),2,5+i,32,32,'同一小巡逻机械第'+str(i+1)+'帧，脚底统一第31行，简洁红灯；保持同方向，可由游戏翻转')
for i in range(4): asset(d,'DashFX'+str(i),4,1+i,32,32,'简洁青色冲刺残影第'+str(i+1)+'帧，无烟雾，最多3色，轮廓从短到长再消失')

e=sheet('E','大型环境装饰','可选第二批。四个独立装饰，用大画布表达大物体，严禁让逻辑像素变大。亮度和对比低于可碰撞地形，不带像可站立平台一样的亮顶边。')
asset(e,'ScrapPile',1,1,96,96,'3×3单位废料箱堆，跨第1～3行第1～3列；轮廓简洁')
asset(e,'BrokenPipe',1,5,96,64,'3×2单位倒放锈蚀管道，跨第1～2行第5～7列，正侧视')
asset(e,'CraneColumn',4,1,64,128,'2×4单位起重机立柱，跨第4～7行第1～2列，避免透视')
asset(e,'Reactor',4,4,96,128,'3×4单位反应堆外壳，跨第4～7行第4～6列，少量青灯；只做后景装饰')

f=sheet('F','远景工厂','可选第二批。本张为一个完整8×8单位的远景模块，不是64个独立物体。占全画布256×256原生像素/2048×2048预览；同样一个原生像素对应8×8输出方块。绘制低对比废弃工厂轮廓，约下方5～6单位高，顶端留透明天空；不要画太阳、文字、前景平台或UI。左右两边在相同高度的轮廓与颜色匹配，便于横向重复，接缝后续仍需验收。')
asset(f,'FarFactory',1,1,256,256,'整张画布一个8×8单位连续远景模块')

for s in sheets:
    occupied=[]
    instructions=[]
    for item in s['assets']:
        x,y,w,h=item['native_rect']
        assert 0<=x<x+w<=256 and 0<=y<y+h<=256,item
        assert item['world_canvas']==[w/32,h/32]
        for bx,by,bw,bh in occupied:
            assert x>=bx+bw or bx>=x+w or y>=by+bh or by>=y+h,(s['id'],item['name'],'overlap')
        occupied.append((x,y,w,h))
        instructions.append(f"第{item['row']}行第{item['column']}列起：{item['name']}，原生{w}×{h}像素，输出{w*8}×{h*8}像素，世界{w/32:g}×{h/32:g}单位。{item['description']}。")
    prompt=COMMON+'\n本张图集：'+s['title']+'。\n'+s['description']+'\n\n精确布局：\n'+'\n'.join(instructions)+NEGATIVE
    (OUT/(s['id']+'_'+s['title']+'.txt')).write_text(prompt,encoding='utf-8')
(OUT/'atlas_layouts.json').write_text(json.dumps(sheets,ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS: 6 sheets,',sum(len(s['assets']) for s in sheets),'asset rectangles; exact PPU and no overlaps')
