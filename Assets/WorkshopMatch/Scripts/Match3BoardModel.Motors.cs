using System;
using System.Collections.Generic;
namespace WorkshopMatch
{
    public sealed partial class Match3BoardModel
    {
        private sealed class MotorGroup
        {
            internal readonly List<Cell> Cells=new();
            internal int Required,Hits;
            internal bool Activated;
        }
        private readonly List<MotorGroup> motorGroups=new();
        private readonly Dictionary<int,MotorGroup> motorOwners=new();
        public void PlaceMotor(Cell anchor,int width,int height,int required=5)
        {
            if(width<1||height<1||!InBounds(anchor)||anchor.X+width>Width||anchor.Y+height>Height)
                throw new ArgumentException("Motor footprint is outside the board.");
            for(int y=anchor.Y;y<anchor.Y+height;y++)for(int x=anchor.X;x<anchor.X+width;x++)
                if(motorOwners.ContainsKey(Index(x,y)))throw new ArgumentException("Motor footprints overlap.");
            var group=new MotorGroup{Required=Math.Max(1,required)};
            motorGroups.Add(group);
            for(int y=anchor.Y;y<anchor.Y+height;y++)for(int x=anchor.X;x<anchor.X+width;x++)
            {
                var cell=new Cell(x,y);PlaceObstacle(cell,CellContent.Motor,group.Required);
                group.Cells.Add(cell);motorOwners.Add(Index(x,y),group);
            }
            UpdateMotorTotals();
        }
        private void EnsureMotorGroups()
        {
            // Legacy direct obstacle placement is grouped by connected footprint.
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
            {
                int id=Index(x,y);
                if(contents[x,y]!=CellContent.Motor||motorOwners.ContainsKey(id))continue;
                var group=new MotorGroup{Required=Math.Max(1,obstacleState[x,y])};motorGroups.Add(group);
                var queue=new Queue<Cell>();queue.Enqueue(new Cell(x,y));motorOwners[id]=group;
                while(queue.Count>0)
                {
                    Cell cell=queue.Dequeue();group.Cells.Add(cell);
                    foreach(var next in new[]{new Cell(cell.X-1,cell.Y),new Cell(cell.X+1,cell.Y),new Cell(cell.X,cell.Y-1),new Cell(cell.X,cell.Y+1)})
                        if(InBounds(next)&&contents[next.X,next.Y]==CellContent.Motor&&!motorOwners.ContainsKey(Index(next.X,next.Y)))
                        {motorOwners[Index(next.X,next.Y)]=group;queue.Enqueue(next);}
                }
            }
            UpdateMotorTotals();
        }
        private void UpdateMotorTotals()
        {
            motorHits=0;motorHitsRequired=0;motorActivated=motorGroups.Count>0;
            foreach(var group in motorGroups){motorHits+=group.Hits;motorHitsRequired+=group.Required;motorActivated&=group.Activated;}
            HasMotor=motorGroups.Count>0;
        }
        public int GetMotorRequired(Cell cell)=>motorOwners.TryGetValue(Index(cell.X,cell.Y),out var group)?group.Required:Math.Max(1,obstacleState[cell.X,cell.Y]);
        private void HitMotor(int x,int y)
        {
            EnsureMotorGroups();
            if(!motorOwners.TryGetValue(Index(x,y),out var group)||group.Activated)return;
            group.Hits=Math.Min(group.Required,group.Hits+1);
            foreach(var cell in group.Cells)obstacleState[cell.X,cell.Y]=group.Required-group.Hits;
            UpdateMotorTotals();
        }
        private void ApplyWholeBoardMagnetMotorHits(HashSet<int> cleared,HashSet<int> hits)
        {
            EnsureMotorGroups();
            // This combination replaces the usual single event hit with one hit per
            // removed candy in each engine's surrounding ring. Count shared edges once.
            hits.RemoveWhere(index=>contents[index%Width,index/Width]==CellContent.Motor);
            foreach(var group in motorGroups)
            {
                if(group.Activated||group.Hits>=group.Required)continue;
                var adjacentCandies=new HashSet<int>();
                foreach(var footprint in group.Cells)
                    for(int y=Math.Max(0,footprint.Y-1);y<=Math.Min(Height-1,footprint.Y+1);y++)
                        for(int x=Math.Max(0,footprint.X-1);x<=Math.Min(Width-1,footprint.X+1);x++)
                            if(contents[x,y]==CellContent.Candy&&cleared.Contains(Index(x,y)))
                                adjacentCandies.Add(Index(x,y));
                group.Hits=Math.Min(group.Required,group.Hits+adjacentCandies.Count);
                foreach(var cell in group.Cells)obstacleState[cell.X,cell.Y]=group.Required-group.Hits;
            }
            UpdateMotorTotals();
        }
        private void CopyMotorGroups(Match3BoardModel source)
        {
            foreach(var old in source.motorGroups)
            {
                var group=new MotorGroup{Required=old.Required,Hits=old.Hits,Activated=old.Activated};
                group.Cells.AddRange(old.Cells);motorGroups.Add(group);
                foreach(var cell in group.Cells)motorOwners[Index(cell.X,cell.Y)]=group;
            }
        }
    }
}
