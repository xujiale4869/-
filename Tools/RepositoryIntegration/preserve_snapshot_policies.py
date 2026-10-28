"""Retain upstream Git policies as inert source files so snapshots preserve exact bytes."""
import hashlib,json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[2]
lock=ROOT/"Tools/AgentSkillSources/sources.lock.json"
items=json.loads(lock.read_text(encoding="utf-8"))
for item in items:
    folder=ROOT/item["snapshot"]
    renamed=item.setdefault("archivedPolicyFiles",{})
    for path in list(folder.rglob(".gitattributes")):
        target=path.with_name(".gitattributes.upstream")
        old=path.relative_to(folder).as_posix()
        path.rename(target)
        renamed[target.relative_to(folder).as_posix()]=old
    item["filesSha256"]={p.relative_to(folder).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in folder.rglob("*") if p.is_file() and "__pycache__" not in p.parts}
lock.write_text(json.dumps(items,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
main=ROOT/"Tools/RepositoryIntegration/integrations.lock.json"
data=json.loads(main.read_text(encoding="utf-8"));data["skillSourceSnapshots"]=items
main.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
note="\n为保留原文件字节，上游技能自带的.gitattributes以.gitattributes.upstream保存，避免其换行策略作用于档案；映射记录在sources.lock.json的archivedPolicyFiles。还原为独立技能时按映射恢复文件名，原内容未修改。\n"
for file in (ROOT/"Tools/AgentSkillSources/README.md",ROOT/"Docs/GitHub内容接入-20261028.md"):
    with file.open("a",encoding="utf-8") as f:f.write(note)
print(json.dumps({"archivedPolicies":sum(len(x.get("archivedPolicyFiles",{})) for x in items),"snapshots":len(items)}))
