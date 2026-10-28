
import os,pathlib,subprocess,json,uuid
ROOT=pathlib.Path.home()/"Desktop"/"深海灯塔-全部版本"
UNITY=ROOT/"深海灯塔-Unity项目合集"
MAIN=UNITY/"深海灯塔-Unity海陆-v0.3"
def git(*args,cwd=MAIN,env=None):
 p=subprocess.run(["git",*map(str,args)],cwd=cwd,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if p.returncode: raise RuntimeError(p.stderr.decode("utf-8","replace")[-4000:])
 return p.stdout.decode("utf-8","replace").strip()
EXTRA="""
# Collaboration bootstrap: generated caches and local outputs.
/[Bb]uild/
/[Bb]uilds/
/[Mm]emoryCaptures/
/[Rr]ecordings/
.vs/
.vscode/
.gradle/
*.unityproj
*.suo
*.userprefs
*.tmp
*.pdb
*.mdb
*.opendb
*.VC.db
*.apk
*.aab
*.unitypackage
*.app
*.exe
*.log
__pycache__/
*.pyc
.env
.env.*
!.env.example
auth.json
credentials.json
!/[Aa]ssets/**/*.meta
"""
DIRS=["Art","Audio","Prefabs/Boat","Prefabs/Buildings","Prefabs/Environment","Prefabs/UI","Data/Fish","Data/Items","Data/Buildings","Scenes","Scripts/Core","Scripts/Sailing","Scripts/Fishing","Scripts/Inventory","Scripts/Building","Scripts/UI"]
for name in ["深海灯塔-Unity浅海-v0.1","深海灯塔-Unity海陆-v0.2","深海灯塔-Unity海陆-v0.3"]:
 q=UNITY/name
 assert (q/"Assets").is_dir() and (q/"ProjectSettings").is_dir()
 ig=q/".gitignore"
 text=ig.read_text(encoding="utf-8-sig") if ig.exists() else ""
 if "# Collaboration bootstrap:" not in text: ig.write_text(text+"\n"+EXTRA,encoding="utf-8")
 for filename in [".gitattributes","ARCHITECTURE.md"]:
  if not (q/filename).exists(): (q/filename).write_bytes((MAIN/filename).read_bytes())
 es=q/"ProjectSettings/EditorSettings.asset"
 data=es.read_bytes()
 import re
 new=re.sub(rb"m_SerializationMode:\s*\d+",b"m_SerializationMode: 2",data)
 if new!=data: es.write_bytes(new)
 for rel in DIRS:
  f=q/"Assets/_Project"/rel;f.mkdir(parents=True,exist_ok=True)
  if not any(f.iterdir()): (f/".gitkeep").touch()
  while f!=q/"Assets":
   meta=f.with_name(f.name+".meta")
   if not meta.exists():meta.write_text("fileFormatVersion: 2\nguid: "+uuid.uuid4().hex+"\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding="utf-8")
   f=f.parent
 print("Prepared:",name,flush=True)
LANTERN=ROOT/"LanternWard-v0.1"
if not (LANTERN/".gitignore").exists():(LANTERN/".gitignore").write_text(EXTRA,encoding="utf-8")
if not (LANTERN/".gitattributes").exists():(LANTERN/".gitattributes").write_text("* -text\n*.png filter=lfs diff=lfs merge=lfs -text\n",encoding="utf-8")
archives=[("archive/unity-v0.1",UNITY/"深海灯塔-Unity浅海-v0.1"),("archive/unity-v0.2",UNITY/"深海灯塔-Unity海陆-v0.2"),("archive/lanternward-v0.1",LANTERN)]
report=[]
for branch,source in archives:
 existing=subprocess.run(["git","show-ref","--verify","--quiet","refs/heads/"+branch],cwd=MAIN).returncode==0
 if existing:
  print("Already archived:",branch,flush=True)
  continue
 index=MAIN/".git"/("archive-index-"+uuid.uuid4().hex)
 env=dict(os.environ,GIT_INDEX_FILE=str(index))
 args=["--git-dir="+str(MAIN/".git"),"--work-tree="+str(source)]
 try:
  git(*args,"read-tree","--empty",cwd=source,env=env)
  git(*args,"add","--all",cwd=source,env=env)
  tree=git(*args,"write-tree",cwd=source,env=env)
  commit=git("commit-tree",tree,"-m","archive: preserve "+branch+" source snapshot")
  git("update-ref","refs/heads/"+branch,commit)
  report.append(dict(branch=branch,commit=commit,source=str(source)))
  print(branch,commit,flush=True)
 finally:
  index.unlink(missing_ok=True)
(MAIN/"Docs/AllVersions-Archive.json").write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding="utf-8")
print(git("check-ignore","Library/","Temp/","Builds/"),flush=True)
print(git("add","--all"),flush=True)
subprocess.run(["python",str(MAIN/"Tools/RepositoryIntegration/verify_repository.py")],cwd=MAIN,check=True)
print("PREPARED",flush=True)
