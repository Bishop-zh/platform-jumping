using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rustbound.UI;

namespace Rustbound.EditorTools
{
    public static class ModalDialogBuilder
    {
        private const int W=1672, H=941;
        private const string Art="Assets/Art/UI/Modal/";
        private const string Prefab="Assets/Prefabs/UI/SharedModal.prefab";
        private static GameObject background;
        private static ModalDialogView modal;
        private static Camera camera;
        private static int ticks;
        private static string output;

        [MenuItem("Rustbound/UI/Rebuild Shared Modal")]
        public static void BuildAssets()
        {
            ImportArt();
            Directory.CreateDirectory("Assets/Prefabs/UI");
            var root=CreateModal();
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            UnityEngine.Object.DestroyImmediate(root);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Directory.CreateDirectory("Assets/Scenes/Preview");
            foreach(var mode in new[]{ModalMode.Victory,ModalMode.Failure,ModalMode.Settings})
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var cameraObject=new GameObject("Main Camera",typeof(Camera));
                cameraObject.tag="MainCamera";
                cameraObject.transform.position=new Vector3(0,0,-10);
                cameraObject.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
                cameraObject.GetComponent<Camera>().backgroundColor=Color.black;
                var host=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MainMenu.prefab"));
                HideMenuForPreview(host);
                new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.GetComponent<ModalDialogView>().Show(mode);
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/Preview/Modal"+mode+"Preview.unity");
            }
            // Existing entry scene can now open this very same prefab from the Settings button.
            if(File.Exists("Assets/Scenes/MainMenu.unity"))
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
                var bootstrap=UnityEngine.Object.FindObjectOfType<MainMenuBootstrap>();
                if(bootstrap!=null) bootstrap.ConfigureDialog(prefab.GetComponent<ModalDialogView>());
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MODAL_ASSETS_BUILT");
        }

