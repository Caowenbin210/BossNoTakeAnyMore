using System;
using System.Collections;
using BossNoTakeAnyMore.Core;
using BossNoTakeAnyMore.Data;
using BossNoTakeAnyMore.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.UI
{
    public sealed class PrototypeView : MonoBehaviour
    {
        [SerializeField] private CargoSurface _surface;
        [SerializeField] private Text _title;
        [SerializeField] private Text _hint;
        [SerializeField] private Text _progress;
        [SerializeField] private Text _selection;
        [SerializeField] private Text _modalTitle;
        [SerializeField] private Button _rotate;
        [SerializeField] private Button _reset;
        [SerializeField] private GameObject _modal;
        [SerializeField] private Text _primaryLabel;
        [SerializeField] private Text[] _names;
        private LevelController _controller;
        private bool _confirmReset;
        private int _index;
        [SerializeField] private Button _pause;
        [SerializeField] private Button _primary;
        [SerializeField] private Button _back;
        [SerializeField] private Button _menu;
        [SerializeField] private WarehouseFeedback _feedback;
        private Coroutine _completionRoutine;
        private bool _completionReady;
        private string _defaultHint;
        private bool _initialized;
        public event Action NextRequested;
        public event Action MenuRequested;

        public void Initialize()
        {
            if (_initialized) { return; }
            if (_surface == null || _title == null || _hint == null || _progress == null ||
                _selection == null || _modalTitle == null || _rotate == null || _reset == null ||
                _modal == null || _primaryLabel == null || _names == null || _names.Length < 5 ||
                _pause == null || _primary == null || _back == null || _menu == null || _feedback == null)
            {
                throw new InvalidOperationException("PrototypeView has missing scene UI references.");
            }
            _initialized = true;
            _surface.Dropped += HandleDrop;
            _rotate.onClick.AddListener(HandleRotate);
            _reset.onClick.AddListener(RequestReset);
            _pause.onClick.AddListener(HandlePause);
            _primary.onClick.AddListener(HandlePrimary);
            _back.onClick.AddListener(HandleBack);
            _menu.onClick.AddListener(HandleMenu);
            _modal.SetActive(false);
        }
        private void HandleMenu()
        {
            _surface.ReleasePointer();
            CancelPresentation();
            _controller.CancelDrag();
            MenuRequested?.Invoke();
        }
        public void Bind(LevelController controller, LevelData data, int index)
        {
            if (_controller != null)
            {
                _controller.Changed -= Refresh;
                _controller.Completed -= HandleCompleted;
            }
            CancelPresentation();
            _completionReady = false;
            _controller = controller; _index = index; _confirmReset = false;
            _surface.Bind(controller); _controller.Changed += Refresh;
            _controller.Completed += HandleCompleted;
            _title.text = $"订单 {index+1:00} · {data.title}";
            string[] hints = { "把货物拖进车厢，空着几格也没关系。", "点选长包裹，再点下方旋转按钮。", "放不下？已装好的也能拿起来重摆。" };
            _defaultHint = hints[index];
            _hint.text = _defaultHint;
            for (int i = 0; i < _names.Length; i++)
            { _names[i].text = i < data.cargo.Length ? data.cargo[i].id+" · "+data.cargo[i].name : ""; }
            Refresh();
        }
        private void Refresh()
        {
            _surface.Refresh();
            int count = 0;
            foreach (var item in _controller.Board.Cargo) { if (item.IsPlaced) { count++; } }
            _progress.text = $"待装区     已装 {count} / {_controller.Board.Cargo.Count}";
            _selection.text = _controller.Selected == null ? "点选货物后可以旋转" : "已选："+_controller.Selected.Name;
            _rotate.interactable = _controller.Phase == LevelPhase.Playing && _controller.Selected != null && _controller.Dragged == null;
            _reset.interactable = _controller.Phase == LevelPhase.Playing && _controller.Dragged == null;
            bool complete = _controller.Phase == LevelPhase.Completed;
            bool paused = _controller.Phase == LevelPhase.Paused;
            _modal.SetActive((complete && _completionReady) || paused);
            _back.gameObject.SetActive(!complete);
            if (complete)
            {
                _modalTitle.text = _index == 2 ? "这车终于装好了！\n原型体验完成" : "全部装好了！\n先别急着打卡……";
                _primaryLabel.text = _index == 2 ? "从头重玩" : "下一张订单";
            }
            else if (paused)
            {
                _modalTitle.text = _confirmReset ? "重新整理这一车？\n货物将回到初始位置。" : "先歇一会儿\n老板暂时找不到你。";
                _primaryLabel.text = _confirmReset ? "确认重置" : "继续装车";
            }
        }
        private void HandleRotate()
        {
            bool success = _controller.Rotate();
            _feedback.Rotation(success);
            _hint.text = success ? _defaultHint : "这里转不开，先取出来或换个位置。";
        }
        private void HandleDrop(bool success)
        {
            if (_controller.Phase == LevelPhase.Completed) { return; }
            if (success) { _hint.text = _defaultHint; return; }
            switch (_surface.LastDropFailure)
            {
                case PlacementFailure.OutOfBounds: _hint.text = "超出车厢了，已回到原位。"; break;
                case PlacementFailure.Overlap: _hint.text = "和其他货物重叠了，换个位置试试。"; break;
                case PlacementFailure.BlockedCell: _hint.text = "这里被固定设备占用了，换个位置吧。"; break;
                default: _hint.text = "这里放不下，已回到原位。"; break;
            }
        }
        private void RequestReset()
        { _confirmReset = true; _surface.ReleasePointer(); _feedback.ResetEffects(); _controller.Pause(); }
        private void HandlePause()
        {
            if (_controller.Phase != LevelPhase.Playing) { return; }
            _confirmReset = false; _surface.ReleasePointer(); _feedback.ResetEffects(); _controller.Pause();
        }
        private void HandleCompleted()
        {
            _hint.text = "装车完成！这次真的塞下了。";
            _completionRoutine = StartCoroutine(ShowCompletion());
        }
        private IEnumerator ShowCompletion()
        {
            // Let the last cargo land before the completion chime and stamp.
            yield return new WaitForSecondsRealtime(0.12f);
            _feedback.Complete();
            yield return new WaitForSecondsRealtime(_feedback.CompletionDelay);
            _completionReady = true;
            _completionRoutine = null;
            Refresh();
        }
        private void CancelPresentation()
        {
            if (_completionRoutine != null) { StopCoroutine(_completionRoutine); _completionRoutine = null; }
            if (_feedback != null) { _feedback.ResetEffects(); }
        }
        private void HandlePrimary()
        {
            if (_controller.Phase == LevelPhase.Completed) { NextRequested?.Invoke(); }
            else if (_confirmReset)
            { _confirmReset = false; _feedback.ResetEffects(); _controller.Reset(); _hint.text = _defaultHint; }
            else { _controller.Resume(); }
        }
        private void HandleBack()
        {
            if (_controller.Phase == LevelPhase.Completed) { return; }
            _confirmReset = false; _controller.Resume();
        }
        private void Update()
        {
            if (_controller != null && Input.GetKeyDown(KeyCode.Escape))
            {
                if (_controller.Phase == LevelPhase.Paused) { HandleBack(); } else { HandlePause(); }
            }
        }
        private void OnApplicationFocus(bool focus) { if (!focus && _controller != null) { HandlePause(); } }
        private void OnApplicationPause(bool paused) { if (paused && _controller != null) { HandlePause(); } }
        private void OnDisable()
        {
            CancelPresentation();
            if (_controller != null && _surface != null) { _surface.ReleasePointer(); _controller.CancelDrag(); }
        }
        private void OnDestroy()
        {
            if (_controller != null)
            { _controller.Changed -= Refresh; _controller.Completed -= HandleCompleted; }
            if (_surface != null) { _surface.Dropped -= HandleDrop; }
            if (!_initialized) { return; }
            _rotate.onClick.RemoveListener(HandleRotate);
            _reset.onClick.RemoveListener(RequestReset);
            _pause.onClick.RemoveListener(HandlePause);
            _primary.onClick.RemoveListener(HandlePrimary);
            _back.onClick.RemoveListener(HandleBack);
            _menu.onClick.RemoveListener(HandleMenu);
        }
    }
}
