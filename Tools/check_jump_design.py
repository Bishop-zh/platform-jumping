"""Estimate proposed movement reach; not a substitute for Unity collider playtests."""
from pathlib import Path
import json, math

ROOT=Path(__file__).resolve().parents[1]
speed,gravity,height,dash_speed,dash_duration=4.5,28.0,2.2,10.0,0.15
launch=math.sqrt(2*gravity*height)

def reach(rise,dash=False,dt=0.02):
    y,x,vy,t=0.0,0.0,launch,0.0
    while t<3:
        previous_y=y
        vy-=gravity*dt
        y+=vy*dt
        used=dt
        landed=vy<0 and y<=rise
        if landed: used=dt*(previous_y-rise)/(previous_y-y)
        overlap=max(0.0,min(t+used,0.45)-max(t,0.30)) if dash else 0.0
        x+=speed*used+(dash_speed-speed)*overlap
        t+=used
        if landed: return {'seconds':round(t,4),'distance':round(x,4)}
    raise RuntimeError('Did not land')

rows=[]
for rise in [0.0,0.5,1.0]:
    airtime=(launch+math.sqrt(launch**2-2*gravity*rise))/gravity
    rows.append({'landing_rise':rise,'continuous_walk':round(speed*airtime,4),
                 'continuous_dash':round(speed*airtime+(dash_speed-speed)*dash_duration,4),
                 'fixed_step_walk':reach(rise),'fixed_step_dash':reach(rise,True)})

# Comfortable center-to-center landing reserves 0.25U at both ends; does not model edge/coyote exploits.
for gap,rise,dash in [(2.0,0.0,False),(3.25,0.0,True),(3.0,0.5,True)]:
    margin=reach(rise,dash)['distance']-(gap+0.5)
    assert margin>0.35,(gap,rise,dash,margin)
    print(f'gap={gap:.2f}U rise={rise:.2f}U dash={dash}: conservative margin={margin:.3f}U')
report={'status':'design estimate only; no PlayerMotor implemented yet',
 'gravity':gravity,'launch_speed':round(launch,5),'walk_speed':speed,
 'dash_speed':dash_speed,'dash_seconds':dash_duration,'dash_vertical_rule':'gravity continues',
 'fixed_dt':0.02,'results':rows}
destination=ROOT/'Artifacts/LevelDesign'
destination.mkdir(parents=True,exist_ok=True)
(destination/'jump_reach.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
