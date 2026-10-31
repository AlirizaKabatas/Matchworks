using System;
using System.Collections.Generic;
using System.Linq;
using WorkshopMatch;
using C=WorkshopMatch.Match3BoardModel.Cell;

partial class Program
{
    static void AuditHammerPace()
    {
        foreach(var tool in new[]{SpecialKind.Solvent,SpecialKind.RayVertical,SpecialKind.RayHorizontal})
        for(int y=0;y<8;y++)for(int x=0;x<8;x++)
        {
            var board=TutorialBoard(8);
            if(board.GetContent(x,y)!=CellContent.Candy)continue;
            var result=board.DebugActivatePower(new C(x,y),tool);int wave=0;
            foreach(var step in result.Steps.Where(s=>s.HasFall&&s.Falls.Count>0))
            {
                var plan=FallAnimationPlan.Build(step.Falls);float max=0;
                foreach(var motion in plan)
                {
                    for(int i=1;i<=512;i++)
                    {
                        float dt=motion.Duration/512;
                        motion.PositionAt(motion.Start+(i-1)*dt,out float ax,out float ay);
                        motion.PositionAt(motion.Start+i*dt,out float bx,out float by);
                        max=Math.Max(max,(float)Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay))/dt);
                    }
                }
                bool mixed=step.Falls.Any(f=>f.Path.Any(p=>p.X!=f.Path[0].X));
                bool normal=!mixed||ShortLegsKeepNormalPace(step.Falls,plan);
                float end=plan.Count==0?0:plan.Max(m=>m.End);
                Console.WriteLine($"AUDIT tool={tool} center={x},{y} wave={++wave} end={end:F3} peak={max:F2} handoffs={plan.Count(m=>!m.Continuous)} mixed={mixed} normal={normal}");
            }
        }
    }
    static bool PlannedFallsDoNotOverlap(List<Match3BoardModel.FallMove> falls)
    {
        var plan=FallAnimationPlan.Build(falls);
        float end=plan.Count==0?0:plan.Max(m=>m.End);
        for(int sample=0;sample<=256;sample++)
        {
            float time=end*sample/256;
            var positions=new List<(int piece,float x,float y)>();
            for(int p=0;p<falls.Count;p++)
            {
                var fall=falls[p];float x=fall.From.X,y=fall.From.Y;
                foreach(var motion in plan)
                    if(motion.Piece==p&&time>=motion.Start)motion.PositionAt(time,out x,out y);
                if(fall.Spawned&&y>fall.SegmentTop+.5f)continue;
                foreach(var other in positions)
                    if((x-other.x)*(x-other.x)+(y-other.y)*(y-other.y)<.69f*.69f)
                    {
                        Console.WriteLine($"OVERLAP time={time} piece={p} at={x},{y} other={other.piece}:{other.x},{other.y} plan={plan.Count} unique={plan.Select(m=>m.Piece).Distinct().Count()} routes={falls.Count}");
                        foreach(int id in new[]{p,other.piece})Console.WriteLine($"ROUTE {id} spawn={falls[id].Spawned} top={falls[id].SegmentTop} {string.Join("->",falls[id].Path)} stages={string.Join(",",falls[id].PathStages)} times={string.Join(",",plan.Where(m=>m.Piece==id).Select(m=>$"{m.Start}:{m.End}"))}");
                        return false;
                    }
                positions.Add((p,x,y));
                if(sample==256&&(Math.Abs(x-fall.To.X)>.001f||Math.Abs(y-fall.To.Y)>.001f))return false;
            }
        }
        return true;
    }
    static void RunFallAnimationChecks()
    {
        CheckHammerRefillTiming();
        CheckClassicVerticalPace();
        CheckMotorToolPace();
        var open=Board();GravityEmpty(open,3,0);GravityEmpty(open,3,1);GravityEmpty(open,3,2);
        var openFalls=SettleGravity(open);string snapshot=open.Snapshot();
        var plan=FallAnimationPlan.Build(openFalls);
        Check(plan.All(m=>m.Start==0)&&plan.Any(m=>m.Entry)&&plan.Any(m=>!m.Entry),
            "top candies and existing vertical stack share the same fall clock");
        Check(plan.Count(m=>m.Entry)==3,"all three queued top candies join the continuous fall");
        Check(open.Snapshot()==snapshot,"animation planning cannot change board RNG state moves or layout");

        var workshop=Board();
        workshop.PlaceMotor(new C(4,4),3,2,5);
        workshop.PlaceObstacle(new C(0,4),CellContent.FrozenGrease,1);
        workshop.PlaceObstacle(new C(1,4),CellContent.FrozenGrease,1);
        GravityEmpty(workshop,4,3);GravityEmpty(workshop,0,3);
        var workshopFalls=SettleGravity(workshop);
        plan=FallAnimationPlan.Build(workshopFalls);
        int purple=workshopFalls.FindIndex(f=>!f.Spawned&&f.From.Equals(new C(3,4))&&f.To.Equals(new C(4,3)));
        int yellow=workshopFalls.FindIndex(f=>!f.Spawned&&f.From.Equals(new C(3,5))&&f.To.Equals(new C(3,4)));
        int top=workshopFalls.FindIndex(f=>f.Spawned&&f.To.Equals(new C(3,7)));
        int local=workshopFalls.FindIndex(f=>f.Spawned&&f.To.Equals(new C(0,3)));
        Check(purple>=0&&yellow>=0&&top>=0&&local>=0,"motor-side reproduction preserves diagonal, following stack and both spawn sources");
        Check(new[]{purple,yellow,top,local}.All(p=>plan.Single(m=>m.Piece==p).Start==0),
            "diagonal candy, following candy, top birth and independent local birth start together");
        Check(Math.Abs(plan.Max(m=>m.End)-(.32f+.060f*(float)Math.Sqrt(2)))<.0001f,
            "ordinary one-diagonal refill keeps its original timing without a pace correction");
        Check(PlannedFallsDoNotOverlap(workshopFalls),
            "concurrent motor-side refill neither intersects pieces nor changes final cells");

        var unsafeFalls=new List<Match3BoardModel.FallMove>{
            new(new C(0,2),new C(1,0),false,7,new[]{new C(0,2),new C(0,1),new C(1,0)},new[]{0,1}),
            new(new C(0,3),new C(0,0),true,7,new[]{new C(0,3),new C(0,0)},new[]{2}),
            new(new C(5,6),new C(5,5),false,7,new[]{new C(5,6),new C(5,5)},new[]{3})};
        plan=FallAnimationPlan.Build(unsafeFalls);
        Check(plan.All(m=>m.Start==0),"shared downward speed lets a following birth move without waiting");
        Check(plan.Single(m=>m.Piece==2).Start==0,"unrelated column does not wait for another route's handoffs");
        Check(PlannedFallsDoNotOverlap(unsafeFalls),"crossing-path scheduling preserves spacing throughout motion");

        var crossing=new List<Match3BoardModel.FallMove>{
            new(new C(0,2),new C(1,1),false,7,new[]{new C(0,2),new C(1,1)},new[]{0}),
            new(new C(1,2),new C(0,1),false,7,new[]{new C(1,2),new C(0,1)},new[]{1})};
        plan=FallAnimationPlan.Build(crossing);
        Check(plan.Any(m=>m.Start>0)&&PlannedFallsDoNotOverlap(crossing),"opposite diagonals receive only local headway without intersecting");

        var weaving=new List<Match3BoardModel.FallMove>{
            new(new C(0,3),new C(0,1),false,7,new[]{new C(0,3),new C(1,2),new C(0,1)},new[]{0,4}),
            new(new C(0,5),new C(1,0),false,7,new[]{new C(0,5),new C(0,3),new C(0,1),new C(1,0)},new[]{1,2,3})};
        plan=FallAnimationPlan.Build(weaving);
        Check(plan.Count==2&&PlannedFallsDoNotOverlap(weaving),
            "necessary intermediate handoffs preserve spacing without compressed playback");

        var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<50;i++)FallAnimationPlan.Build(workshopFalls);
        Console.WriteLine($"TIMING motor-side animation plan average_ms={timer.Elapsed.TotalMilliseconds/50:F3}");
    }

    static void CheckClassicVerticalPace()
    {
        foreach(int depth in new[]{1,2,3,5})
        {
            var board=Board();
            // An ordinary horizontal match leaves a gap in neighbouring columns.
            for(int x=2;x<=4;x++)for(int y=0;y<depth;y++)GravityEmpty(board,x,y);
            var falls=SettleGravity(board);var plan=FallAnimationPlan.Build(falls);
            float expectedDuration=.32f+.060f*depth;
            Check(plan.All(m=>m.Start==0&&Math.Abs(m.Duration-expectedDuration)<.00001f),
                "ordinary vertical drop retains original duration for "+depth+" rows");
            foreach(float fraction in new[]{.1f,.25f,.5f,.75f,.9f})
            {
                float eased=fraction*fraction*(3-2*fraction);
                Check(plan.All(m=>{
                    m.PositionAt(expectedDuration*fraction,out float x,out float y);
                    var fall=falls[m.Piece];
                    return Math.Abs(x-fall.From.X)<.00001f&&Math.Abs(y-(fall.From.Y+(fall.To.Y-fall.From.Y)*eased))<.0001f;
                }),"ordinary vertical drop retains original SmoothStep at "+fraction+" for "+depth+" rows");
            }
        }
    }

    static void CheckHammerRefillTiming()
    {
        // Ali's screenshot, top row first. Reproduction only; no level JSON edits.
        string[] rows={"GBPGBPYG","PRYRYPRY","YGYPBBBY","YGGBBBBG",
            "GPBBPRYP","BRGGPRBP","PPGBYPGR","PRYRRHRR"};
        var board=Board();
        for(int row=0;row<8;row++)for(int x=0;x<8;x++)
        {
            int color="RBGYP".IndexOf(rows[row][x]);
            board.DebugSetCandy(new C(x,7-row),color>=0?color:3);
        }
        board.PlaceMotor(new C(4,4),3,2,5);
        board.DebugPlaceSpecial(new C(5,0),SpecialKind.Solvent,3);
        board.SealObjectives();
        var result=board.TrySwap(new C(5,0),new C(5,1));
        Check(result.Accepted,"screenshot hammer-up swap reproduces cleared motor-side pocket");
        int wave=0;
        foreach(var step in result.Steps.Where(s=>s.HasFall&&s.Falls.Count>0))
        {
            var plan=FallAnimationPlan.Build(step.Falls);
            float end=plan.Count==0?0:plan.Max(m=>m.End);
            Console.WriteLine($"HAMMER_REFILL wave={++wave} seconds={end:F3} motions={plan.Count} pieces={plan.Select(m=>m.Piece).Distinct().Count()}");
            Check(end<3f,"screenshot hammer refill flows without the former eight-second stage backlog");
            Check(plan.GroupBy(m=>m.Piece).All(g=>g.Count()==1)&&plan.All(m=>m.Continuous),"screenshot hammer routes never fall back to stop-start stage playback");
            Check(PlannedFallsDoNotOverlap(step.Falls.ToList()),"screenshot hammer pieces preserve spacing through the complete refill");
        }
        Check(wave>0,"screenshot hammer produces actual falling waves");
    }

    static float AtHeight(FallAnimationPlan.Motion motion,float height)
    {
        float lo=motion.Start,hi=motion.End;
        for(int i=0;i<28;i++)
        {
            float mid=(lo+hi)*.5f;motion.PositionAt(mid,out _,out float y);
            if(y>height)lo=mid;else hi=mid;
        }
        return (lo+hi)*.5f;
    }

    static bool ShortLegsKeepNormalPace(IReadOnlyList<Match3BoardModel.FallMove> falls,List<FallAnimationPlan.Motion> plan)
    {
        foreach(var motion in plan)
        {
            var fall=falls[motion.Piece];var path=fall.Path;
            for(int s=0;s<path.Count-1;s++)
            {
                int dx=path[s+1].X-path[s].X,dy=path[s+1].Y-path[s].Y;
                if(Math.Abs(dx)!=1||dy!=-1)continue;
                if(s>0&&(path[s].X-path[s-1].X)*dy==(path[s].Y-path[s-1].Y)*dx)continue;
                if(s+2<path.Count&&(path[s+2].X-path[s+1].X)*dy==(path[s+2].Y-path[s+1].Y)*dx)continue;
                float halfTravel=AtHeight(motion,path[s].Y-.75f)-AtHeight(motion,path[s].Y-.25f);
                float normalDuration=.32f+.060f*(float)Math.Sqrt(2);
                // The middle half cannot be crossed faster than at the peak
                // speed of a normal one-cell diagonal SmoothStep animation.
                if(halfTravel<normalDuration/3*.99f)return false;
            }
            if(fall.Spawned&&path.Count==2&&path[0].X==path[1].X&&fall.SegmentTop-path[1].Y==0)
            {
                float halfTravel=AtHeight(motion,fall.To.Y+.25f)-AtHeight(motion,fall.To.Y+.75f);
                if(halfTravel<.38f/3*.99f)return false;
            }
        }
        return true;
    }

    static void CheckMotorToolPace()
    {
        foreach(var tool in new[]{SpecialKind.Solvent,SpecialKind.RayVertical,SpecialKind.RayHorizontal})
        foreach(var center in new[]{new C(5,1),new C(4,2),new C(6,2),new C(5,3),new C(3,3),new C(1,1)})
        {
            var board=TutorialBoard(8);var result=board.DebugActivatePower(center,tool);int checkedWaves=0;
            foreach(var step in result.Steps.Where(s=>s.HasFall&&s.Falls.Count>0))
            {
                if(!step.Falls.Any(f=>f.Path.Any(p=>p.X!=f.Path[0].X)))continue;
                var plan=FallAnimationPlan.Build(step.Falls);
                Check(ShortLegsKeepNormalPace(step.Falls,plan),
                    $"level 8 {tool} at {center} preserves normal visible diagonal and birth pace, wave {++checkedWaves}");
                if(center.Equals(new C(5,1)))Check(plan.All(m=>m.Continuous),
                    "reported tool location uses complete routes rather than compressed stage handoffs: "+tool);
            }
        }
    }
}
