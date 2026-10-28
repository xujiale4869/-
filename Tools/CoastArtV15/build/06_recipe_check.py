from pathlib import Path
import os,subprocess,sys,shutil,hashlib,json
R=Path(__file__).resolve().parents[1];P=Path(r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3')
sandbox=R/'review/recipe-sandbox';backup=P/'Tools/CoastArtV15/Before-20261024'
owned=['Assets/Deepsea/Scripts/Runtime/'+n for n in ['CoastShelfShape.cs','CoastGroundSmoothing.cs','CoastArtDetails.cs','CoastMaterialsAppearance.cs','CoastBlockoutCheck.cs']]+['Assets/Deepsea/Scripts/Editor/CoastMaterialsBuild.cs','Assets/Deepsea/Scripts/Editor/CoastShelfBuild.cs','Assets/Deepsea/Shaders/CoastHabitatGround.shader']
for rel in owned:
    dst=sandbox/'Tools/CoastArtV15/Before-20261024'/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(backup/rel,dst)
env=dict(os.environ,COAST_PROJECT_ROOT=str(sandbox))
for name in ['01_foundation.py','02_refine.py','03_geology.py']:
    subprocess.run([sys.executable,str(R/'build'/name)],env=env,check=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
records=[{'path':rel,'matchesLiveSource':sha(sandbox/rel)==sha(P/rel),'sha256':sha(P/rel)} for rel in owned]
report={'scope':'Source recipe rebuilt in a separate scratch tree. Not byte reproducibility of Unity assets or Blender generated meshes.','passed':all(r['matchesLiveSource'] for r in records),'files':records}
(R/'review/recipe-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
assert report['passed'],report
print('SOURCE_RECIPE_MATCHES',len(records))
