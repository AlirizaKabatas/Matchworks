using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WorkshopMatch;
using C = WorkshopMatch.Match3BoardModel.Cell;

partial class Program
{
    static List<Match3BoardModel.FallMove> SettleGravity(Match3BoardModel board)
    {
        var falls=new List<Match3BoardModel.FallMove>();
        typeof(Match3BoardModel).GetMethod("CollapseAndRefill",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,new object[]{falls});
        return falls;
    }
    static void GravityEmpty(Match3BoardModel board,int x,int y)
    {
        ((CellContent[,])typeof(Match3BoardModel).GetField("contents",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board))[x,y]=CellContent.Empty;
    }
    static bool GravityStagesDoNotOverlap(List<Match3BoardModel.FallMove> falls)
    {
        var occupied=new HashSet<C>(falls.Where(f=>!f.Spawned).Select(f=>f.From));
        var stages=new SortedDictionary<int,List<(C from,C to,bool firstSpawn)>>();
        foreach(var fall in falls)
        {
            if(fall.PathStages==null || fall.PathStages.Count!=fall.Path.Count-1)return false;
            for(int i=0;i<fall.PathStages.Count;i++)
            {
                int stage=fall.PathStages[i];
                if(i>0 && stage<=fall.PathStages[i-1])return false;
                if(!stages.TryGetValue(stage,out var batch))stages[stage]=batch=new();
                batch.Add((fall.Path[i],fall.Path[i+1],fall.Spawned&&i==0));
            }
        }
        foreach(var batch in stages.Values)
        {
            foreach(var move in batch)if(!move.firstSpawn && !occupied.Remove(move.from))return false;
            foreach(var move in batch)if(!occupied.Add(move.to))return false;
        }
        return occupied.Count==falls.Count;
    }
    static void RunGravityChecks()
    {
        var open=Board();
        GravityEmpty(open,3,2);GravityEmpty(open,3,3);
        Check(SettleGravity(open).All(f=>f.From.X==f.To.X),"unobstructed board retains vertical gravity");
        var board=Board();
        board.PlaceObstacle(new C(3,4),CellContent.FrozenGrease,1);
        board.DebugPlaceSpecial(new C(2,4),SpecialKind.RayHorizontal,2);
        board.SetOil(new C(2,4));
        GravityEmpty(board,3,3);
        var falls=SettleGravity(board);
        var diagonal=falls.FirstOrDefault(f=>f.From.Equals(new C(2,4))&&f.To.Equals(new C(3,3)));
        Check(diagonal.Path!=null && board.GetSpecial(3,3)==SpecialKind.RayHorizontal && board.GetColor(3,3)==2,"diagonal supplies blocked pocket and preserves special/color");
        Check(board.HasOil(2,4) && !board.HasOil(3,3),"diagonal flow leaves ground oil in place");
        Check(board.GetContent(3,4)==CellContent.FrozenGrease,"diagonal flow preserves fixed blocker");
        Check(falls.Any(f=>!f.Spawned&&f.From.Equals(new C(2,5))&&f.To.Equals(new C(2,4))),
            "diagonal departure pulls the candy immediately above into its source cell");
        Check(falls.Any(f=>!f.Spawned&&f.From.Equals(new C(2,6))&&f.To.Equals(new C(2,5)))&&
            falls.Any(f=>!f.Spawned&&f.From.Equals(new C(2,7))&&f.To.Equals(new C(2,6))),
            "entire source stack follows diagonal departure in its original order");
        Check(falls.Where(f=>f.Spawned&&f.From.X==2).All(f=>f.To.Y==7),
            "replacement enters at top of source stack instead of bypassing existing candies");

        var enclosed=Board();
        for(int x=0;x<8;x++)enclosed.PlaceObstacle(new C(x,4),CellContent.FrozenGrease,1);
        GravityEmpty(enclosed,3,3);
        falls=SettleGravity(enclosed);
        Check(enclosed.GetContent(3,3)==CellContent.Candy && falls.Any(f=>f.Spawned&&f.To.Equals(new C(3,3))&&f.SegmentTop==3),"enclosed pocket retains local refill");

        for(int seed=1;seed<=40;seed++)
        {
            var a=Board();var b=Board();var random=new Random(seed);
            for(int y=0;y<7;y++)for(int x=0;x<8;x++)
            {
                int kind=random.Next(7);
                if(kind==0){a.PlaceObstacle(new C(x,y),CellContent.FrozenGrease,1);b.PlaceObstacle(new C(x,y),CellContent.FrozenGrease,1);}
                else if(kind==1){a.PlaceObstacle(new C(x,y),CellContent.Bolt,2);b.PlaceObstacle(new C(x,y),CellContent.Bolt,2);}
                else if(kind<5){GravityEmpty(a,x,y);GravityEmpty(b,x,y);}
            }
            int bolts=Enumerable.Range(0,64).Count(i=>a.GetContent(i%8,i/8)==CellContent.Bolt);
            var af=SettleGravity(a);var bf=SettleGravity(b);
            Check(a.Snapshot()==b.Snapshot() && af.Count==bf.Count,"gravity deterministic scenario "+seed);
            Check(Enumerable.Range(0,64).All(i=>a.GetContent(i%8,i/8)!=CellContent.Empty) && Enumerable.Range(0,64).Count(i=>a.GetContent(i%8,i/8)==CellContent.Bolt)==bolts,"gravity fills board and conserves bolts "+seed);
            Check(af.Where(f=>!f.Spawned && a.GetContent(f.To.X,f.To.Y)==CellContent.Bolt).All(f=>f.From.X==f.To.X),"bolts stay in columns "+seed);
            Check(af.All(f=>f.Path.Zip(f.Path.Skip(1),(from,to)=>to.Y<from.Y && (from.X==to.X || Math.Abs(from.X-to.X)==1&&from.Y-to.Y==1)).All(v=>v)),"gravity paths descend with one-cell diagonals "+seed);
            Check(af.All(f=>f.Path.Skip(1).All(c=>a.GetContent(c.X,c.Y)!=CellContent.FrozenGrease)),"gravity paths avoid blockers "+seed);
            Check(GravityStagesDoNotOverlap(af),"animation stages vacate destinations before entry "+seed);
            Check(PlannedFallsDoNotOverlap(af),"continuous top spawn schedule preserves occupancy "+seed);
        }
    }
}
