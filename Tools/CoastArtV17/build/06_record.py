from pathlib import Path
import json,shutil
R=Path(__file__).resolve().parents[1];W=R.parent;P=Path(r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3');T=P/'Tools/CoastArtV17'
result=json.loads((R/'runtime/result.json').read_text(encoding='utf-8'));assert result['passed']
val=json.loads((R/'review/validate.json').read_text(encoding='utf-8'));assert not val['summary']['fail'] and not val['summary']['warn']
rt=json.loads((R/'review/roundtrip/roundtrip.json').read_text());assert not rt['errors'];assert sum(x['tris'] for x in rt['meshes'])==149281
entry={'version':'v17-material-refinement','originalPlayableVersion':'v15','previousDirectionApproved':'v16','userFinalV17Approval':False,'project':str(P),'report':str(R/'reference-analysis.md'),'scene':'Assets/Deepsea/Scenes/CoastArtRefined.unity','executable':'Builds/CoastArtV17/MistCape-Materials.exe','entrypoint':str(T/'Rebuild-MaterialStudy.ps1'),'scope':'Independent material/footing sample; no gameplay integration. Same v16 composition/sun/post/water. F7 switches actual original geometry/tower and refined geometry/tower under same camera.','focus':'Tower +4 selected shore rocks;43 footing stones,3 dry land contact patches;359 vertices max .08558m shift','performance':result,'environmentTriangles':743425,'batches':68,'indexedVertices':618510,'sourceValidation':'Scoped shore/footing149281tris,0warning0fail,GLB roundtrip0errors. Does not erase original full-scene warnings.','preservation':'339 old scripts/shaders exact;9 frozen v16 inputs exact;58 other environment meshes exact;water core unchanged','frozenSync':'6 v17 inputs restored and verified; baseline dependencies checked','qualityLimit':'Still below concept: repeated rock forms, hard joins, card foliage and simplified windows remain.'}
(R/'verification-summary.json').write_text(json.dumps(entry,ensure_ascii=False,indent=2),encoding='utf-8')
config=W/'.codex/art-tools.json';d=json.loads(config.read_text(encoding='utf-8-sig'));d['previous_independent_art_sample']=d.get('independent_art_sample');d['independent_art_sample']=entry;config.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
note='''

## 2026-10-24 v17：认可方向上的灯塔和局部岸石精修

用户认可v16“改善很多、方向正确”，授权先做灯塔与旁侧一组岸石。v17仍是独立美术样板，原可玩版v15与美术样板v16均保留。报告 C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔/art-v17/reference-analysis.md。桌面“深海灯塔-灯塔岸石精修-v17”；程序 Builds/CoastArtV17/MistCape-Materials.exe，场景 CoastArtRefined.unity，构建 Deepsea.Editor.CoastRefinementBuild.Build。

新灰白灰泥贴图1254平方，局部基座露石/窗下雨痕/轻微金属风化。43块真实基座石，4块岸石干湿/粗糙度/少量苔藓，359个底部顶点最大调整.08558m，3处干地接合补片；水下环状失败补片已去除。构图、太阳、后期、水核心、礁体和绿色海带不变。F7在同镜头切换真实v16/v17环境与塔材质，5/6为灯塔/岸石近景。

最终可见窗口原生1080p固定镜头1200帧平均62.21FPS，P95 18.84ms。环境743425面68组618510索引顶点；较v16增加5281面/5组。岸石/基座源档149281面，模型0警告0失败，GLB回读面数一致0错误；不把整个旧场景历史警告报成0。339旧脚本/着色器、v16冻结9输入和58项其他环境网格逐字节一致。新冻结入口Tools/CoastArtV17/Rebuild-MaterialStudy.ps1默认恢复6输入且已测试，-Build构建，-GenerateDraft为需重验新稿。完整源档含岸石/基座与灯塔两份Blender文件，精确风化在Unity。

未集成原航行/步行/存档，没有新玩法通过声明；本轮未重测沿用的鱼群路线。v17尚未由用户评价，仍有扫描岩形重复、岩脚硬边、门窗简化与植被卡片感，不能称为已匹配原图。后续在此方向继续，不退回旧构图和水面。
'''
for path in [W/'AGENTS.md',P/'AGENTS.md']:
    s=path.read_text(encoding='utf-8-sig')
    if '## 2026-10-24 v17：认可方向上' not in s:path.write_text(s.rstrip()+note,encoding='utf-8')
shutil.copy2(R/'reference-analysis.md',P/'Docs/灯塔与岸石材质精修-v17.md');shutil.copy2(R/'verification-summary.json',T/'review/verification-summary.json');shutil.copy2(R/'runtime/result.json',T/'review/final-result.json')
print('V17_RECORD_COMPLETE',round(result['fps'],2),'FPS')
