using System;
using System.Collections.Generic;

namespace WorkshopMatch
{
    // Presentation only. Independent routes share a clock; only routes that
    // would overlap wait for one another. A bend does not stop a falling piece.
    internal static class FallAnimationPlan
    {
        internal sealed class Motion
        {
            internal readonly int Piece;
            internal readonly bool Entry;
            internal float Duration { get; private set; }
            internal float Start;
            internal float End => Start + Duration;
            private readonly IReadOnlyList<Match3BoardModel.Cell> path;
            private readonly float[] distances;
            private readonly float[] legSpeedLimits;
            private readonly float ceiling;
            private float speed;
            private float[] handoffStarts, handoffEnds;
            private bool classicVertical;
            private float sourceClock, playbackClock, sourceStart;
            internal bool Continuous => handoffStarts == null;
            private readonly int minX, maxX, minY;
            private readonly float maxY;

            internal Motion(int piece, IReadOnlyList<Match3BoardModel.Cell> route, bool entry, int top, float cellsPerSecond)
            {
                Piece = piece; path = route; Entry = entry; ceiling = top + .5f; speed = cellsPerSecond;
                Duration = cellsPerSecond <= 0 ? 0 : (path[0].Y - path[path.Count-1].Y) / speed;
                distances = new float[path.Count];
                minX = maxX = path[0].X; minY = path[0].Y;
                maxY = entry ? Math.Min(ceiling, path[0].Y) : path[0].Y;
                for (int i = 1; i < path.Count; i++)
                {
                    distances[i] = distances[i-1] + path[i-1].Y - path[i].Y;
                    minX = Math.Min(minX, path[i].X); maxX = Math.Max(maxX, path[i].X);
                    minY = Math.Min(minY, path[i].Y);
                }
                legSpeedLimits=new float[Math.Max(0,path.Count-1)];
                for(int first=0;first<legSpeedLimits.Length;)
                {
                    int last=first;
                    int dx=path[first+1].X-path[first].X,dy=path[first+1].Y-path[first].Y;
                    while(last+1<legSpeedLimits.Length)
                    {
                        int nx=path[last+2].X-path[last+1].X,ny=path[last+2].Y-path[last+1].Y;
                        if(dx*ny!=dy*nx)break;
                        last++;
                    }
                    float legTop=entry&&first==0?Math.Min(path[first].Y,top+1f):path[first].Y;
                    float height=legTop-path[last+1].Y;
                    float length=(float)Math.Sqrt((path[last+1].X-path[first].X)*(path[last+1].X-path[first].X)+height*height);
                    length=Math.Max(1,length);
                    float limit=1.5f*length/(.32f+.060f*length);
                    for(int s=first;s<=last;s++)legSpeedLimits[s]=limit;
                    first=last+1;
                }
            }

            internal void PositionAt(float time, out float x, out float y)
            {
                if(sourceClock>0)time=sourceClock*Ease(time/playbackClock);
                if (handoffStarts != null)
                {
                    int segment = 0;
                    while (segment < handoffEnds.Length-1 && time >= handoffEnds[segment]) segment++;
                    float blendTime = Math.Max(0, Math.Min(1, (time-handoffStarts[segment]) /
                        (handoffEnds[segment]-handoffStarts[segment])));
                    x = path[segment].X + (path[segment+1].X-path[segment].X)*blendTime;
                    y = path[segment].Y + (path[segment+1].Y-path[segment].Y)*blendTime;
                    return;
                }
                float t = Math.Max(0, time - (sourceClock>0?sourceStart:Start));
                // All pieces use the same downward speed. Following candies
                // keep their spacing through bends instead of restarting easing.
                float distance = classicVertical ? Ease(t/Duration)*distances[distances.Length-1] : Duration <= 0 ? distances[distances.Length-1] :
                    Math.Min(distances[distances.Length-1], speed*t);
                int end = 1;
                while (end < path.Count-1 && distances[end] < distance) end++;
                if (path.Count == 1) { x = path[0].X; y = path[0].Y; return; }
                float length = distances[end] - distances[end-1];
                float blend = length <= 0 ? 1 : (distance - distances[end-1]) / length;
                x = path[end-1].X + (path[end].X - path[end-1].X) * blend;
                y = path[end-1].Y + (path[end].Y - path[end-1].Y) * blend;
            }

            internal void SetClassicVertical(float duration)
            {
                classicVertical=true;Start=0;Duration=duration;
            }

            internal void UseSharedClock(float sourceDuration,float playbackDuration)
            {
                float sourceEnd=End;
                sourceStart=Start;sourceClock=sourceDuration;playbackClock=playbackDuration;
                Start=playbackDuration*InverseEase(sourceStart/sourceDuration);
                Duration=playbackDuration*InverseEase(sourceEnd/sourceDuration)-Start;
            }

