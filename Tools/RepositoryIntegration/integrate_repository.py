"""Bring existing Unity integrations and installed tool sources under one Git history.
This does not enable rejected visual effects or import unrelated gameplay systems.
"""
import datetime, hashlib, json, os, pathlib, shutil, subprocess
ROOT=pathlib.Path(__file__).resolve().parents[2]
WORK=pathlib.Path("C:/Users/许嘉乐/Documents/ChatGPT/深海灯塔")
HERE=ROOT/"Tools/RepositoryIntegration"
def run(*args,check=True):
    p=subprocess.run(list(args),cwd=ROOT,capture_output=True,text=True,encoding="utf-8",errors="replace")
    if check and p.returncode: raise RuntimeError(str(args[:3])+": "+p.stderr[-1500:])
    return p.stdout.strip()
def sha(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for b in iter(lambda:f.read(1024*1024),b""):h.update(b)
    return h.hexdigest()
def main():
    audit=json.loads((HERE/"dependency-audit-before.json").read_text(encoding="utf-8"))
    if not (ROOT/".git").exists():
        print(run("git","init","-b","main"),flush=True)
        run("git","config","user.name","xujiale4869")
        run("git","config","user.email","339454689+xujiale4869@users.noreply.github.com")
    run("git","config","core.quotepath","false")
    run("git","config","core.longpaths","true")
    run("git","config","core.autocrlf","false")
    print(run("git","lfs","install","--local"),flush=True)
    extra="""
# Repository integration: retain local history/outputs on disk without committing duplicates.
/Verification/*
!/Verification/repository-integration/
/Tools/*Backup*/
/Tools/*-Stage/
/Tools/*-stage/
/Tools/*-Release/
/Tools/*-Release-Final/
/Tools/*/Before*/
/Tools/**/PerformanceBefore/
/Tools/WaterSource/boat-water.zip
*.log
*.blend1
__pycache__/
*.pyc
.env
.env.*
!.env.example
auth.json
credentials.json
"""
    ig=ROOT/".gitignore"
    old=ig.read_text(encoding="utf-8-sig")
    if "# Repository integration:" not in old:ig.write_text(old.rstrip()+"\n"+extra,encoding="utf-8")
    attrs=["* text=auto","*.cs text eol=lf","*.shader text eol=lf","*.hlsl text eol=lf","*.md text eol=lf","*.ps1 text eol=crlf"]
    patterns=["*.asset","*.fbx","*.FBX","*.png","*.jpg","*.jpeg","*.tga","*.tif","*.tiff","*.blend","*.glb","*.gz","*.zip","*.wav","*.ogg","*.mp4","*.ttf","*.otf"]
    for d in ("Assets","Tools"):
        for base,dirs,files in os.walk(ROOT/d):
            dirs[:]=[x for x in dirs if x not in (".git","node_modules","__pycache__")]
            for n in files:
                p=pathlib.Path(base)/n
                if p.suffix==".json" and p.stat().st_size>50000000:patterns.append(p.relative_to(ROOT).as_posix())
    attrs += [s+" filter=lfs diff=lfs merge=lfs -text" for s in sorted(set(patterns))]
    (ROOT/".gitattributes").write_text("\n".join(attrs)+"\n",encoding="utf-8")
    modules=[]
    for item in audit["nestedRepositories"]:
        if item["dirty"]:raise RuntimeError("Nested upstream has changes: "+item["path"])
        modules += ['[submodule "'+item["path"]+'"]','\tpath = '+item["path"],'\turl = '+item["url"]]
    (ROOT/".gitmodules").write_text("\n".join(modules)+"\n",encoding="utf-8")
    installed=json.loads((WORK/".codex/art-tools.json").read_text(encoding="utf-8-sig"))
    snapshots=[]
    for tool in installed["tools"]:
        if tool["name"] not in ("style-loop","ai-game-art-pipeline","blender-image-to-3d"):continue
        src=pathlib.Path(tool["installed_path"]);dst=ROOT/"Tools/AgentSkillSources"/tool["name"]
        if not src.is_dir():raise RuntimeError("Missing installed skill source: "+str(src))
        if dst.exists():raise RuntimeError("Snapshot destination already exists; review before replacing")
        shutil.copytree(src,dst,ignore=shutil.ignore_patterns(".git","__pycache__","*.pyc",".env",".env.*","auth.json","credentials.json"))
        files=[p for p in dst.rglob("*") if p.is_file()]
        for p in files:
            if sha(p)!=sha(src/p.relative_to(dst)):raise RuntimeError("Snapshot differs: "+str(p))
        snapshots.append({"name":tool["name"],"repository":tool["repository"],"commit":tool["commit"],"sourceSubdirectory":tool.get("source_path","."),"snapshot":dst.relative_to(ROOT).as_posix(),"files":len(files),"installedAlready":True,"installedFilesChanged":False})
    (ROOT/"Tools/AgentSkillSources/sources.lock.json").write_text(json.dumps(snapshots,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    skillsreadme="""# 已安装美术技能的固定源码
这里保存此前已安装的三个技能的原文件副本，未修改用户技能目录，也未声称运行了新的美术生产。
sources.lock.json 记录原始仓库、提交、子目录及文件数量。
这些是制作工具，不是 Unity 运行组件；当前安装仍在用户 .agents/skills 目录。
现有 Tools/CoastArtV12、CoastArtV13 等为实际资产生产入口，不能用技能安装代替资产导入和实机验收。
"""
    (ROOT/"Tools/AgentSkillSources/README.md").write_text(skillsreadme,encoding="utf-8")
    for name in ("审美工具接入说明.md","制作交接-海湾生活-v21-2026-10-28.md"):
        shutil.copy2(WORK/"docs"/name,ROOT/"Docs"/name)
    deps=[
      {"name":"BoatAttack water","state":"selectively-integrated","source":"Assets/Deepsea/ThirdParty/BoatAttack/SOURCE.md","usage":"Assets/Deepsea/Shaders/Ocean.shader","note":"原波浪/贴图已接入；v22新增波形与尾迹为项目代码，不冒称采用官方整包船控。"},
      {"name":"Tiled Directional Flow","state":"adapted-runtime-source","source":"Assets/Deepsea/ThirdParty/TiledDirectionalFlow/SOURCE.md","usage":"Assets/Deepsea/Shaders/Includes/TiledCurrent.hlsl","note":"原Ocean水shader已调用；不声称v22独立shader使用同一水流实现。"},
      {"name":"FastNoiseLite","state":"integrated","source":"Assets/Deepsea/ThirdParty/FastNoiseLite/SOURCE.md","usage":"Assets/Deepsea/Scripts/Runtime/CoastShelfShape.cs"},
      {"name":"NVJOB Boids","state":"selective-mesh-reuse","source":"Assets/Deepsea/ThirdParty/NVJOB/SOURCE.md","usage":"Assets/Deepsea/Resources/Wildlife/Seabird.fbx","note":"海鸟网格复用，非安装全套NVJOB控制器。"},
      {"name":"Lucide","state":"integrated-icons","source":"Assets/Deepsea/Resources/UI/Lucide-version.txt","usage":"Assets/Deepsea/Resources/UI/Lucide-LICENSE.txt"},
      {"name":"ACG Importer","state":"installed-editor-package","source":"Tools/StyleCandidates/ACGImporter","usage":"Packages/manifest.json"},
      {"name":"UnityMeshSimplifier","state":"installed-production-package","source":"Tools/CoastSlice/UnityMeshSimplifier","usage":"Packages/manifest.json"},
      {"name":"VolumetricLight","state":"installed-disabled-effect","source":"Tools/StyleCandidates/VolumetricLight","usage":"Packages/manifest.json","note":"保留已安装源码，不启用此前停用的体积光。"},
      {"name":"StylizedTrees","state":"evaluated-not-adopted","source":"Tools/StyleCandidates/StylizedTrees","usage":None},
      {"name":"Vertex Blend Triplanar","state":"evaluated-source-retained","source":"Tools/StyleCandidates/VertexBlend","usage":None}
    ]
    for item in deps:
        if not (ROOT/item["source"]).exists():raise RuntimeError("Evidence missing: "+item["source"])
        if item["usage"] and not (ROOT/item["usage"]).exists():raise RuntimeError("Usage path missing: "+item["usage"])
    data={"date":"2026-10-28","scope":"Existing integrations and installed tool sources in the actual Unity project; no new gameplay claim.","dependencies":deps,"skillSourceSnapshots":snapshots,"submodules":audit["nestedRepositories"],"candidateOnly":["Crest","FusionWater","Yarn Spinner","UI Extensions","FairyGUI"],"githubPushed":False}
    (HERE/"integrations.lock.json").write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    doc="""# GitHub内容实际接入与版本保存 · 2026-10-28

实际工程已建立本地主仓库。Assets、Packages、ProjectSettings、制作入口、冻结输入、许可与固定版本一起纳入版本管理。
这次整理的是实际源码与依赖，不把历史调研候选算作已安装，不改动现有水色、红枫、海带、旧版程序和玩家存档。

## 还原工程

需要安装 Git、Git LFS 和对应 Unity 2022.3 编辑器。克隆主仓库时使用 --recurse-submodules；然后运行 git lfs pull。
已有克隆执行 git submodule update --init --recursive 和 git lfs pull。
五个上游子仓库均固定在 gitlink 提交，.gitmodules 保存公开来源，避免只保存空目录。
Unity包引用保持原有本地相对路径，不升级或替换已验证的依赖。

## 实际状态

| 内容 | 仓库中保存的内容 | 应用边界 |
|---|---|---|
| BoatAttack Water | 波浪源码、贴图、许可、来源提交 | 已选择性接入旧水面；v22增量为项目代码 |
| Tiled Directional Flow | 原出处、许可和URP适配shader | 旧Ocean水面已调用，不能代称v22新shader |
| FastNoiseLite | C#源码、许可、固定提交 | 地形/礁面噪声调用 |
| NVJOB/Boids | 来源、许可、复用网格 | 复用海鸟资产，非完整生态模拟 |
| Lucide | 离线图标、版本和许可 | 原有界面图标 |
| ACG Importer | 固定子仓库与UPM引用 | 已安装编辑器包 |
| UnityMeshSimplifier | 固定子仓库与UPM引用 | 已安装制作工具包 |
| VolumetricLight | 固定子仓库与UPM引用 | 已安装，效果维持停用 |
| StylizedTrees / VertexBlend | 固定子仓库 | 已试用/保留参考，不声称最新场景采用 |
| 三个美术技能 | Tools/AgentSkillSources原文件副本和来源锁定 | 已有本机技能，不是游戏运行组件 |

Crest、FusionWater、Yarn Spinner、UI Extensions、FairyGUI仍为历史候选；本次不将其改写为已安装。
完整文件证据与状态见 Tools/RepositoryIntegration/integrations.lock.json。
Blender Rock Generator与珊瑚生长工具保留在 Tools/CoastArtV12、CoastArtV13，连同各自源码、许可和冻结输入保存。

## 验证与边界

运行 Tools/RepositoryIntegration/verify_repository.py 核对本地包、子仓库提交、技能副本和主仓库跟踪状态。
大型Unity资产使用Git LFS；构建程序、缓存、重复备份和历史录帧保留在原硬盘，不重复提交。
本地提交与GitHub推送分别验收。没有远端提交SHA前，不声称已保存到GitHub。
"""
    (ROOT/"Docs/GitHub内容接入-20261028.md").write_text(doc,encoding="utf-8")
    readme=ROOT/"README.md";original=readme.read_text(encoding="utf-8-sig")
    banner="""<!-- repository-integration-20261028 -->
> 当前生活试玩入口为 Builds/SeaLifeWaterV22/Guichao-SeaWater.exe，场景 Assets/Deepsea/Scenes/CoastSeaLifeWaterV22.unity。
> 本地主仓库、五个固定上游子仓库与Git LFS已建立。首次克隆需 --recurse-submodules，再执行 git lfs pull。
> 实际接入与候选状态见 [GitHub内容接入记录](Docs/GitHub内容接入-20261028.md)。以下旧玩法说明为历史版本，不代表已重新接入当前生活切片。

"""
    if "<!-- repository-integration-20261028 -->" not in original:readme.write_text(banner+original,encoding="utf-8")
    print(json.dumps({"initialized":True,"submodulesRegistered":len(modules)//3,"skillSnapshots":snapshots,"lfsPatterns":len(set(patterns)),"mainRemoteConfigured":bool(run("git","remote"))},ensure_ascii=False,indent=2),flush=True)
if __name__=="__main__":main()
