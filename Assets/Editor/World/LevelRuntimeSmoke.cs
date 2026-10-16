using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rustbound.World;

[InitializeOnLoad]
public static class LevelRuntimeSmoke
{
    static double readyAt;
    static LevelRuntimeSmoke(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpLayoutRuntime")>=0)EditorApplication.update+=Poll;}
    public static void Run(){SessionState.SetBool("LayoutSmoke",true);SessionState.SetInt("LayoutSmokeLevel",1);EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");EditorApplication.isPlaying=true;}
    static void Poll()
    {
        if(!SessionState.GetBool("LayoutSmoke",false)||!EditorApplication.isPlaying)return;
        if(readyAt==0){readyAt=EditorApplication.timeSinceStartup+2;return;}if(EditorApplication.timeSinceStartup<readyAt)return;
        try
        {
            int id=SessionState.GetInt("LayoutSmokeLevel",1);var session=LevelSession.Current;
            if(session==null||session.level!=id||!session.Playing||session.hud==null||session.player==null||!session.player.Grounded)throw new Exception("Level "+id+" start/session/HUD/grounding");
            if(session.Total!=(id==1?13:id==2?17:10)||session.BatteryTotal!=(id==2?2:1))throw new Exception("Level "+id+" pickup totals "+session.Total+"/"+session.BatteryTotal);
            if(UnityEngine.Object.FindObjectOfType<WorldCamera>()==null||GameObject.Find("GameViewCamera")==null)throw new Exception("Missing gameplay camera");
            Debug.Log("LAYOUT_RUNTIME_LEVEL_OK "+id);
            if(id<3){SessionState.SetInt("LayoutSmokeLevel",id+1);SceneManager.LoadScene("Level0"+(id+1));readyAt=EditorApplication.timeSinceStartup+2;return;}
            Directory.CreateDirectory("Artifacts/World");File.WriteAllText("Artifacts/World/layout-runtime-validation.txt","Three scenes start with grounded controllable player, independent HUD, camera, crystal totals 13/17/10, chargers 1/2/1.\n");
            SessionState.SetBool("LayoutSmoke",false);Debug.Log("LEVEL_LAYOUT_RUNTIME_OK");EditorApplication.Exit(0);
        }
        catch(Exception error){SessionState.SetBool("LayoutSmoke",false);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
