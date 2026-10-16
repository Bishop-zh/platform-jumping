using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Rustbound.UI;
using Rustbound.World;

[InitializeOnLoad]
public static class PauseHudValidation
{
    static readonly List<string> checks=new List<string>();
    static double ready;static int phase;static Camera preview;static ModalDialogView modal;static WorldHud hud;
    const string Output="Artifacts/PauseHud/";
    static PauseHudValidation(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpPauseHudValidation")>=0)EditorApplication.update+=Poll;}
    public static void Run(){Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/Level03.unity");EditorApplication.isPlaying=true;}
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add("PASS: "+name);}
    static void Poll()
    {
        if(!EditorApplication.isPlaying)return;if(ready==0){ready=EditorApplication.timeSinceStartup+2;return;}if(EditorApplication.timeSinceStartup<ready)return;
        try
        {
            if(phase==0)
            {
                var session=LevelSession.Current;hud=session.hud;session.OpenSettings();
                modal=UnityEngine.Object.FindObjectOfType<ModalDialogView>();
                Check(!session.Playing&&Time.timeScale==0,"Gameplay settings pauses simulation");
                Check(modal.name.StartsWith("GameplayPauseModal"),"Gameplay uses separate editable pause prefab");
                Check(modal.transform.Find("Composition/PauseTitle").GetComponent<TextMeshProUGUI>().text=="暂停","Pause title uses TMP Chinese text");
                Check(!modal.transform.Find("Composition/HeaderTitle").GetComponent<Image>().enabled,"Original Settings image hidden only in pause");
                var buttons=modal.transform.Find("Composition/SettingsContent");var a=(RectTransform)buttons.Find("RestoreDefaults");var b=(RectTransform)buttons.Find("Return");var c=(RectTransform)buttons.Find("ReturnMainMenu");
                Check(Mathf.Abs(a.rect.width-b.rect.width)<.01f&&Mathf.Abs(b.rect.width-c.rect.width)<.01f,"Pause has three equal width buttons");
                Check(Mathf.Abs((b.anchoredPosition.x-a.anchoredPosition.x)-(c.anchoredPosition.x-b.anchoredPosition.x))<.01f,"Pause buttons spaced evenly");
                Check(c.GetComponentInChildren<TextMeshProUGUI>().text=="退出主菜单","Main menu button uses Chinese TMP label");
                preview=new GameObject("UI preview").AddComponent<Camera>();preview.transform.position=new Vector3(0,0,-10);preview.orthographic=true;preview.cullingMask=1<<5;preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=new Color32(90,85,67,255);
                foreach(var root in new[]{hud.gameObject,modal.gameObject})
                {
                    foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=5;
                    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=preview;canvas.planeDistance=10;
                }
                Resize(1280,720);
            }
            else if(phase==1||phase==2)
            {
                CheckBounds(hud.gameObject,"HUD");CheckBounds(modal.gameObject,"Pause");Save(phase==1?"pause-16x9.png":"pause-4x3.png");
                if(phase==1)
                {
                    UnityEngine.Object.DestroyImmediate(hud.GetComponent<FullScreenCanvas>());
                    var edited=(RectTransform)hud.transform.Find("Composition/Settings");edited.anchoredPosition+=new Vector2(120,0);
                    FullScreenCanvas.Attach(hud.gameObject);Resize(1024,768);
                    Check(true,"Simulated HUD edit beyond nominal Composition width");
                }
                else modal.transform.Find("Composition/SettingsContent/ReturnMainMenu").GetComponent<Button>().onClick.Invoke();
            }
            else
            {
                Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu","Pause Exit loads main menu");
                Check(LevelSession.Current==null&&Time.timeScale==1,"Pause Exit restores time and removes level session");
                var shared=UnityEngine.Object.Instantiate(GameRuntimeAssets.Load().modal);shared.Show(ModalMode.Settings);
                Check(shared.transform.Find("Composition/SettingsContent/ReturnMainMenu")==null,"Outside game settings retains two buttons");
                Check(shared.transform.Find("Composition/HeaderTitle").GetComponent<Image>().enabled,"Outside settings keeps original Settings title");
                File.WriteAllLines(Output+"validation.txt",checks);Debug.Log("PAUSE_HUD_VALIDATION_OK "+checks.Count);EditorApplication.Exit(0);return;
            }
            phase++;ready=EditorApplication.timeSinceStartup+1;
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllLines(Output+"validation.txt",checks);EditorApplication.Exit(1);}
    }
    static void Resize(int width,int height){if(preview.targetTexture!=null){preview.targetTexture.Release();UnityEngine.Object.Destroy(preview.targetTexture);}preview.targetTexture=new RenderTexture(width,height,24);preview.targetTexture.Create();preview.orthographicSize=height/2f;}
    static void CheckBounds(GameObject root,string name)
    {
        Canvas.ForceUpdateCanvases();var corners=new Vector3[4];var rt=preview.targetTexture;
        foreach(var graphic in root.GetComponentsInChildren<Graphic>())
        {
            if(!graphic.isActiveAndEnabled)continue;graphic.rectTransform.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var p=preview.WorldToViewportPoint(corner);
                Check(p.x>=-.002f&&p.x<=1.002f&&p.y>=-.002f&&p.y<=1.002f,name+" "+graphic.name+" within "+rt.width+"x"+rt.height);
            }
        }
    }
    static void Save(string name)
    {
        preview.Render();var previous=RenderTexture.active;RenderTexture.active=preview.targetTexture;
        var image=new Texture2D(preview.targetTexture.width,preview.targetTexture.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Output+name,image.EncodeToPNG());UnityEngine.Object.Destroy(image);RenderTexture.active=previous;
    }
}
