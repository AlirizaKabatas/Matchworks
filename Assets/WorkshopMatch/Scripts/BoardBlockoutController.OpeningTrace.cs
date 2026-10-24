using System.Diagnostics;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#endif
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private double openingTraceStarted,openingTraceLastFrame,openingTracePhaseAt,openingTraceWorst;
        private int openingTraceFrames,openingTraceOver50;
        private bool openingTraceReplay;
        private string openingTracePhase;
        private readonly List<string> openingTraceStages=new();
#endif
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        private void BeginOpeningTrace(bool replay)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            openingTraceStarted=openingTraceLastFrame=openingTracePhaseAt=Time.realtimeSinceStartupAsDouble;
            openingTraceFrames=openingTraceOver50=0;openingTraceWorst=0;
            openingTraceReplay=replay;openingTracePhase=replay?"replay":"opening-art";
            openingTraceStages.Clear();
#endif
        }
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        private void TraceOpeningPhase(string phase)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            double now=Time.realtimeSinceStartupAsDouble;
            openingTraceStages.Add($"{openingTracePhase}:{(now-openingTracePhaseAt)*1000:F1}ms");
            openingTracePhase=phase;openingTracePhaseAt=now;
#endif
        }
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        private void SampleOpeningTrace()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            double now=Time.realtimeSinceStartupAsDouble;
            double ms=(now-openingTraceLastFrame)*1000;
            openingTraceLastFrame=now;openingTraceFrames++;
            if(ms>openingTraceWorst)openingTraceWorst=ms;
            if(ms>50)openingTraceOver50++;
#endif
        }
        [Conditional("UNITY_EDITOR"),Conditional("DEVELOPMENT_BUILD")]
        private void EndOpeningTrace()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SampleOpeningTrace();
            TraceOpeningPhase("closed");
            var text=new StringBuilder("MATCHWORKS_STARTUP ");
            text.Append($"mode={(openingTraceReplay?"replay":"cold")} platform={Application.platform} ");
            text.Append($"elapsedMs={(Time.realtimeSinceStartupAsDouble-openingTraceStarted)*1000:F1} ");
            text.Append($"updates={openingTraceFrames} worstUpdateGapMs={openingTraceWorst:F1} gapsOver50ms={openingTraceOver50} ");
            text.Append("stagesWallTime=[").Append(string.Join(", ",openingTraceStages)).Append(']');
            UnityEngine.Debug.Log(text.ToString());
#endif
        }
    }
}