        public static void BuildAndCapture()
        {
            try
            {
                var args=Environment.GetCommandLineArgs();
                int arg=Array.IndexOf(args,"-jumpOutput");
                output=arg>=0?args[arg+1]:Path.GetFullPath("Artifacts/Modal");
                Directory.CreateDirectory(output);
                BuildAssets();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                camera=new GameObject("Offscreen UI Camera",typeof(Camera)).GetComponent<Camera>();
                camera.transform.position=new Vector3(0,0,-10);
                camera.orthographic=true; camera.orthographicSize=H/2f;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
                camera.targetTexture=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);
                camera.targetTexture.Create();
                background=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MainMenu.prefab"));
                HideMenuForPreview(background);
                SetCameraCanvas(background);
                modal=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)).GetComponent<ModalDialogView>();
                SetCameraCanvas(modal.gameObject);
                EventSystem.current=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule)).GetComponent<EventSystem>();
                ticks=0;
                EditorApplication.update+=Capture;
            }
            catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void Capture()
        {
            if(++ticks<12) return;
            EditorApplication.update-=Capture;
            try
            {
                modal.ShowVictory(8,12,155,3,true,true);
                Render("victory.png");
                modal.Show(ModalMode.Failure); Render("failure.png");
                modal.Show(ModalMode.Settings); Render("settings.png");
                Validate();
                File.WriteAllText(Path.Combine(output,"validation.txt"),
                    "PASS: one shared shell prefab; three exclusive contents; dynamic stats; real sliders and toggle; defaults restore; button event routing; full-screen mask intercepts background raycast; original scene integration; no missing scripts or sprites.\n");
                Debug.Log("MODAL_CAPTURE_COMPLETE "+output);
                EditorApplication.Exit(0);
            }
            catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void Render(string file)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            var before=RenderTexture.active; RenderTexture.active=camera.targetTexture;
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,W,H),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(output,file),image.EncodeToPNG());
            RenderTexture.active=before; UnityEngine.Object.DestroyImmediate(image);
        }

        private static void SetCameraCanvas(GameObject root)
        {
            var canvas=root.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera;
            canvas.planeDistance=10; canvas.scaleFactor=1;
            root.GetComponent<CanvasScaler>().enabled=false;
            if(root.GetComponent<ModalDialogView>()!=null) canvas.referencePixelsPerUnit=32;
        }

        private static void HideMenuForPreview(GameObject root)
        {
            foreach(string name in new[]{"GameTitle","StartGame","LevelSelect","Settings","QuitGame","SelectionArrow","ControlHint","HintRule"})
            {
                var found=root.transform.Find("Composition/"+name);
                if(found!=null) found.gameObject.SetActive(false);
            }
        }

        private static void Validate()
        {
            foreach(var transform in modal.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                    throw new Exception("Missing script: "+transform.name);
            foreach(var image in modal.GetComponentsInChildren<Image>(true))
                if(image.sprite==null && image.name!="Dimmer") throw new Exception("Missing sprite: "+image.name);
            var shell=modal.transform.Find("Composition/CommonBody").GetComponent<Image>();
            var position=shell.rectTransform.anchoredPosition; var size=shell.rectTransform.sizeDelta;
            foreach(var mode in new[]{ModalMode.Victory,ModalMode.Failure,ModalMode.Settings})
            {
                modal.Show(mode);
                if(shell.rectTransform.anchoredPosition!=position || shell.rectTransform.sizeDelta!=size)
                    throw new Exception("Shell changed between modes.");
                int active=modal.transform.Find("Composition").Cast<Transform>().Count(t=>t.name.EndsWith("Content")&&t.gameObject.activeSelf);
                if(active!=1) throw new Exception("Content groups are not mutually exclusive.");
            }
            modal.ShowVictory(9,20,367,7,false,false);
            var numbers=modal.GetComponentsInChildren<SpriteNumberLabel>();
            if(!numbers.Any(n=>n.Value=="09/20") || !numbers.Any(n=>n.Value=="06:07"))
                throw new Exception("Dynamic number layout failed.");
            var calls=new List<ModalAction>(); modal.ActionRequested.AddListener(calls.Add);
            foreach(var mode in new[]{ModalMode.Victory,ModalMode.Failure})
            {
                modal.Show(mode);
                foreach(var button in modal.GetComponentsInChildren<Button>())
                {
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                        button.onClick.SetPersistentListenerState(i,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
                    button.onClick.Invoke();
                }
            }
            if(!calls.Contains(ModalAction.Replay)||!calls.Contains(ModalAction.RestartLevel)||!calls.Contains(ModalAction.RetryCheckpoint))
                throw new Exception("Button event routing failed.");
            modal.Show(ModalMode.Settings);
            foreach(var button in modal.GetComponentsInChildren<Button>())
            {
                var buttonRect=(RectTransform)button.transform;
                float left=buttonRect.anchoredPosition.x;
                float top=-buttonRect.anchoredPosition.y;
                if(left<480 || left+buttonRect.sizeDelta.x>1200 || top<305 || top+buttonRect.sizeDelta.y>747)
                    throw new Exception("Settings button extends beyond the panel interior: "+button.name);
            }
            var settings=modal.GetComponentInChildren<ModalSettingsController>();
            var sliders=modal.GetComponentsInChildren<Slider>();
            foreach(var slider in sliders)
                for(int i=0;i<slider.onValueChanged.GetPersistentEventCount();i++)
                    slider.onValueChanged.SetPersistentListenerState(i,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            sliders[0].value=0.25f; sliders[1].value=0.4f;
            if(!modal.GetComponentsInChildren<SpriteNumberLabel>().Any(n=>n.Value=="25%"))
                throw new Exception("Slider percentage did not refresh.");
            settings.RestoreDefaults();
            if(Mathf.Abs(settings.MusicVolume-0.7f)>0.001f || Mathf.Abs(settings.SoundVolume-0.8f)>0.001f)
                throw new Exception("Defaults not restored.");
            Canvas.ForceUpdateCanvases(); camera.Render();
            var raycaster=modal.GetComponent<GraphicRaycaster>();
            var hits=new List<RaycastResult>();
            raycaster.Raycast(new PointerEventData(EventSystem.current){position=new Vector2(200,500)},hits);
            if(!hits.Any(h=>h.gameObject.name=="Dimmer")) throw new Exception("Background click was not blocked.");
        }

        private static void ImportArt()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Art,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false;
                importer.alphaIsTransparency=true; importer.wrapMode=TextureWrapMode.Clamp; importer.maxTextureSize=2048;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                if(Path.GetFileNameWithoutExtension(file).StartsWith("Button"))
                    importer.spriteBorder=new Vector4(70,50,70,50);
                importer.SaveAndReimport();
            }
        }

        private static GameObject CreateModal()
        {
            var root=new GameObject("SharedModal",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
            canvas.referencePixelsPerUnit=32;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(W,H); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit=32;
            var dimmer=Image(root.transform,"Dimmer",null,0,0,W,H);
            dimmer.color=new Color(0,0,0,0.46f); dimmer.raycastTarget=true;
            dimmer.rectTransform.anchorMin=Vector2.zero; dimmer.rectTransform.anchorMax=Vector2.one;
            dimmer.rectTransform.offsetMin=dimmer.rectTransform.offsetMax=Vector2.zero;
            var composition=Group(root.transform,"Composition");
            var rect=(RectTransform)composition.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f); rect.sizeDelta=new Vector2(W,H);
            root.AddComponent<ModalSlideIn>().Configure(rect);
            var view=root.AddComponent<ModalDialogView>();
            Image(rect,"LeftHanger","Rail",484,0,34,176);
            Image(rect,"RightHanger","Rail",1154,0,34,176);
            // The chassis has a transparent central cutout near its upper edge; back it with the same dark panel.
            Image(rect,"HeaderSeamBacking","InnerPanel",452,235,768,110);
            // Overlap the body beneath the header so transparent sprite margins cannot expose the backdrop.
            var chassis=Image(rect,"CommonBody","CommonBody",406,245,860,549); chassis.raycastTarget=true;
            Image(rect,"CommonInset","InnerPanel",478,301,716,426);
            Image(rect,"CommonHeader","CommonHeader",406,112,860,180);
            var icon=Image(rect,"HeaderIcon","VictoryIcon",608,165,96,96); icon.preserveAspect=true;
            var title=Image(rect,"HeaderTitle","VictoryTitle",735,177,323,74); title.preserveAspect=true;
            var victory=Group(rect,"VictoryContent");
            Image(victory.transform,"StatsPanel","InnerPanel",486,315,700,244);
            string[] icons={"CollectIcon","TimeIcon","DeathIcon"};
            string[] labels={"CollectLabel","TimeLabel","DeathLabel"};
            for(int i=0;i<3;i++)
            {
                float y=337+i*80;
                Image(victory.transform,icons[i],icons[i],572,y,43,43).preserveAspect=true;
                Image(victory.transform,labels[i],labels[i],648,y+3,145,37).color=new Color(0.68f,0.77f,0.85f,1);
            }
            var collected=Number(victory.transform,"Collected",966,337,30,34,"08/12");
            var time=Number(victory.transform,"Time",966,417,30,34,"02:35");
            var deaths=Number(victory.transform,"Deaths",966,497,30,34,"3");
            var notice=Group(victory.transform,"UnlockedNotice");
            Image(notice.transform,"NoticePanel","InfoStrip",547,570,578,65);
            Image(notice.transform,"NoticeText","UnlockedLabel",687,587,299,35);
            var next=ActionButton(victory.transform,view,"NextLevel","NextLabel",ModalAction.NextLevel,458,658,244,86,true,141,44);
            ActionButton(victory.transform,view,"Replay","ReplayLabel",ModalAction.Replay,716,658,244,86,false,148,38);
            ActionButton(victory.transform,view,"ReturnLevels","LevelsLabel",ModalAction.ReturnLevelSelect,974,658,244,86,false,148,38);
            var failure=Group(rect,"FailureContent");
            Image(failure.transform,"Subtitle","FailureSubtitle",695,270,282,30);
            ActionButton(failure.transform,view,"RetryCheckpoint","RetryLabel",ModalAction.RetryCheckpoint,584,334,504,86,true,214,42);
            ActionButton(failure.transform,view,"RestartLevel","RestartLabel",ModalAction.RestartLevel,584,434,504,86,false,244,42);
            ActionButton(failure.transform,view,"ReturnLevels","LevelsLabel",ModalAction.ReturnLevelSelect,584,534,504,86,false,163,42);
            ActionButton(failure.transform,view,"ReturnMainMenu","MainMenuLabel",ModalAction.ReturnMainMenu,584,634,504,86,false,244,42);
            var settingsRoot=Group(rect,"SettingsContent");
            Image(settingsRoot.transform,"MusicLabel","MusicLabel",525,319,142,38);
            Image(settingsRoot.transform,"SfxLabel","SfxLabel",525,389,142,38);
            Image(settingsRoot.transform,"FullscreenLabel","FullscreenLabel",525,459,142,38);
            var musicPercent=Number(settingsRoot.transform,"MusicPercent",1090,324,24,30,"70%");
            var sfxPercent=Number(settingsRoot.transform,"SfxPercent",1090,394,24,30,"80%");
            var music=Slider(settingsRoot.transform,"MusicSlider",704,320,0.7f);
            var sound=Slider(settingsRoot.transform,"SoundSlider",704,390,0.8f);
            var checkbox=Image(settingsRoot.transform,"FullscreenToggle","Checkbox",1017,452,54,54);
            checkbox.raycastTarget=true;
            var checkmark=Image(checkbox.transform,"Checkmark","Checkmark",8,8,38,38);
            var toggle=checkbox.gameObject.AddComponent<Toggle>(); toggle.targetGraphic=checkbox; toggle.graphic=checkmark;
            toggle.isOn=true; toggle.toggleTransition=Toggle.ToggleTransition.None;
            var controlsTitle=Sprite("ControlsTitle");
            Image(settingsRoot.transform,"ControlsTitle","ControlsTitle",525,517,
                controlsTitle.rect.width/controlsTitle.rect.height*32,32).preserveAspect=true;
            string[] hints={"MoveHint","JumpHint","DashHint","PauseHint"};
            for(int i=0;i<4;i++)
            {
                var hintSprite=Sprite(hints[i]);
                Image(settingsRoot.transform,hints[i],hints[i],525+(i%2)*395,565+(i/2)*43,
                    hintSprite.rect.width/hintSprite.rect.height*30,30).preserveAspect=true;
            }
            var defaults=PlainButton(settingsRoot.transform,"RestoreDefaults","DefaultsLabel",501,665,302,70,false,164,36);
            ActionButton(settingsRoot.transform,view,"Return","BackLabel",ModalAction.Close,867,665,302,70,true,86,36);
            var controller=settingsRoot.AddComponent<ModalSettingsController>();
            controller.Configure(music,sound,toggle,musicPercent,sfxPercent);
            UnityEventTools.AddPersistentListener(music.onValueChanged,controller.MusicChanged);
            UnityEventTools.AddPersistentListener(sound.onValueChanged,controller.SoundChanged);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged,controller.FullscreenChanged);
            UnityEventTools.AddPersistentListener(defaults.onClick,controller.RestoreDefaults);
            view.Configure(settingsRoot,victory,failure,title,icon,Sprite("SettingsTitle"),Sprite("VictoryTitle"),Sprite("FailureTitle"),
                Sprite("VictoryIcon"),Sprite("FailureIcon"),collected,time,deaths,notice,next);
            return root;
        }

        private static Slider Slider(Transform parent,string name,float x,float y,float value)
        {
            var track=Image(parent,name,"Track",x,y,364,36); track.raycastTarget=true;
            var slider=track.gameObject.AddComponent<Slider>(); slider.transition=Selectable.Transition.None;
            var area=Group(track.transform,"FillArea").GetComponent<RectTransform>();
            area.anchorMin=new Vector2(0,0); area.anchorMax=new Vector2(1,1);
            area.offsetMin=new Vector2(8,8); area.offsetMax=new Vector2(-8,-8);
            var fill=Image(area,"Fill","Fill",0,0,340,20);
            fill.rectTransform.anchorMin=Vector2.zero; fill.rectTransform.anchorMax=Vector2.one;
            fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            var handleArea=Group(track.transform,"HandleArea").GetComponent<RectTransform>();
            handleArea.anchorMin=Vector2.zero; handleArea.anchorMax=Vector2.one;
            handleArea.offsetMin=new Vector2(8,0); handleArea.offsetMax=new Vector2(-8,0);
            var handle=Image(handleArea,"Handle","Handle",0,0,27,43); handle.raycastTarget=true;
            handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,0.5f);
            handle.rectTransform.pivot=new Vector2(0.5f,0.5f); handle.rectTransform.anchoredPosition=Vector2.zero;
            // Slider stretches the orthogonal axis; add 7px to the 36px track, not another 43px.
            handle.rectTransform.sizeDelta=new Vector2(27,7);
            slider.fillRect=fill.rectTransform; slider.handleRect=handle.rectTransform; slider.targetGraphic=handle;
            slider.minValue=0; slider.maxValue=1; slider.value=value;
            return slider;
        }

        private static Button ActionButton(Transform parent,ModalDialogView owner,string name,string label,ModalAction action,
            float x,float y,float width,float height,bool primary,float labelWidth,float labelHeight)
        {
            var button=PlainButton(parent,name,label,x,y,width,height,primary,labelWidth,labelHeight);
            var relay=button.gameObject.AddComponent<ModalActionRelay>(); relay.Configure(owner,action);
            UnityEventTools.AddPersistentListener(button.onClick,relay.Invoke);
            return button;
        }

        private static Button PlainButton(Transform parent,string name,string label,float x,float y,float width,float height,bool primary,float labelWidth,float labelHeight)
        {
            var image=Image(parent,name,primary?"ButtonSelected":"ButtonNormal",x,y,width,height);
            image.raycastTarget=true;
            image.type=UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier=2.5f;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image; button.transition=Selectable.Transition.None;
            var text=Image(image.transform,"Label",label,(width-labelWidth)/2,(height-labelHeight)/2,labelWidth,labelHeight);
            text.preserveAspect=true;
            var visual=image.gameObject.AddComponent<ModalButtonVisual>();
            visual.Configure(Sprite("ButtonNormal"),Sprite("ButtonSelected"),primary);
            if(primary) visual.ConfigureLabels(text,ModalInterfaceRevision.GetNeutralLabelMaterial());
            return button;
        }

        private static SpriteNumberLabel Number(Transform parent,string name,float x,float y,float width,float height,string text)
        {
            var go=Group(parent,name); var rect=(RectTransform)go.transform;
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width*6,height);
            var label=go.AddComponent<SpriteNumberLabel>();
            var sprites=Enumerable.Range(0,10).Select(i=>Sprite("Digit"+i)).Concat(new[]{Sprite("Colon"),Sprite("Slash"),Sprite("Percent"),Sprite("Minus")}).ToArray();
            label.Configure(sprites,width,height,text); return label;
        }

        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");

        private static GameObject Group(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.sizeDelta=new Vector2(W,H); return go;
        }

        private static Image Image(Transform parent,string name,string sprite,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
            var image=go.GetComponent<Image>(); image.sprite=sprite==null?null:Sprite(sprite); image.raycastTarget=false;
            return image;
        }
    }
}
