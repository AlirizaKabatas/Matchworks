using System;
using System.Collections.Generic;

namespace WorkshopMatch
{
    // Presentation only: move a birth into an earlier batch when its whole
    // straight path is clear. Model decisions, RNG and path order stay intact.
    internal static class FallAnimationPlan
    {
        internal readonly struct Motion
        {
            internal Motion(int piece,int segment,Match3BoardModel.Cell from,Match3BoardModel.Cell to,bool entry)
            {Piece=piece;Segment=segment;From=from;To=to;Entry=entry;}
            internal readonly int Piece,Segment;
            internal readonly Match3BoardModel.Cell From,To;
            internal readonly bool Entry;
        }

        internal static List<List<Motion>> Build(IReadOnlyList<Match3BoardModel.FallMove> falls,Match3BoardModel.BoardFrame frame)
        {
            var phases=new SortedDictionary<int,List<Motion>>();
            var births=new List<(int stage,Motion motion)>();
            for(int p=0;p<falls.Count;p++)
            {
                var fall=falls[p];var path=fall.Path??new[]{fall.From,fall.To};
                for(int s=0;s<path.Count-1;s++)
                {
                    if(path[s].Equals(path[s+1]))continue;
                    int stage=fall.PathStages==null?s:fall.PathStages[s];
                    if(!phases.TryGetValue(stage,out var batch))phases[stage]=batch=new();
                    var motion=new Motion(p,s,path[s],path[s+1],fall.Spawned&&s==0);
                    batch.Add(motion);if(motion.Entry)births.Add((stage,motion));
                }
            }
            births.Sort((a,b)=>a.stage.CompareTo(b.stage));
            foreach(var birth in births)
            {
                int lastTouch=-1;
                foreach(var phase in phases)
                {
                    if(phase.Key>=birth.stage)break;
                    foreach(var m in phase.Value)
                        if(m.From.Equals(birth.motion.To)||m.To.Equals(birth.motion.To))lastTouch=phase.Key;
                }
                var positions=new Dictionary<int,Match3BoardModel.Cell>();
                for(int p=0;p<falls.Count;p++)if(!falls[p].Spawned)positions[p]=falls[p].From;
                foreach(var phase in phases)
                {
                    if(phase.Key>=birth.stage)break;
                    if(phase.Key>=lastTouch&&CanJoin(birth.motion,phase.Value,positions,frame))
                    {
                        phases[birth.stage].Remove(birth.motion);phase.Value.Add(birth.motion);break;
                    }
                    foreach(var m in phase.Value)positions[m.Piece]=m.To;
                }
            }
            var result=new List<List<Motion>>();
            foreach(var phase in phases.Values)if(phase.Count>0)result.Add(phase);
            return result;
        }

        private static bool CanJoin(Motion entry,List<Motion> batch,Dictionary<int,Match3BoardModel.Cell> positions,Match3BoardModel.BoardFrame frame)
        {
            foreach(var pair in positions)
            {
                var to=pair.Value;
                foreach(var m in batch)if(m.Piece==pair.Key){to=m.To;break;}
                if(!Separated(entry.From,entry.To,pair.Value,to))return false;
            }
            foreach(var m in batch)
                if(m.Entry&&!positions.ContainsKey(m.Piece)&&!Separated(entry.From,entry.To,m.From,m.To))return false;
            if(frame!=null)for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++)
                if(frame.GetContent(x,y) is CellContent.Motor or CellContent.FrozenGrease)
                {
                    var cell=new Match3BoardModel.Cell(x,y);
                    if(!Separated(entry.From,entry.To,cell,cell))return false;
                }
            return true;
        }

        private static bool Separated(Match3BoardModel.Cell a,Match3BoardModel.Cell b,Match3BoardModel.Cell c,Match3BoardModel.Cell d)
        {
            double x=a.X-c.X,y=a.Y-c.Y,vx=b.X-a.X-d.X+c.X,vy=b.Y-a.Y-d.Y+c.Y;
            double vv=vx*vx+vy*vy,t=vv<1e-9?0:Math.Max(0,Math.Min(1,-(x*vx+y*vy)/vv));
            x+=vx*t;y+=vy*t;
            return x*x+y*y>=.999;
        }
    }
}
