using System;
using System.Reflection;
using BossNoTakeAnyMore.Core;
using BossNoTakeAnyMore.Data;
using BossNoTakeAnyMore.Gameplay;
using BossNoTakeAnyMore.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.EditorTools
{
    public static class FeedbackSmokeChecks
    {
        private static PrototypeView _view;
        private static CargoSurface _surface;
        private static WarehouseFeedback _feedback;
        private static LevelCatalog _catalog;
        private static LevelController _controller;
        private static bool _sound;
        private static bool _motion;
        private static Vector2 _position;
        private static GridPosition[] _anchors;
        private static float _started;
        private static int _startedFrame;
        private static int _stage;

        [MenuItem("Warehouse/Run Feedback Checks")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || _stage != 0) { throw new InvalidOperationException("Run once in Warehouse Play mode"); }
            _view = UnityEngine.Object.FindObjectOfType<PrototypeView>();
            _surface = UnityEngine.Object.FindObjectOfType<CargoSurface>();
            _feedback = UnityEngine.Object.FindObjectOfType<WarehouseFeedback>();
            if (_view == null || _surface == null || _feedback == null) { throw new InvalidOperationException("Open Warehouse first"); }
            _catalog = LevelCatalog.Load();
            _sound = _feedback.SoundEnabled; _motion = _feedback.MotionEnabled;
            try
            {
                Require(UnityEngine.Object.FindObjectsOfType<AudioListener>().Length == 1, "One audio listener");
                FeedbackProfile profile = _feedback.Profile;
                foreach (AudioClip clip in new[] { profile.Pickup, profile.Rotate, profile.Place, profile.Invalid, profile.Return, profile.Complete })
                { Require(clip != null && clip.samples > 0 && clip.channels == 1, "Six valid mono clips"); }
                _feedback.SetSoundEnabled(false);
                _feedback.Rotation(true);
                Require(!_feedback.AudioSource.isPlaying, "Mute suppresses one-shot playback");
                _feedback.SetSoundEnabled(true);
                _feedback.Rotation(true);
                Require(_feedback.AudioSource.isPlaying, "Enabled sound starts playback");
                _feedback.SetMotionEnabled(true);
                var trial = new LevelController(_catalog.levels[0].CreateBoard());
                _view.Bind(trial, _catalog.levels[0], 0);
                CargoState first = trial.Board.Cargo[0];
                DropCargo(first, new Vector2(-220, 800));
                Require(!first.IsPlaced && _surface.LastDropFailure == PlacementFailure.OutOfBounds, "Illegal drop preserves layout and reports reason");
                Require(_surface.ActiveLandingCount == 1, "Invalid drop has return feedback");
                BeginCargo(first);
                Require(_surface.ActiveLandingCount == 0, "Re-grab cancels old landing animation");
                _surface.ReleasePointer(); trial.CancelDrag();
                _feedback.SetMotionEnabled(false);
                Require(_surface.ActiveLandingCount == 0, "Reduced motion clears animations");
                _feedback.SetMotionEnabled(true);
                _controller = PointerSmokeChecks.CompleteLevel(_view, _surface, _catalog.levels[0], 0);
                Require(!Field<GameObject>(_view, "_modal").activeSelf, "Final layout visible before results");
                Require(_surface.ActiveLandingCount > 0, "Success landing animation starts");
                _position = _surface.rectTransform.anchoredPosition;
                _anchors = new GridPosition[_controller.Board.Cargo.Count];
                for (int i = 0; i < _anchors.Length; i++) { _anchors[i] = _controller.Board.Cargo[i].Anchor; }
                _stage = 1; _started = Time.unscaledTime; _startedFrame = Time.frameCount;
                EditorApplication.update += Tick;
            }
            catch (Exception exception) { Debug.LogException(exception); Cleanup(); }
        }

        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || _view == null) { throw new Exception("Feedback check interrupted"); }
                EditorApplication.QueuePlayerLoopUpdate();
                float elapsed = Time.unscaledTime - _started;
                if (_stage == 1 && elapsed > 0.35f && Time.frameCount - _startedFrame >= 3)
                {
                    Require(Field<CanvasGroup>(_feedback, "_completionStamp").alpha > 0, "Completion stamp appears");
                    Require(_surface.rectTransform.anchoredPosition == _position, "Shake does not move hit rectangle");
                    for (int i = 0; i < _anchors.Length; i++)
                    { Require(_controller.Board.Cargo[i].Anchor.Equals(_anchors[i]), "Effects cannot change board layout"); }
                    _stage = 2;
                }
                if (_stage == 2 && elapsed > _feedback.CompletionDelay + 0.45)
                {
                    Require(Field<GameObject>(_view, "_modal").activeSelf, "Results appear after feedback delay");
                    Require(_surface.ActiveLandingCount == 0, "Landing animation expires");
                    _view.Bind(new LevelController(_catalog.levels[0].CreateBoard()), _catalog.levels[0], 0);
                    Require(Field<CanvasGroup>(_feedback, "_completionStamp").alpha == 0, "New level clears stamp");
                    Require(!Field<FeedbackOverlay>(_feedback, "_overlay").HasEffects, "New level clears sparks");
                    Require(!_feedback.AudioSource.isPlaying, "New level stops old sound");
                    _feedback.Rotation(true);
                    Field<Button>(_view, "_pause").onClick.Invoke();
                    Require(!_feedback.AudioSource.isPlaying && _surface.ActiveLandingCount == 0, "Pause clears active feedback");
                    Debug.Log("PASS feedback checks: six clips, mute/playback, invalid rollback, re-grab, reduced motion, delayed results, stable hit targets, pause/reset cleanup.");
                    Cleanup();
                }
            }
            catch (Exception exception) { Debug.LogException(exception); Cleanup(); }
        }

        private static PointerEventData BeginCargo(CargoState item)
        {
            float scale = (float)typeof(CargoSurface).GetMethod("TrayScale", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_surface, new object[] { item });
            GridPosition first = item.Cells[0];
            Vector2 point = _surface.GetCargoOrigin(item) + new Vector2(first.X + 0.5f, first.Y + 0.5f) * scale;
            var pointer = new PointerEventData(EventSystem.current) { pointerId = -1, button = PointerEventData.InputButton.Left };
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, _surface.rectTransform.TransformPoint(point));
            _surface.OnPointerDown(pointer); _surface.OnBeginDrag(pointer);
            return pointer;
        }
        private static void DropCargo(CargoState item, Vector2 destination)
        {
            PointerEventData pointer = BeginCargo(item);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, _surface.rectTransform.TransformPoint(destination));
            _surface.OnDrag(pointer); _surface.OnPointerUp(pointer); _surface.OnEndDrag(pointer);
        }
        private static T Field<T>(object target, string name)
        { return (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
        private static void Require(bool result, string message)
        { if (!result) { throw new Exception("Feedback check failed: " + message); } }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; _stage = 0;
            if (_feedback != null)
            {
                _feedback.SetSoundEnabled(_sound); _feedback.SetMotionEnabled(_motion);
                _view.Bind(new LevelController(_catalog.levels[0].CreateBoard()), _catalog.levels[0], 0);
                Field<Toggle>(_feedback, "_soundToggle").SetIsOnWithoutNotify(_sound);
                Field<Toggle>(_feedback, "_motionToggle").SetIsOnWithoutNotify(_motion);
            }
        }
    }
}
