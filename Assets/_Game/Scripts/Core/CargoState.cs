using System;
using System.Collections.Generic;

namespace BossNoTakeAnyMore.Core
{
    public sealed class CargoState
    {
        private readonly IReadOnlyList<GridPosition>[] _orientations = new IReadOnlyList<GridPosition>[4];
        public string Id { get; }
        public string Name { get; }
        public int InitialTurns { get; }
        public int Turns { get; internal set; }
        public bool IsPlaced { get; internal set; }
        public GridPosition Anchor { get; internal set; }
        public IReadOnlyList<GridPosition> Cells { get { return CellsAt(Turns); } }

        public CargoState(string id, string name, GridPosition[] cells, int turns)
        {
            if (string.IsNullOrEmpty(id) || cells == null || cells.Length == 0 || turns < 0 || turns > 3)
            { throw new ArgumentException("Invalid cargo: " + id); }
            Id = id; Name = name; InitialTurns = turns; Turns = turns;
            for (int t = 0; t < 4; t++)
            {
                var result = new GridPosition[cells.Length];
                int minX = int.MaxValue, minY = int.MaxValue;
                for (int i = 0; i < cells.Length; i++)
                {
                    int x = cells[i].X, y = cells[i].Y;
                    for (int r = 0; r < t; r++) { int oldX = x; x = y; y = -oldX; }
                    result[i] = new GridPosition(x, y);
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                }
                var unique = new HashSet<GridPosition>();
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = new GridPosition(result[i].X - minX, result[i].Y - minY);
                    if (!unique.Add(result[i])) { throw new ArgumentException("Duplicate shape cell: " + id); }
                }
                _orientations[t] = Array.AsReadOnly(result);
            }
        }
        public IReadOnlyList<GridPosition> CellsAt(int turns)
        {
            if (turns < 0 || turns > 3) { throw new ArgumentOutOfRangeException(nameof(turns)); }
            return _orientations[turns];
        }
    }
}
