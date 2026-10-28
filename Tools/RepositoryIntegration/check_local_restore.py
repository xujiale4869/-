"""Restore from local Git + LFS object stores; never contacts GitHub or edits the source project."""
import datetime,json,os,pathlib,shutil,stat,subprocess,sys,tempfile
ROOT=pathlib.Path(__file__).resolve().parents[2]
env=dict(os.environ,GIT_LFS_SKIP_SMUDGE="1",GIT_TERMINAL_PROMPT="0")
temp=pathlib.Path(tempfile.mkdtemp(prefix="deepsea-repository-restore-"))
clone=temp/"project"
report={"passed":False,"source":"local Git and local LFS objects; GitHub upload is not verified","checks":[]}
def run(cwd,*args):
    p=subprocess.run(list(args),cwd=cwd,capture_output=True,text=True,encoding="utf-8",errors="replace",env=env)
    if p.returncode:raise RuntimeError(str(args[:3])+": "+(p.stdout+p.stderr)[-2500:])
    return p.stdout.strip()
try:
    report["commit"]=run(ROOT,"git","rev-parse","HEAD")
    print("Cloning committed source locally...",flush=True)
    run(temp,"git","clone","--no-hardlinks",str(ROOT),str(clone))
    report["checks"].append("Main source cloned from committed objects")
    lock=json.loads((clone/"Tools/RepositoryIntegration/integrations.lock.json").read_text(encoding="utf-8"))
    for item in lock["submodules"]:
        run(clone,"git","config","submodule."+item["path"]+".url",str(ROOT/item["path"]))
    print("Restoring five fixed upstream checkouts from local copies...",flush=True)
    run(clone,"git","-c","protocol.file.allow=always","submodule","update","--init","--recursive")
    report["checks"].append("Five fixed upstream checkouts restored")
    print("Restoring LFS asset bytes...",flush=True)
    shutil.copytree(ROOT/".git/lfs/objects",clone/".git/lfs/objects",dirs_exist_ok=True)
    run(clone,"git","lfs","checkout")
    run(clone,"git","lfs","fsck","--objects")
    report["checks"].append("LFS assets restored and object hashes verified")
    if run(clone,"git","diff","--name-only"):raise RuntimeError("Restored tracked bytes differ from the commit")
    report["checks"].append("Restored tracked bytes match the commit")
    summary=run(clone,sys.executable,"Tools/RepositoryIntegration/verify_repository.py")
    repository=json.loads(summary)
    if not repository["passed"]:raise RuntimeError("Restored dependency check failed")
    report["repositoryChecks"]=repository["checks"]
    report["checks"].append("Restored dependency and skill snapshot validation passed")
    report["passed"]=True
except Exception as ex:
    report["error"]=str(ex)
finally:
    def force_remove(func,path,exc):
        os.chmod(path,stat.S_IWRITE);func(path)
    shutil.rmtree(temp,onerror=force_remove)
    report["temporaryCloneRemoved"]=not temp.exists()
    target=ROOT/"Verification/repository-integration/local-restore.json"
    target.write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False,indent=2),flush=True)
sys.exit(0 if report["passed"] else 2)
