using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _start;
        [SerializeField] private Button _help;
        [SerializeField] private Button _closeHelp;
        [SerializeField] private GameObject _helpPanel;
        [SerializeField] private string _gameScene = "Warehouse";
        private bool _loading;
        private void Awake()
        {
            if (_start == null || _help == null || _closeHelp == null || _helpPanel == null)
            {
                Debug.LogError("MainMenu has missing UI references.", this);
                enabled = false;
                return;
            }
            Screen.orientation = ScreenOrientation.Portrait;
            _start.onClick.AddListener(StartGame);
            _help.onClick.AddListener(ShowHelp);
            _closeHelp.onClick.AddListener(HideHelp);
            _helpPanel.SetActive(false);
        }
        private void StartGame()
        {
            if (_loading) { return; }
            _loading = true;
            _start.interactable = false;
            SceneManager.LoadScene(_gameScene);
        }
        private void ShowHelp() { _helpPanel.SetActive(true); }
        private void HideHelp() { _helpPanel.SetActive(false); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { HideHelp(); }
        }
        private void OnDestroy()
        {
            if (_start != null) { _start.onClick.RemoveListener(StartGame); }
            if (_help != null) { _help.onClick.RemoveListener(ShowHelp); }
            if (_closeHelp != null) { _closeHelp.onClick.RemoveListener(HideHelp); }
        }
    }
}
