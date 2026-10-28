from pathlib import Path
import struct
ROOT=Path(__file__).resolve().parents[1]
b=(ROOT/'dist/LanternWard.exe').read_bytes()
def u16(o): return struct.unpack_from('<H',b,o)[0]
def u32(o): return struct.unpack_from('<I',b,o)[0]
assert b[:2]==b'MZ';pe=u32(60);assert b[pe:pe+4]==b'PE\0\0'
assert u16(pe+4)==0x8664
optional=pe+24;assert u16(optional)==0x20b;assert u16(optional+68)==2
section_count=u16(pe+6);section_base=optional+u16(pe+20)
sections=[]
for i in range(section_count):
 p=section_base+i*40;sections.append((u32(p+12),u32(p+8),u32(p+20),u32(p+16)))
def offset(rva):
 for va,vs,raw,rs in sections:
  if va<=rva<va+max(vs,rs):return raw+rva-va
 raise ValueError('RVA not mapped')
resource=offset(u32(optional+112+2*8))
def entries(rel):
 p=resource+rel;n=u16(p+12)+u16(p+14)
 return [(u32(p+16+i*8),u32(p+20+i*8)) for i in range(n)]
def descend(rel,id):
 matches=[v for k,v in entries(rel) if k==id];assert len(matches)==1
 assert matches[0]&0x80000000
 return matches[0]&0x7fffffff
raw_dir=descend(descend(0,10),101)
lang_entries=entries(raw_dir);assert len(lang_entries)==1
_,data=lang_entries[0];assert not data&0x80000000
p=resource+data;blob=b[offset(u32(p)):offset(u32(p))+u32(p+4)]
assert blob==(ROOT/'dist/LanternWard.html').read_bytes()
assert 3 in [k for k,v in entries(0)] and 14 in [k for k,v in entries(0)]
imports=offset(u32(optional+112+8));dlls=[]
while u32(imports) or u32(imports+12):
 p=offset(u32(imports+12));end=b.index(b'\0',p);dlls.append(b[p:end].decode().lower());imports+=20
assert set(dlls)<={'kernel32.dll','msvcrt.dll','shell32.dll','user32.dll'}
print('PASS Windows x64 GUI PE, icon resources, embedded HTML byte-for-byte, standard system DLL dependencies:',', '.join(dlls))
