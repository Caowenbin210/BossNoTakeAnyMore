using BossNoTakeAnyMore.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.UI
{
    /// <summary>Presentation only. Never changes board positions, timing rules or rewards.</summary>
    public sealed class WarehouseFeedback : MonoBehaviour
    {
        private const string SoundKey = "Warehouse.SoundEnabled";
        private const string MotionKey = "Warehouse.MotionEnabled";
        [SerializeField] private FeedbackProfile _profile;
        [SerializeField] private CargoSurface _surface;
        [SerializeField] private FeedbackOverlay _overlay;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private CanvasGroup _completionStamp;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _motionToggle;
        private bool _soundEnabled;
        private bool _motionEnabled;
        private float _shakeStart = -10;
        private float _shakeStrength;
        private float _stampStart = -10;
        public float CompletionDelay => _profile.CompletionDelay;
        public bool SoundEnabled => _soundEnabled;
        public bool MotionEnabled => _motionEnabled;
        public FeedbackProfile Profile => _profile;
        public AudioSource AudioSource => _audioSource;

        private void Awake()
        {
            if (_profile == null || _surface == null || _overlay == null || _audioSource == null ||
                _completionStamp == null || _soundToggle == null || _motionToggle == null)
            {
                Debug.LogError("WarehouseFeedback has missing scene references.", this);
                enabled = false;
                return;
            }
            _soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
            _motionEnabled = PlayerPrefs.GetInt(MotionKey, 1) != 0;
            _soundToggle.SetIsOnWithoutNotify(_soundEnabled);
            _motionToggle.SetIsOnWithoutNotify(_motionEnabled);
            _soundToggle.onValueChanged.AddListener(SetSoundEnabled);
            _motionToggle.onValueChanged.AddListener(SetMotionEnabled);
            _completionStamp.alpha = 0;
        }
        private void OnEnable()
        {
            if (_surface == null) { return; }
            _surface.PickupRequested += HandlePickup;
            _surface.LandingRequested += HandleLanding;
        }
        private void OnDisable()
        {
            if (_surface != null)
            {
                _surface.PickupRequested -= HandlePickup;
                _surface.LandingRequested -= HandleLanding;
            }
            ResetEffects();
        }
        private void OnDestroy()
        {
            if (_soundToggle != null) { _soundToggle.onValueChanged.RemoveListener(SetSoundEnabled); }
            if (_motionToggle != null) { _motionToggle.onValueChanged.RemoveListener(SetMotionEnabled); }
        }
        private void HandlePickup(CargoState cargo) { Play(_profile.Pickup, 0.65f); }
        private void HandleLanding(CargoState cargo, Vector2 from, bool success, bool toTray)
        {
            _surface.PlayLanding(cargo, from, success, _profile, _motionEnabled);
            if (!success) { Play(_profile.Invalid, 0.8f); return; }
            if (toTray) { Play(_profile.Return, 0.65f); return; }
            Play(_profile.Place, 1);
            _overlay.BurstAt(_surface.GetCargoCenter(cargo), 38, false);
            Shake(_profile.PlaceShake);
        }
        public void Rotation(bool success)
        {
            Play(success ? _profile.Rotate : _profile.Invalid, 0.8f);
        }
        public void Complete()
        {
            Play(_profile.Complete, 1);
            _overlay.BurstAt(_surface.BoardCenter, 130, true);
            _stampStart = Time.unscaledTime;
            _completionStamp.alpha = 1;
            Shake(_profile.CompleteShake);
        }
        public void ResetEffects()
        {
            _shakeStrength = 0;
            _stampStart = -10;
            if (_surface != null) { _surface.ClearVisuals(); }
            if (_overlay != null) { _overlay.Clear(); }
            if (_audioSource != null) { _audioSource.Stop(); }
            if (_completionStamp != null)
            {
                _completionStamp.alpha = 0;
                _completionStamp.transform.localScale = Vector3.one;
            }
        }
        private void Play(AudioClip clip, float gain)
        {
            if (_soundEnabled && clip != null) { _audioSource.PlayOneShot(clip, _profile.Volume * gain); }
        }
        private void Shake(float strength)
        {
            if (!_motionEnabled) { return; }
            _shakeStart = Time.unscaledTime;
            _shakeStrength = strength;
        }
        private void Update()
        {
            if (_shakeStrength > 0)
            {
                float t = (Time.unscaledTime - _shakeStart) / _profile.ShakeDuration;
                float amount = _shakeStrength * Mathf.Max(0, 1 - t);
                _surface.SetVisualShake(new Vector2(Mathf.Sin(t * 35), Mathf.Sin(t * 48 + 1)) * amount);
                if (t >= 1) { _shakeStrength = 0; _surface.SetVisualShake(Vector2.zero); }
            }
            if (_completionStamp.alpha > 0 && _motionEnabled)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - _stampStart) / 0.3f);
                _completionStamp.transform.localScale = Vector3.one * (1 + Mathf.Sin(t * Mathf.PI) * 0.08f);
            }
        }
        public void SetSoundEnabled(bool value)
        {
            _soundEnabled = value;
            if (!value) { _audioSource.Stop(); }
            PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
        public void SetMotionEnabled(bool value)
        {
            _motionEnabled = value;
            if (!value) { _shakeStrength = 0; _surface.ClearVisuals(); _completionStamp.transform.localScale = Vector3.one; }
            PlayerPrefs.SetInt(MotionKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
