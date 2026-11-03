using System;
using System.Collections.Generic;
using BossNoTakeAnyMore.Core;
using BossNoTakeAnyMore.Gameplay;

internal static class CoreSmokeTests
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) { throw new Exception(message); }
        _checks++;
    }
    private static CargoState Cargo(string id, params GridPosition[] cells)
    { return new CargoState(id,id,cells,0); }
    private static GridPosition P(int x, int y) { return new GridPosition(x,y); }
    private static void Main()
    {
        var ell = Cargo("L",P(0,0),P(0,1),P(0,2),P(1,0));
        var dot = Cargo("D",P(0,0));
        var board = new BoardState(4,4,new[] { ell,dot },new[] { P(3,3) });
        Check(board.TryPlace(ell,P(0,0),0),"L placement");
        Check(board.TryPlace(dot,P(1,1),0),"L hollow must remain usable");
        Check(board.Check(dot,P(0,0),0) == PlacementFailure.Overlap,"Overlap");
        Check(board.Check(dot,P(3,3),0) == PlacementFailure.BlockedCell,"Blocked");
        foreach (GridPosition outside in new[] { P(-1,0),P(0,-1),P(4,0),P(0,4) })
        { Check(board.Check(dot,outside,0) == PlacementFailure.OutOfBounds,"Boundary"); }
        Check(!board.TryPlace(dot,P(3,3),0) && dot.Anchor.Equals(P(1,1)),"Invalid placement atomicity");
        board.ReturnToTray(dot);
        for (int i = 0; i < 4; i++) { Check(board.TryRotate(ell),"Self collision exclusion"); }
        Check(ell.Turns == 0 && ell.Anchor.Equals(P(0,0)),"Four rotations restore anchor");
        Check(new HashSet<GridPosition>(ell.Cells).SetEquals(new[] { P(0,0),P(0,1),P(0,2),P(1,0) }),"Four rotations restore cells");
        Check(board.TryPlace(dot,P(2,1),0),"Place rotation blocker");
        Check(!board.TryRotate(ell) && ell.Turns == 0,"Invalid rotation rollback");
        board.Reset();
        var game = new LevelController(board);
        game.Select(ell); Check(game.BeginDrag(ell),"Begin drag");
        Check(!game.BeginDrag(dot),"Single drag ownership");
        game.Pause();
        Check(game.Dragged == null && !ell.IsPlaced && game.Phase == LevelPhase.Paused,"Pause cancels tray drag");
        Check(!game.BeginDrag(dot),"Paused input blocked");
        game.Resume(); game.BeginDrag(ell); game.Drop(P(0,0),false);
        game.BeginDrag(ell); game.Drop(P(4,4),false);
        Check(ell.IsPlaced && ell.Anchor.Equals(P(0,0)),"Illegal drop preserves old layout");
        game.BeginDrag(ell); game.Pause(); game.Resume();
        Check(ell.IsPlaced && ell.Anchor.Equals(P(0,0)),"Pause preserves placed cargo");
        game.BeginDrag(ell); game.Drop(P(0,0),true);
        Check(!ell.IsPlaced,"Return to tray releases space");
        int completions = 0; game.Completed += () => completions++;
        game.BeginDrag(ell); game.Drop(P(0,0),false);
        game.BeginDrag(dot);
        Check(completions == 0 && game.Phase == LevelPhase.Playing,"Preview cannot complete");
        game.Drop(P(1,1),false);
        Check(completions == 1 && game.Phase == LevelPhase.Completed,"Completion once");
        game.Drop(P(0,0),false); game.Rotate(); game.Reset();
        Check(completions == 1 && board.IsComplete,"Completed state locked");
        Console.WriteLine("PASS: " + _checks + " core/gameplay checks");
    }
}
