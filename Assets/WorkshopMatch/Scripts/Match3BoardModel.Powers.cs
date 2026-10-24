using System;
using System.Collections.Generic;

namespace WorkshopMatch
{
    public sealed partial class Match3BoardModel
    {
        public const int MagnetComboCount = 10;
        public enum PowerShape { Normal, Cross, WideRay, LargeHammer, WholeBoard, Conversion }

        public readonly struct PowerEvent
        {
            public PowerEvent(Cell cell, SpecialKind kind, int color = -1,
                PowerShape shape = PowerShape.Normal, Cell? secondary = null)
            { Cell = cell; Kind = kind; Color = color; Shape = shape; Secondary = secondary; }
            public Cell Cell { get; }
            public SpecialKind Kind { get; }
            public int Color { get; }
            public PowerShape Shape { get; }
            public Cell? Secondary { get; }
        }

        private static bool IsRay(SpecialKind kind) =>
            kind == SpecialKind.RayHorizontal || kind == SpecialKind.RayVertical;

        private int ActivateCombination(Cell first, Cell destination, SpecialKind a, SpecialKind b,
            List<ResolutionStep> steps)
        {
            bool magnet = a == SpecialKind.Magnet || b == SpecialKind.Magnet;
            if (magnet && a != b)
            {
                BoardFrame before = CaptureFrame();
                List<Cell> candidates = new();
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        Cell cell = new(x, y);
                        if (!cell.Equals(first) && !cell.Equals(destination) && IsNormalCandy(x, y))
                            candidates.Add(cell);
                    }
                HashSet<int> sources = new() { Index(first.X, first.Y), Index(destination.X, destination.Y) };
                int removed = ClearCandyCells(sources, false);
                List<Cell> converted = new();
                List<PowerEvent> powers = new();
                SpecialKind partner = a == SpecialKind.Magnet ? b : a;
                int count = Math.Min(MagnetComboCount, candidates.Count);
                for (int i = 0; i < count; i++)
                {
                    int pick = i + NextInt(candidates.Count - i);
                    (candidates[i], candidates[pick]) = (candidates[pick], candidates[i]);
                    Cell cell = candidates[i];
                    SpecialKind kind = IsRay(partner)
                        ? (NextInt(2) == 0 ? SpecialKind.RayHorizontal : SpecialKind.RayVertical)
                        : SpecialKind.Solvent;
                    specials[cell.X, cell.Y] = kind;
                    converted.Add(cell);
                    powers.Add(new PowerEvent(cell, kind));
                }
                // Conversion is not a match: it must never award permanent tool pieces.
                steps.Add(new ResolutionStep(before, CaptureFrame(), ToCells(sources), new List<Cell>(),
                    activation: new PowerEvent(destination, SpecialKind.Magnet, shape: PowerShape.Conversion),
                    hasFall: false, convertedCells: converted));
                removed += ResolvePowerChain(powers, steps);
                return removed;
            }
            PowerShape shape = a == SpecialKind.Magnet && b == SpecialKind.Magnet ? PowerShape.WholeBoard
                : IsRay(a) && IsRay(b) ? PowerShape.Cross
                : a == SpecialKind.Solvent && b == SpecialKind.Solvent ? PowerShape.LargeHammer
                : PowerShape.WideRay;
            SpecialKind direction = IsRay(a) ? a : b;
            return ResolvePowerChain(new List<PowerEvent>
            {
                new(destination, direction, shape: shape, secondary: first)
            }, steps);
        }

