from pathlib import Path
import os
P=Path(os.environ.get('COAST_PROJECT_ROOT',r'C:/Users/许嘉乐/Desktop/深海灯塔-Unity海陆-v0.3'));path=P/'Assets/Deepsea/Scripts/Runtime/CoastRefinementRuntime.cs'
s=path.read_text(encoding='utf-8-sig')
if 'PrepareComparison()' not in s:
    s=s.replace('  Camera view;', '  GameObject baseline;GameObject[] refinedGeometry;bool originalView;\n  Camera view;')
    s=s.replace('font=Font.CreateDynamicFontFromOSFont','PrepareComparison();font=Font.CreateDynamicFontFromOSFont')
    s=s.replace('  void SetGrey(bool value)', '''  void PrepareComparison(){
   var children=new List<GameObject>();foreach(Transform child in world)if((child.GetComponent<MeshRenderer>()!=null&&child.name!="ShowcaseWater")||child.name=="HeroLighthouse")children.Add(child.gameObject);refinedGeometry=children.ToArray();
   baseline=Instantiate(Resources.Load<GameObject>("CoastShowcase/Environment"),world,false);baseline.name="v16 baseline comparison";
   var oldTower=Instantiate(Resources.Load<GameObject>("CoastFoundation/Lighthouse"),baseline.transform);oldTower.transform.position=new Vector3(-24.4f,3.8f,4);oldTower.transform.rotation=Quaternion.Euler(0,180,0);oldTower.transform.localScale=Vector3.one*.9f;baseline.SetActive(false);
  }
  void SetComparison(bool original){originalView=original;foreach(var obj in refinedGeometry)obj.SetActive(!original);baseline.SetActive(original);if(view!=null)SetGrey(grey);}
  void SetGrey(bool value)''')
    s=s.replace('if(Input.GetKeyDown(KeyCode.F4))','if(Input.GetKeyDown(KeyCode.F7))SetComparison(!originalView);if(Input.GetKeyDown(KeyCode.F4))')
    s=s.replace('yield return Capture("reference");SetGrey(true);','SetComparison(true);yield return Capture("reference-before");SetComparison(false);yield return Capture("reference");SetGrey(true);')
    s=s.replace('size=8.6f;yield return Capture("shore");','size=8.6f;SetComparison(true);yield return Capture("shore-before");SetComparison(false);yield return Capture("shore");')
    s=s.replace('size=4.8f;yield return Capture("lighthouse");','size=4.8f;SetComparison(true);yield return Capture("lighthouse-before");SetComparison(false);yield return Capture("lighthouse");')
    s=s.replace('"雾岬岛 · 东岸",style','originalView?"雾岬岛 · v16 对照":"雾岬岛 · 东岸",style')
    s=s.replace('F4 白模   F10','F7 前后对照   F4 白模   F10')
    path.write_text(s,encoding='utf-8')
print('COMPARISON_TOGGLE_READY')
