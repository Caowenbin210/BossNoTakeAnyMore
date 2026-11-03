using System;
using System.Collections.Generic;
using BossNoTakeAnyMore.Core;
using UnityEngine;

namespace BossNoTakeAnyMore.Data
{
    // Public fields are serialization DTOs only, never exposed as gameplay state.
    [Serializable] public sealed class CellData { public int x; public int y; }
    [Serializable] public sealed class CargoData
    {
        public string id; public string name; public int initialTurns; public CellData[] cells;
        public int solutionX; public int solutionY; public int solutionTurns;
    }
    [Serializable] public sealed class LevelData
    {
        public string id; public string title; public int width; public int height;
        public CellData[] blocked; public CargoData[] cargo;
        public BoardState CreateBoard()
        {
            var items = new List<CargoState>();
            foreach (CargoData item in cargo)
            {
                var cells = new GridPosition[item.cells.Length];
                for (int i = 0; i < cells.Length; i++) { cells[i] = new GridPosition(item.cells[i].x,item.cells[i].y); }
                items.Add(new CargoState(item.id,item.name,cells,item.initialTurns));
            }
            var blockedCells = new List<GridPosition>();
            foreach (CellData cell in blocked) { blockedCells.Add(new GridPosition(cell.x,cell.y)); }
            return new BoardState(width,height,items,blockedCells);
        }
    }
    [Serializable] public sealed class LevelCatalog
    {
        public LevelData[] levels;
        public static LevelCatalog Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("PrototypeLevels");
            if (asset == null) { throw new InvalidOperationException("Missing PrototypeLevels runtime data"); }
            LevelCatalog result = JsonUtility.FromJson<LevelCatalog>(asset.text);
            if (result == null || result.levels == null || result.levels.Length != 3)
            { throw new InvalidOperationException("Prototype requires three levels"); }
            foreach (LevelData level in result.levels)
            {
                BoardState board = level.CreateBoard();
                for (int i = 0; i < level.cargo.Length; i++)
                {
                    CargoData item = level.cargo[i];
                    if (!board.TryPlace(board.Cargo[i],new GridPosition(item.solutionX,item.solutionY),item.solutionTurns))
                    { throw new InvalidOperationException("Invalid solution: " + level.id + "/" + item.id); }
                }
            }
            return result;
        }
    }
}
