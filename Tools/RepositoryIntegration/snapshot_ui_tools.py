"""Archive two previously installed UI tools without changing their installation."""
import hashlib,json,pathlib,shutil
ROOT=pathlib.Path(__file__).resolve().parents[2]
folder=ROOT/"Tools/AgentSkillSources"
lock=folder/"sources.lock.json"
items=json.loads(lock.read_text(encoding="utf-8"))
for name in ("impeccable","ui-ux-pro-max"):
    src=pathlib.Path("C:/Users/许嘉乐/.agents/skills")/name
    dst=folder/name
    if dst.exists():raise RuntimeError("Existing snapshot requires manual review: "+name)
    shutil.copytree(src,dst,ignore=shutil.ignore_patterns(".git","__pycache__","*.pyc",".env",".env.*","auth.json","credentials.json"))
    files=[p for p in dst.rglob("*") if p.is_file()]
    for p in files:
        if hashlib.sha256(p.read_bytes()).digest()!=hashlib.sha256((src/p.relative_to(dst)).read_bytes()).digest():raise RuntimeError("Snapshot differs: "+str(p))
    items.append({"name":name,"repository":None,"commit":None,"source":"Previously installed local skill; upstream revision not established in this pass","snapshot":dst.relative_to(ROOT).as_posix(),"files":len(files),"installedAlready":True,"installedFilesChanged":False})
for item in items:
    item["filesSha256"]={p.relative_to(ROOT/item["snapshot"]).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/item["snapshot"]).rglob("*") if p.is_file()}
lock.write_text(json.dumps(items,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
main=ROOT/"Tools/RepositoryIntegration/integrations.lock.json"
data=json.loads(main.read_text(encoding="utf-8"));data["skillSourceSnapshots"]=items
main.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
for path in (ROOT/"Docs/GitHub内容接入-20261028.md",folder/"README.md"):
    text=path.read_text(encoding="utf-8").replace("三个美术技能","五个美术与UI技能").replace("此前已安装的三个技能","此前已安装的五个技能")
    text+="\nImpeccable与UI UX Pro Max也保存了现有安装的原文件副本；原上游提交本轮未确定，来源锁中明确为空，不伪造固定版本。五个工具快照逐文件保存SHA256。\n"
    path.write_text(text,encoding="utf-8")
print(json.dumps({"skillSnapshots":len(items),"files":sum(x["files"] for x in items),"installedFilesChanged":False},ensure_ascii=False))
