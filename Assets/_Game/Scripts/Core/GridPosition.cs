using System;

namespace BossNoTakeAnyMore.Core
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }
        public GridPosition(int x, int y) { X = x; Y = y; }
        public bool Equals(GridPosition other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is GridPosition other && Equals(other); }
        public override int GetHashCode() { return X * 397 ^ Y; }
        public static GridPosition operator +(GridPosition a, GridPosition b)
        { return new GridPosition(a.X + b.X, a.Y + b.Y); }
    }
}
