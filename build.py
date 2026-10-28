#!/usr/bin/env python3
"""Assemble the self-contained game and embed it in a Windows launcher."""
from pathlib import Path
import subprocess, os, struct
ROOT=Path(__file__).resolve().parent
src=ROOT/'src'; dist=ROOT/'dist'; dist.mkdir(exist_ok=True)
html=(src/'index.html').read_text()
for marker,name in [('CSS','style.css'),('ENGINE','engine.js'),('GAME','game.js')]:
    html=html.replace('/*__'+marker+'__*/',(src/name).read_text())
(dist/'LanternWard.html').write_text(html,encoding='utf-8')
print('HTML:',len(html.encode('utf-8')),'bytes')
if os.environ.get('MINGW_CC'):
    # RCDATA contains the complete UTF-8 game, so the EXE is portable by itself.
    (src/'game.rc').write_text('101 RCDATA "../dist/LanternWard.html"\n1 ICON "icon.ico"\n1 VERSIONINFO\nFILEVERSION 0,1,0,0\nPRODUCTVERSION 0,1,0,0\nFILEOS 0x40004\nFILETYPE 1\nBEGIN\n BLOCK "StringFileInfo"\n BEGIN\n BLOCK "040904b0"\n BEGIN\n VALUE "FileDescription", "Lantern Ward - Original Tower Defense\\0"\n VALUE "ProductName", "Lantern Ward\\0"\n VALUE "ProductVersion", "0.1.0\\0"\n END\n END\n BLOCK "VarFileInfo"\n BEGIN\n VALUE "Translation", 0x409, 1200\n END\nEND\n')
    windres=os.environ['MINGW_WINDRES']
    subprocess.run([windres,'game.rc','-O','coff','-o','game.res'],cwd=src,check=True)
    subprocess.run([os.environ['MINGW_CC'],'-Os','-s','-static','-municode','-mwindows','-Wall','-Wextra','launcher.c','game.res','-o',str(dist/'LanternWard.exe'),'-lshell32'],cwd=src,check=True)
    print('EXE:',(dist/'LanternWard.exe').stat().st_size,'bytes')
