using System;
using UnityEngine;

namespace WorkshopMatch
{
    public readonly struct OpeningScreenLayout
    {
        public readonly Rect Viewport, Design, Logo, Cluster, Wall, TopRail, BottomRail;
        public readonly float LeftGearSize, BottomGearSize;
        public readonly Vector2 LeftGearCenter, RightGearCenter, BottomGearCenter;

        public OpeningScreenLayout(Rect viewport, Rect safe)
        {
            Viewport = viewport;
            float scale = Math.Min(Math.Min(viewport.width * .90f, safe.width * .96f) / 941f,
                Math.Min(viewport.height * .94f, safe.height * .98f) / 1672f);
            Design = new Rect(safe.center.x - 941f * scale * .5f,
                safe.center.y - 1672f * scale * .5f, 941f * scale, 1672f * scale);
            // Slots retain the approved arrangement, with transparent padding in the new tight crops.
            Logo = Map(Design, 12, 390, 917, 306);
            Cluster = Map(Design, 61, 674, 822, 730);
            float rail = viewport.width * (1672f * .038f / 941f);
            TopRail = new Rect(viewport.x, viewport.y, viewport.width, rail);
            BottomRail = new Rect(viewport.x, viewport.yMax - rail, viewport.width, rail);
            Wall = new Rect(viewport.x, TopRail.yMax, viewport.width, viewport.height - rail * 2);
            LeftGearSize = viewport.width * .82f;
            BottomGearSize = viewport.width * 1.08f;
            // Centers are tied to the frame edges, not screen height. A tall phone cannot
            // pull the upper wheels away from the top or expose their empty outer padding.
            LeftGearCenter = new Vector2(viewport.x - viewport.width * .075f,
                Wall.y + LeftGearSize * .28f);
            RightGearCenter = new Vector2(viewport.xMax + viewport.width * .075f, LeftGearCenter.y);
            BottomGearCenter = new Vector2(viewport.xMax + viewport.width * .15f,
                Wall.yMax + viewport.width * .04f);
        }

        private static Rect Map(Rect area, float x, float y, float width, float height) =>
            new Rect(area.x + area.width * x / 941f, area.y + area.height * y / 1672f,
                area.width * width / 941f, area.height * height / 1672f);

        public static Rect Fit(Rect slot, float aspect)
        {
            float width = Math.Min(slot.width, slot.height * aspect);
            float height = width / aspect;
            return new Rect(slot.center.x - width * .5f, slot.center.y - height * .5f, width, height);
        }
    }
}
