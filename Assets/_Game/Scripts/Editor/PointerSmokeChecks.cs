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
    public static class PointerSmokeChecks
    {
        [MenuItem("Warehouse/Run Pointer Smoke Checks %&t")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) { throw new InvalidOperationException("Enter Play mode first"); }
            PrototypeView view = UnityEngine.Object.FindObjectOfType<PrototypeView>();
            CargoSurface surface = UnityEngine.Object.FindObjectOfType<CargoSurface>();
            LevelCatalog catalog = LevelCatalog.Load();
            try
            {
                for (int l = 0; l < catalog.levels.Length; l++)
                {
                    CompleteLevel(view, surface, catalog.levels[l], l);
                }
            }
            finally
            {
                view.Bind(new LevelController(catalog.levels[0].CreateBoard()),catalog.levels[0],0);
            }
        }
        public static LevelController CompleteLevel(PrototypeView view, CargoSurface surface, LevelData data, int index)
        {
            var controller = new LevelController(data.CreateBoard());
            view.Bind(controller,data,index); Canvas.ForceUpdateCanvases();
            int completions = 0; controller.Completed += () => completions++;
            for (int i = 0; i < data.cargo.Length; i++)
            {
                CargoState item = controller.Board.Cargo[i];
                controller.Select(item);
                Button rotate = (Button)typeof(PrototypeView).GetField("_rotate",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
                while (item.Turns != data.cargo[i].solutionTurns) { rotate.onClick.Invoke(); }
                Vector2 size = Vector2.zero;
                foreach (GridPosition cell in item.Cells)
                { size = Vector2.Max(size,new Vector2(cell.X+1,cell.Y+1)); }
                var scaleMethod = typeof(CargoSurface).GetMethod("TrayScale", BindingFlags.NonPublic | BindingFlags.Instance);
                var originMethod = typeof(CargoSurface).GetMethod("Origin", BindingFlags.NonPublic | BindingFlags.Instance);
                float scale = (float)scaleMethod.Invoke(surface, new object[] { item });
                Vector2 origin = (Vector2)originMethod.Invoke(surface, new object[] { item, i });
                GridPosition first = item.Cells[0];
                Vector2 grab = new Vector2(first.X+0.5f,first.Y+0.5f);
                Vector2 from = origin + grab * scale;
                var anchor = new GridPosition(data.cargo[i].solutionX,data.cargo[i].solutionY);
                Vector2 boardOrigin = (Vector2)typeof(CargoSurface).GetProperty("BoardOrigin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(surface);
                float cellSize = (float)typeof(CargoSurface).GetProperty("CellSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(surface);
                Vector2 to = boardOrigin + new Vector2(anchor.X,anchor.Y) * cellSize + grab * cellSize;
                var pointer = new PointerEventData(EventSystem.current) { pointerId = -1,button = PointerEventData.InputButton.Left };
                pointer.position = ScreenPoint(surface,from);
                surface.OnPointerDown(pointer); surface.OnBeginDrag(pointer);
                pointer.position = ScreenPoint(surface,to);
                surface.OnDrag(pointer); surface.OnPointerUp(pointer); surface.OnEndDrag(pointer);
                if (!item.IsPlaced || !item.Anchor.Equals(anchor))
                { throw new Exception("Pointer placement failed: " + data.id + "/" + item.Id); }
            }
            if (controller.Phase != LevelPhase.Completed || completions != 1)
            { throw new Exception("Completion failed: " + data.id); }
            Debug.Log("PASS Unity pointer events + rotate button + completion: " + data.id);
            return controller;
        }
        private static Vector2 ScreenPoint(CargoSurface surface, Vector2 local)
        { return RectTransformUtility.WorldToScreenPoint(null,surface.rectTransform.TransformPoint(local)); }
    }
}
