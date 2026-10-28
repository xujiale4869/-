"""Verify the actual repository, fixed upstream checkouts, and local Unity packages."""
import hashlib, json, pathlib, subprocess, sys
ROOT=pathlib.Path(__file__).resolve().parents[2]
def git(*args):
    p=subprocess.run(["git","-C",str(ROOT),*args],capture_output=True,text=True,encoding="utf-8",errors="replace")
    if p.returncode:raise RuntimeError(p.stderr[-2000:])
    return p.stdout.strip()
def digest(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda:f.read(1048576),b""):h.update(chunk)
    return h.hexdigest()
def main():
    report={"passed":False,"checks":[],"failures":[],"scope":"Repository restoration and dependency evidence; not new gameplay validation"}
    def check(ok,label):
        report["checks"].append({"label":label,"passed":bool(ok)})
        if not ok:report["failures"].append(label)
    manifest=json.loads((ROOT/"Packages/manifest.json").read_text(encoding="utf-8-sig"))
    for name,version in manifest["dependencies"].items():
        if version.startswith("file:"):
            path=(ROOT/"Packages"/version[5:]).resolve()
            check(path.is_dir() and (path/"package.json").is_file(),"Local Unity package exists: "+name)
    lock=json.loads((ROOT/"Tools/RepositoryIntegration/integrations.lock.json").read_text(encoding="utf-8"))
    index={}
    for line in git("ls-files","--stage").splitlines():
        fields,path=line.split("\t",1); mode,obj,stage=fields.split()
        index[path]=(mode,obj)
    for module in lock["submodules"]:
        mode,obj=index.get(module["path"],("",""))
        check(mode=="160000" and obj==module["sha"],"Fixed registered submodule: "+module["path"])
        head=subprocess.run(["git","-C",str(ROOT/module["path"]),"rev-parse","HEAD"],capture_output=True,text=True).stdout.strip()
        check(head==module["sha"],"Installed checkout matches: "+module["path"])
    check(sum(mode=="160000" for mode,obj in index.values())==len(lock["submodules"]),"No unregistered embedded repositories")
    for item in lock["dependencies"]:
        check((ROOT/item["source"]).exists(),"Dependency source retained: "+item["name"])
        if item.get("usage"):check((ROOT/item["usage"]).exists(),"Actual integration path exists: "+item["name"])
    sources=json.loads((ROOT/"Tools/AgentSkillSources/sources.lock.json").read_text(encoding="utf-8"))
    for item in sources:
        directory=ROOT/item["snapshot"]
        files=[p for p in directory.rglob("*") if p.is_file() and "__pycache__" not in p.parts]
        check(len(files)==item["files"],"Complete skill source snapshot: "+item["name"])
        check((directory/"SKILL.md").is_file(),"Skill entry retained: "+item["name"])
        check(all((directory/rel).is_file() and digest(directory/rel)==expected for rel,expected in item.get("filesSha256",{}).items()),"Skill snapshot file hashes match: "+item["name"])
    for path in ("Assets/Deepsea/Scripts/Runtime/SeaLifeV21/SeaLifeRuntime.cs","Assets/Deepsea/Scripts/Runtime/SeaLifeWaterV22/SeaLifeWaterV22.cs","Assets/Deepsea/Scenes/CoastSeaLifeWaterV22.unity","Packages/manifest.json","ProjectSettings/ProjectVersion.txt","Tools/SeaLifeWaterV22/Rebuild-Water.ps1","Tools/AgentSkillSources/sources.lock.json",".gitmodules",".gitattributes"):
        check(path in index,"Tracked in main repository: "+path)
    oids=sorted(set(obj for mode,obj in index.values() if mode!="160000"))
    p=subprocess.run(["git","-C",str(ROOT),"cat-file","--batch-check=%(objectsize)"],input="\n".join(oids)+"\n",capture_output=True,text=True)
    sizes=[int(x) for x in p.stdout.splitlines() if x.isdigit()]
    report["trackedEntries"]=len(index); report["largestGitBlobBytes"]=max(sizes,default=0)
    check(len(sizes)==len(oids) and all(x<100000000 for x in sizes),"Large assets are LFS pointers rather than oversized Git blobs")
    report["passed"]=not report["failures"]
    target=ROOT/"Verification/repository-integration/repository-check.json"
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"passed":report["passed"],"checks":len(report["checks"]),"trackedEntries":len(index),"largestGitBlobBytes":report["largestGitBlobBytes"],"failures":report["failures"]},ensure_ascii=False,indent=2))
    return 0 if report["passed"] else 2
if __name__=="__main__":sys.exit(main())
