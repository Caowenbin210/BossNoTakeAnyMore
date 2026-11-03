using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FeedbackOverlay : MaskableGraphic
    {
        private struct Burst
        {
            public Vector2 Center;
            public float Start;
            public float Radius;
            public Color Tint;
        }
        private readonly List<Burst> _bursts = new List<Burst>(8);
        public bool HasEffects => _bursts.Count > 0;
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }
        public void BurstAt(Vector2 center, float radius, bool completion)
        {
            if (_bursts.Count >= 8) { _bursts.RemoveAt(0); }
            _bursts.Add(new Burst
            {
                Center = center, Start = Time.unscaledTime, Radius = radius,
                Tint = completion ? new Color(1, 0.7f, 0.18f, 1) : new Color(0.18f, 0.7f, 0.48f, 1)
            });
            SetVerticesDirty();
        }
        public void Clear()
        {
            _bursts.Clear();
            SetVerticesDirty();
        }
        private void Update()
        {
            if (_bursts.Count == 0) { return; }
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime - _bursts[i].Start >= 0.45f) { _bursts.RemoveAt(i); }
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            foreach (Burst burst in _bursts)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - burst.Start) / 0.45f);
                Color tint = burst.Tint; tint.a = (1 - t) * 0.8f;
                float radius = burst.Radius * (0.65f + t * 0.65f);
                for (int i = 0; i < 24; i++)
                {
                    float a = i * Mathf.PI * 2 / 24;
                    float b = (i + 1) * Mathf.PI * 2 / 24;
                    RingSegment(helper, burst.Center, a, b, radius, 3 * (1 - t), tint);
                }
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4 + 0.2f;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 at = burst.Center + direction * burst.Radius * (0.8f + 1.1f * t);
                    at.y -= 24 * t * t;
                    float size = 7 * (1 - t);
                    AddQuad(helper, at - Vector2.one * size / 2, at + Vector2.one * size / 2, tint);
                }
            }
        }
        private static void RingSegment(VertexHelper helper, Vector2 center, float a, float b, float radius, float thickness, Color tint)
        {
            int start = helper.currentVertCount;
            Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            helper.AddVert(center + da * radius, tint, Vector2.zero);
            helper.AddVert(center + db * radius, tint, Vector2.zero);
            helper.AddVert(center + db * (radius + thickness), tint, Vector2.zero);
            helper.AddVert(center + da * (radius + thickness), tint, Vector2.zero);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }
        private static void AddQuad(VertexHelper helper, Vector2 min, Vector2 max, Color tint)
        {
            int start = helper.currentVertCount;
            helper.AddVert(new Vector2(min.x, min.y), tint, Vector2.zero);
            helper.AddVert(new Vector2(min.x, max.y), tint, Vector2.zero);
            helper.AddVert(new Vector2(max.x, max.y), tint, Vector2.zero);
            helper.AddVert(new Vector2(max.x, min.y), tint, Vector2.zero);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
