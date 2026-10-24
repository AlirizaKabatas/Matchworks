using System;
using UnityEditor;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    public static class WorkshopMatchLayoutChecks
    {
        [MenuItem("Window/Workshop Match/Validate Mobile Layout")]
        public static void Validate()
        {
            Check(new Rect(0, 0, 1080, 1920), new Rect(0, 0, 1080, 1920));
            Check(new Rect(0, 0, 1170, 2532), new Rect(0, 141, 1170, 2289));
            Check(new Rect(0, 0, 1080, 2400), new Rect(0, 90, 1080, 2220));
            Debug.Log("T12_EDGE_LAYOUT_PASS profiles=3 full_width=1 square_cells=1 safe_controls=1");
        }

        private static void Check(Rect display, Rect safe)
        {
            var layout = WorkshopMatchScreenLayout.Calculate(display, safe, 8, 8, 0.92f);
            Require(Mathf.Abs(layout.Frame.x - display.x) < 0.01f &&
                Mathf.Abs(layout.Frame.xMax - display.xMax) < 0.01f, "Frame must touch both display edges");
            Require(layout.Frame.y >= safe.y && layout.Hint.yMax <= safe.yMax + 0.1f, "Vertical safe-area overflow");
            Require(layout.Hud.y >= safe.y && layout.Toolbar.yMax <= safe.yMax, "Controls outside safe area");
            float xScale = layout.Aperture.width / (8 * 0.92f + 0.34f);
            float yScale = layout.Aperture.height / (9 * 0.92f + 0.34f);
            Require(Mathf.Abs(xScale - yScale) < 0.01f, "Cell aspect ratio mismatch");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
