using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
namespace Deepsea.Editor
{
    public static class UIPrepare
    {
        public static void Run()
        {
            string essentials="Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
            if(!Directory.Exists("Assets/TextMesh Pro/Resources"))AssetDatabase.ImportPackage(essentials,false);
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/Deepsea/Resources/UI","*.png"))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.SaveAndReimport();
            }
            const string target="Assets/Deepsea/Resources/UI/ChineseUI.asset";
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Deepsea/Resources/UI/GuichaoSans.ttf");
            if(font==null)throw new Exception("Bundled font missing");
            var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(target);
            if(asset==null)
            {
                asset=TMP_FontAsset.CreateFontAsset(font,36,5,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);asset.name="ChineseUI";asset.isMultiAtlasTexturesEnabled=true;
                AssetDatabase.CreateAsset(asset,target);AssetDatabase.AddObjectToAsset(asset.material,asset);
                foreach(var tex in asset.atlasTextures)AssetDatabase.AddObjectToAsset(tex,asset);
                string chars=string.Concat(Directory.GetFiles("Assets/Deepsea/Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
                string missing;asset.TryAddCharacters(new string(chars.Distinct().ToArray()),out missing);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();ProjectSetup.Verify();
            File.WriteAllText("Verification/ui-font-result.json","{\"font\":\"bundled GuichaoSans subset of Noto Sans SC / OFL\",\"characters\":"+asset.characterTable.Count+",\"ugui\":\"1.0.0\",\"textmeshpro\":\"3.0.6\"}");
            Debug.Log("DEEPSEA_UI_PREPARED "+asset.characterTable.Count);
        }
    }
}