        private int ResolvePowerChain(List<PowerEvent> initial, List<ResolutionStep> steps)
        {
            Queue<(int id, PowerEvent power)> pending = new();
            Queue<(int id, PowerEvent power, int parent)> contactQueue = new();
            HashSet<int> contacted = new();
            int[,] identities = new int[Width, Height];
            int nextIdentity = 0;
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                if (specials[x,y] != SpecialKind.None) identities[x,y] = ++nextIdentity;
            HashSet<int> scheduled = new();
            HashSet<(int, SpecialKind)> initialEvents = new();
            foreach (PowerEvent power in initial)
            {
                int index = Index(power.Cell.X, power.Cell.Y);
                if (!initialEvents.Add((index, power.Kind))) continue;
                int id = identities[power.Cell.X,power.Cell.Y];
                if (id == 0 || specials[power.Cell.X,power.Cell.Y] != power.Kind) id = ++nextIdentity;
                scheduled.Add(id);
                pending.Enqueue((id, power));
                if (power.Secondary.HasValue)
                    scheduled.Add(identities[power.Secondary.Value.X, power.Secondary.Value.Y]);
            }
            void Refill()
            {
                BoardFrame beforeFall = CaptureFrame();
                List<FallMove> falls = AddFallStep(steps);
                int[,] moved = new int[Width, Height];
                Dictionary<int, Cell> locations = new();
                foreach (FallMove fall in falls)
                    if (!fall.Spawned && beforeFall.GetSpecial(fall.From.X,fall.From.Y) != SpecialKind.None)
                    {
                        int id = identities[fall.From.X,fall.From.Y];
                        moved[fall.To.X,fall.To.Y] = id;
                        locations[id] = fall.To;
                    }
                identities = moved;
                int count = pending.Count;
                while (count-- > 0)
                {
                    var item = pending.Dequeue();
                    PowerEvent p = item.power;
                    if (locations.TryGetValue(item.id, out Cell location))
                        p = new PowerEvent(location,p.Kind,p.Color,p.Shape,p.Secondary);
                    pending.Enqueue((item.id,p));
                }
            }
            int removed = 0;
            while (pending.Count > 0 || contactQueue.Count > 0)
            {
                int parent = -1;
                int activeId;
                PowerEvent power;
                if (contactQueue.Count > 0)
                {
                    var item = contactQueue.Dequeue();
                    activeId=item.id;power=item.power;parent=item.parent;
                }
                else { var item=pending.Dequeue();activeId=item.id;power=item.power; }
                contacted.Add(activeId);
                int stepIndex = steps.Count;
                bool directContact = true;
                BoardFrame before = CaptureFrame();
                HashSet<int> cleared = new();
                HashSet<int> hits = new();
                ExpandPower(power, cleared, hits);
                cleared.Add(Index(power.Cell.X, power.Cell.Y));
                if (power.Secondary.HasValue)
                    cleared.Add(Index(power.Secondary.Value.X, power.Secondary.Value.Y));
                void Enqueue(Cell cell, SpecialKind kind, int color)
                {
                    if (kind == SpecialKind.None) return;
                    int id = identities[cell.X,cell.Y];
                    if (id == 0) identities[cell.X,cell.Y] = id = ++nextIdentity;
                    var next = new PowerEvent(cell, kind, kind == SpecialKind.Magnet ? PickNearbyColor(cell) : color);
                    if (directContact)
                    {
                        if (!contacted.Add(id)) return;
                        // A converted tool already waiting in the ordinary queue is
                        // promoted to this contact wave, never activated twice.
                        int count=pending.Count;
                        while(count-- > 0) { var queued=pending.Dequeue(); if(queued.id!=id)pending.Enqueue(queued); }
                        scheduled.Add(id);
                        contactQueue.Enqueue((id,next,stepIndex));
                    }
                    else if (scheduled.Add(id)) pending.Enqueue((id,next));
                }
                // Stable coordinate order keeps the same seed/gesture deterministic.
                List<int> ordered = new(cleared);
                ordered.Sort();
                foreach (int index in ordered)
                    if (contents[index % Width, index / Width] == CellContent.Candy)
                    {
                        bool source = index == Index(power.Cell.X,power.Cell.Y) &&
                            (specials[index % Width,index / Width] == power.Kind || power.Shape != PowerShape.Normal) ||
                            power.Secondary.HasValue && index == Index(power.Secondary.Value.X,power.Secondary.Value.Y);
                        if (!source)
                            Enqueue(new Cell(index % Width, index / Width),
                                specials[index % Width, index / Width], colors[index % Width, index / Width]);
                        // Queued tools remain visible and retain identity through refill.
                        // They are consumed only by their own activation, never as candy.
                        if (!source && specials[index % Width,index / Width] != SpecialKind.None)
                            cleared.Remove(index);
                    }
                if(power.Shape==PowerShape.WholeBoard&&power.Kind==SpecialKind.Magnet)
                    ApplyWholeBoardMagnetMotorHits(cleared,hits);
                ApplyObstacleHits(hits);
                removed += ClearCandyCells(cleared, power.Kind == SpecialKind.Magnet);
                directContact = false;
                steps.Add(new ResolutionStep(before, CaptureFrame(), ToCells(cleared), ToCells(hits),
                    activation:power, hasFall:false, triggerStepIndex:parent));
                removed += ExplodeReadyMotor(steps, Enqueue);
                if(contactQueue.Count==0)
                {
                    Refill();
                    if(HasReadyBolts()){AppendBoltExitStep(steps,Enqueue);Refill();}
                }
            }
            return removed;
        }

        private List<FallMove> AddFallStep(List<ResolutionStep> steps)
        {
            BoardFrame beforeFall = CaptureFrame();
            List<FallMove> falls = new();
            CollapseAndRefill(falls);
            steps.Add(new ResolutionStep(beforeFall, CaptureFrame(), new List<Cell>(), new List<Cell>(), falls: falls));
            return falls;
        }

