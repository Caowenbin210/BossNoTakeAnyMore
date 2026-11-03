using System;
using System.Collections.Generic;
using BossNoTakeAnyMore.Core;
using BossNoTakeAnyMore.Gameplay;
using BossNoTakeAnyMore.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossNoTakeAnyMore.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CargoSurface : MaskableGraphic, IPointerDownHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _boardArea;
        [SerializeField] private RectTransform _trayArea;
        [SerializeField] private RectTransform[] _slots;
        [SerializeField] private float _touchLift = 85;
        [SerializeField] private Color _boardColor = new Color32(219, 229, 219, 255);
        [SerializeField] private Color _slotColor = new Color32(232, 236, 223, 255);
        [SerializeField] private Color[] _colors = { new Color32(244,184,75,255),new Color32(78,176,148,255),
            new Color32(159,133,208,255),new Color32(93,164,207,255),new Color32(231,130,97,255) };
        private LevelController _controller;
        private CargoState _pressed;
        private Vector2 _grab;
        private Vector2 _dragOrigin;
        private int _pointer = int.MinValue;
        private bool _wasDrag;
        private sealed class Landing
        {
            public Vector2 From;
            public float Start;
            public float Duration;
            public float Bounce;
            public bool Success;
        }
        private readonly Dictionary<CargoState, Landing> _landings = new Dictionary<CargoState, Landing>();
        private readonly List<CargoState> _expired = new List<CargoState>(5);
        private Vector2 _visualShake;
        public event Action<bool> Dropped;
        public event Action<CargoState> PickupRequested;
        public event Action<CargoState, Vector2, bool, bool> LandingRequested;
        public PlacementFailure LastDropFailure { get; private set; }
        public Vector2 BoardCenter => BoardOrigin + new Vector2(_controller.Board.Width, _controller.Board.Height) * CellSize / 2;
        public int ActiveLandingCount => _landings.Count;
        private void Update()
        {
            if (!Application.isPlaying) { SetVerticesDirty(); }
            if (_landings.Count == 0) { return; }
            _expired.Clear();
            foreach (var entry in _landings)
            {
                if (Time.unscaledTime - entry.Value.Start >= entry.Value.Duration) { _expired.Add(entry.Key); }
            }
            foreach (CargoState cargo in _expired) { _landings.Remove(cargo); }
            SetVerticesDirty();
        }
        private float CellSize { get { return Mathf.Min(Area(_boardArea).width / 4, Area(_boardArea).height / 4); } }
        private Vector2 BoardOrigin
        {
            get
            {
                Rect area = Area(_boardArea);
                return new Vector2(area.center.x - _controller.Board.Width * CellSize / 2, area.yMin);
            }
        }
        public void Bind(LevelController controller) { ClearVisuals(); _controller = controller; ReleasePointer(); }
        public void SetVisualShake(Vector2 offset) { _visualShake = offset; SetVerticesDirty(); }
        public void ClearVisuals() { _landings.Clear(); _visualShake = Vector2.zero; SetVerticesDirty(); }
        public void PlayLanding(CargoState cargo, Vector2 from, bool success, FeedbackProfile profile, bool motionEnabled)
        {
            _landings[cargo] = new Landing
            {
                From = motionEnabled ? from : GetCargoOrigin(cargo), Start = Time.unscaledTime,
                Duration = profile.LandingDuration, Bounce = motionEnabled ? profile.Bounce : 0, Success = success
            };
            SetVerticesDirty();
        }
        public Vector2 GetCargoOrigin(CargoState cargo)
        {
            for (int i = 0; i < _controller.Board.Cargo.Count; i++)
            { if (_controller.Board.Cargo[i] == cargo) { return Origin(cargo, i); } }
            throw new ArgumentException("Unknown cargo");
        }
        public Vector2 GetCargoCenter(CargoState cargo)
        { return GetCargoOrigin(cargo) + Size(cargo) * (cargo.IsPlaced ? CellSize : TrayScale(cargo)) / 2; }
        public void Refresh() { SetVerticesDirty(); }
        public void ReleasePointer() { _pointer = int.MinValue; _pressed = null; _wasDrag = false; SetVerticesDirty(); }
        private Rect Slot(int index) { return Area(_slots[index]); }
        private Rect Area(RectTransform target)
        {
            Vector3 min = rectTransform.InverseTransformPoint(target.TransformPoint(target.rect.min));
            Vector3 max = rectTransform.InverseTransformPoint(target.TransformPoint(target.rect.max));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private Vector2 Size(CargoState cargo)
        {
            int width = 0, height = 0;
            foreach (GridPosition cell in cargo.Cells) { width = Mathf.Max(width,cell.X+1); height = Mathf.Max(height,cell.Y+1); }
            return new Vector2(width,height);
        }
        private float TrayScale(CargoState cargo)
        {
            Vector2 size = Size(cargo);
            int index = 0;
            for (int i = 0; i < _controller.Board.Cargo.Count; i++)
            { if (_controller.Board.Cargo[i] == cargo) { index = i; break; } }
            Rect slot = Slot(index);
            return Mathf.Min(44, (slot.width - 26) / size.x, (slot.height - 38) / size.y);
        }
        private Vector2 Origin(CargoState cargo, int index)
        {
            if (cargo.IsPlaced) { return BoardOrigin+new Vector2(cargo.Anchor.X,cargo.Anchor.Y)*CellSize; }
            return Slot(index).center-Size(cargo)*TrayScale(cargo)/2;
        }
        private Vector2 Local(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,data.position,data.pressEventCamera,out Vector2 local);
            return local;
        }
        private GridPosition Candidate()
        {
            Vector2 delta = (_dragOrigin-BoardOrigin)/CellSize;
            return new GridPosition(Mathf.FloorToInt(delta.x+0.5f),Mathf.FloorToInt(delta.y+0.5f));
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (_controller == null || _pointer != int.MinValue || _controller.Phase != LevelPhase.Playing || data.button != PointerEventData.InputButton.Left) { return; }
            _pointer = data.pointerId; _wasDrag = false;
            Vector2 point = Local(data);
            for (int i = _controller.Board.Cargo.Count-1; i >= 0; i--)
            {
                CargoState cargo = _controller.Board.Cargo[i];
                float scale = cargo.IsPlaced ? CellSize : TrayScale(cargo);
                Vector2 origin = Origin(cargo,i);
                foreach (GridPosition cell in cargo.Cells)
                {
                    if (!new Rect(origin+new Vector2(cell.X,cell.Y)*scale,Vector2.one*scale).Contains(point)) { continue; }
                    _pressed = cargo; _grab = (point-origin)/scale; _controller.Select(cargo); return;
                }
            }
            _controller.Select(null);
        }
        public void OnBeginDrag(PointerEventData data)
        {
            if (data.pointerId != _pointer || _pressed == null) { return; }
            _landings.Remove(_pressed);
            _wasDrag = _controller.BeginDrag(_pressed);
            if (_wasDrag) { PickupRequested?.Invoke(_pressed); }
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != _pointer || !_wasDrag || _controller.Dragged == null) { return; }
            _dragOrigin = Local(data)-_grab*CellSize+(data.pointerId >= 0 ? new Vector2(0,_touchLift) : Vector2.zero);
            SetVerticesDirty();
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == _pointer && !_wasDrag) { ReleasePointer(); }
        }
        public void OnEndDrag(PointerEventData data)
        {
            if (data.pointerId != _pointer || !_wasDrag) { return; }
            CargoState cargo = _controller.Dragged;
            if (cargo != null)
            {
                Vector2 center = _dragOrigin+Size(cargo)*CellSize/2;
                bool toTray = Area(_trayArea).Contains(center);
                LastDropFailure = toTray ? PlacementFailure.None : _controller.Board.Check(cargo, Candidate(), cargo.Turns);
                bool success = _controller.Drop(Candidate(),toTray);
                LandingRequested?.Invoke(cargo, _dragOrigin, success, toTray);
                Dropped?.Invoke(success);
            }
            ReleasePointer();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (_controller == null && !Application.isPlaying && _boardArea != null && _slots != null && _slots.Length >= 5)
            {
                // Edit-mode preview uses fresh state and never edits configuration assets.
                _controller = new LevelController(LevelCatalog.Load().levels[0].CreateBoard());
            }
            if (_controller == null) { return; }
            BoardState board = _controller.Board;
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Color tint = board.IsBlocked(new GridPosition(x,y)) ? (Color)new Color32(70,85,91,255) : _boardColor;
                    Quad(helper,new Rect(BoardOrigin+new Vector2(x,y)*CellSize+Vector2.one*3,Vector2.one*(CellSize-6)),tint);
                }
            }
            for (int i = 0; i < board.Cargo.Count; i++)
            {
                CargoState cargo = board.Cargo[i];
                Quad(helper,Slot(i),_slotColor);
                if (cargo == _controller.Dragged) { continue; }
                DrawCargo(helper,cargo,Origin(cargo,i),cargo.IsPlaced ? CellSize : TrayScale(cargo),_colors[i % _colors.Length],cargo == _controller.Selected);
            }
            CargoState moving = _controller.Dragged;
            if (moving == null) { return; }
            GridPosition candidate = Candidate();
            bool valid = board.Check(moving,candidate,moving.Turns) == PlacementFailure.None;
            Color preview = valid ? new Color(0.1f,0.6f,0.4f,0.7f) : new Color(0.85f,0.15f,0.15f,0.7f);
            Vector2 anchor = BoardOrigin+new Vector2(candidate.X,candidate.Y)*CellSize;
            if (new Rect(BoardOrigin,new Vector2(board.Width,board.Height)*CellSize).Overlaps(new Rect(_dragOrigin,Size(moving)*CellSize)))
            { DrawCargo(helper,moving,anchor,CellSize,preview,true,false); }
            int index = 0;
            for (int i = 0; i < board.Cargo.Count; i++) { if (board.Cargo[i] == moving) { index = i; break; } }
            Color color = _colors[index % _colors.Length]; color.a = 0.7f;
            DrawCargo(helper,moving,_dragOrigin,CellSize,color,false,false);
        }
        private void DrawCargo(VertexHelper helper, CargoState cargo, Vector2 origin, float scale, Color tint, bool selected, bool animate = true)
        {
            Vector2 stretch = Vector2.one;
            if (animate && _landings.TryGetValue(cargo, out Landing landing))
            {
                float t = Mathf.Clamp01((Time.unscaledTime - landing.Start) / landing.Duration);
                origin = Vector2.Lerp(landing.From, origin, 1 - Mathf.Pow(1 - t, 3));
                float pulse = Mathf.Sin(t * Mathf.PI);
                stretch = new Vector2(1 + pulse * landing.Bounce, 1 - pulse * landing.Bounce * 0.6f);
                if (!landing.Success) { tint = Color.Lerp(tint, new Color(0.93f, 0.28f, 0.23f), pulse * 0.7f); }
            }
            Vector2 center = origin + Size(cargo) * scale / 2;
            foreach (GridPosition cell in cargo.Cells)
            {
                Vector2 at = origin+new Vector2(cell.X,cell.Y)*scale;
                if (selected) { Quad(helper,Stretch(new Rect(at+Vector2.one,Vector2.one*(scale-2)),center,stretch),new Color32(39,62,67,255)); }
                Quad(helper,Stretch(new Rect(at+Vector2.one*4,Vector2.one*(scale-8)),center,stretch),tint);
                Quad(helper,Stretch(new Rect(at+new Vector2(8,scale-11),new Vector2(scale-16,3)),center,stretch),new Color(1,1,1,0.4f));
            }
        }
        private static Rect Stretch(Rect rect, Vector2 center, Vector2 scale)
        {
            Vector2 min = center + Vector2.Scale(rect.min - center, scale);
            return new Rect(min, Vector2.Scale(rect.size, scale));
        }
        private void Quad(VertexHelper helper, Rect rect, Color tint)
        {
            rect.position += _visualShake;
            int start = helper.currentVertCount;
            helper.AddVert(new Vector3(rect.xMin,rect.yMin,0),tint,Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin,rect.yMax,0),tint,Vector2.zero);
            helper.AddVert(new Vector3(rect.xMax,rect.yMax,0),tint,Vector2.zero);
            helper.AddVert(new Vector3(rect.xMax,rect.yMin,0),tint,Vector2.zero);
            helper.AddTriangle(start,start+1,start+2); helper.AddTriangle(start,start+2,start+3);
        }
    }
}