            internal float VisiblePaceScale()
            {
                // Compare actual visible travel with the SAME SmoothStep pace as
                // an ordinary move of this leg's length, rather than total wave time.
                const int samples=512;
                float scale=1,dt=Duration/samples;
                if(dt<=0)return scale;
                PositionAt(Start,out float ax,out float ay);
                for(int i=1;i<=samples;i++)
                {
                    PositionAt(Start+i*dt,out float bx,out float by);
                    if(!Entry||Math.Min(ay,by)<=ceiling+.5f)
                    {
                        int segment=0;float middle=(ay+by)*.5f;
                        while(segment<legSpeedLimits.Length-1&&middle<path[segment+1].Y)segment++;
                        float vx=bx-ax,vy=by-ay;
                        scale=Math.Max(scale,(float)Math.Sqrt(vx*vx+vy*vy)/(dt*legSpeedLimits[segment]));
                    }
                    ax=bx;ay=by;
                }
                return scale;
            }

            internal void StretchPlayback(float scale)
            {
                Start*=scale;Duration*=scale;playbackClock*=scale;
            }

            private static float Ease(float t)
            { t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t); }

            private static float InverseEase(float progress)
            {
                if(progress<=0)return 0;if(progress>=1)return 1;
                float lo=0,hi=1;
                for(int i=0;i<20;i++){float mid=(lo+hi)*.5f;if(Ease(mid)<progress)lo=mid;else hi=mid;}
                return (lo+hi)*.5f;
            }

            internal void SetHandoffs(float[] starts, float[] ends)
            {
                handoffStarts=starts; handoffEnds=ends;
                Start=starts[0]; Duration=ends[ends.Length-1]-Start;
            }

            private bool Visible(float time, float y) => !Entry || time >= Start && y <= ceiling;

            internal bool SharesArea(Motion other) => !(minX > other.maxX + .70f || other.minX > maxX + .70f ||
                minY > other.maxY + .70f || other.minY > maxY + .70f);

            internal bool SeparatedFrom(Motion other, float tick)
            {
                // A legal one-cell diagonal passes the occupied corner at
                // sqrt(.5) cells; reject crossings closer than that route.
                const float clearance = .70f;
                if (!SharesArea(other)) return true;
                float until = Math.Max(End, other.End);
                bool previousVisible = false;
                float previousX = 0, previousY = 0;
                for (float time = 0; ; time = Math.Min(until, time + tick))
                {
                    PositionAt(time, out float x, out float y);
                    other.PositionAt(time, out float ox, out float oy);
                    bool visible = Visible(time, y) && other.Visible(time, oy);
                    float rx = x - ox, ry = y - oy;
                    if (visible)
                    {
                        if (rx*rx + ry*ry < clearance*clearance) return false;
                        if (previousVisible)
                        {
                            // Check between samples as well, including diagonal crossings.
                            float vx = rx - previousX, vy = ry - previousY, vv = vx*vx + vy*vy;
                            float t = vv < 1e-9f ? 0 : Math.Max(0, Math.Min(1, -(previousX*vx + previousY*vy)/vv));
                            float cx = previousX + t*vx, cy = previousY + t*vy;
                            if (cx*cx + cy*cy < clearance*clearance) return false;
                        }
                    }
                    previousVisible = visible; previousX = rx; previousY = ry;
                    if (time >= until) return true;
                }
            }
        }

        internal static List<Motion> Build(IReadOnlyList<Match3BoardModel.FallMove> falls)
        {
            bool verticalOnly=true;
            float longestExisting=0,longestEntry=0;
            float longest = 0;
            foreach (var fall in falls)
            {
                longest=Math.Max(longest,fall.From.Y-fall.To.Y);
                var path=Route(fall);float distance=0;
                for(int s=1;s<path.Count;s++)
                {
                    int dx=path[s].X-path[s-1].X,dy=path[s].Y-path[s-1].Y;
                    if(dx!=0)verticalOnly=false;
                    distance+=(float)Math.Sqrt(dx*dx+dy*dy);
                }
                if(fall.Spawned)longestEntry=Math.Max(longestEntry,distance);
                else longestExisting=Math.Max(longestExisting,distance);
            }
            // Preserve the original vertical phase duration and SmoothStep,
            // including its existing-piece priority over longer offscreen queues.
            float playbackDuration=.32f+.060f*(longestExisting>0?longestExisting:longestEntry);
            if(verticalOnly)
            {
                var vertical=new List<Motion>();
                for(int p=0;p<falls.Count;p++)
                {
                    var fall=falls[p];if(fall.From.Equals(fall.To))continue;
                    var motion=new Motion(p,Route(fall),fall.Spawned,fall.SegmentTop,0);
                    motion.SetClassicVertical(playbackDuration);vertical.Add(motion);
                }
                return vertical;
            }
            float speed = Math.Max(12f, longest/.60f);
            var pending = new List<Motion>();
            for (int p = 0; p < falls.Count; p++)
            {
                var fall = falls[p]; var path = Route(fall);
                bool moves = path.Count > 1 && !fall.From.Equals(fall.To);
                var motion = new Motion(p, path, fall.Spawned, fall.SegmentTop, moves ? speed : 0);
                pending.Add(motion);
            }
            var result = new List<Motion>();
            while (pending.Count > 0)
            {
                var group = new List<Motion> { pending[0] }; pending.RemoveAt(0);
                for (int i = 0; i < group.Count; i++)
                    for (int j = pending.Count-1; j >= 0; j--)
                        if (group[i].SharesArea(pending[j])) { group.Add(pending[j]); pending.RemoveAt(j); }
                result.AddRange(ScheduleGroup(group, falls, speed));
            }
            float end = 0;
            foreach (var motion in result) end = Math.Max(end, motion.End);
            // Re-time the concurrent routes together to the original fall pace.
            // One easing clock preserves spacing without restarting at each bend.
            if(end>0)foreach(var motion in result)motion.UseSharedClock(end,playbackDuration);
            float paceScale=1;
            foreach(var motion in result)paceScale=Math.Max(paceScale,motion.VisiblePaceScale());
            // Keep all trajectories on the same clock, preserving their spacing.
            // Only anomalously fast mixed refills are stretched; never squeeze a
            // short visible leg into a fraction of the overall refill duration.
            if(paceScale>1.001f)foreach(var motion in result)motion.StretchPlayback(paceScale*1.002f);
            return result;
        }

