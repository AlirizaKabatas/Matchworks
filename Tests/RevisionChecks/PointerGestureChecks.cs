using UnityEngine;
using WorkshopMatch;

partial class Program
{
    static void RunPointerGestureChecks()
    {
        var board = new BoardPointerGesture();
        foreach (var delta in new[] { new Vector2(35, 4), new Vector2(-35, 4), new Vector2(4, 35), new Vector2(4, -35) })
        {
            board.Begin(new Vector2(100, 100));
            var action = board.Sample(new Vector2(100, 100) + delta, true, false, 20, out int dx, out int dy);
            Check(action == BoardGestureAction.Swap && dx == (delta.x > 20 ? 1 : delta.x < -20 ? -1 : 0) &&
                dy == (delta.y > 20 ? 1 : delta.y < -20 ? -1 : 0), "board drag selects exactly one neighbour in each direction");
            Check(board.Sample(new Vector2(300, 300), true, false, 20, out _, out _) == BoardGestureAction.None &&
                board.Sample(new Vector2(300, 300), false, true, 20, out _, out _) == BoardGestureAction.None,
                "continued hold and release cannot swap again");
        }
        board.Begin(Vector2.zero);
        Check(board.Sample(new Vector2(3, 2), true, false, 20, out _, out _) == BoardGestureAction.None && board.Tracking,
            "stationary press waits without swapping");
        Check(board.Sample(new Vector2(3, 2), false, true, 20, out _, out _) == BoardGestureAction.Tap,
            "small touch jitter preserves two-tap controls");
        board.Begin(Vector2.zero);
        Check(board.Sample(new Vector2(80, 60), false, true, 20, out int x, out int y) == BoardGestureAction.Swap && x == 1 && y == 0,
            "fast diagonal release picks its dominant axis without requiring held frames");
        board.Begin(Vector2.zero);
        board.Reset();
        Check(board.Sample(new Vector2(50, 0), false, true, 20, out _, out _) == BoardGestureAction.None,
            "cancelled board gesture cannot become a stale swap");

        foreach (float width in new[] { 320f, 390f, 720f, 1080f })
        foreach (int direction in new[] { -1, 1 })
        {
            var map = new MapSwipeGesture();
            float threshold = TutorialPresentationRules.MapSwipeThreshold(width);
            int swipes = 0;
            for (int gesture = 1; gesture <= 2; gesture++)
            {
                Check(!map.Sample(200, true, true, false, threshold), "press alone does not count as map swipe");
                Check(!map.Sample(200 + direction * (threshold + 1), false, true, false, threshold), "map swipe waits for release");
                // Returning to the starting point still counts the drag already performed.
                if (map.Sample(200, false, false, true, threshold)) swipes++;
                Check(swipes == gesture, "each up/down hold-drag-release counts exactly once at width " + width);
                Check(!map.Sample(200, false, false, true, threshold), "duplicate release cannot advance map lesson");
            }
            Check(swipes == 2, "two independent drags meet map lesson limit");
            map.Sample(200, true, true, false, threshold);
            Check(!map.Sample(201, false, false, true, threshold), "tap jitter cannot count as map swipe");
            map.Sample(200, true, true, false, threshold);
            Check(map.Sample(200 + direction * (threshold + 1), false, false, true, threshold), "fast release-only flick counts");
            map.Sample(200, true, true, false, threshold);
            map.Reset();
            Check(!map.Sample(400, false, false, true, threshold), "focus loss cancels pending map gesture");
        }
    }
}
