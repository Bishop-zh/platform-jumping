"""Original deterministic cartoon-wasteland chiptune assets, stereo 44.1 kHz PCM."""
from pathlib import Path
import json, wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Audio';OUT.mkdir(parents=True,exist_ok=True)
RATE=44100
RNG=np.random.default_rng(20261007)

def note(midi,duration,tone='lead'):
    t=np.arange(round(duration*RATE))/RATE;f=440*2**((midi-69)/12)
    if tone=='bass':s=np.sin(2*np.pi*f*t)+.18*np.sin(2*np.pi*f*2*t)
    elif tone=='pad':s=np.sin(2*np.pi*f*t)+.16*np.sin(2*np.pi*f*3*t)
    else:s=np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*f*2*t)+.12*np.sin(2*np.pi*f*4*t)
    attack=.008 if tone!='pad' else .06
    env=np.minimum(t/attack,1)*np.minimum((duration-t)/(.07 if tone!='pad' else .18),1)
    return s*np.maximum(env,0)

def add(track,voice,start,gain,pan=0,loop=False):
    index=np.arange(len(voice))+round(start*RATE)
    stereo=voice[:,None]*gain*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])
    if loop:np.add.at(track,index%len(track),stereo)
    else:
        valid=index<len(track);track[index[valid]]+=stereo[valid]

def drum(kind):
    d=.18 if kind=='kick' else .13 if kind=='snare' else .035;t=np.arange(round(d*RATE))/RATE
    if kind=='kick':return np.sin(2*np.pi*(48*t+5*(1-np.exp(-t*35))))*np.exp(-t*25)
    noise=RNG.normal(0,1,len(t));bright=noise-np.roll(noise,1)
    return (bright*.3+np.sin(2*np.pi*190*t)*.14)*np.exp(-t*(36 if kind=='snare' else 110))

def save(name,sound,peak=.75):
    sound=np.tanh(sound);maximum=np.max(np.abs(sound));sound*=peak/max(maximum,1e-9)
    fade=min(220,len(sound)//4);ramp=np.linspace(0,1,fade);sound[:fade]*=ramp[:,None];sound[-fade:]*=ramp[::-1,None]
    pcm=np.round(sound*32767).astype('<i2')
    with wave.open(str(OUT/name),'wb') as wav:wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(RATE);wav.writeframes(pcm.tobytes())
    return {'file':name,'seconds':len(sound)/RATE,'peak':float(np.max(np.abs(sound))),'sample_rate':RATE,'channels':2}

click=np.zeros((round(.13*RATE),2));t=np.arange(len(click))/RATE
voice=(np.sin(2*np.pi*(1200*t-1600*t*t))*.6+np.sin(2*np.pi*2200*t)*.17)*np.exp(-t*65)
voice+=RNG.normal(0,.07,len(t))*np.exp(-t*170)
add(click,voice,0,.8)
reports=[save('UI_Click.wav',click,.65)]

beat=60/112;bars=16;music=np.zeros((round(bars*4*beat*RATE),2))
chords=[(50,53,57),(46,50,53),(48,52,55),(45,48,52)]
melodies=[[74,None,77,76,74,72,69,None],[70,74,77,None,74,72,70,69],
          [72,None,76,79,77,76,72,None],[69,72,76,77,76,72,69,None]]
for bar in range(bars):
    chord=chords[bar%4];melody=melodies[bar%4];base=bar*4*beat
    for b in range(4):
        add(music,drum('kick'),base+b*beat,.24,loop=True)
        if b%2==1:add(music,drum('snare'),base+b*beat,.12,.1,True)
        add(music,note(chord[0]-12,beat*.64,'bass'),base+b*beat,.20,-.1,True)
        for h in (0,.5):add(music,drum('hat'),base+(b+h)*beat,.065,-.35 if h==0 else .35,True)
    for pitch in chord:add(music,note(pitch+12,beat*3.8,'pad'),base,.032,.15,True)
    for step,pitch in enumerate(melody):
        if pitch is not None:add(music,note(pitch,beat*.38),base+step*beat*.5,.105,-.12,True)
    for step in range(8):add(music,note(chord[step%3]+24,beat*.22,'pad'),base+step*beat*.5,.028,.5,True)
reports.append(save('Wasteland_Play_Loop.wav',music,.72))

victory=np.zeros((round(3.1*RATE),2))
for i,pitch in enumerate([62,66,69,74,78,81]):add(victory,note(pitch,.36),i*.16,.15,(-.2 if i%2==0 else .2))
for pitch in (62,66,69,74):add(victory,note(pitch,1.75,'pad'),1.02,.085)
for i,pitch in enumerate([86,81,86,90]):add(victory,note(pitch,.22),1.16+i*.18,.05,.4)
add(victory,drum('kick'),1.02,.16)
reports.append(save('Victory.wav',victory,.75))
artifact=ROOT/'Artifacts/Audio';artifact.mkdir(parents=True,exist_ok=True)
(artifact/'generation.json').write_text(json.dumps({'style':'Original light industrial cartoon chiptune; no samples or external copyrighted music','music_bpm':112,'music_bars':16,'clips':reports},indent=2),encoding='utf-8')
print(json.dumps(reports,ensure_ascii=False))
