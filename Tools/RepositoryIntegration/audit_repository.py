"""Inspect project dependencies and nested repositories without printing credentials."""
import json, os, subprocess, pathlib, urllib.parse
ROOT=pathlib.Path(__file__).resolve().parents[2]
def git(folder,*args):
    p=subprocess.run(["git","-C",str(folder),*args],capture_output=True,text=True,encoding="utf-8",errors="replace")
    return p.returncode,p.stdout.strip()
def safe_url(url):
    if "://" in url:
        u=urllib.parse.urlsplit(url)
        return urllib.parse.urlunsplit((u.scheme,u.hostname or "",u.path,"",""))
    return url
def main():
    repos=[]
    for base,dirs,files in os.walk(ROOT/"Tools"):
        if ".git" in dirs or ".git" in files:
            folder=pathlib.Path(base)
            _,sha=git(folder,"rev-parse","HEAD")
            _,url=git(folder,"remote","get-url","origin")
            _,dirty=git(folder,"status","--porcelain")
            repos.append({"path":folder.relative_to(ROOT).as_posix(),"sha":sha,"url":safe_url(url),"dirty":bool(dirty),"changes":dirty.splitlines()[:6]})
            if ".git" in dirs: dirs.remove(".git")
        dirs[:]=[x for x in dirs if x not in ("node_modules","__pycache__")]
    manifest=json.loads((ROOT/"Packages/manifest.json").read_text(encoding="utf-8-sig"))
    result={"root":str(ROOT),"nestedRepositories":repos,"dependencies":manifest["dependencies"]}
    path=ROOT/"Tools/RepositoryIntegration/dependency-audit-before.json"
    path.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
    print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=="__main__":main()
