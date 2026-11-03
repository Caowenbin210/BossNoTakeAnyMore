using System;
using BossNoTakeAnyMore.Core;

namespace BossNoTakeAnyMore.Gameplay
{
    public enum LevelPhase { Playing, Paused, Completed }

    public sealed class LevelController
    {
        public BoardState Board { get; }
        public LevelPhase Phase { get; private set; }
        public CargoState Selected { get; private set; }
        public CargoState Dragged { get; private set; }
        public event Action Changed;
        public event Action Completed;
        public LevelController(BoardState board) { Board = board; }
        public void Select(CargoState cargo)
        {
            if (cargo != null && !Contains(cargo)) { throw new ArgumentException("Unknown cargo"); }
            if (Phase == LevelPhase.Playing && Dragged == null) { Selected = cargo; Changed?.Invoke(); }
        }
        // Preview excludes the moving object from collision queries; committed state stays
        // untouched until drop. Cancellation therefore preserves the entire original layout.
        public bool BeginDrag(CargoState cargo)
        {
            if (Phase != LevelPhase.Playing || Dragged != null || !Contains(cargo)) { return false; }
            Selected = cargo; Dragged = cargo; Changed?.Invoke(); return true;
        }
        public bool Drop(GridPosition anchor, bool toTray)
        {
            if (Dragged == null || Phase != LevelPhase.Playing) { return false; }
            CargoState cargo = Dragged;
            bool success = toTray || Board.TryPlace(cargo, anchor, cargo.Turns);
            if (toTray) { Board.ReturnToTray(cargo); }
            Dragged = null; FinishMutation(); return success;
        }
        public void CancelDrag() { Dragged = null; Changed?.Invoke(); }
        public bool Rotate()
        {
            if (Phase != LevelPhase.Playing || Dragged != null || Selected == null) { return false; }
            bool success = Board.TryRotate(Selected); FinishMutation(); return success;
        }
        public void Pause()
        {
            if (Phase != LevelPhase.Playing) { return; }
            Dragged = null; Selected = null; Phase = LevelPhase.Paused; Changed?.Invoke();
        }
        public void Resume()
        { if (Phase == LevelPhase.Paused) { Phase = LevelPhase.Playing; Changed?.Invoke(); } }
        public void Reset()
        {
            if (Phase == LevelPhase.Completed) { return; }
            Dragged = null; Selected = null; Board.Reset(); Phase = LevelPhase.Playing; Changed?.Invoke();
        }
        private bool Contains(CargoState cargo)
        { foreach (CargoState item in Board.Cargo) { if (ReferenceEquals(item,cargo)) { return true; } } return false; }
        private void FinishMutation()
        {
            bool complete = Phase == LevelPhase.Playing && Dragged == null && Board.IsComplete;
            if (complete) { Phase = LevelPhase.Completed; Selected = null; }
            Changed?.Invoke();
            if (complete) { Completed?.Invoke(); }
        }
    }
}
