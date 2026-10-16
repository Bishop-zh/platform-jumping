using UnityEditor;
using UnityEngine;
using Rustbound.UI;
public static class MainMenuActorsSetup
{
    public static void Configure()
    {
        string menuPath="Assets/Prefabs/UI/MainMenu.prefab",actorPath="Assets/Prefabs/UI/MainMenuBackdropActors.prefab";
        var root=PrefabUtility.LoadPrefabContents(menuPath);
        try
        {
            var composition=root.transform.Find("Composition");
            if(composition.GetComponentInChildren<MainMenuBackdropActors>(true)!=null){Debug.Log("ACTORS_ALREADY_CONFIGURED");return;}
            var robot=composition.Find("Robot");var station=composition.Find("ChargingStation");
            if(robot==null||station==null)throw new System.Exception("Expected editable main-menu actor images");
            var go=new GameObject("MainMenuBackdropActors",typeof(RectTransform),typeof(MainMenuBackdropActors));
            var rect=go.GetComponent<RectTransform>();rect.SetParent(composition,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=((RectTransform)composition).sizeDelta;
            robot.SetParent(rect,false);station.SetParent(rect,false);
            var actors=go.GetComponent<MainMenuBackdropActors>();actors.robot=robot as RectTransform;actors.chargingStation=station as RectTransform;actors.AlignFeet();
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,actorPath);Object.DestroyImmediate(go);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,composition);instance.name="MainMenuBackdropActors";
            var serialized=new SerializedObject(root.GetComponent<MainMenuView>());serialized.FindProperty("backdropActorsPrefab").objectReferenceValue=prefab.GetComponent<MainMenuBackdropActors>();serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,menuPath);Debug.Log("MAIN_MENU_ACTORS_SETUP_PASSED");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void ConfigureAndValidate(){Configure();AudioSetup.ConfigureAndValidate();}
}
