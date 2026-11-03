using UnityEngine;

namespace BossNoTakeAnyMore.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFrame : MonoBehaviour
    {
        [SerializeField] private Vector2 _referenceSize = new Vector2(720, 1280);
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.zero;
            _lastSafeArea = default;
            Apply();
        }
        private void Update() { Apply(); }
        private void Apply()
        {
            if (_rect == null) { return; }
            Rect safe = Screen.safeArea;
            if (safe == _lastSafeArea) { return; }
            _lastSafeArea = safe;
            Canvas canvas = GetComponentInParent<Canvas>();
            float canvasScale = canvas == null ? 1 : canvas.scaleFactor;
            Vector2 available = safe.size / canvasScale;
            float scale = Mathf.Min(available.x / _referenceSize.x, available.y / _referenceSize.y);
            _rect.localScale = Vector3.one * scale;
            _rect.anchoredPosition = safe.position / canvasScale + (available - _referenceSize * scale) / 2;
        }
    }
}