        private static List<Motion> ScheduleGroup(List<Motion> group, IReadOnlyList<Match3BoardModel.FallMove> falls, float speed)
        {
            // One greedy order can fail even when continuous routes fit. Retry
            // deterministic model/height precedences before requiring handoffs.
            for(int order=0;order<8;order++)
            {
                var scheduled=TryScheduleGroup(group,falls,speed,order);
                if(scheduled!=null)return scheduled;
            }
            return ScheduleHandoffs(group,falls);
        }

        private static List<Motion> TryScheduleGroup(List<Motion> group, IReadOnlyList<Match3BoardModel.FallMove> falls, float speed,int order)
        {
            var waiting = new List<Motion>();
            var occupied = new List<Motion>();
            foreach (var motion in group)
                if (motion.Duration > 0) waiting.Add(motion); else occupied.Add(motion);
            // Lower pieces lead. Their followers can start with them if the
            // complete routes keep enough space, including offscreen births.
            waiting.Sort((a,b) => {
                if(order>0)
                {
                    var af=falls[a.Piece];var bf=falls[b.Piece];
                    int ap=af.PathStages==null||af.PathStages.Count==0?0:af.PathStages[order%2==0?af.PathStages.Count-1:0];
                    int bp=bf.PathStages==null||bf.PathStages.Count==0?0:bf.PathStages[order%2==0?bf.PathStages.Count-1:0];
                    int priority=order<5?ap.CompareTo(bp):af.From.Y.CompareTo(bf.From.Y);
                    if(order==3||order==4||order==7)priority=-priority;
                    if(priority!=0)return priority;
                }
                int row = falls[a.Piece].To.Y.CompareTo(falls[b.Piece].To.Y);
                if (row != 0) return row;
                row = falls[a.Piece].From.Y.CompareTo(falls[b.Piece].From.Y);
                return row != 0 ? row : a.Piece.CompareTo(b.Piece);
            });
            var result = new List<Motion>();
            float end = 0, tick = 1 / (speed * 32);
            foreach (var motion in waiting)
            {
                bool placed = false;
                for (int delay = 0; delay <= (int)Math.Ceiling(end/tick) + 2; delay++)
                {
                    motion.Start = delay*tick;
                    bool clear = true;
                    foreach (var other in occupied)
                        if (!motion.SeparatedFrom(other, tick)) { clear = false; break; }
                    if (!clear) continue;
                    placed = true; break;
                }
                if (!placed) return null;
                occupied.Add(motion); result.Add(motion); end = Math.Max(end, motion.End);
            }
            return result;
        }

        private static IReadOnlyList<Match3BoardModel.Cell> Route(Match3BoardModel.FallMove fall)
            => fall.Path ?? new[] { fall.From, fall.To };

        private static List<Motion> ScheduleHandoffs(List<Motion> group, IReadOnlyList<Match3BoardModel.FallMove> falls)
        {
            // A rare weaving route can genuinely need an intermediate handoff.
            // Keep its causal order on ONE short clock, with linear waypoint
            // transitions; never restart a full easing animation for each stage.
            var stages=new SortedSet<int>();
            foreach(var motion in group)
            {
                var fall=falls[motion.Piece];var path=Route(fall);
                for(int s=0;s<path.Count-1;s++)stages.Add(fall.PathStages==null?s:fall.PathStages[s]);
            }
            var times=new Dictionary<int,float>();int rank=0;
            float tick=.60f/Math.Max(1,stages.Count);
            foreach(int stage in stages)times[stage]=rank++*tick;
            var result=new List<Motion>();
            foreach(var motion in group)
            {
                var fall=falls[motion.Piece];var path=Route(fall);
                if(path.Count<2||fall.From.Equals(fall.To))continue;
                var starts=new float[path.Count-1];var ends=new float[path.Count-1];
                for(int s=0;s<starts.Length;s++)
                { starts[s]=times[fall.PathStages==null?s:fall.PathStages[s]];ends[s]=starts[s]+tick; }
                motion.SetHandoffs(starts,ends);result.Add(motion);
            }
            return result;
        }
    }
}
