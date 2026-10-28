from pathlib import Path
import os
P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'))
path=P/'Assets/Deepsea/Scripts/Runtime/CoastShelfShape.cs'
s=path.read_text(encoding='utf-8')
s=s.replace('float top=(far?-4.2f:-2.9f)+broad.GetNoise(z,far?73:113)*.85f;', '''float saddle=3.4f*Mathf.Exp(-Mathf.Pow((z-91.5f)/4.1f,4))+2.5f*Mathf.Exp(-Mathf.Pow((z-114.5f)/3.8f,4));
  float top=(far?-4.2f:-2.45f)+broad.GetNoise(z,far?73:113)*1.2f-saddle;''')
s=s.replace('weight=S(-9.0f,-5.5f,raw)', 'weight=S(-9.2f,-3.3f,raw)')
s=s.replace('float w=Mathf.Exp(-d*5.5f);','float w=Mathf.Exp(-d*14f);')
path.write_text(s,encoding='utf-8')
path=P/'Assets/Deepsea/Shaders/CoastHabitatGround.shader'
s=path.read_text(encoding='utf-8')
# Rejected cliff-material experiments produced flat pale faces. Keep the stable v14
# dry-ground material construction; only its overly yellow cover tint is reduced.
base=(P/'Tools/CoastArtV15/Before-20261024/Assets/Deepsea/Shaders/CoastHabitatGround.shader').read_text(encoding='utf-8-sig')
start=base.index(' if(i.world.y>.12');end=base.index(' }else{',start)
branch=base[start:end].replace('half3(.30,.48,.14)','half3(.29,.365,.17)')
start=s.index(' if(i.world.y>.12');end=s.index(' }else{',start)
s=s[:start]+branch+s[end:]
path.write_text(s,encoding='utf-8')
print('Broad saddle breaks and graded landward shoulder')
