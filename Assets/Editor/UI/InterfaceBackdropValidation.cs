using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rustbound.UI;
using Rustbound.World;

[InitializeOnLoad]
public static class InterfaceBackdropValidation
{
    const string Output="Artifacts/UIBackdrop/";
    static readonly List<string> checks=new List<string>();
    static Camera preview;
    static ModalDialogView modal;
    static int frames,phase;
    static double ready;
    static Vector3[] origins;
    static SceneryParallax[] layers;
    static Vector3 cameraOrigin;
    static InterfaceBackdropValidation(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-jumpBackdropValidation")>=0)EditorApplication.update+=BackdropPoll;}
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add("PASS: "+name);}
    public static void RenderUi()
    {
        Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        preview=new GameObject("UI validation camera").AddComponent<Camera>();preview.transform.position=new Vector3(0,0,-10);
        preview.orthographic=true;preview.orthographicSize=941/2f;preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=new Color32(192,179,140,255);
        preview.targetTexture=new RenderTexture(1672,941,24);preview.targetTexture.Create();
        var eventSystem=new GameObject("EventSystem",typeof(EventSystem)).GetComponent<EventSystem>();
        modal=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SharedModal.prefab")).GetComponent<ModalDialogView>();
        var canvas=modal.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=preview;canvas.planeDistance=10;canvas.scaleFactor=1;canvas.referencePixelsPerUnit=32;
        modal.GetComponent<CanvasScaler>().enabled=false;
        EditorApplication.update+=UiPoll;
    }
    static void UiPoll()
    {
        if(++frames<15)return;EditorApplication.update-=UiPoll;
        try
        {
            modal.Show(ModalMode.Settings);TestButton("Composition/SettingsContent/Return");Capture("settings-neutral.png");
            modal.ShowVictory(13,13,16,0,true,true);TestButton("Composition/VictoryContent/NextLevel");Capture("victory-three-buttons.png");
            var next=(RectTransform)modal.transform.Find("Composition/VictoryContent/NextLevel");
            var replay=(RectTransform)modal.transform.Find("Composition/VictoryContent/Replay");var levels=(RectTransform)modal.transform.Find("Composition/VictoryContent/ReturnLevels");
            var positions=new[]{next.anchoredPosition,replay.anchoredPosition,levels.anchoredPosition};
            float center=(Edge(next,false)+Edge(levels,true))/2;
            modal.ShowVictory(10,10,29,0,false,false);Canvas.ForceUpdateCanvases();
            Check(!next.gameObject.activeSelf,"Final victory hides next button");
            Check(Mathf.Abs((Edge(replay,false)+Edge(levels,true))/2-center)<.01f,"Final two buttons centered within original row");
            Check(replay.anchoredPosition.y==positions[1].y&&levels.anchoredPosition.y==positions[2].y,"Final row preserves authored height");
            Capture("victory-final-two-buttons.png");modal.ShowVictory(13,13,16,0,true,true);
            Check(next.anchoredPosition==positions[0]&&replay.anchoredPosition==positions[1]&&levels.anchoredPosition==positions[2],"Three-button layout restored on earlier levels");
            modal.Show(ModalMode.Failure);TestButton("Composition/FailureContent/RetryCheckpoint");Capture("failure-neutral.png");
            File.WriteAllLines(Output+"ui-validation.txt",checks);Debug.Log("INTERFACE_VALIDATION_OK "+checks.Count);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static float Edge(RectTransform r,bool right)=>r.anchoredPosition.x+(right?1-r.pivot.x:-r.pivot.x)*r.rect.width;
    static void TestButton(string path)
    {
        var go=modal.transform.Find(path).gameObject;var visual=go.GetComponent<ModalButtonVisual>();var image=go.GetComponent<Image>();var label=go.transform.Find("Label").GetComponent<Image>();
        var baseData=new BaseEventData(EventSystem.current);var pointer=new PointerEventData(EventSystem.current);
        visual.OnDeselect(baseData);Check(image.sprite.name=="ButtonNormal",go.name+" idle neutral frame");
        Check(label.material.shader.name=="Rustbound/UI/Neutral Modal Label",go.name+" idle warm label material");
        visual.OnSelect(baseData);Check(image.sprite.name=="ButtonSelected",go.name+" keyboard selection highlights");
        visual.OnDeselect(baseData);Check(image.sprite.name=="ButtonNormal",go.name+" keyboard deselect restores");
        visual.OnPointerEnter(pointer);Check(image.sprite.name=="ButtonSelected",go.name+" pointer enter highlights");
        visual.OnPointerExit(pointer);Check(image.sprite.name=="ButtonNormal"&&label.material.shader.name=="Rustbound/UI/Neutral Modal Label",go.name+" pointer exit restores frame and label");
    }
    static void Capture(string file)
    {
        Canvas.ForceUpdateCanvases();preview.Render();Save(preview.targetTexture,Output+file);
    }
    static void Save(RenderTexture rt,string file)
    {
        var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(file,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=previous;
    }
    public static void RunBackdrop(){Directory.CreateDirectory(Output);EditorSceneManager.OpenScene("Assets/Scenes/Level03.unity");EditorApplication.isPlaying=true;}
    static void BackdropPoll()
    {
        if(!EditorApplication.isPlaying)return;if(ready==0){ready=EditorApplication.timeSinceStartup+2;return;}if(EditorApplication.timeSinceStartup<ready)return;
        try
        {
            var camera=UnityEngine.Object.FindObjectOfType<WorldCamera>();var motor=LevelSession.Current.player;
            if(phase==0)
            {
                motor.Enabled=false;motor.Body.simulated=false;layers=UnityEngine.Object.FindObjectsOfType<SceneryParallax>();origins=new Vector3[layers.Length];
                for(int i=0;i<layers.Length;i++)origins[i]=layers[i].transform.position;cameraOrigin=camera.transform.position;
                Check(layers.Length>=9,"Upper floor industrial decorations installed");
                Save(camera.GetComponent<Camera>().targetTexture,Output+"tower-ground.png");motor.transform.position=new Vector3(motor.transform.position.x,12,0);
            }
            else if(phase==1||phase==2)
            {
                float delta=camera.transform.position.y-cameraOrigin.y;
                for(int i=0;i<layers.Length;i++)Check(Mathf.Abs(layers[i].transform.position.y-origins[i].y-delta*(1-layers[i].verticalScrollRatio))<.04f,layers[i].name+" vertical follow floor "+phase);
                Save(camera.GetComponent<Camera>().targetTexture,Output+(phase==1?"tower-middle.png":"tower-summit.png"));
                if(phase==1)motor.transform.position=new Vector3(motor.transform.position.x,19.5f,0);else motor.transform.position=new Vector3(motor.transform.position.x,0,0);
            }
            else
            {
                for(int i=0;i<layers.Length;i++)Check(Vector3.Distance(layers[i].transform.position,origins[i])<.04f,layers[i].name+" returns without drift");
                Check(camera.transform.Find("DistantFactory")!=null,"Base backdrop remains attached to camera");
                var backdrop=camera.transform.Find("DistantFactory");var cover=backdrop.GetComponent<CameraBackdropCover>();
                Check(cover!=null,"Camera backdrop cover installed");
                var owner=camera.GetComponent<Camera>();float originalAspect=owner.aspect;
                foreach(float aspect in new[]{4f/3f,16f/9f})
                {
                    owner.aspect=aspect;cover.RefreshCover();var bounds=backdrop.GetComponent<SpriteRenderer>().bounds;
                    float halfHeight=owner.orthographicSize,halfWidth=halfHeight*aspect;var center=owner.transform.position;
                    Check(bounds.min.x<=center.x-halfWidth+.01f&&bounds.max.x>=center.x+halfWidth-.01f&&bounds.min.y<=center.y-halfHeight+.01f&&bounds.max.y>=center.y+halfHeight-.01f,"Backdrop covers viewport aspect "+aspect);
                }
                owner.aspect=originalAspect;cover.RefreshCover();
                var decor=UnityEngine.Object.FindObjectsOfType<SceneryParallax>();
                foreach(var layer in decor)if(layer.transform.parent.name=="UpperTowerDecoration")Check(layer.GetComponent<Collider2D>()==null,"Decoration has no gameplay collision "+layer.name);
                File.WriteAllLines(Output+"backdrop-validation.txt",checks);Debug.Log("BACKDROP_VALIDATION_OK "+checks.Count);EditorApplication.Exit(0);return;
            }
            phase++;ready=EditorApplication.timeSinceStartup+1;
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