        private int ExplodeReadyMotor(List<ResolutionStep> steps, Action<Cell, SpecialKind, int> enqueue)
        {
            EnsureMotorGroups();
            int totalRemoved=0;
            foreach(var group in motorGroups)
            {
                if(group.Activated||group.Hits<group.Required)continue;
                int minX=Width,minY=Height,maxX=-1,maxY=-1;
                foreach(var cell in group.Cells){minX=Math.Min(minX,cell.X);minY=Math.Min(minY,cell.Y);maxX=Math.Max(maxX,cell.X);maxY=Math.Max(maxY,cell.Y);}
                BoardFrame before=CaptureFrame();var cleared=new HashSet<int>();var hits=new HashSet<int>();
                foreach(var cell in group.Cells)
                {contents[cell.X,cell.Y]=CellContent.Empty;colors[cell.X,cell.Y]=EmptyColor;specials[cell.X,cell.Y]=SpecialKind.None;obstacleState[cell.X,cell.Y]=0;oil[cell.X,cell.Y]=false;}
                for(int y=Math.Max(0,minY-1);y<=Math.Min(Height-1,maxY+1);y++)
                    for(int x=Math.Max(0,minX-1);x<=Math.Min(Width-1,maxX+1);x++)
                    {
                        // The blast cleans exposed floor oil, including under queued powers.
                        if(contents[x,y]!=CellContent.FrozenGrease && contents[x,y]!=CellContent.Motor) oil[x,y]=false;
                        if(contents[x,y]==CellContent.Candy)
                        {
                            if(specials[x,y]!=SpecialKind.None&&enqueue!=null){enqueue(new Cell(x,y),specials[x,y],colors[x,y]);continue;}
                            cleared.Add(Index(x,y));
                        }
                        else if(contents[x,y] is CellContent.Bolt or CellContent.FrozenGrease)hits.Add(Index(x,y));
                    }
                ApplyObstacleHits(hits);
                group.Activated=true;UpdateMotorTotals();
                totalRemoved+=ClearCandyCells(cleared,true);
                steps.Add(new ResolutionStep(before,CaptureFrame(),ToCells(cleared),ToCells(hits),hasFall:false,motorExplosion:true));
            }
            UpdateMotorTotals();return totalRemoved;
        }

        private int PickNearbyColor(Cell cell)
        {
            List<int> nearby = new();
            for (int y = Math.Max(0, cell.Y - 1); y <= Math.Min(Height - 1, cell.Y + 1); y++)
                for (int x = Math.Max(0, cell.X - 1); x <= Math.Min(Width - 1, cell.X + 1); x++)
                    if ((x != cell.X || y != cell.Y) && contents[x, y] == CellContent.Candy && colors[x, y] >= 0)
                        nearby.Add(colors[x, y]);
            if (nearby.Count == 0)
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        if (contents[x, y] == CellContent.Candy && colors[x, y] >= 0) nearby.Add(colors[x, y]);
            return nearby.Count == 0 ? NextInt(colorCount) : nearby[NextInt(nearby.Count)];
        }

        private void ExpandPower(PowerEvent power, HashSet<int> cleared, HashSet<int> hits)
        {
            int selectedColor = power.Color >= 0 ? power.Color :
                power.Kind == SpecialKind.Magnet ? PickNearbyColor(power.Cell) : -1;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int dx = Math.Abs(x - power.Cell.X), dy = Math.Abs(y - power.Cell.Y);
                    bool area = power.Shape == PowerShape.LargeHammer ||
                        power.Shape == PowerShape.Normal && power.Kind == SpecialKind.Solvent;
                    bool affected = power.Shape switch
                    {
                        PowerShape.WholeBoard => true,
                        PowerShape.Cross => dx == 0 || dy == 0,
                        PowerShape.WideRay => power.Kind == SpecialKind.RayVertical ? dx <= 1 : dy <= 1,
                        PowerShape.LargeHammer => dx <= 2 && dy <= 2,
                        _ => power.Kind switch
                        {
                            SpecialKind.RayHorizontal => dy == 0,
                            SpecialKind.RayVertical => dx == 0,
                            SpecialKind.Solvent => dx <= 1 && dy <= 1,
                            SpecialKind.Magnet => contents[x, y] == CellContent.Candy && colors[x, y] == selectedColor,
                            _ => false
                        }
                    };
                    if (!affected) continue;
                    if ((area || power.Kind == SpecialKind.RayHorizontal || power.Kind == SpecialKind.RayVertical) &&
                        contents[x, y] != CellContent.FrozenGrease) oil[x, y] = false;
                    AddPowerCell(cleared, hits, x, y, area);
                    if (power.Kind == SpecialKind.Magnet && contents[x, y] == CellContent.Candy)
                    {
                        AddObstacleNeighbor(hits, x - 1, y);
                        AddObstacleNeighbor(hits, x + 1, y);
                        AddObstacleNeighbor(hits, x, y - 1);
                        AddObstacleNeighbor(hits, x, y + 1);
                    }
                }
        }
    }
}
