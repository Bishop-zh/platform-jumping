using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Rustbound.UI;
using Rustbound.World;

public static class PauseHudRevision
{
    public static void Run()
    {
        AssetDatabase.Refresh();
        const string hudOld="Assets/Prefabs/World/GameplayHUD.prefab",hudNew="Assets/Prefabs/UI/GameplayHUD.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(hudNew)==null)
        {
            string error=AssetDatabase.MoveAsset(hudOld,hudNew);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var hud=PrefabUtility.LoadPrefabContents(hudNew);
        try {if(hud.GetComponent<FullScreenCanvas>()==null)hud.AddComponent<FullScreenCanvas>();PrefabUtility.SaveAsPrefabAsset(hud,hudNew);}
        finally {PrefabUtility.UnloadPrefabContents(hud);}

        const string fontPath="Assets/Art/UI/Fonts/PauseChinese.asset";
        Directory.CreateDirectory("Assets/Art/UI/Fonts");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(font==null)
        {
            const string temporaryFont="Assets/Editor/PauseFontSource.ttf";
            File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"simhei.ttf"),temporaryFont,true);
            AssetDatabase.ImportAsset(temporaryFont,ImportAssetOptions.ForceSynchronousImport);
            var systemFont=AssetDatabase.LoadAssetAtPath<Font>(temporaryFont);
            font=TMP_FontAsset.CreateFontAsset(systemFont,96,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,512,512,AtlasPopulationMode.Dynamic);
            font.name="PauseChinese";
            if(!font.TryAddCharacters("暂停退出主菜单",out string missing))throw new Exception("Missing Chinese glyphs: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            var serializedFont=new SerializedObject(font);serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue=null;serializedFont.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(font,fontPath);
            foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
            AssetDatabase.AddObjectToAsset(font.material,font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();AssetDatabase.DeleteAsset(temporaryFont);
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/SharedModal.prefab");
        try
        {
            root.name="GameplayPauseModal";
            var view=root.GetComponent<ModalDialogView>();
            var settings=root.transform.Find("Composition/SettingsContent");
            var header=root.transform.Find("Composition/HeaderTitle");
            var pause=Text(header.parent,"PauseTitle",font,"暂停",74);
            CopyRect(header.GetComponent<RectTransform>(),pause.rectTransform);
            pause.rectTransform.sizeDelta=new Vector2(323,90);
            pause.rectTransform.anchoredPosition=new Vector2(747,-177)+Vector2.Scale(new Vector2(178,74)-pause.rectTransform.sizeDelta,new Vector2(.5f,.5f)-pause.rectTransform.pivot);
            view.ConfigurePauseTitle(pause);
            var prototype=root.transform.Find("Composition/VictoryContent/ReturnLevels");
            var exit=UnityEngine.Object.Instantiate(prototype.gameObject,settings,false);exit.name="ReturnMainMenu";
            exit.GetComponent<ModalActionRelay>().Configure(view,ModalAction.ReturnMainMenu);
            UnityEngine.Object.DestroyImmediate(exit.transform.Find("Label").gameObject);
            var label=Text(exit.transform,"Label",font,"退出主菜单",34);
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=new Vector2(12,5);label.rectTransform.offsetMax=new Vector2(-12,-5);
            var buttons=new[]{settings.Find("RestoreDefaults"),settings.Find("Return"),exit.transform};
            var first=(RectTransform)buttons[0];var second=(RectTransform)buttons[1];
            float left=first.anchoredPosition.x-first.pivot.x*first.sizeDelta.x;
            float right=second.anchoredPosition.x+(1-second.pivot.x)*second.sizeDelta.x;
            float width=(right-left-24)/3;float y=first.anchoredPosition.y;
            for(int i=0;i<3;i++)
            {
                var rect=(RectTransform)buttons[i];rect.sizeDelta=new Vector2(width,first.sizeDelta.y);
                rect.anchoredPosition=new Vector2(left+i*(width+12)+rect.pivot.x*width,y);
            }
            view.Show(ModalMode.Settings);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/UI/GameplayPauseModal.prefab");
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
        var bank=AssetDatabase.LoadAssetAtPath<GameRuntimeAssets>("Assets/Resources/GameRuntimeAssets.asset");
        bank.pauseModal=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GameplayPauseModal.prefab").GetComponent<ModalDialogView>();
        bank.gameplayHud=AssetDatabase.LoadAssetAtPath<GameObject>(hudNew).GetComponent<WorldHud>();
        EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();Debug.Log("PAUSE_HUD_REVISION_OK");
    }
    static TextMeshProUGUI Text(Transform parent,string name,TMP_FontAsset font,string value,float size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;
        text.alignment=TextAlignmentOptions.Center;text.color=new Color32(255,227,181,255);
        text.raycastTarget=false;text.fontStyle=FontStyles.Bold;text.enableWordWrapping=false;
        return text;
    }
    static void CopyRect(RectTransform source,RectTransform target)
    {target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=source.pivot;target.sizeDelta=source.sizeDelta;target.anchoredPosition=source.anchoredPosition;}
}
