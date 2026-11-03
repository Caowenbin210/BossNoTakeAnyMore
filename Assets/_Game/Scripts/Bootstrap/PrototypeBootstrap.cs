using BossNoTakeAnyMore.Data;
using BossNoTakeAnyMore.Gameplay;
using BossNoTakeAnyMore.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossNoTakeAnyMore.Bootstrap
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private PrototypeView _view;
        [SerializeField] private string _mainMenuScene = "MainMenu";
        private LevelCatalog _catalog;
        private int _index;
        private void Start()
        {
            if (_view == null)
            {
                Debug.LogError("Warehouse requires an assigned PrototypeView.", this);
                enabled = false;
                return;
            }
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            _catalog = LevelCatalog.Load();
            _view.Initialize(); _view.NextRequested += HandleNext; LoadLevel();
            _view.MenuRequested += HandleMenu;
        }
        private void HandleMenu() { SceneManager.LoadScene(_mainMenuScene); }
        private void HandleNext() { _index = (_index+1)%_catalog.levels.Length; LoadLevel(); }
        private void LoadLevel()
        {
            LevelData data = _catalog.levels[_index];
            _view.Bind(new LevelController(data.CreateBoard()),data,_index);
        }
        private void OnDestroy()
        {
            if (_view == null) { return; }
            _view.NextRequested -= HandleNext;
            _view.MenuRequested -= HandleMenu;
        }
    }
}
