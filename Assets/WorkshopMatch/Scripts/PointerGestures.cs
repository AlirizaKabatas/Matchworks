using System;
using UnityEngine;

namespace WorkshopMatch
{
    internal enum BoardGestureAction { None, Tap, Swap }

    // Screen coordinates use bottom-left origin, just like board cell coordinates.
    internal sealed class BoardPointerGesture
    {
        private Vector2 start;
        internal bool Tracking { get; private set; }
        internal void Begin(Vector2 point) { start = point; Tracking = true; }
        internal void Reset() { Tracking = false; }

        internal BoardGestureAction Sample(Vector2 point, bool held, bool released, float threshold,
            out int dx, out int dy)
        {
            dx = dy = 0;
            if (!Tracking) return BoardGestureAction.None;
            if (!held && !released) { Reset(); return BoardGestureAction.None; }
            Vector2 delta = point - start;
            if (Math.Max(Math.Abs(delta.x), Math.Abs(delta.y)) >= threshold)
            {
                if (Math.Abs(delta.x) >= Math.Abs(delta.y)) dx = delta.x > 0 ? 1 : -1;
                else dy = delta.y > 0 ? 1 : -1;
                Reset(); // One swap per press, even if held through an entire resolution.
                return BoardGestureAction.Swap;
            }
            if (!released) return BoardGestureAction.None;
            Reset();
            return BoardGestureAction.Tap;
        }
    }

    internal sealed class MapSwipeGesture
    {
        private float startY, distance;
        private bool tracking;
        internal void Reset() { tracking = false; distance = 0; }

        internal bool Sample(float y, bool pressed, bool held, bool released, float threshold)
        {
            if (pressed) { startY = y; distance = 0; tracking = true; }
            if (!tracking) return false;
            if (!held && !released) { Reset(); return false; }
            distance = Math.Max(distance, Math.Abs(y - startY));
            if (!released) return false;
            bool swiped = distance >= threshold;
            Reset();
            return swiped;
        }
    }
}
