from pathlib import Path
import json,hashlib,gzip,shutil
R=Path(__file__).resolve().parents[1];P=Path(r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3');T=P/'Tools/CoastArtV17'
def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for block in iter(lambda:f.read(1048576),b''):h.update(block)
    return h.hexdigest()
def js(path,data):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
before=T/'Before';checked=0;changed=[]
for category in ['Scripts','Shaders']:
    for old in (before/category).rglob('*'):
        if old.is_file():
            checked+=1;current=P/'Assets/Deepsea'/old.relative_to(before)
            if not current.exists() or sha(old)!=sha(current):changed.append(str(old.relative_to(before)))
waterHash=sha(P/'Assets/Deepsea/Shaders/CoastFoundationWater.shader')
assert not changed and waterHash=='4a7fdfa9164a208a8136c192cb3284ea335d2b60e5ec03f7fe7a652ac02c015c'
geometryMatched=[];geometryDifferences=[]
base=P/'Assets/Deepsea/Resources/CoastShowcase';new=P/'Assets/Deepsea/Resources/CoastRefinement'
for candidate in new.glob('EN_*.asset'):
    old=base/candidate.name
    if old.exists():
        (geometryMatched if sha(old)==sha(candidate) else geometryDifferences).append(candidate.name)
assert not geometryDifferences,geometryDifferences
manifest=json.loads((P/'Tools/CoastArtV16/accepted-manifest.json').read_text())
baseInputs=[]
for e in manifest['files']:
    equal=sha(P/e['path'])==e['sha256'];baseInputs.append({'path':e['path'],'same':equal});assert equal,e['path']
integrity={'oldScriptShaderFilesCompared':checked,'changedOriginalFiles':changed,'unchangedV16SourceInputs':baseInputs,'unchangedOtherEnvironmentMeshes':geometryMatched,'unexpectedMeshDifferences':geometryDifferences,'waterCoreSha256':waterHash,'v16ExecutableRetained':(P/'Builds/CoastArtV16/MistCape-ArtSample.exe').exists(),'scope':'Byte comparisons cover old source and frozen v16 inputs plus same-named unchanged environment mesh assets. No original gameplay integration is claimed.'}
js(R/'review/preservation-audit.json',integrity)
for d in ['build','textures','exports','review']:(T/d).mkdir(parents=True,exist_ok=True)
for source in (R/'build').glob('*.py'):shutil.copy2(source,T/'build'/source.name)
shutil.copy2(R/'textures/Limewash-Albedo.png',T/'textures/Limewash-Albedo.png')
shutil.copy2(R/'asset-brief.md',T/'asset-brief.md')
inputs=[P/'Assets/Deepsea/ThirdParty/CoastArtV17/LandPatch.json',P/'Assets/Deepsea/ThirdParty/CoastArtV17/Limewash-Albedo.png',P/'Assets/Deepsea/Scripts/Editor/CoastRefinementBuild.cs',P/'Assets/Deepsea/Scripts/Runtime/CoastRefinementRuntime.cs',P/'Assets/Deepsea/Shaders/CoastRefinedArchitecture.shader',P/'Assets/Deepsea/Shaders/CoastRefinedStone.shader'];frozen=[]
for source in inputs:
    relative=source.relative_to(P);compressed=source.suffix=='.json';fp=Path(str(relative)+('.gz' if compressed else ''));dest=T/'accepted'/fp;dest.parent.mkdir(parents=True,exist_ok=True)
    if compressed:
        with source.open('rb') as i,gzip.open(dest,'wb',compresslevel=5) as o:shutil.copyfileobj(i,o,1048576)
    else:shutil.copy2(source,dest)
    frozen.append({'path':relative.as_posix(),'frozenPath':fp.as_posix(),'encoding':'gzip' if compressed else 'copy','sha256':sha(source),'sourceSha256':sha(dest)})
dependencies=[P/'Assets/Deepsea/ThirdParty/CoastArtV16/CoastalShowcase.json',P/'Assets/Deepsea/ThirdParty/CoastSlice/boulder_01/boulder_01_rough_2k.exr']
js(T/'accepted-manifest.json',{'version':'v17-material-refinement','frozenMeaning':'Technically checked delivery; no user final art approval implied','files':frozen,'dependencies':[{'path':p.relative_to(P).as_posix(),'sha256':sha(p)} for p in dependencies]})
for source in (P/'Verification/coast-art-v17-final').iterdir():
    if source.is_file():shutil.copy2(source,R/'runtime'/source.name)
shutil.copy2(P/'Verification/coast-art-v17-build/mesh-summary.json',R/'review/engine-mesh-summary.json')
for name in ['patch-audit.json','validate.json','preservation-audit.json']:shutil.copy2(R/'review'/name,T/'review'/name)
for name in ['asset-manifest.json']:shutil.copy2(R/'exports'/name,T/'exports'/name)
from PIL import Image
img=Image.open(R/'textures/Limewash-Albedo.png')
ledger={'newTexture':{'path':'textures/Limewash-Albedo.png','sha256':sha(R/'textures/Limewash-Albedo.png'),'sourcePixels':list(img.size),'mode':img.mode,'provider':'Native image_gen','promptBrief':'Seamless square neutral grey-white lime-plaster PBR base colour for coastal lighthouse. Fine mineral grains, tiny cracks and sparse chips exposing pale limestone, low broad contrast, no yellow/brown blotches or bricks; no perspective, shadows, objects or text. Strong base/rain weathering supplied separately by engine masks. Requested2048; actual returned dimensions recorded.','usage':'sRGB, repeat, trilinear, max2048, BC high-quality importer; existing fine plaster normal with .23 strength, localized shader masks and roughness; no newly baked matched normal claimed.'},'existing':'CC0 scan/albedo/normal/roughness from existing boulder_01; existing plaster normal and original tower mesh; no new GitHub dependency/download. New shaders use source textures with localized masks, not a landscape image.','scope':'Only tower and4 adjacent rocks have new materials;43 real footing blocks and3 dry-land contact patches. Refined stone shader has wet/dry, source roughness, upper moss, contact colour; architecture has localized base exposure/window rain/limited metal oxidation.'}
js(R/'source-ledger.json',ledger);js(T/'source-ledger.json',ledger)
shutil.copy2(P/'Assets/Deepsea/ThirdParty/CoastSlice/SOURCE-LICENSE.txt',T/'SOURCE-LICENSE-existing.txt')
js(R/'review/delivery-hashes.json',{str(p.relative_to(R)):sha(p) for p in [R/'EN_LighthouseShore_master.blend',R/'AR_LighthouseMaterial_master.blend',R/'exports/EN_LighthouseShore.glb',R/'exports/LandPatch.json']})
print('V17_PACKAGED',len(frozen),'inputs;',checked,'old source files retained;',len(geometryMatched),'other meshes exact')
