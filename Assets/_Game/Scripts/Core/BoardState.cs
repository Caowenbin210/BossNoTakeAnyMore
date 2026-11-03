using System;
using System.Collections.Generic;

namespace BossNoTakeAnyMore.Core
{
    public enum PlacementFailure { None, OutOfBounds, BlockedCell, Overlap }

    public sealed class BoardState
    {
        private readonly List<CargoState> _cargo;
        private readonly HashSet<GridPosition> _blocked;
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<CargoState> Cargo { get; }
        public bool IsComplete
        {
            get
            {
                foreach (CargoState item in _cargo) { if (!item.IsPlaced) { return false; } }
                return true;
            }
        }
        public BoardState(int width, int height, IEnumerable<CargoState> cargo, IEnumerable<GridPosition> blocked)
        {
            if (width <= 0 || height <= 0) { throw new ArgumentException("Invalid board size"); }
            Width = width; Height = height; _cargo = new List<CargoState>(cargo);
            _blocked = new HashSet<GridPosition>(blocked);
            var ids = new HashSet<string>();
            if (_cargo.Count == 0) { throw new ArgumentException("Empty order"); }
            foreach (CargoState item in _cargo)
            { if (item == null || !ids.Add(item.Id)) { throw new ArgumentException("Invalid or duplicate cargo"); } }
            foreach (GridPosition cell in _blocked)
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= width || cell.Y >= height)
                { throw new ArgumentException("Blocked cell outside board"); }
            }
            Cargo = _cargo.AsReadOnly();
        }
        public bool IsBlocked(GridPosition cell) { return _blocked.Contains(cell); }
        public PlacementFailure Check(CargoState cargo, GridPosition anchor, int turns)
        {
            RequireMember(cargo);
            foreach (GridPosition local in cargo.CellsAt(turns))
            {
                GridPosition cell = local + anchor;
                if (cell.X < 0 || cell.Y < 0 || cell.X >= Width || cell.Y >= Height)
                { return PlacementFailure.OutOfBounds; }
                if (IsBlocked(cell)) { return PlacementFailure.BlockedCell; }
                foreach (CargoState other in _cargo)
                {
                    if (!other.IsPlaced || ReferenceEquals(cargo, other)) { continue; }
                    foreach (GridPosition occupied in other.Cells)
                    { if ((occupied + other.Anchor).Equals(cell)) { return PlacementFailure.Overlap; } }
                }
            }
            return PlacementFailure.None;
        }
        public bool TryPlace(CargoState cargo, GridPosition anchor, int turns)
        {
            if (Check(cargo, anchor, turns) != PlacementFailure.None) { return false; }
            cargo.Anchor = anchor; cargo.Turns = turns; cargo.IsPlaced = true;
            return true;
        }
        public void ReturnToTray(CargoState cargo) { RequireMember(cargo); cargo.IsPlaced = false; }
        public void Reset()
        {
            foreach (CargoState cargo in _cargo)
            { cargo.IsPlaced = false; cargo.Turns = cargo.InitialTurns; cargo.Anchor = default; }
        }
        public bool TryRotate(CargoState cargo)
        {
            RequireMember(cargo);
            int turns = (cargo.Turns + 1) % 4;
            if (cargo.IsPlaced) { return TryPlace(cargo, cargo.Anchor, turns); }
            cargo.Turns = turns; return true;
        }
        private void RequireMember(CargoState cargo)
        { if (!_cargo.Contains(cargo)) { throw new ArgumentException("Cargo does not belong to this board"); } }
    }
}
