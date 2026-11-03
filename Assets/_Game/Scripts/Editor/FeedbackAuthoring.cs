using System;
using System.IO;
using BossNoTakeAnyMore.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.EditorTools
{
    public static class FeedbackAuthoring
    {
        public const string ProfilePath = "Assets/_Game/Settings/WarehouseFeedback.asset";
        [MenuItem("Warehouse/Install Operation Feedback")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { throw new InvalidOperationException("Exit Play mode first"); }
            Scene original = SceneManager.GetActiveScene();
            const string path = "Assets/_Game/Scenes/Warehouse.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.isLoaded;
            if (wasLoaded && scene.isDirty) { throw new InvalidOperationException("Save Warehouse scene edits before migration"); }
            if (!wasLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
            SceneManager.SetActiveScene(scene);
            try
            {
                PrototypeView view = null;
                CargoSurface surface = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponent<PrototypeView>() != null) { view = root.GetComponent<PrototypeView>(); }
                    CargoSurface found = root.GetComponentInChildren<CargoSurface>(true);
                    if (found != null) { surface = found; }
                }
                if (view == null || surface == null) { throw new InvalidOperationException("Warehouse UI references not found"); }
                var viewData = new SerializedObject(view);
                if (viewData.FindProperty("_feedback").objectReferenceValue != null)
                { Debug.Log("Feedback already installed; preserving scene and profile edits."); return; }
                FeedbackProfile profile = CreateProfile();
                var feedbackObject = new GameObject("WarehouseFeedback", typeof(AudioSource), typeof(WarehouseFeedback));
                AudioSource source = feedbackObject.GetComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false; source.spatialBlend = 0;
                WarehouseFeedback feedback = feedbackObject.GetComponent<WarehouseFeedback>();
                RectTransform frame = (RectTransform)surface.transform.parent;
                GameObject modal = (GameObject)viewData.FindProperty("_modal").objectReferenceValue;
                RectTransform overlayRect = Rect("PlacementEffects", frame, new Rect(0, 0, 720, 1280));
                FeedbackOverlay overlay = overlayRect.gameObject.AddComponent<FeedbackOverlay>();
                overlay.raycastTarget = false;
                overlayRect.SetSiblingIndex(modal.transform.GetSiblingIndex());
                RectTransform stamp = Rect("CompletionStamp", frame, new Rect(160, 808, 400, 96));
                stamp.gameObject.AddComponent<Image>().color = new Color32(49, 142, 103, 245);
                stamp.GetComponent<Image>().raycastTarget = false;
                CanvasGroup stampGroup = stamp.gameObject.AddComponent<CanvasGroup>();
                stampGroup.alpha = 0; stampGroup.blocksRaycasts = false; stampGroup.interactable = false;
                Text stampText = Label("Text", "装 车 完 成！", stamp, new Rect(0, 0, 400, 96), 42);
                stampText.color = Color.white;
                stamp.SetSiblingIndex(modal.transform.GetSiblingIndex());
                Toggle sound = Toggle("SoundToggle", "音效", modal.transform, new Vector2(160, 210));
                Toggle motion = Toggle("MotionToggle", "震动 / 弹性", modal.transform, new Vector2(370, 210));
                Assign(feedback, "_profile", profile); Assign(feedback, "_surface", surface);
                Assign(feedback, "_overlay", overlay); Assign(feedback, "_audioSource", source);
                Assign(feedback, "_completionStamp", stampGroup);
                Assign(feedback, "_soundToggle", sound); Assign(feedback, "_motionToggle", motion);
                Assign(view, "_feedback", feedback);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Camera camera = root.GetComponent<Camera>();
                    if (camera != null && camera.GetComponent<AudioListener>() == null)
                    { camera.gameObject.AddComponent<AudioListener>(); break; }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("PASS operation feedback installed: six clips, overlay, stamp, audio/motion toggles, profile references.");
            }
            finally
            {
                if (!wasLoaded) { EditorSceneManager.CloseScene(scene, true); }
                if (original.IsValid()) { SceneManager.SetActiveScene(original); }
            }
        }
        private static FeedbackProfile CreateProfile()
        {
            FeedbackProfile profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProfilePath);
            if (profile != null) { return profile; }
            Directory.CreateDirectory("Assets/_Game/Settings");
            profile = ScriptableObject.CreateInstance<FeedbackProfile>();
            string[] fields = { "_pickup", "_rotate", "_place", "_invalid", "_return", "_complete" };
            string[] clips = { "cargo_pickup", "cargo_rotate", "cargo_place", "cargo_invalid", "cargo_return", "level_complete" };
            for (int i = 0; i < fields.Length; i++)
            {
                string path = "Assets/_Game/Audio/SFX/" + clips[i] + ".wav";
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) { throw new InvalidOperationException("Audio not imported: " + path); }
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) { throw new InvalidOperationException("Invalid clip: " + path); }
                Assign(profile, fields[i], clip);
            }
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }
        private static RectTransform Rect(string name, Transform parent, Rect bounds)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = bounds.position; rect.sizeDelta = bounds.size;
            return rect;
        }
        private static Text Label(string name, string text, Transform parent, Rect bounds, int size)
        {
            Text label = Rect(name, parent, bounds).gameObject.AddComponent<Text>();
            label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/NotoSansCJKsc-Regular.otf");
            label.text = text; label.fontSize = size; label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
            return label;
        }
        private static Toggle Toggle(string name, string text, Transform parent, Vector2 position)
        {
            RectTransform root = Rect(name, parent, new Rect(position, new Vector2(190, 54)));
            root.gameObject.AddComponent<Image>().color = Color.clear;
            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            RectTransform box = Rect("Box", root, new Rect(0, 11, 32, 32));
            Image background = box.gameObject.AddComponent<Image>(); background.color = new Color32(235, 239, 225, 255);
            Image check = Rect("Check", box, new Rect(6, 6, 20, 20)).gameObject.AddComponent<Image>();
            check.color = new Color32(49, 142, 103, 255);
            toggle.targetGraphic = background; toggle.graphic = check; toggle.isOn = true;
            Label("Label", text, root, new Rect(42, 0, 148, 54), 23);
            return toggle;
        }
        private static void Assign(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
