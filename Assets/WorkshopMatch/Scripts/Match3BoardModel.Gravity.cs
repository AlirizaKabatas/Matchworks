using System;
using System.Collections.Generic;

namespace WorkshopMatch
{
    public sealed partial class Match3BoardModel
    {
        private sealed class GravityTrack
        {
            public Cell From;
            public bool Spawned;
            public int Top;
            public readonly List<Cell> Path = new();
            public readonly List<int> Stages = new();
        }

        private void CollapseAndRefill(List<FallMove> falls)
        {
            var tracks = new GravityTrack[Width, Height];
            var spawnCounts = new int[Width, Height+1];
            int stage=0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (CanFall(contents[x,y]))
                    {
                        var track = new GravityTrack { From = new Cell(x,y), Top = Height-1 };
                        track.Path.Add(track.From);
                        tracks[x,y] = track;
                    }

            // Every transfer descends at least one row. Top-fed columns supply
            // pockets beside obstacles before any enclosed-pocket fallback.
            while (true)
            {
                bool changed = CompactGravityColumns(tracks,stage++);
                var diagonalCells=new HashSet<Cell>();
                int diagonalStage=stage++;
                for (int y = 0; y < Height-1; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        if (contents[x,y] != CellContent.Empty) continue;
                        bool blockedAbove=false;
                        for(int above=y+1;above<Height;above++)
                            if(IsFixed(contents[x,above])) { blockedAbove=true; break; }
                        if(!blockedAbove)continue;
                        // A direct vertical supply always takes precedence.
                        if (CanFall(contents[x,y+1])) continue;
                        int first = ((x+y)&1)==0 ? -1 : 1;
                        for (int choice = 0; choice < 2; choice++)
                        {
                            int sourceX = x + (choice==0 ? first : -first);
                            if (sourceX<0 || sourceX>=Width) continue;
                            // Bolts remain in their columns. An open corner is
                            // required; never cut through a fixed obstacle.
                            if (contents[sourceX,y+1]!=CellContent.Candy || IsFixed(contents[sourceX,y])) continue;
                            if (contents[sourceX,y]==CellContent.Empty) continue;
                            var from=new Cell(sourceX,y+1);var to=new Cell(x,y);var corner=new Cell(sourceX,y);
                            if(diagonalCells.Contains(from)||diagonalCells.Contains(to)||diagonalCells.Contains(corner))
                            { diagonalCells.Clear();diagonalStage=stage++; }
                            diagonalCells.Add(from);diagonalCells.Add(to);diagonalCells.Add(corner);
                            MoveGravityPiece(sourceX,y+1,x,y,tracks,diagonalStage);
                            changed = true;
                            break;
                        }
                    }
                // Diagonal departures leave holes in their source columns. Drop
                // the existing stack into those holes before creating any candy;
                // otherwise a new piece would pass straight through that stack.
                changed |= CompactGravityColumns(tracks,stage++);
                changed |= FillGravitySources(tracks, spawnCounts, false,stage++);
                if (!changed) break;
            }

            // Some level layouts have fully enclosed regions. Keep those
            // playable with their existing local supply after sideways flow.
            FillGravitySources(tracks, spawnCounts, true,stage++);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var track = tracks[x,y];
                    // Stationary pieces also carry provenance for queued-power identities.
                    if (track==null) continue;
                    falls.Add(new FallMove(track.From,new Cell(x,y),track.Spawned,track.Top,track.Path.ToArray(),track.Stages.ToArray()));
                }
        }

        private static bool CanFall(CellContent content) => content==CellContent.Candy || content==CellContent.Bolt;

        private bool CompactGravityColumns(GravityTrack[,] tracks,int stage)
        {
            bool changed = false;
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (IsFixed(contents[x,y])) { write=y+1; continue; }
                    if (!CanFall(contents[x,y])) continue;
                    if (write!=y) { MoveGravityPiece(x,y,x,write,tracks,stage); changed=true; }
                    write++;
                }
            }
            return changed;
        }

        private void MoveGravityPiece(int fromX,int fromY,int toX,int toY,GravityTrack[,] tracks,int stage)
        {
            contents[toX,toY]=contents[fromX,fromY];
            colors[toX,toY]=colors[fromX,fromY];
            obstacleState[toX,toY]=obstacleState[fromX,fromY];
            specials[toX,toY]=specials[fromX,fromY];
            partVariants[toX,toY]=partVariants[fromX,fromY];
            tracks[toX,toY]=tracks[fromX,fromY];
            tracks[toX,toY].Path.Add(new Cell(toX,toY));
            tracks[toX,toY].Stages.Add(stage);
            tracks[fromX,fromY]=null;
            contents[fromX,fromY]=CellContent.Empty;
            colors[fromX,fromY]=EmptyColor;
            obstacleState[fromX,fromY]=0;
            specials[fromX,fromY]=SpecialKind.None;
            partVariants[fromX,fromY]=0;
            // Oil is a ground layer and stays in its original cell.
        }

        private bool FillGravitySources(GravityTrack[,] tracks,int[,] spawnCounts,bool enclosed,int stage)
        {
            bool changed=false;
            for (int x=0;x<Width;x++)
            {
                int start=0;
                for (int boundary=0;boundary<=Height;boundary++)
                {
                    if (boundary<Height && !IsFixed(contents[x,boundary])) continue;
                    if (enclosed || boundary==Height)
                    {
                        for (int y=start;y<boundary;y++)
                            if (contents[x,y]==CellContent.Empty)
                            {
                                contents[x,y]=CellContent.Candy;
                                colors[x,y]=NextInt(colorCount);
                                obstacleState[x,y]=0;
                                specials[x,y]=SpecialKind.None;
                                partVariants[x,y]=0;
                                var track=new GravityTrack { From=new Cell(x,boundary+spawnCounts[x,boundary]++), Spawned=true, Top=boundary-1 };
                                track.Path.Add(track.From);
                                track.Path.Add(new Cell(x,y));
                                track.Stages.Add(stage);
                                tracks[x,y]=track;
                                changed=true;
                            }
                    }
                    start=boundary+1;
                }
            }
            return changed;
        }
    }
}
