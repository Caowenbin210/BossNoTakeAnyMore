using System;
using System.Reflection;
using BossNoTakeAnyMore.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.EditorTools
{
    public static class SceneFlowChecks
    {
        private static int _stage;

        [MenuItem("Warehouse/Run Scene Flow Checks")]
        private static void Run()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MainMenu")
            { throw new InvalidOperationException("Run from MainMenu in Play mode"); }
            MainMenuController menu = UnityEngine.Object.FindObjectOfType<MainMenuController>();
            GameObject panel = Field<GameObject>(menu, "_helpPanel");
            Field<Button>(menu, "_help").onClick.Invoke();
            Require(panel.activeSelf, "Help opens");
            Field<Button>(menu, "_closeHelp").onClick.Invoke();
            Require(!panel.activeSelf, "Help closes");
            _stage = 1;
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Field<Button>(menu, "_start").onClick.Invoke();
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            EditorApplication.delayCall += CheckAfterStart;
        }

        private static void CheckAfterStart()
        {
            try
            {
                if (!EditorApplication.isPlaying) { throw new InvalidOperationException("Play mode interrupted"); }
                if (_stage == 1)
                {
                    Require(SceneManager.GetActiveScene().name == "Warehouse", "Start loads Warehouse");
                    Require(UnityEngine.Object.FindObjectsOfType<CargoSurface>().Length == 1, "Exactly one surface");
                    Require(UnityEngine.Object.FindObjectsOfType<Canvas>().Length == 1, "Exactly one canvas");
                    PointerSmokeChecks.Run();
                    PrototypeView view = UnityEngine.Object.FindObjectOfType<PrototypeView>();
                    Field<Button>(view, "_pause").onClick.Invoke();
                    Require(Field<GameObject>(view, "_modal").activeSelf, "Pause modal opens");
                    _stage = 2;
                    Field<Button>(view, "_menu").onClick.Invoke();
                }
                else if (_stage == 2)
                {
                    Require(SceneManager.GetActiveScene().name == "MainMenu", "Return loads MainMenu");
                    Require(UnityEngine.Object.FindObjectsOfType<CargoSurface>().Length == 0, "Old surface destroyed");
                    Require(UnityEngine.Object.FindObjectsOfType<Canvas>().Length == 1, "Menu canvas not duplicated");
                    MainMenuController menu = UnityEngine.Object.FindObjectOfType<MainMenuController>();
                    Require(Field<Button>(menu, "_start").interactable, "Start enabled after return");
                    Debug.Log("PASS scene flow: help open/close, MainMenu -> Warehouse, three levels, pause -> MainMenu, no duplicate UI.");
                    SceneManager.sceneLoaded -= OnLoaded;
                    _stage = 0;
                }
            }
            catch (Exception exception)
            {
                SceneManager.sceneLoaded -= OnLoaded;
                _stage = 0;
                Debug.LogException(exception);
            }
        }

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void Require(bool result, string message)
        {
            if (!result) { throw new Exception("Scene flow failed: " + message); }
        }
    }
}
