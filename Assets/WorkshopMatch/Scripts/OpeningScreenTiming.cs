namespace WorkshopMatch
{
    // No Unity clock dependency: the minimum starts when the complete art is painted,
    // not when loading starts. Readiness and elapsed time are independent gates.
    public sealed class OpeningScreenTiming
    {
        public const double MinimumSeconds = 3.0;
        public bool Visible { get; private set; }
        public bool HasPresented { get; private set; }
        private double presentedAt;

        public void Begin() { Visible = true; HasPresented = false; }
        public void Presented(double now)
        {
            if (!Visible || HasPresented) return;
            presentedAt = now;
            HasPresented = true;
        }
        public bool CanClose(double now, bool ready) =>
            Visible && HasPresented && ready && now - presentedAt >= MinimumSeconds;
        public void Close() { Visible = false; }
    }
}
