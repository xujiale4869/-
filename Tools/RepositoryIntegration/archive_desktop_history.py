
import pathlib,os,uuid,subprocess
ROOT=pathlib.Path.home()/"Desktop"/"深海灯塔-全部版本"
MAIN=ROOT/"深海灯塔-Unity项目合集/深海灯塔-Unity海陆-v0.3"
SOURCE=ROOT/"深海灯塔－版本与视频"
allowed={".cs",".shader",".compute",".cginc",".hlsl",".json",".md",".txt",".html",".js",".css",".py",".ps1",".yml",".yaml",".meta",".unity",".mat",".asset",".asmdef",".png",".jpg",".jpeg",".glb",".fbx",".blend"}
paths=[]
for p in SOURCE.rglob("*"):
 if not p.is_file():continue
 rel=p.relative_to(SOURCE)
 if any(s.endswith("_Data") or s in {"MonoBleedingEdge","Library","Temp",".git","__pycache__"} for s in rel.parts):continue
 if p.suffix.lower() in allowed:paths.append(rel.as_posix())
def git(*args,env=None,cwd=MAIN):
 r=subprocess.run(["git",*map(str,args)],cwd=cwd,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if r.returncode:raise RuntimeError(r.stderr.decode("utf-8","replace")[-3000:])
 return r.stdout.decode("utf-8","replace").strip()
branch="archive/desktop-history"
if subprocess.run(["git","show-ref","--verify","--quiet","refs/heads/"+branch],cwd=MAIN).returncode==0:raise RuntimeError("Archive branch exists")
attrs=SOURCE/".gitattributes"
if not attrs.exists():attrs.write_bytes((MAIN/".gitattributes").read_bytes())
paths.append(".gitattributes")
idx=MAIN/".git"/("history-index-"+uuid.uuid4().hex)
env=dict(os.environ,GIT_INDEX_FILE=str(idx))
args=["--git-dir="+str(MAIN/".git"),"--work-tree="+str(SOURCE)]
try:
 git(*args,"read-tree","--empty",cwd=SOURCE,env=env)
 for offset in range(0,len(paths),30):git(*args,"add","--",*paths[offset:offset+30],cwd=SOURCE,env=env)
 tree=git(*args,"write-tree",cwd=SOURCE,env=env)
 commit=git("commit-tree",tree,"-m","archive: preserve expanded desktop history sources and documents")
 git("update-ref","refs/heads/"+branch,commit)
 print(branch,commit,"files",len(paths),flush=True)
finally:idx.unlink(missing_ok=True)
