using System;
using System.Collections.Generic;
using System.IO;
using BossNoTakeAnyMore.Bootstrap;
using BossNoTakeAnyMore.Data;
using BossNoTakeAnyMore.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.EditorTools
{
    /// <summary>One-time authoring tool. Runtime never builds or regenerates the UI hierarchy.</summary>
    public static class SceneAuthoring
    {
        private const string Folder = "Assets/_Game/Scenes";
        private static Font _font;

        [MenuItem("Warehouse/Open Main Menu Scene")]
        private static void OpenMenu() { OpenAuthoredScene("MainMenu"); }

        [MenuItem("Warehouse/Open Warehouse Scene")]
        private static void OpenWarehouse() { OpenAuthoredScene("Warehouse"); }

        private static void OpenAuthoredScene(string name)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogWarning("Exit Play mode before opening an authored scene."); return; }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                { Debug.LogWarning("Save your scene edits before switching scenes."); return; }
            }
            Scene scene = EditorSceneManager.OpenScene(Folder + "/" + name + ".unity");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas canvas = root.GetComponent<Canvas>();
                if (canvas == null) { continue; }
                Selection.activeGameObject = canvas.gameObject;
                if (SceneView.lastActiveSceneView != null) { SceneView.lastActiveSceneView.FrameSelected(); }
            }
        }

        [MenuItem("Warehouse/Create Initial Authored Scenes")]
        public static void CreateScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play mode before authoring scenes.");
                return;
            }
            Directory.CreateDirectory(Folder);
            if (File.Exists(Folder + "/MainMenu.unity") || File.Exists(Folder + "/Warehouse.unity"))
            {
                throw new InvalidOperationException("Authored scenes already exist. Tool will not overwrite scene edits.");
            }
            Scene original = SceneManager.GetActiveScene();
            bool dirty = false;
            for (int i = 0; i < SceneManager.sceneCount; i++) { dirty |= SceneManager.GetSceneAt(i).isDirty; }
            PrepareFont();
            CreateMenu();
            CreateWarehouse();
            var builds = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(Folder + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(Folder + "/Warehouse.unity", true)
            };
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != Folder + "/MainMenu.unity" && scene.path != Folder + "/Warehouse.unity")
                { builds.Add(new EditorBuildSettingsScene(scene.path, false)); }
            }
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!dirty) { EditorSceneManager.OpenScene(Folder + "/MainMenu.unity"); }
            else if (original.IsValid()) { SceneManager.SetActiveScene(original); }
            Debug.Log("PASS authored MainMenu and Warehouse scenes saved; Build Settings configured.");
        }

        private static void PrepareFont()
        {
            _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/NotoSansCJKsc-Regular.otf");
            if (_font != null) { return; }
            throw new InvalidOperationException("Import the bundled Noto font before authoring scenes.");
        }

        [MenuItem("Warehouse/Apply Bundled Font To Authored Scenes")]
        public static void ApplyBundledFont()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { throw new InvalidOperationException("Exit Play mode first"); }
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/NotoSansCJKsc-Regular.otf");
            if (font == null) { throw new InvalidOperationException("Bundled font not imported"); }
            Scene original = SceneManager.GetActiveScene();
            foreach (string name in new[] { "MainMenu", "Warehouse" })
            {
                string path = Folder + "/" + name + ".unity";
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasLoaded = scene.isLoaded;
                if (wasLoaded && scene.isDirty) { throw new InvalidOperationException("Save your scene edits first: " + name); }
                if (!wasLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Text label in root.GetComponentsInChildren<Text>(true)) { label.font = font; }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (!wasLoaded) { EditorSceneManager.CloseScene(scene, true); }
            }
            if (original.IsValid()) { SceneManager.SetActiveScene(original); }
            Debug.Log("PASS bundled Chinese font assigned in both authored scenes.");
        }

        private static Scene NewScene(string title)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(38, 57, 61, 255);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return scene;
        }

        private static RectTransform Frame()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 1;
            RectTransform frame = Rect("SafeArea", canvasObject.transform, new Rect(0, 0, 720, 1280));
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            frame.gameObject.AddComponent<Image>().color = new Color32(248, 246, 234, 255);
            frame.gameObject.AddComponent<SafeAreaFrame>();
            return frame;
        }

        private static void CreateMenu()
        {
            Scene scene = NewScene("MainMenu");
            RectTransform frame = Frame();
            Label("Eyebrow", "LAST ORDER / 仓库下班计划", frame, new Rect(40, 1150, 640, 40), 22);
            Label("GameTitle", "老板，\n真塞不下了！", frame, new Rect(40, 920, 640, 210), 64);
            Label("Tagline", "装完这车就下班……\n老板，你怎么又来一车？", frame, new Rect(60, 800, 600, 100), 27);
            RectTransform illustration = Rect("DecorativeCargo", frame, new Rect(150, 585, 420, 180));
            for (int i = 0; i < 3; i++)
            {
                RectTransform box = Rect("Box" + (i + 1), illustration, new Rect(i * 140, i % 2 * 35, 126, 126));
                box.gameObject.AddComponent<Image>().color = i == 0 ? new Color32(244,184,75,255) : new Color32(78,176,148,255);
                Label("BoxMark", i == 2 ? "下班" : "待装", box, new Rect(0, 30, 126, 60), 27);
            }
            Button start = Button("StartButton", "开始装车", frame, new Rect(140, 425, 440, 85));
            Button help = Button("HelpButton", "怎么玩", frame, new Rect(140, 315, 440, 75));
            Label("Version", "前 3 关试玩 · 无倒计时 · 随时重新整理", frame, new Rect(40, 150, 640, 60), 22);
            GameObject helpPanel = Panel("HelpPanel", frame);
            Label("HelpTitle", "装车小贴士", helpPanel.transform, new Rect(80, 940, 560, 70), 40).color = Color.white;
            Label("HelpText", "拖动货物放进车厢\n\n点选货物，再点旋转按钮\n\n已装货物可以取出、重新摆放\n\n全部装入就能完成订单", helpPanel.transform, new Rect(70, 460, 580, 450), 29).color = Color.white;
            Button close = Button("CloseHelpButton", "知道了", helpPanel.transform, new Rect(160, 290, 400, 80));
            MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            Assign(controller, "_start", start); Assign(controller, "_help", help);
            Assign(controller, "_closeHelp", close); Assign(controller, "_helpPanel", helpPanel);
            helpPanel.SetActive(false);
            EditorSceneManager.SaveScene(scene, Folder + "/MainMenu.unity");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateWarehouse()
        {
            Scene scene = NewScene("Warehouse");
            RectTransform frame = Frame();
            CargoSurface surface = Rect("CargoSurface", frame, new Rect(0, 0, 720, 1280)).gameObject.AddComponent<CargoSurface>();
            RectTransform board = Rect("BoardArea", surface.transform, new Rect(144, 650, 432, 432));
            RectTransform tray = Rect("TrayArea", surface.transform, new Rect(0, 160, 720, 410));
            var slots = new RectTransform[5];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Rect("CargoSlot_" + (char)('A' + i), surface.transform, new Rect(28 + i % 3 * 228, 365 - i / 3 * 190, 208, 158));
            }
            Assign(surface, "_boardArea", board); Assign(surface, "_trayArea", tray); AssignArray(surface, "_slots", slots);
            Label("Eyebrow", "LAST ORDER / 装完这车就下班", frame, new Rect(32, 1210, 656, 30), 19);
            Label("GameTitle", "老板，真塞不下了！", frame, new Rect(32, 1150, 656, 58), 40);
            Text title = Label("OrderTitle", "订单 01 · 就三个小件", frame, new Rect(32, 1090, 656, 45), 29);
            Text hint = Label("Hint", "把货物拖进车厢，空着几格也没关系。", frame, new Rect(32, 590, 656, 52), 23);
            Text progress = Label("Progress", "待装区     已装 0 / 3", frame, new Rect(32, 540, 656, 36), 22);
            var names = new Text[5];
            LevelData firstLevel = LevelCatalog.Load().levels[0];
            for (int i = 0; i < names.Length; i++)
            {
                string text = i < firstLevel.cargo.Length ? firstLevel.cargo[i].id + " · " + firstLevel.cargo[i].name : "";
                names[i] = Label("CargoName_" + (char)('A' + i), frame, text, new Rect(28 + i % 3 * 228, 333 - i / 3 * 190, 208, 30), 17);
            }
            Text selection = Label("Selection", "点选货物后可以旋转", frame, new Rect(32, 103, 656, 32), 21);
            Button rotate = Button("RotateButton", "旋转", frame, new Rect(32, 30, 210, 60));
            Button reset = Button("ResetButton", "重新整理", frame, new Rect(255, 30, 210, 60));
            Button pause = Button("PauseButton", "暂停", frame, new Rect(478, 30, 210, 60));
            GameObject modal = Panel("PauseAndResultPanel", frame);
            Text modalTitle = Label("ModalTitle", "先歇一会儿\n老板暂时找不到你。", modal.transform, new Rect(60, 740, 600, 180), 36);
            modalTitle.color = Color.white;
            Button primary = Button("PrimaryButton", "继续装车", modal.transform, new Rect(160, 570, 400, 72));
            Button back = Button("BackButton", "取消 / 返回", modal.transform, new Rect(160, 460, 400, 65));
            Button menu = Button("MainMenuButton", "返回主菜单", modal.transform, new Rect(160, 350, 400, 65));
            PrototypeView view = new GameObject("WarehouseView").AddComponent<PrototypeView>();
            Assign(view, "_surface", surface); Assign(view, "_title", title); Assign(view, "_hint", hint);
            Assign(view, "_progress", progress); Assign(view, "_selection", selection); Assign(view, "_modalTitle", modalTitle);
            Assign(view, "_rotate", rotate); Assign(view, "_reset", reset); Assign(view, "_pause", pause);
            Assign(view, "_modal", modal); Assign(view, "_primary", primary); Assign(view, "_back", back); Assign(view, "_menu", menu);
            Assign(view, "_primaryLabel", primary.GetComponentInChildren<Text>()); AssignArray(view, "_names", names);
            PrototypeBootstrap bootstrap = new GameObject("WarehouseController").AddComponent<PrototypeBootstrap>();
            Assign(bootstrap, "_view", view);
            modal.SetActive(false);
            surface.SetAllDirty();
            EditorSceneManager.SaveScene(scene, Folder + "/Warehouse.unity");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static RectTransform Rect(string name, Transform parent, Rect bounds)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = bounds.position;
            rect.sizeDelta = bounds.size;
            return rect;
        }
        private static Text Label(string name, string text, Transform parent, Rect bounds, int size)
        {
            Text label = Rect(name, parent, bounds).gameObject.AddComponent<Text>();
            label.font = _font; label.fontSize = size; label.text = text;
            label.color = new Color32(44, 65, 69, 255);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }
        private static Text Label(string name, Transform parent, string text, Rect bounds, int size)
        { return Label(name, text, parent, bounds, size); }
        private static Button Button(string name, string text, Transform parent, Rect bounds)
        {
            RectTransform rect = Rect(name, parent, bounds);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color32(194, 220, 193, 255);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label("Text", text, rect, new Rect(0, 0, bounds.width, bounds.height), 26);
            return button;
        }
        private static GameObject Panel(string name, Transform parent)
        {
            GameObject panel = Rect(name, parent, new Rect(0, 0, 720, 1280)).gameObject;
            panel.AddComponent<Image>().color = new Color(0.09f, 0.17f, 0.18f, 0.94f);
            return panel;
        }
        private static void Assign(UnityEngine.Object owner, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void AssignArray<T>(UnityEngine.Object owner, string name, T[] values) where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty array = serialized.FindProperty(name);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) { array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
