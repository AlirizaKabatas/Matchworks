using UnityEngine;

namespace WorkshopMatch
{
    /// <summary>Full-bleed decoration and safe-area controls are separate coordinate spaces.</summary>
    public static class WorkshopMatchScreenLayout
    {
        public readonly struct Gameplay
        {
            public readonly Rect Frame, Aperture, Hud, Gears, Toolbar, Hint;
            public Gameplay(Rect frame, Rect aperture, Rect hud, Rect gears, Rect toolbar, Rect hint)
            {
                Frame = frame; Aperture = aperture; Hud = hud;
                Gears = gears; Toolbar = toolbar; Hint = hint;
            }
        }

        // The well, stock label and progress boxes form one vertical group.
        // Leave a visible bottom inset instead of sizing only the icon well.
        public static float ToolbarWellSize(Rect toolbar) => Mathf.Min(toolbar.width * .24f,
            Mathf.Max(0f, toolbar.height * .98f - toolbar.width * .025f) / 1.405f);

        public static Gameplay Calculate(Rect display, Rect safe, int columns, int rows, float pitch)
        {
            float width = display.width;
            float sourceScale = width / 829f;
            float side = 47.5f * sourceScale;
            float cap = 60.5f * sourceScale;
            float apertureWidth = width - side * 2f;
            float apertureHeight = apertureWidth * rows / columns;
            float frameHeight = apertureHeight + cap * 2f;
            float gap = width * 0.008f;
            float hintHeight = 0f;
            float toolbarHeight = width * 0.285f;
            float headerHeight = safe.height - frameHeight - toolbarHeight - hintHeight;
            // Portrait phones have room for a width-driven square-cell board. Very short
            // desktop previews retain edge-to-edge framing and fit cells inside the aperture.
            if (headerHeight < width * 0.28f)
            {
                headerHeight = width * 0.28f;
                frameHeight = Mathf.Max(cap * 2f + width * 0.35f,
                    safe.height - headerHeight - toolbarHeight - hintHeight);
                apertureHeight = frameHeight - cap * 2f;
            }
            float hudHeight = Mathf.Min(width * 0.27f, headerHeight * 0.68f);
            float hudWidth = hudHeight * 910f / 315f;
            Rect hud = new(display.center.x - hudWidth * 0.5f, safe.y, hudWidth, hudHeight);
            Rect gears = new(display.x, hud.yMax - width * 0.015f, width, headerHeight - hudHeight);
            Rect frame = new(display.x, safe.y + headerHeight, width, frameHeight);
            Rect aperture = new(frame.x + side, frame.y + cap, apertureWidth, apertureHeight);
            Rect toolbar = new(display.x, safe.yMax - toolbarHeight, width, toolbarHeight);
            Rect hint = new(display.x, toolbar.y - hintHeight - gap, width, hintHeight);
            return new Gameplay(frame, aperture, hud, gears, toolbar, hint);
        }
    }
}
