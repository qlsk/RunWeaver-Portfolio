import json, re, sys
ids=json.load(open(sys.argv[1]))
base=sys.argv[2]
FPS=10; N=8
for st in range(1,6):
    p=f'{base}/SoulFlame_Stage{st}.anim'
    s=open(p,encoding='utf-8').read()
    g=ids[str(st)]['guid']; fid=ids[str(st)]['ids']
    seq=list(range(N))+[N-1]
    keys=''.join(f"    - time: {i/FPS:.8g}\n      value: {{fileID: {fid[f]}, guid: {g}, type: 3}}\n" for i,f in enumerate(seq))
    s=re.sub(r'(  m_PPtrCurves:\n  - serializedVersion: 2\n    curve:\n)(?:    - time: .*\n      value: .*\n)+', lambda m: m.group(1)+keys, s)
    mapping=''.join(f"    - {{fileID: {fid[f]}, guid: {g}, type: 3}}\n" for f in range(N))
    s=re.sub(r'(    pptrCurveMapping:\n)(?:    - .*\n)+', lambda m: m.group(1)+mapping, s)
    s=re.sub(r'm_SampleRate: \d+', f'm_SampleRate: {FPS}', s)
    s=re.sub(r'm_StopTime: [\d.]+', f'm_StopTime: {N/FPS:.8g}', s)
    s=s.replace('m_LoopTime: 0','m_LoopTime: 1')
    open(p,'w',encoding='utf-8').write(s)
    print(p, s.count('time:'))
