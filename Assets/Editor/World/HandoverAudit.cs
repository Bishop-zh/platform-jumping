using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class HandoverAudit
{
    [Serializable]class Report {public string[] scenes,used,reusable,all;}
    public static void Run()
    {
        Export();EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;
    }
    public static void Export()
    {
        AssetDatabase.Refresh();Directory.CreateDirectory("Artifacts/Handover");
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        if(scenes.Length!=4||scenes.Any(s=>AssetDatabase.LoadAssetAtPath<SceneAsset>(s)==null))throw new Exception("Expected MainMenu and three enabled map scenes");
        var runtime=AssetDatabase.FindAssets("",new[]{"Assets/Resources"}).Select(AssetDatabase.GUIDToAssetPath).Where(s=>!AssetDatabase.IsValidFolder(s)).ToArray();
        var used=AssetDatabase.GetDependencies(scenes.Concat(runtime).ToArray(),true).Where(s=>s.StartsWith("Assets/")).Distinct().ToArray();
        var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/World"}).Select(AssetDatabase.GUIDToAssetPath).Where(s=>!used.Contains(s)&&!s.EndsWith("/Battery.prefab")).ToArray();
        var reusable=AssetDatabase.GetDependencies(prefabs,true).Where(s=>s.StartsWith("Assets/")&&!used.Contains(s)).Distinct().ToArray();
        var all=AssetDatabase.GetAllAssetPaths().Where(s=>s.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(s)).ToArray();
        File.WriteAllText("Artifacts/Handover/dependencies.json",JsonUtility.ToJson(new Report{scenes=scenes,used=used,reusable=reusable,all=all},true));
        Debug.Log("HANDOVER_DEPENDENCIES_OK");
    }
}
