using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Rustbound.UI;
using Rustbound.World;

[InitializeOnLoad]
public static class GameplayUiEditingValidation
{
    static double ready;static int phase;static RectTransform sample;static readonly List<string> checks=new List<string>();
    static GameplayUiEditingValidation(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpUiEditingValidation")>=0)EditorApplication.update+=Poll;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Level03.unity");EditorApplication.isPlaying=true;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add("PASS: "+message);}
    static void Poll()
    {
        if(!EditorApplication.isPlaying)return;if(ready==0){ready=EditorApplication.timeSinceStartup+2;return;}if(EditorApplication.timeSinceStartup<ready)return;
        try
        {
            if(phase==0)
            {
                var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GameplayHUD.prefab");
                var actual=LevelSession.Current.hud.transform.Find("Composition");var authored=template.transform.Find("Composition");
                foreach(RectTransform child in authored)
                {
                    var instance=(RectTransform)actual.Find(child.name);
                    Check(instance!=null&&instance.anchoredPosition==child.anchoredPosition&&instance.sizeDelta==child.sizeDelta,"HUD authored layout "+child.name);
                }
                LevelSession.Current.OpenSettings();
                var test=new GameObject("Offset animation test",typeof(RectTransform));test.SetActive(false);
                sample=new GameObject("Composition",typeof(RectTransform)).GetComponent<RectTransform>();sample.SetParent(test.transform,false);sample.anchoredPosition=new Vector2(31,-12);
                var animation=test.AddComponent<ModalSlideIn>();animation.Configure(sample);test.SetActive(true);animation.Play();
            }
            else if(phase==1)
            {
                Check(sample.anchoredPosition==new Vector2(31,-12),"Slide animation preserves authored offset");
                var modal=UnityEngine.Object.FindObjectOfType<ModalDialogView>();
                var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GameplayPauseModal.prefab");
                foreach(string path in new[]{"Composition","Composition/PauseTitle","Composition/SettingsContent/RestoreDefaults","Composition/SettingsContent/Return","Composition/SettingsContent/ReturnMainMenu"})
                {
                    var current=(RectTransform)modal.transform.Find(path);var authored=(RectTransform)template.transform.Find(path);
                    Check(current.anchoredPosition==authored.anchoredPosition&&current.sizeDelta==authored.sizeDelta,"Pause preserves prefab layout "+path);
                }
                modal.transform.Find("Composition/SettingsContent/ReturnMainMenu").GetComponent<Button>().onClick.Invoke();
            }
            else
            {
                Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu"&&Time.timeScale==1,"Pause Exit still returns to main menu");
                Directory.CreateDirectory("Artifacts/PauseHud");File.WriteAllLines("Artifacts/PauseHud/editing-validation.txt",checks);
                Debug.Log("UI_EDITING_VALIDATION_OK "+checks.Count);EditorApplication.Exit(0);return;
            }
            phase++;ready=EditorApplication.timeSinceStartup+1;
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
