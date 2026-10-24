using WorkshopMatch;

partial class Program
{
    static void RunOpeningScreenChecks()
    {
        var gate = new OpeningScreenTiming();
        gate.Begin();
        Check(!gate.CanClose(100, true), "opening does not count asset loading as visible exposure");
        gate.Presented(100);
        Check(!gate.CanClose(102.9999, true), "opening holds complete image for at least 3 real seconds");
        Check(gate.CanClose(103, true), "opening fast load exits at 3 seconds without another delay");
        Check(!gate.CanClose(110, false), "opening waits beyond minimum while game is unready");
        Check(gate.CanClose(110, true), "opening long load exits as soon as readiness arrives");
        gate.Presented(109);
        Check(gate.CanClose(110, true), "opening later repaint cannot restart the minimum timer");
        gate.Close();
        Check(!gate.Visible && !gate.CanClose(120, true), "opening closed state stops gating normal gameplay");
        gate.Begin();
        Check(!gate.HasPresented && !gate.CanClose(130, true), "opening replay requires a new full repaint");
        gate.Presented(130);
        Check(!gate.CanClose(132.9999, true) && gate.CanClose(133, true), "opening replay has its own 3 second exposure");
        foreach (var dimensions in new[] { (1170,2532), (1080,1920), (1080,2400), (1536,2048), (750,1334), (1440,3200) })
        {
            var viewport = new UnityEngine.Rect(0,0,dimensions.Item1,dimensions.Item2);
            var safe = new UnityEngine.Rect(0,100,dimensions.Item1,dimensions.Item2-180);
            var layout = new OpeningScreenLayout(viewport,safe);
            var logo = OpeningScreenLayout.Fit(layout.Logo,2172f/724);
            var cluster = OpeningScreenLayout.Fit(layout.Cluster,1314f/1197);
            Check(logo.xMin>=viewport.width*.045f && logo.xMax<=viewport.width*.955f &&
                cluster.xMin>=viewport.width*.045f && cluster.xMax<=viewport.width*.955f,
                "opening title and tools keep side gutters " + dimensions);
            Check(logo.yMin>=safe.yMin && cluster.yMax<=safe.yMax,
                "opening artwork stays inside safe height " + dimensions);
            Check(System.Math.Abs(logo.width/logo.height-3)<.001 &&
                System.Math.Abs(cluster.width/cluster.height-1314f/1197)<.001,
                "opening native image proportions retained " + dimensions);
            Check(layout.LeftGearCenter.y-layout.LeftGearSize*.40f<layout.Wall.yMin,
                "opening top rotors overlap frame at every tooth phase " + dimensions);
        }
    }
}
