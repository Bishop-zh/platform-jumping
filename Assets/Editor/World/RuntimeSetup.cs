using System.IO;
using UnityEditor;
using UnityEngine;
using Rustbound.World;
using Rustbound.UI;
public static class RuntimeSetup
{
    [MenuItem("Rustbound/运行准备/绑定共享 UI 并移除地图内 HUD")]
    public static void Configure()
    {
        Directory.CreateDirectory("Assets/Resources");
        var assets=AssetDatabase.LoadAssetAtPath<GameRuntimeAssets>("Assets/Resources/GameRuntimeAssets.asset");
        if(assets==null){assets=ScriptableObject.CreateInstance<GameRuntimeAssets>();AssetDatabase.CreateAsset(assets,"Assets/Resources/GameRuntimeAssets.asset");}
        assets.gameplayHud=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GameplayHUD.prefab").GetComponent<WorldHud>();
        assets.modal=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SharedModal.prefab").GetComponent<ModalDialogView>();
        EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
        MainMenuBootstrap menu=null;
        foreach(var root in scene.GetRootGameObjects()){menu=root.GetComponentInChildren<MainMenuBootstrap>(true);if(menu!=null)break;}
        if(menu==null)throw new System.Exception("MainMenu bootstrap missing");
        assets=AssetDatabase.LoadAssetAtPath<GameRuntimeAssets>("Assets/Resources/GameRuntimeAssets.asset");
        assets.mainMenu=PrefabUtility.SaveAsPrefabAsset(menu.gameObject,"Assets/Resources/MainMenuRuntime.prefab").GetComponent<MainMenuBootstrap>();
        EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
        for(int i=1;i<=3;i++)
        {
            string path="Assets/Prefabs/Levels/Level0"+i+"_Map.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{var session=root.GetComponentInChildren<LevelSession>(true);session.hud=null;foreach(var hud in root.GetComponentsInChildren<WorldHud>(true))Object.DestroyImmediate(hud.gameObject);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("RUNTIME_SETUP_PASSED");
    }
    public static void ConfigureAndValidate(){Configure();HandoverAudit.Run();}
}
