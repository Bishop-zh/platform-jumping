using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rustbound.UI;
using Rustbound.Progress;

namespace Rustbound.EditorTools
{
    public static class LevelSelectBuilder
    {
        private const int W=1672, H=941;
        private const string Art="Assets/Art/UI/LevelSelect/";
        private const string Numbers="Assets/Art/UI/Modal/";
        private const string Prefab="Assets/Prefabs/UI/LevelSelect.prefab";
        private static LevelSelectView preview;
        private static Camera camera;
        private static string output;
        private static int ticks;

        [MenuItem("Rustbound/UI/Rebuild Level Select")]
        public static void BuildAssets()
        {
            ImportArt();
            var root=CreateView();
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            UnityEngine.Object.DestroyImmediate(root);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var mainCamera=new GameObject("Main Camera",typeof(Camera));
            mainCamera.tag="MainCamera"; mainCamera.transform.position=new Vector3(0,0,-10);
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var view=((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<LevelSelectView>();
            view.Show(new LevelProgressRepository());
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Preview/LevelSelectPreview.unity");
            scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var bootstrap=UnityEngine.Object.FindObjectOfType<MainMenuBootstrap>();
            if(bootstrap==null) throw new Exception("Main menu bootstrap missing.");
            bootstrap.ConfigureLevelSelect(prefab.GetComponent<LevelSelectView>());
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL_SELECT_ASSETS_BUILT");
        }

        public static void BuildAndCapture()
        {
            try
            {
                var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-jumpOutput");
                output=index>=0?args[index+1]:Path.GetFullPath("Artifacts/LevelSelect");
                Directory.CreateDirectory(output);
                // Also upgrade the existing modal's numeric configuration to the shared punctuation set.
                ModalDialogBuilder.BuildAssets();
                BuildAssets();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                camera=new GameObject("Offscreen UI Camera",typeof(Camera)).GetComponent<Camera>();
                camera.transform.position=new Vector3(0,0,-10); camera.orthographic=true; camera.orthographicSize=H/2f;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
                camera.targetTexture=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32); camera.targetTexture.Create();
                new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                preview=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)).GetComponent<LevelSelectView>();
                var canvas=preview.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.worldCamera=camera; canvas.planeDistance=10; canvas.scaleFactor=1; canvas.referencePixelsPerUnit=32;
                preview.GetComponent<CanvasScaler>().enabled=false;
                ticks=0; EditorApplication.update+=Capture;
            }
            catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void Capture()
        {
            if(++ticks<12) return;
            EditorApplication.update-=Capture;
            try
            {
                var progress=new LevelProgressRepository();
                preview.Show(progress); Render("initial.png");
                progress.CompleteLevel(1,155,8,12);
                preview.Show(progress); Render("level2_unlocked.png");
                progress.MarkEntered(2); progress.CompleteLevel(2,203,10,15);
                preview.Show(progress); Render("level3_unlocked.png");
                Validate();
                File.WriteAllText(Path.Combine(output,"validation.txt"),
                    "PASS: initial unlocks; locked selection/entry rejected; sequential unlocks; preferred new/last played selection; one cyan outline; independent best records; save round trip and malformed save fallback; card/enter/back button events; home/back/reopen navigation and forwarded level request; shared numeric sprites; no missing scripts or artwork.\n");
                Debug.Log("LEVEL_SELECT_CAPTURE_COMPLETE "+output); EditorApplication.Exit(0);
            }
            catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void Validate()
        {
            foreach(var transform in preview.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                    throw new Exception("Missing script: "+transform.name);
            foreach(var image in preview.GetComponentsInChildren<Image>(true))
                if(image.sprite==null && image.name!="Sky") throw new Exception("Missing sprite: "+image.name);
            var progress=new LevelProgressRepository(); preview.Show(progress);
            if(preview.SelectedLevel!=1 || preview.Cards.Count(c=>c.Unlocked)!=1 || preview.SelectLevel(2) || progress.MarkEntered(3))
                throw new Exception("Initial lock rules failed.");
            if(progress.CompleteLevel(2,100,5,10)) throw new Exception("Locked level completion accepted.");
            if(progress.CompleteLevel(1,float.NaN,0,12)) throw new Exception("Invalid timing accepted.");
            progress.CompleteLevel(1,155,8,12); preview.Show(progress);
            if(preview.SelectedLevel!=2 || preview.Cards.Count(c=>c.Selected)!=1 || progress.UnlockedCount!=2)
                throw new Exception("Newly unlocked level not selected.");
            progress.CompleteLevel(1,180,10,12);
            var record=progress.GetRecord(1);
            if(record.bestSeconds!=155 || record.bestCollected!=10) throw new Exception("Best records were overwritten.");
            var reloaded=new LevelProgressRepository(progress.ToJson()); preview.Show(reloaded);
            if(preview.SelectedLevel!=2) throw new Exception("New-unlock selection lost on reload.");
            int requested=0,backs=0;
            preview.LevelRequested.AddListener(id=>requested=id);
            preview.BackRequested.AddListener(()=>backs++);
            foreach(var button in preview.GetComponentsInChildren<Button>(true))
                for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                    button.onClick.SetPersistentListenerState(i,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            preview.Cards[0].Button.onClick.Invoke();
            if(preview.SelectedLevel!=1 || preview.Cards.Count(c=>c.Selected)!=1) throw new Exception("Card selection event failed.");
            preview.transform.Find("Composition/EnterLevel").GetComponent<Button>().onClick.Invoke();
            preview.transform.Find("Composition/Back").GetComponent<Button>().onClick.Invoke();
            if(requested!=1 || backs!=1 || reloaded.LastPlayed!=1 || reloaded.NewlyUnlocked!=0)
                throw new Exception("Enter/back events or last-played save failed.");
            preview.Show(new LevelProgressRepository(reloaded.ToJson()));
            if(preview.SelectedLevel!=1) throw new Exception("Last played level not restored.");
            reloaded.CompleteLevel(2,203,10,15); preview.Show(reloaded);
            if(preview.SelectedLevel!=3 || reloaded.UnlockedCount!=3) throw new Exception("Third level unlock failed.");
            reloaded.CompleteLevel(3,250,12,18);
            if(reloaded.UnlockedCount!=3) throw new Exception("Unlocked nonexistent level.");
            var recovered=new LevelProgressRepository("broken json");
            if(recovered.UnlockedCount!=1) throw new Exception("Corrupt save fallback failed.");
            foreach(var label in preview.GetComponentsInChildren<SpriteNumberLabel>(true))
            {
                var serialized=new SerializedObject(label);
                var glyphs=serialized.FindProperty("glyphs");
                for(int i=0;i<glyphs.arraySize;i++)
                    if(!AssetDatabase.GetAssetPath(glyphs.GetArrayElementAtIndex(i).objectReferenceValue).StartsWith(Numbers))
                        throw new Exception("Selection UI uses a different numeric atlas.");
            }
            ValidateMenuNavigation();
        }

        private static void ValidateMenuNavigation()
        {
            var host=new GameObject("NavigationValidation").AddComponent<MainMenuBootstrap>();
            host.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MainMenu.prefab").GetComponent<MainMenuView>());
            host.ConfigureLevelSelect(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<LevelSelectView>());
            host.InitializeForValidation();
            host.View.LevelSelectRequested.Invoke();
            var screen=UnityEngine.Object.FindObjectsOfType<LevelSelectView>(true).Single(v=>v!=preview);
            if(host.View.gameObject.activeSelf || !screen.gameObject.activeSelf) throw new Exception("Home to selection navigation failed.");
            int requested=0; host.LevelRequested.AddListener(id=>requested=id);
            screen.RequestEnter();
            if(requested!=screen.SelectedLevel) throw new Exception("Level request not forwarded by entry scene.");
            screen.RequestBack();
            if(!host.View.gameObject.activeSelf || screen.gameObject.activeSelf) throw new Exception("Selection back navigation failed.");
            host.View.LevelSelectRequested.Invoke();
            if(UnityEngine.Object.FindObjectsOfType<LevelSelectView>(true).Length!=2)
                throw new Exception("Reopening selection created duplicate screens.");
            screen.RequestBack();
            UnityEngine.Object.DestroyImmediate(host.View.gameObject);
            UnityEngine.Object.DestroyImmediate(screen.gameObject);
            UnityEngine.Object.DestroyImmediate(host.gameObject);
        }

        private static void Render(string name)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            var before=RenderTexture.active; RenderTexture.active=camera.targetTexture;
            var texture=new Texture2D(W,H,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,W,H),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());
            RenderTexture.active=before; UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ImportArt()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Art,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true; importer.wrapMode=TextureWrapMode.Clamp; importer.maxTextureSize=2048;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static GameObject CreateView()
        {
            var root=new GameObject("LevelSelect",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=10;
            canvas.referencePixelsPerUnit=32;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(W,H); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit=32;
            var view=root.AddComponent<LevelSelectView>();
            var composition=Group(root.transform,"Composition"); var rect=(RectTransform)composition.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f); rect.sizeDelta=new Vector2(W,H);
            Image(rect,"Sky",null,0,0,W,H).color=new Color32(166,157,133,255);
            Image(rect,"Background","Background",0,0,W,H);
            Image(rect,"Foreground","Foreground",0,0,W,H);
            Image(rect,"Header","Header",590,0,492,169);
            var back=Image(rect,"Back","BackButton",31,22,124,88); back.raycastTarget=true;
            var backButton=back.gameObject.AddComponent<Button>(); backButton.transition=Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(backButton.onClick,view.RequestBack);
            var cards=new LevelCardView[3]; float[] xs={151,625,1090};
            for(int i=0;i<3;i++)
            {
                var card=Group(rect,"Level"+(i+1)); var cardRect=(RectTransform)card.transform;
                cardRect.anchoredPosition=new Vector2(xs[i],-207); cardRect.sizeDelta=new Vector2(432,555);
                var chassis=Image(card.transform,"Chassis","CardUnlocked",0,0,432,555); chassis.raycastTarget=true;
                var button=card.AddComponent<Button>(); button.targetGraphic=chassis; button.transition=Selectable.Transition.None;
                var thumbnail=Image(card.transform,"Thumbnail","Thumbnail"+(i+1),27,79,378,222);
                var unlocked=Group(card.transform,"UnlockedContent");
                Image(unlocked.transform,"AvailableLabel","AvailableLabel",172,383,92,29).preserveAspect=true;
                Image(unlocked.transform,"BestTimeLabel","BestTimeLabel",80,454,109,27).preserveAspect=true;
                Image(unlocked.transform,"BestCollectLabel","BestCollectLabel",80,496,109,27).preserveAspect=true;
                var time=Number(unlocked.transform,"BestTime",247,457,16,21,"--:--");
                var collection=Number(unlocked.transform,"BestCollection",247,499,16,21,"--/--");
                var locked=Group(card.transform,"LockedContent");
                Image(locked.transform,"LockIcon","LockIcon",190,303,52,57).preserveAspect=true;
                Image(locked.transform,"StatusPlate","LockedStatusPlate",106,412,220,45);
                Image(locked.transform,"Divider","Divider",54,465,324,3);
                Image(locked.transform,"LockedLabel","LockedLabel",173,421,88,28).preserveAspect=true;
                Image(locked.transform,"UnlockHint","UnlockHint",115,492,202,28).preserveAspect=true;
                var name=Image(card.transform,"LevelName","LevelName"+(i+1),106,310,220,42); name.preserveAspect=true;
                var number=Number(card.transform,"LevelNumber",180,18,35,42,(i+1).ToString("00"));
                var outline=Group(card.transform,"Selection");
                Image(outline.transform,"Outline","SelectionFrame",-7,-5,446,566);
                Image(outline.transform,"Arrow","SelectionArrow",198,-37,36,23);
                cards[i]=card.AddComponent<LevelCardView>();
                cards[i].Configure(i+1,view,button,chassis,Sprite("CardUnlocked"),
                    Sprite("CardLocked"),unlocked,locked,outline,number,time,collection,name,thumbnail);
                UnityEventTools.AddPersistentListener(button.onClick,cards[i].SelectThis);
            }
            var enterImage=Image(rect,"EnterLevel","EnterButton",647,803,379,100); enterImage.raycastTarget=true;
            var enter=enterImage.gameObject.AddComponent<Button>(); enter.transition=Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(enter.onClick,view.RequestEnter);
            // Explicit navigation prevents keyboard focus from jumping to a locked card.
            for(int i=0;i<3;i++)
            {
                var navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=backButton,selectOnDown=enter};
                if(i>0) navigation.selectOnLeft=cards[i-1].Button;
                if(i<2) navigation.selectOnRight=cards[i+1].Button;
                cards[i].Button.navigation=navigation;
            }
            enter.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=cards[0].Button,selectOnLeft=backButton};
            backButton.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnDown=cards[0].Button,selectOnRight=cards[0].Button};
            view.Configure(cards,enter); view.Show(new LevelProgressRepository());
            return root;
        }

        private static SpriteNumberLabel Number(Transform parent,string name,float x,float y,float pitch,float height,string text)
        {
            var go=Group(parent,name); var rect=(RectTransform)go.transform;
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(pitch*6,height);
            var label=go.AddComponent<SpriteNumberLabel>();
            var glyphs=Enumerable.Range(0,10).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(Numbers+"Digit"+i+".png"))
                .Concat(new[]{"Colon","Slash","Percent","Minus"}.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>(Numbers+n+".png"))).ToArray();
            label.Configure(glyphs,pitch,height,text); return label;
        }
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static GameObject Group(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.sizeDelta=new Vector2(W,H);
            return go;
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
