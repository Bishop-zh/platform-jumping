using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rustbound.UI;

namespace Rustbound.EditorTools
{
    /// <summary>Builds real serialized UGUI assets and renders them offscreen in batch mode.</summary>
    public static class MainMenuBuilder
    {
        private const int Width = 1672;
        private const int Height = 941;
        private const string Art = "Assets/Art/UI/MainMenu/";
        private const string PrefabPath = "Assets/Prefabs/UI/MainMenu.prefab";
        private static GameObject previewRoot;
        private static Camera previewCamera;
        private static int ticks;
        private static string output;

        [MenuItem("Rustbound/UI/Rebuild Main Menu")]
        public static void BuildAssets()
        {
            Directory.CreateDirectory("Assets/Prefabs/UI");
            ImportArt();
            var root = CreateMenu();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color32(23, 27, 34, 255);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<MainMenuView>();
            new GameObject("UIBootstrap", typeof(MainMenuBootstrap)).GetComponent<MainMenuBootstrap>().Configure(prefab);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
            // A separate scene is editable without Play mode; the entry scene still instantiates by code.
            var previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var previewCameraObject = new GameObject("Main Camera", typeof(Camera));
            previewCameraObject.tag = "MainCamera";
            previewCameraObject.transform.position = new Vector3(0, 0, -10);
            previewCameraObject.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            previewCameraObject.GetComponent<Camera>().backgroundColor = new Color32(23, 27, 34, 255);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Directory.CreateDirectory("Assets/Scenes/Preview");
            EditorSceneManager.SaveScene(previewScene, "Assets/Scenes/Preview/MainMenuPreview.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("MAIN_MENU_ASSETS_BUILT");
        }

        public static void BuildAndCapture()
        {
            try
            {
                output = GetArgument("-jumpOutput") ?? Path.GetFullPath("Artifacts/MainMenu");
                Directory.CreateDirectory(output);
                BuildAssets();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                previewCamera = new GameObject("Offscreen UI Camera", typeof(Camera)).GetComponent<Camera>();
                previewCamera.transform.position = new Vector3(0, 0, -10);
                previewCamera.orthographic = true;
                previewCamera.orthographicSize = Height / 2f;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = Color.black;
                previewRoot = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                var canvas = previewRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = previewCamera;
                canvas.planeDistance = 10;
                previewRoot.GetComponent<CanvasScaler>().enabled = false;
                canvas.scaleFactor = 1;
                previewCamera.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                previewCamera.targetTexture.Create();
                ticks = 0;
                EditorApplication.update += CaptureWhenReady;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureWhenReady()
        {
            if (++ticks < 12) return;
            EditorApplication.update -= CaptureWhenReady;
            try
            {
                Capture("main_menu_frame_0.png");
                var animations = previewRoot.GetComponentsInChildren<UIFrameAnimation>();
                foreach (var animation in animations) animation.ShowFrame(animation.FrameCount - 1);
                Capture("main_menu_frame_3.png");
                ValidatePrefab();
                File.WriteAllText(Path.Combine(output, "validation.txt"),
                    "PASS: UGUI prefab and scene references; four independent button events; no duplicate listeners on re-enable; selection state changes; two four-frame animations advance; two real offscreen rendered frames.\n");
                Debug.Log("MAIN_MENU_CAPTURE_COMPLETE " + output);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Capture(string filename)
        {
            Canvas.ForceUpdateCanvases();
            previewCamera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = previewCamera.targetTexture;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(output, filename), texture.EncodeToPNG());
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ValidatePrefab()
        {
            if (previewRoot.GetComponentsInChildren<Button>().Length != 4)
                throw new Exception("Expected exactly four menu buttons.");
            if (previewRoot.GetComponentsInChildren<UIFrameAnimation>().Length != 2)
                throw new Exception("Expected station and robot frame animations.");
            foreach (var animation in previewRoot.GetComponentsInChildren<UIFrameAnimation>())
                if (animation.FrameCount != 4) throw new Exception("Animation must contain all four supplied frames.");
            foreach (var image in previewRoot.GetComponentsInChildren<Image>())
                if (image.sprite == null && image.name != "Sky" && image.name != "LeftShade" && image.name != "HintRule")
                    throw new Exception("Missing sprite: " + image.name);
            foreach (var transform in previewRoot.GetComponentsInChildren<Transform>())
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    throw new Exception("Missing script: " + transform.name);

            var view = previewRoot.GetComponent<MainMenuView>();
            int[] requests = new int[4];
            view.StartGameRequested.AddListener(() => requests[0]++);
            view.LevelSelectRequested.AddListener(() => requests[1]++);
            view.SettingsRequested.AddListener(() => requests[2]++);
            view.QuitRequested.AddListener(() => requests[3]++);
            view.SendMessage("OnDisable");
            view.SendMessage("OnEnable");
            var buttons = previewRoot.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++) buttons[i].onClick.Invoke();
            if (requests.Any(count => count != 1)) throw new Exception("Button event routing failed.");
            view.SendMessage("OnDisable");
            view.SendMessage("OnEnable");
            buttons[0].onClick.Invoke();
            if (requests[0] != 2) throw new Exception("Reopening the menu duplicated its listeners.");
            view.SelectButton(2);
            if (buttons[2].image.sprite == buttons[0].image.sprite)
                throw new Exception("Selection state did not change.");
            foreach (var animation in previewRoot.GetComponentsInChildren<UIFrameAnimation>())
            {
                animation.ShowFrame(0);
                animation.Advance(1.4f);
                if (animation.FrameIndex == 0) throw new Exception("Animation timeline did not advance.");
            }
        }

        private static void ImportArt()
        {
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(Art, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 4096;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

        private static GameObject CreateMenu()
        {
            var root = new GameObject("MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            // Expand margins on wider screens without distorting the artwork.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var content = new GameObject("Composition", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            content.SetParent(root.transform, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(Width, Height);
            var view = root.AddComponent<MainMenuView>();

            Solid(content, "Sky", 0, 0, Width, Height, new Color32(175, 174, 153, 255));
            Picture(content, "DistantFactory", "Background", 0, -155, Width, Height);
            // Supplied background is transparent in the title area; add a quiet steel shadow.
            Picture(content, "LeftShade", "LeftShadeMask", 0, 0, Width, Height);
            Picture(content, "ForegroundSteelwork", "Foreground", 0, 32, Width, Height);
            Picture(content, "GameTitle", "Title", 69, 84, 579, 229);

            var station = Picture(content, "ChargingStation", "Station_0", 1404, 418, 179, 263);
            station.gameObject.AddComponent<UIFrameAnimation>().Configure(
                Enumerable.Range(0,4).Select(i => Sprite("Station_" + i)).ToArray(),
                new[] { 0.55f, 0.55f, 0.55f, 0.7f });
            var robot = Picture(content, "Robot", "Robot_0", 1235, 552, 111, 129);
            robot.gameObject.AddComponent<UIFrameAnimation>().Configure(
                Enumerable.Range(0,4).Select(i => Sprite("Robot_" + i)).ToArray(),
                new[] { 1.35f, 0.35f, 0.12f, 0.35f });

            var arrow = Picture(content, "SelectionArrow", "SelectionArrow", 80, 402, 25, 36);
            var arrowRect = arrow.rectTransform;
            arrowRect.pivot = new Vector2(0, 0.5f);
            var names = new[] { "StartGame", "LevelSelect", "Settings", "QuitGame" };
            var labels = new[] { "LabelStartNormal", "LabelLevels", "LabelSettings", "LabelQuit" };
            var labelWidths = new[] { 181f, 174f, 83f, 176f };
            var labelHeights = new[] { 41f, 39f, 39f, 39f };
            var buttons = new Button[4];
            var labelImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float top = 382 + i * 101;
                var image = Picture(content, names[i], "ButtonNormal", 116, top, 410, 80);
                image.raycastTarget = true;
                buttons[i] = image.gameObject.AddComponent<Button>();
                buttons[i].transition = Selectable.Transition.None;
                buttons[i].targetGraphic = image;
                labelImages[i] = Picture(image.transform, names[i] + "Label", labels[i],
                    (410 - labelWidths[i]) / 2, (80 - labelHeights[i]) / 2,
                    labelWidths[i], labelHeights[i]);
                image.gameObject.AddComponent<MenuButtonSelection>().Configure(view, i);
            }
            for (int i = 0; i < 4; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i+3)%4], selectOnDown = buttons[(i+1)%4] };
                buttons[i].navigation = nav;
            }
            Picture(content, "ControlHint", "ControlHint", 111, 853, 548, 33);
            Solid(content, "HintRule", 74, 887, 661, 3, new Color32(125, 139, 144, 200));
            view.Configure(buttons, labelImages, Sprite("ButtonNormal"), Sprite("ButtonSelected"), arrowRect, Sprite("LabelStart"));
            return root;
        }

        private static Image Picture(Transform parent, string name, string sprite, float x, float y, float width, float height)
        {
            var image = Solid(parent, name, x, y, width, height, Color.white);
            image.sprite = Sprite(sprite);
            return image;
        }

        private static Image Solid(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static string GetArgument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i+1];
            return null;
        }
    }
}
