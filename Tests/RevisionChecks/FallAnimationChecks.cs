using System;
using System.Collections.Generic;
using System.Linq;
using WorkshopMatch;
using C=WorkshopMatch.Match3BoardModel.Cell;

partial class Program
{
    static Match3BoardModel.BoardFrame AnimationFrame(Match3BoardModel board)=>(Match3BoardModel.BoardFrame)typeof(Match3BoardModel).GetMethod("CaptureFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(board,null);
    static bool PlannedStagesDoNotOverlap(List<Match3BoardModel.FallMove> falls,Match3BoardModel.BoardFrame frame)
    {
        var occupied=new HashSet<C>(falls.Where(f=>!f.Spawned).Select(f=>f.From));
        foreach(var batch in FallAnimationPlan.Build(falls,frame))
        {
            foreach(var motion in batch)if(!motion.Entry&&!occupied.Remove(motion.From))return false;
            foreach(var motion in batch)if(!occupied.Add(motion.To))return false;
        }
        return occupied.SetEquals(falls.Select(f=>f.To));
    }
    static void RunFallAnimationChecks()
    {
        var open=Board();GravityEmpty(open,3,0);GravityEmpty(open,3,1);GravityEmpty(open,3,2);
        var openFalls=SettleGravity(open);string snapshot=open.Snapshot();
        var plan=FallAnimationPlan.Build(openFalls,AnimationFrame(open));
        Check(plan[0].Any(m=>m.Entry)&&plan[0].Any(m=>!m.Entry),"new top candies begin with existing vertical fall instead of waiting");
        Check(plan.SelectMany(b=>b).Count(m=>m.Entry)==3,"all three queued top candies join the continuous fall");
        Check(open.Snapshot()==snapshot,"animation planning cannot change board RNG state moves or layout");

        var unsafeFalls=new List<Match3BoardModel.FallMove>{
            new(new C(0,2),new C(1,1),false,7,new[]{new C(0,2),new C(0,1),new C(1,1)},new[]{0,1}),
            new(new C(0,3),new C(0,1),true,7,new[]{new C(0,3),new C(0,1)},new[]{2})};
        plan=FallAnimationPlan.Build(unsafeFalls,null);
        Check(plan.FindIndex(b=>b.Any(m=>m.Entry))>plan.FindIndex(b=>b.Any(m=>m.Piece==0&&m.Segment==1)),"spawn waits when joining a lateral departure would intersect its path");

    }
}
