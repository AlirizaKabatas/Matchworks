using System.Collections.Generic;
namespace WorkshopMatch
{
    internal static class TutorialMovePlanner
    {
        internal enum Goal { Match, Bolt, Rocket, Hammer, Magnet, Oil, Frozen, Motor }
        internal static bool FindDirect(Match3BoardModel board,Goal goal,out Match3BoardModel.Cell from,out Match3BoardModel.Cell to)
        {
            var preferred=goal==Goal.Rocket?new[]{(new Match3BoardModel.Cell(2,4),new Match3BoardModel.Cell(1,4)),(new Match3BoardModel.Cell(2,4),new Match3BoardModel.Cell(3,4))}:
                goal==Goal.Hammer?new[]{(new Match3BoardModel.Cell(4,3),new Match3BoardModel.Cell(4,2))}:new (Match3BoardModel.Cell,Match3BoardModel.Cell)[0];
            foreach(var p in preferred)if(TryGoal(board,p.Item1,p.Item2,goal)){from=p.Item1;to=p.Item2;return true;}
            foreach(var p in Pairs(board))if(TryGoal(board,p.a,p.b,goal)){from=p.a;to=p.b;return true;}
            from=to=default;return false;
        }
        internal static bool Find(Match3BoardModel board, Goal goal, out Match3BoardModel.Cell from, out Match3BoardModel.Cell to, out bool direct)
        {
            direct=true;if(FindDirect(board,goal,out from,out to))return true;
            foreach(var p in Pairs(board))
            {
                var next=board.CreatePreviewCopy();if(!next.TrySwap(p.a,p.b).Accepted || next.State!=RoundState.Playing)continue;
                if(FindDirect(next,goal,out _,out _)){from=p.a;to=p.b;direct=false;return true;}
            }
            // No unrelated move is labelled as a target hit. The caller can explain
            // the rule and release the board when no honest example is available.
            from=to=default;direct=false;return false;
        }
        internal static IEnumerable<(Match3BoardModel.Cell a,Match3BoardModel.Cell b)> Pairs(Match3BoardModel board)
        {
            for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++)
                foreach(var b in new[]{new Match3BoardModel.Cell(x+1,y),new Match3BoardModel.Cell(x,y+1)})
                {
                    var a=new Match3BoardModel.Cell(x,y);
                    if(b.X<board.Width&&b.Y<board.Height&&board.GetContent(a)==CellContent.Candy&&board.GetContent(b)==CellContent.Candy&&board.GetSpecial(x,y)==SpecialKind.None&&board.GetSpecial(b.X,b.Y)==SpecialKind.None)yield return(a,b);
                }
        }
        internal static bool Matches(Match3BoardModel.SwapResult result,Goal goal)
        {
            if(!result.Accepted||result.Steps.Count==0)return false;
            var step=result.Steps[0];if(step.Activation.HasValue)return false;
            if(goal==Goal.Match)return true;
            if(goal==Goal.Bolt||goal==Goal.Frozen||goal==Goal.Motor)
            {
                var content=goal==Goal.Bolt?CellContent.Bolt:goal==Goal.Frozen?CellContent.FrozenGrease:CellContent.Motor;
                foreach(var hit in step.HitTargets)if(step.BeforeClear.GetContent(hit.X,hit.Y)==content)return true;
                return false;
            }
            if(goal==Goal.Oil)
            {foreach(var cell in step.MatchedCells)if(step.BeforeClear.HasOil(cell.X,cell.Y))return true;return false;}
            for(int y=0;y<step.AfterFall.Height;y++)for(int x=0;x<step.AfterFall.Width;x++)
            {
                var special=step.AfterFall.GetSpecial(x,y);
                if(IsSpecial(special,goal)&&special!=step.BeforeClear.GetSpecial(x,y))return true;
            }
            return false;
        }
        internal static bool IsSpecial(SpecialKind special,Goal goal)=>goal==Goal.Rocket?(special==SpecialKind.RayHorizontal||special==SpecialKind.RayVertical):goal==Goal.Hammer?special==SpecialKind.Solvent:goal==Goal.Magnet&&special==SpecialKind.Magnet;
        private static bool TryGoal(Match3BoardModel board,Match3BoardModel.Cell a,Match3BoardModel.Cell b,Goal goal)
        {
            if(a.X<0||a.Y<0||b.X<0||b.Y<0||a.X>=board.Width||a.Y>=board.Height||b.X>=board.Width||b.Y>=board.Height)return false;
            if(board.GetContent(a)!=CellContent.Candy||board.GetContent(b)!=CellContent.Candy||board.GetSpecial(a.X,a.Y)!=SpecialKind.None||board.GetSpecial(b.X,b.Y)!=SpecialKind.None)return false;
            return Matches(board.CreatePreviewCopy().TrySwap(a,b),goal);
        }
        internal static bool FindActivation(Match3BoardModel board,Goal goal,out Match3BoardModel.Cell from,out Match3BoardModel.Cell to)
        {
            for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++)if(IsSpecial(board.GetSpecial(x,y),goal))
                foreach(var b in new[]{new Match3BoardModel.Cell(x-1,y),new Match3BoardModel.Cell(x+1,y),new Match3BoardModel.Cell(x,y-1),new Match3BoardModel.Cell(x,y+1)})
                    if(b.X>=0&&b.Y>=0&&b.X<board.Width&&b.Y<board.Height&&board.GetContent(b)==CellContent.Candy&&board.GetSpecial(b.X,b.Y)==SpecialKind.None)
                    {from=new Match3BoardModel.Cell(x,y);to=b;return true;}
            from=to=default;return false;
        }
    }
}
