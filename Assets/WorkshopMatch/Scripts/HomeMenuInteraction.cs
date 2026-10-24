using UnityEngine;

namespace WorkshopMatch
{
    internal static class HomeMenuLayout
    {
        // Controls sit below the actual transparent opening, not below the whole
        // 190 px nine-slice cap (which includes transparent paper around the corners).
        internal static float ControlsY(Rect portrait, Rect safe) =>
            Mathf.Max(safe.y + portrait.width * .020f,
                portrait.y + portrait.width * .140f);
        internal static Rect MenuButton(Rect portrait, Rect safe) =>
            new(portrait.xMax - portrait.width * .280f, ControlsY(portrait, safe),
                portrait.width * .155f, portrait.width * .120f);
        internal static Rect Paper(Rect p) => Rect.MinMaxRect(p.x+p.width*.09f,
            p.y+p.width*.065f,p.xMax-p.width*.09f,p.yMax-p.width*.13f);
        internal static Rect Route(Rect p,Rect safe,float playTop) => Rect.MinMaxRect(
            p.x+p.width*.10f,ControlsY(p,safe)+p.width*.205f,
            p.xMax-p.width*.10f,playTop-p.width*.035f);
        internal static float RoutePitch(Rect route,float portraitWidth) =>
            Mathf.Max(portraitWidth*.02f,(route.height-portraitWidth*.16f)*.5f);
        internal static Vector2 RoutePoint(Rect portrait,Rect route,float level,float focus) =>
            new(portrait.width*(.5f-.24f*Mathf.Cos(level*Mathf.PI*.5f)),
                route.center.y-portrait.y+(focus-level)*RoutePitch(route,portrait.width));
    }

    // Screen-space input, independent of IMGUI MouseUp synthesis and GUI groups.
    internal sealed class MenuMapTapGesture
    {
        private bool tracking, dragged;
        private Vector2 start;
        internal void Reset() { tracking = dragged = false; }
        internal bool Sample(Rect area, Vector2 point, bool pressed, bool held, bool released, float slop)
        {
            if (pressed) { start = point; tracking = true; dragged = false; }
            if (!tracking) return false;
            if (Vector2.Distance(start, point) >= slop) dragged = true;
            if (released)
            {
                bool tapped = !dragged && area.Contains(start) && area.Contains(point);
                Reset();
                return tapped;
            }
            if (!held) Reset();
            return false;
        }
    }
}
