using System;
using System.Collections.Generic;
using System.Text;

namespace WorkshopMatch
{
    public enum CellContent
    {
        Candy,
        Bolt,
        Motor,
        FrozenGrease,
        Empty
    }

    public enum RoundState
    {
        Playing,
        Won,
        Lost
    }

    public enum SpecialKind
    {
        None,
        RayHorizontal,
        RayVertical,
        Solvent,
        Magnet
    }

    public sealed partial class Match3BoardModel
    {
        public readonly struct Cell : IEquatable<Cell>
        {
            public Cell(int x, int y) { X = x; Y = y; }
            public int X { get; }
            public int Y { get; }
            public bool Equals(Cell other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is Cell other && Equals(other);
            public override int GetHashCode() => (X * 397) ^ Y;
            public override string ToString() => $"({X},{Y})";
        }

        public readonly struct SwapResult
        {
            public SwapResult(bool accepted, int removed, int cascades, IReadOnlyList<ResolutionStep> steps = null)
            {
                Accepted = accepted;
                Removed = removed;
                Cascades = cascades;
                Steps = steps ?? Array.Empty<ResolutionStep>();
            }

            public bool Accepted { get; }
            public int Removed { get; }
            public int Cascades { get; }
            public IReadOnlyList<ResolutionStep> Steps { get; }
        }

        public sealed class BoardFrame
        {
            internal BoardFrame(CellContent[,] contents, int[,] colors, bool[,] oil, int[,] obstacleState,
                SpecialKind[,] specials, Objectives objectives, int[,] partVariants)
            {
                Contents = (CellContent[,])contents.Clone();
                Colors = (int[,])colors.Clone();
                Oil = (bool[,])oil.Clone();
                ObstacleState = (int[,])obstacleState.Clone();
                Specials = (SpecialKind[,])specials.Clone();
                Objectives = objectives;
                PartVariants=(int[,])partVariants.Clone();
            }

            private CellContent[,] Contents { get; }
            private int[,] Colors { get; }
            private bool[,] Oil { get; }
            private int[,] ObstacleState { get; }
            private SpecialKind[,] Specials { get; }
            private int[,] PartVariants { get; }
            public Objectives Objectives { get; }
            public int Width => Contents.GetLength(0);
            public int Height => Contents.GetLength(1);
            public CellContent GetContent(int x, int y) => Contents[x, y];
            public int GetColor(int x, int y) => Colors[x, y];
            public int GetPartVariant(int x,int y)=>PartVariants[x,y];
            public bool HasOil(int x, int y) => Oil[x, y];
            public int GetObstacleState(int x, int y) => ObstacleState[x, y];
            public SpecialKind GetSpecial(int x, int y) => Specials[x, y];
        }

        public readonly struct FallMove
        {
            public FallMove(Cell from, Cell to, bool spawned, int top, IReadOnlyList<Cell> path = null, IReadOnlyList<int> pathStages = null)
            { From = from; To = to; Spawned = spawned; SegmentTop = top; Path = path; PathStages = pathStages; }
            public Cell From { get; }
            public Cell To { get; }
            public bool Spawned { get; }
            public int SegmentTop { get; }
            public IReadOnlyList<Cell> Path { get; }
            public IReadOnlyList<int> PathStages { get; }
        }

        public sealed class ResolutionStep
        {
            internal ResolutionStep(BoardFrame beforeClear, BoardFrame afterFall, List<Cell> matchedCells, List<Cell> hitTargets,
                List<Cell> exitedBolts = null, PowerEvent? activation = null, bool hasFall = true,
                List<Cell> convertedCells = null, bool motorExplosion = false, List<FallMove> falls = null,
                int automaticMatchGroups = 0, int triggerStepIndex = -1)
            {
                BeforeClear = beforeClear;
                AfterFall = afterFall;
                MatchedCells = matchedCells;
                HitTargets = hitTargets;
                ExitedBolts = exitedBolts ?? new List<Cell>();
                Activation = activation;
                HasFall = hasFall;
                ConvertedCells = convertedCells ?? new List<Cell>();
                MotorExplosion = motorExplosion;
                Falls = falls ?? new List<FallMove>();
                AutomaticMatchGroups = automaticMatchGroups;
                TriggerStepIndex = triggerStepIndex;
            }

            public BoardFrame BeforeClear { get; }
            public BoardFrame AfterFall { get; }
            public IReadOnlyList<Cell> MatchedCells { get; }
            public IReadOnlyList<Cell> HitTargets { get; }
            public IReadOnlyList<Cell> ExitedBolts { get; }
            public PowerEvent? Activation { get; }
            public bool HasFall { get; }
            public IReadOnlyList<Cell> ConvertedCells { get; }
            public bool MotorExplosion { get; }
            public IReadOnlyList<FallMove> Falls { get; }
            public int AutomaticMatchGroups { get; }
            // Index of the power whose travelling impact starts this activation.
            // No refill separates a contact-triggered child from its parent.
            public int TriggerStepIndex { get; }
        }

        public readonly struct Objectives
        {
            public Objectives(int totalBolts, int collectedBolts, int totalOil, int remainingOil,
                int motorHits, int motorRequired, bool motorReady, bool motorActivated, RoundState state, int totalFrozenOil = 0, int remainingFrozenOil = 0)
            {
                TotalBolts = totalBolts;
                CollectedBolts = collectedBolts;
                TotalOil = totalOil;
                RemainingOil = remainingOil;
                TotalFrozenOil = totalFrozenOil;
                RemainingFrozenOil = remainingFrozenOil;
                MotorHits = motorHits;
                MotorRequired = motorRequired;
                MotorReady = motorReady;
                MotorActivated = motorActivated;
                State = state;
            }

            public int TotalBolts { get; }
            public int CollectedBolts { get; }
            public int TotalOil { get; }
            public int RemainingOil { get; }
            public int TotalFrozenOil { get; }
            public int RemainingFrozenOil { get; }
            public int MotorHits { get; }
            public int MotorRequired { get; }
            public bool MotorReady { get; }
            public bool MotorActivated { get; }
            public RoundState State { get; }
        }

        private const int EmptyColor = -1;
        private readonly int[,] colors;
        private readonly CellContent[,] contents;
        private readonly bool[,] oil;
        private readonly int[,] obstacleState;
        private readonly SpecialKind[,] specials;
        private readonly int colorCount;
        private readonly Action<SpecialKind> onSpecialProduced;
        private uint randomState;
        private int appearanceSeed;
        private readonly int[,] partVariants;
        public int GetPartVariant(int x,int y)=>partVariants[x,y];
        private int totalBolts;
        private int collectedBolts;
        private int totalOil;
        private int totalFrozenOil;
        private int motorHits;
        private int motorHitsRequired;
        private bool objectivesSealed;
        private bool motorActivated;
        private int producedRays;
        private int producedSolvents;
        private int producedMagnets;

        public Match3BoardModel(int width, int height, int colorCount, int seed, int startingMoves,
            Action<SpecialKind> specialProduced = null)
        {
            if (width < 3 || height < 3)
                throw new ArgumentOutOfRangeException(nameof(width), "Board must be at least 3x3.");
            if (colorCount < 3)
                throw new ArgumentOutOfRangeException(nameof(colorCount), "At least three colors are required.");

            appearanceSeed=seed;partVariants=new int[width,height];
            Width = width;
            Height = height;
            this.colorCount = colorCount;
            onSpecialProduced = specialProduced;
            MovesRemaining = startingMoves;
            randomState = seed == 0 ? 1u : unchecked((uint)seed);
            colors = new int[width, height];
            contents = new CellContent[width, height];
            oil = new bool[width, height];
            obstacleState = new int[width, height];
            specials = new SpecialKind[width, height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    contents[x, y] = CellContent.Candy;
            GeneratePlayableBoard();
        }

        public int Width { get; }
        public int Height { get; }
        public int MovesRemaining { get; private set; }
        public int MoveGoal { get; private set; }
        public int ValidMovesMade { get; private set; }
        public void ConfigureMoveGoal(int target)
        { MoveGoal = Math.Max(0,target); ValidMovesMade = 0; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugAdjustMoves(int delta)
        {
            if (State != RoundState.Playing) return;
            MovesRemaining = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)MovesRemaining + delta));
            EvaluateOutcome();
        }
#endif
        public bool IsStable => FindMatchGroups().Count == 0;
        public int GetColor(int x, int y) => colors[x, y];
        public CellContent GetContent(int x, int y) => contents[x, y];
        public CellContent GetContent(Cell cell) => contents[cell.X, cell.Y];
        public bool HasOil(int x, int y) => oil[x, y];
        public int GetObstacleState(int x, int y) => obstacleState[x, y];
        public SpecialKind GetSpecial(int x, int y) => specials[x, y];
        public int ProducedRays => producedRays;
        public int ProducedSolvents => producedSolvents;
        public int ProducedMagnets => producedMagnets;

        public Objectives GetObjectives()
        {
            return new Objectives(totalBolts, collectedBolts, totalOil, CountOil(), motorHits,
                motorHitsRequired, HasMotor && motorHits >= motorHitsRequired, motorActivated, State, totalFrozenOil, CountFrozenOil());
        }

        public RoundState State { get; private set; } = RoundState.Playing;
        public bool HasMotor { get; private set; }

        public void PlaceObstacle(Cell cell, CellContent content, int state = 1)
        {
            if (!InBounds(cell) || content == CellContent.Candy || content == CellContent.Empty)
                throw new ArgumentException("A valid obstacle cell and content are required.");

            if(content==CellContent.Bolt)
            {
                unchecked
                {
                    uint h=(uint)appearanceSeed^(uint)(cell.X*374761393)^(uint)(cell.Y*668265263);
                    h=(h^(h>>13))*1274126177u;h^=h>>16;partVariants[cell.X,cell.Y]=(int)(h&3);
                }
            }
            contents[cell.X, cell.Y] = content;
            colors[cell.X, cell.Y] = EmptyColor;
            obstacleState[cell.X, cell.Y] = Math.Max(0, state);
            specials[cell.X, cell.Y] = SpecialKind.None;
            if (content == CellContent.Motor)
            {
                HasMotor = true;
                motorHitsRequired = Math.Max(1, state);
            }
            objectivesSealed = false;
        }

        public void SetOil(Cell cell, bool present = true)
        {
            if (!InBounds(cell))
                throw new ArgumentOutOfRangeException(nameof(cell));
            oil[cell.X, cell.Y] = present;
            objectivesSealed = false;
        }

        public void SealObjectives()
        {
            totalBolts = 0;
            collectedBolts = 0;
            totalOil = 0;
            totalFrozenOil = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (contents[x, y] == CellContent.Bolt)
                        totalBolts++;
                    if (oil[x, y])
                        totalOil++;
                    if (contents[x, y] == CellContent.FrozenGrease)
                        totalFrozenOil++;
                }
            }
            EnsureMotorGroups();
            objectivesSealed = true;
            EnsurePlayableAfterSetup();
            EvaluateOutcome();
        }

        public bool TryFindValidSwap(out Cell first, out Cell second) => TryFindSwap(true, out first, out second);
        public bool TryFindInvalidSwap(out Cell first, out Cell second) => TryFindSwap(false, out first, out second);

        public bool TryFindBestHintSwap(out Cell first, out Cell second)
        {
            int bestScore = int.MinValue;
            first = default;
            second = default;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell origin = new(x, y);
                    if (GetContent(origin) != CellContent.Candy) continue;
                    if (x + 1 < Width)
                        ScoreHintCandidate(origin, new Cell(x + 1, y), ref bestScore, ref first, ref second);
                    if (y + 1 < Height)
                        ScoreHintCandidate(origin, new Cell(x, y + 1), ref bestScore, ref first, ref second);
                }
            }
            return bestScore != int.MinValue;
        }

        public SwapResult TryUseTool(Cell target, SpecialKind kind)
        {
            if (!objectivesSealed) SealObjectives();
            if (State != RoundState.Playing || MovesRemaining <= 0 || !InBounds(target) ||
                !IsNormalCandy(target.X, target.Y) ||
                !(kind is SpecialKind.RayHorizontal or SpecialKind.RayVertical or SpecialKind.Solvent or SpecialKind.Magnet))
                return new SwapResult(false, 0, 0);
            BoardFrame before = CaptureFrame();
            specials[target.X, target.Y] = kind;
            // Placement consumes inventory only. No activation, refill, production
            // reward, shuffle, or move spend until the player later uses the piece.
            return new SwapResult(true, 0, 0, new List<ResolutionStep>
            {
                new(before, CaptureFrame(), new List<Cell>(), new List<Cell>(),
                    hasFall: false, convertedCells: new List<Cell> { target })
            });
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal SwapResult DebugActivatePower(Cell target, SpecialKind kind)
        {
            if (!objectivesSealed) SealObjectives();
            if (State != RoundState.Playing || MovesRemaining <= 0 || !InBounds(target) ||
                GetContent(target) != CellContent.Candy || kind == SpecialKind.None)
                return new SwapResult(false, 0, 0);

            int selectedColor = colors[target.X, target.Y];
            MovesRemaining--;
            List<ResolutionStep> steps = new();
            int removed = ActivateSpecial(target, kind, selectedColor, steps);
            int cascades = ResolveCascades(steps, default, false, ref removed);
            FinishTurn();
            return new SwapResult(true, removed, cascades + 1, steps);
        }
#endif

        public SwapResult TrySwap(Cell first, Cell second)
        {
            if (!objectivesSealed)
                SealObjectives();
            if (State != RoundState.Playing || !InBounds(first) || !InBounds(second) ||
                ManhattanDistance(first, second) != 1 || MovesRemaining <= 0 ||
                GetContent(first) != CellContent.Candy || GetContent(second) != CellContent.Candy)
                return new SwapResult(false, 0, 0);

            SpecialKind firstSpecial = specials[first.X, first.Y];
            SpecialKind secondSpecial = specials[second.X, second.Y];
            if (firstSpecial != SpecialKind.None && secondSpecial != SpecialKind.None)
            {
                SwapCandy(first, second);
                MovesRemaining--;
                ValidMovesMade++;
                List<ResolutionStep> comboSteps = new();
                int comboRemoved = ActivateCombination(first, second, firstSpecial, secondSpecial, comboSteps);
                int comboCascades = ResolveCascades(comboSteps, default, false, ref comboRemoved);
                FinishTurn();
                return new SwapResult(true, comboRemoved, comboCascades + 1, comboSteps);
            }
            bool activatesSpecial = (firstSpecial != SpecialKind.None) != (secondSpecial != SpecialKind.None);
            if (activatesSpecial)
            {
                Cell specialFrom = firstSpecial != SpecialKind.None ? first : second;
                Cell candyFrom = firstSpecial != SpecialKind.None ? second : first;
                SpecialKind kind = specials[specialFrom.X, specialFrom.Y];
                int selectedColor = colors[candyFrom.X, candyFrom.Y];
                SwapCandy(first, second);
                MovesRemaining--;
                ValidMovesMade++;
                List<ResolutionStep> activationSteps = new();
                int removedByPower = ActivateSpecial(candyFrom, kind, selectedColor, activationSteps);
                int activationCascades = ResolveCascades(activationSteps, default, false, ref removedByPower);
                FinishTurn();
                return new SwapResult(true, removedByPower, activationCascades + 1, activationSteps);
            }

            if (firstSpecial != SpecialKind.None || secondSpecial != SpecialKind.None)
                return new SwapResult(false, 0, 0);

            SwapCandy(first, second);
            List<HashSet<int>> groups = FindMatchGroups();
            if (groups.Count == 0)
            {
                SwapCandy(first, second);
                return new SwapResult(false, 0, 0);
            }

            MovesRemaining--;
                ValidMovesMade++;
            int totalRemoved = 0;
            List<ResolutionStep> steps = new();
            int cascades = ResolveCascades(steps, second, true, ref totalRemoved, groups);
            FinishTurn();
            return new SwapResult(true, totalRemoved, cascades, steps);
        }

        private int ResolveCascades(List<ResolutionStep> steps, Cell preferred, bool usePreferred,
            ref int totalRemoved, List<HashSet<int>> groups = null)
        {
            groups ??= FindMatchGroups();
            int cascades = 0;
            while (groups.Count > 0 && cascades < 64)
            {
                BoardFrame beforeClear = CaptureFrame();
                HashSet<int> matchedIndices = new();
                HashSet<int> hitIndices = new();
                for (int i = 0; i < groups.Count; i++)
                {
                    matchedIndices.UnionWith(groups[i]);
                    CollectObstacleNeighbors(groups[i], hitIndices);
                    Cell? spawn = usePreferred && groups[i].Contains(Index(preferred.X, preferred.Y)) ? preferred : null;
                    totalRemoved += ResolveMatchGroup(groups[i], spawn, true);
                }
                List<PowerEvent> swept = new();
                steps.Add(new ResolutionStep(beforeClear, CaptureFrame(), ToCells(matchedIndices),
                    ToCells(hitIndices), hasFall: false,
                    automaticMatchGroups: usePreferred ? 0 : groups.Count));
                if(HasReadyBolts())
                {
                    AddFallStep(steps);
                    AppendBoltExitStep(steps,(cell,kind,color)=>swept.Add(new PowerEvent(cell,kind,
                        kind==SpecialKind.Magnet?PickNearbyColor(cell):color)));
                }
                totalRemoved += ExplodeReadyMotor(steps, (cell, kind, color) => swept.Add(new PowerEvent(cell, kind,
                    kind == SpecialKind.Magnet ? PickNearbyColor(cell) : color)));
                if(HasReadyBolts())
                {
                    AddFallStep(steps);
                    AppendBoltExitStep(steps,(cell,kind,color)=>swept.Add(new PowerEvent(cell,kind,
                        kind==SpecialKind.Magnet?PickNearbyColor(cell):color)));
                }
                if (swept.Count > 0) totalRemoved += ResolvePowerChain(swept, steps);
                else AddFallStep(steps);
                cascades++;
                usePreferred = false;
                groups = FindMatchGroups();
            }
            return cascades;
        }

        private void FinishTurn()
        {
            EvaluateOutcome();
            if (State == RoundState.Playing && !HasAnyValidSwap())
                ShuffleUntilPlayable();
        }

        private BoardFrame CaptureFrame() => new(contents, colors, oil, obstacleState, specials, GetObjectives(),partVariants);

        private List<Cell> ToCells(HashSet<int> indices)
        {
            List<Cell> cells = new(indices.Count);
            foreach (int index in indices)
                cells.Add(new Cell(index % Width, index / Width));
            return cells;
        }

        private void CollectObstacleNeighbors(HashSet<int> group, HashSet<int> targets)
        {
            foreach (int index in group)
            {
                int x = index % Width;
                int y = index / Width;
                if (contents[x, y] != CellContent.Candy)
                    continue;
                AddObstacleNeighbor(targets, x - 1, y);
                AddObstacleNeighbor(targets, x + 1, y);
                AddObstacleNeighbor(targets, x, y - 1);
                AddObstacleNeighbor(targets, x, y + 1);
            }
        }

        private int ActivateSpecial(Cell center, SpecialKind kind, int selectedColor, List<ResolutionStep> steps)
        {
            List<PowerEvent> powers = new() { new(center, kind, selectedColor) };
            SpecialKind hitPower = specials[center.X, center.Y];
            if (hitPower != SpecialKind.None && hitPower != kind)
                powers.Add(new PowerEvent(center, hitPower, hitPower == SpecialKind.Magnet ? PickNearbyColor(center) : selectedColor));
            return ResolvePowerChain(powers, steps);
        }


        private void AddPowerCell(HashSet<int> cleared, HashSet<int> hits, int x, int y, bool areaEffect)
        {
            CellContent content = contents[x, y];
            if (content == CellContent.Candy)
            {
                cleared.Add(Index(x, y));
                return;
            }
            if (content == CellContent.Bolt || content == CellContent.Motor || content == CellContent.FrozenGrease)
                hits.Add(Index(x, y));
        }

        private int ClearCandyCells(HashSet<int> cells, bool clearMatchedOil)
        {
            int removed = 0;
            foreach (int index in cells)
            {
                int x = index % Width;
                int y = index / Width;
                if (contents[x, y] != CellContent.Candy)
                    continue;
                if (clearMatchedOil)
                    oil[x, y] = false;
                contents[x, y] = CellContent.Empty;
                colors[x, y] = EmptyColor;
                specials[x, y] = SpecialKind.None;
                removed++;
            }
            return removed;
        }

        public string Snapshot()
        {
            StringBuilder builder = new(Width * Height * 3 + Height);
            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    builder.Append((int)contents[x, y]);
                    builder.Append(contents[x, y] == CellContent.Candy ? colors[x, y] : obstacleState[x, y]);
                    builder.Append((int)specials[x, y]);
                    builder.Append(oil[x, y] ? 'o' : '-');
                }
                if (y > 0)
                    builder.Append('/');
            }
            builder.Append('|').Append(MovesRemaining).Append('|').Append(collectedBolts).Append('|').Append(motorHits);
            return builder.ToString();
        }

        public uint SnapshotHash()
        {
            uint hash = 2166136261u;
            string snapshot = Snapshot();
            for (int i = 0; i < snapshot.Length; i++)
            {
                hash ^= snapshot[i];
                hash *= 16777619u;
            }
            return hash;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void DebugResolveGroup(params Cell[] group)
        {
            HashSet<int> indices = new();
            for (int i = 0; i < group.Length; i++)
                if (InBounds(group[i]) && GetContent(group[i]) == CellContent.Candy)
                    indices.Add(Index(group[i].X, group[i].Y));
            ResolveMatchGroup(indices, null, false);
            ExitReadyBolts();
            ExplodeReadyMotor(new List<ResolutionStep>(), null);
            EvaluateOutcome();
        }
#endif

        internal void DebugSetCandy(Cell cell, int color)
        {
            contents[cell.X, cell.Y] = CellContent.Candy;
            colors[cell.X, cell.Y] = color % colorCount;
            obstacleState[cell.X, cell.Y] = 0;
            specials[cell.X, cell.Y] = SpecialKind.None;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal SpecialKind DebugResolveClassifiedGroup(Cell preferred, params Cell[] group)
        {
            HashSet<int> indices = new();
            for (int i = 0; i < group.Length; i++)
                indices.Add(Index(group[i].X, group[i].Y));
            SpecialKind kind = ClassifySpecial(indices);
            ResolveMatchGroup(indices, preferred, true);
            return kind;
        }
#endif

        internal void DebugPlaceSpecial(Cell cell, SpecialKind kind, int color = 0)
        {
            DebugSetCandy(cell, color);
            specials[cell.X, cell.Y] = kind;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal int DebugActivateSpecial(Cell cell, int selectedColor)
        {
            List<ResolutionStep> steps = new();
            return ActivateSpecial(cell, specials[cell.X, cell.Y], selectedColor, steps);
        }
#endif

        private void GeneratePlayableBoard()
        {
            for (int attempt = 0; attempt < 256; attempt++)
            {
                FillCandyCellsWithoutMatches();
                if (FindMatchGroups().Count == 0 && HasAnyValidSwap())
                    return;
            }
            throw new InvalidOperationException("Could not generate a stable playable board.");
        }

        private void FillCandyCellsWithoutMatches()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (IsNormalCandy(x, y))
                        colors[x, y] = EmptyColor;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!IsNormalCandy(x, y))
                        continue;
                    int start = NextInt(colorCount);
                    for (int offset = 0; offset < colorCount; offset++)
                    {
                        colors[x, y] = (start + offset) % colorCount;
                        if (!CreatesMatchAt(x, y))
                            break;
                    }
                }
            }
        }

        private void EnsurePlayableAfterSetup()
        {
            if (FindMatchGroups().Count > 0 || !HasAnyValidSwap())
                ShuffleUntilPlayable();
        }

        private bool TryFindSwap(bool shouldBeValid, out Cell first, out Cell second)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cell origin = new(x, y);
                    if (GetContent(origin) != CellContent.Candy)
                        continue;
                    if (x + 1 < Width)
                    {
                        Cell right = new(x + 1, y);
                        if (GetContent(right) == CellContent.Candy && WouldSwapMatch(origin, right) == shouldBeValid)
                        {
                            first = origin;
                            second = right;
                            return true;
                        }
                    }
                    if (y + 1 < Height)
                    {
                        Cell up = new(x, y + 1);
                        if (GetContent(up) == CellContent.Candy && WouldSwapMatch(origin, up) == shouldBeValid)
                        {
                            first = origin;
                            second = up;
                            return true;
                        }
                    }
                }
            }
            first = default;
            second = default;
            return false;
        }

        private bool HasAnyValidSwap() => TryFindValidSwap(out _, out _);

        private void ScoreHintCandidate(Cell origin, Cell neighbour, ref int bestScore, ref Cell bestFirst, ref Cell bestSecond)
        {
            if (GetContent(neighbour) != CellContent.Candy || !WouldSwapMatch(origin, neighbour)) return;

            SpecialKind originSpecial = specials[origin.X, origin.Y];
            SpecialKind neighbourSpecial = specials[neighbour.X, neighbour.Y];
            int score;
            if (originSpecial != SpecialKind.None || neighbourSpecial != SpecialKind.None)
            {
                score = 50;
            }
            else
            {
                SwapCandy(origin, neighbour);
                List<HashSet<int>> groups = FindMatchGroups();
                score = ScoreHintGroups(groups);
                SwapCandy(origin, neighbour);
            }

            if (score <= bestScore) return;
            bestScore = score;
            bestFirst = origin;
            bestSecond = neighbour;
        }

        private int ScoreHintGroups(List<HashSet<int>> groups)
        {
            int score = 0;
            HashSet<int> scoredTargets = new();
            foreach (HashSet<int> group in groups)
            {
                score += group.Count * 2;
                SpecialKind result = ClassifySpecial(group);
                if (result == SpecialKind.Magnet) score += 35;
                else if (result == SpecialKind.Solvent) score += 25;
                else if (result is SpecialKind.RayHorizontal or SpecialKind.RayVertical) score += 20;
                foreach (int index in group)
                {
                    int x = index % Width;
                    int y = index / Width;
                    if (oil[x, y]) score += 80;
                    ScoreHintTarget(x - 1, y, scoredTargets, ref score);
                    ScoreHintTarget(x + 1, y, scoredTargets, ref score);
                    ScoreHintTarget(x, y - 1, scoredTargets, ref score);
                    ScoreHintTarget(x, y + 1, scoredTargets, ref score);
                }
            }
            return score;
        }

        private void ScoreHintTarget(int x, int y, HashSet<int> scoredTargets, ref int score)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            int index = Index(x, y);
            if (!scoredTargets.Add(index)) return;
            if (contents[x, y] == CellContent.Bolt || contents[x, y] == CellContent.FrozenGrease) score += 60;
            else if (contents[x, y] == CellContent.Motor && motorHits < motorHitsRequired) score += 45;
        }

        private bool WouldSwapMatch(Cell first, Cell second)
        {
            bool anySpecial = specials[first.X, first.Y] != SpecialKind.None || specials[second.X, second.Y] != SpecialKind.None;
            if (anySpecial)
                return true;
            SwapCandy(first, second);
            bool matched = FindMatchGroups().Count > 0;
            SwapCandy(first, second);
            return matched;
        }

        private List<HashSet<int>> FindMatchGroups()
        {
            List<HashSet<int>> groups = new();
            for (int y = 0; y < Height; y++)
            {
                int x = 0;
                while (x < Width)
                {
                    if (!IsNormalCandy(x, y)) { x++; continue; }
                    int start = x++;
                    while (x < Width && IsNormalCandy(x, y) && colors[x, y] == colors[start, y]) x++;
                    if (x - start >= 3)
                    {
                        HashSet<int> run = new();
                        for (int add = start; add < x; add++) run.Add(Index(add, y));
                        MergeGroup(groups, run);
                    }
                }
            }

            for (int x = 0; x < Width; x++)
            {
                int y = 0;
                while (y < Height)
                {
                    if (!IsNormalCandy(x, y)) { y++; continue; }
                    int start = y++;
                    while (y < Height && IsNormalCandy(x, y) && colors[x, y] == colors[x, start]) y++;
                    if (y - start >= 3)
                    {
                        HashSet<int> run = new();
                        for (int add = start; add < y; add++) run.Add(Index(x, add));
                        MergeGroup(groups, run);
                    }
                }
            }

            for (int y = 0; y < Height - 1; y++)
            {
                for (int x = 0; x < Width - 1; x++)
                {
                    if (!IsNormalCandy(x, y))
                        continue;
                    int color = colors[x, y];
                    if (IsNormalCandy(x + 1, y) && IsNormalCandy(x, y + 1) &&
                        IsNormalCandy(x + 1, y + 1) && colors[x + 1, y] == color &&
                        colors[x, y + 1] == color && colors[x + 1, y + 1] == color)
                        MergeGroup(groups, new HashSet<int> { Index(x, y), Index(x + 1, y), Index(x, y + 1), Index(x + 1, y + 1) });
                }
            }
            return groups;
        }

        private static void MergeGroup(List<HashSet<int>> groups, HashSet<int> incoming)
        {
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                bool overlaps = false;
                foreach (int value in incoming)
                    if (groups[i].Contains(value)) { overlaps = true; break; }
                if (!overlaps) continue;
                incoming.UnionWith(groups[i]);
                groups.RemoveAt(i);
            }
            groups.Add(incoming);
        }

        private bool CreatesMatchAt(int x, int y)
        {
            if (!IsNormalCandy(x, y) || colors[x, y] == EmptyColor)
                return false;
            int color = colors[x, y];
            if (x >= 2 && IsCandyColor(x - 1, y, color) && IsCandyColor(x - 2, y, color)) return true;
            if (y >= 2 && IsCandyColor(x, y - 1, color) && IsCandyColor(x, y - 2, color)) return true;
            return x >= 1 && y >= 1 && IsCandyColor(x - 1, y, color) && IsCandyColor(x, y - 1, color) && IsCandyColor(x - 1, y - 1, color);
        }

        private int ResolveMatchGroup(HashSet<int> group, Cell? preferredSpawn, bool produceSpecial)
        {
            SpecialKind produced = produceSpecial ? ClassifySpecial(group) : SpecialKind.None;
            int spawnIndex = produced == SpecialKind.None ? -1 : ChooseSpecialSpawn(group, preferredSpawn);
            int producedColor = produced == SpecialKind.None ? 0 : FindGroupColor(group);
            HashSet<int> adjacentTargets = new();
            foreach (int index in group)
            {
                int x = index % Width;
                int y = index / Width;
                if (contents[x, y] != CellContent.Candy)
                    continue;
                oil[x, y] = false;
                AddObstacleNeighbor(adjacentTargets, x - 1, y);
                AddObstacleNeighbor(adjacentTargets, x + 1, y);
                AddObstacleNeighbor(adjacentTargets, x, y - 1);
                AddObstacleNeighbor(adjacentTargets, x, y + 1);
            }

            ApplyObstacleHits(adjacentTargets);

            int removed = ClearCandyCells(group, true);
            if (spawnIndex >= 0)
            {
                int spawnX = spawnIndex % Width;
                int spawnY = spawnIndex / Width;
                contents[spawnX, spawnY] = CellContent.Candy;
                colors[spawnX, spawnY] = producedColor;
                specials[spawnX, spawnY] = produced;
                removed--;
                if (produced == SpecialKind.Solvent) producedSolvents++;
                else if (produced == SpecialKind.Magnet) producedMagnets++;
                else producedRays++;
                onSpecialProduced?.Invoke(produced);
            }
            return removed;
        }

        private SpecialKind ClassifySpecial(HashSet<int> group)
        {
            int longestHorizontal = 0;
            int longestVertical = 0;
            for (int y = 0; y < Height; y++)
            {
                int run = 0;
                for (int x = 0; x < Width; x++)
                {
                    if (group.Contains(Index(x, y))) run++; else run = 0;
                    longestHorizontal = Math.Max(longestHorizontal, run);
                }
            }
            for (int x = 0; x < Width; x++)
            {
                int run = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (group.Contains(Index(x, y))) run++; else run = 0;
                    longestVertical = Math.Max(longestVertical, run);
                }
            }
            int longest = Math.Max(longestHorizontal, longestVertical);
            if (longest >= 5) return SpecialKind.Magnet;
            if (longest == 4) return longestHorizontal >= 4 ? SpecialKind.RayHorizontal : SpecialKind.RayVertical;
            for (int y = 0; y < Height - 1; y++)
                for (int x = 0; x < Width - 1; x++)
                    if (group.Contains(Index(x, y)) && group.Contains(Index(x + 1, y)) &&
                        group.Contains(Index(x, y + 1)) && group.Contains(Index(x + 1, y + 1)))
                        return SpecialKind.Solvent;
            return SpecialKind.None;
        }

        private int ChooseSpecialSpawn(HashSet<int> group, Cell? preferred)
        {
            if (preferred.HasValue && group.Contains(Index(preferred.Value.X, preferred.Value.Y)))
                return Index(preferred.Value.X, preferred.Value.Y);
            int chosen = int.MaxValue;
            foreach (int index in group)
                if (index < chosen) chosen = index;
            return chosen == int.MaxValue ? -1 : chosen;
        }

        private int FindGroupColor(HashSet<int> group)
        {
            foreach (int index in group)
            {
                int color = colors[index % Width, index / Width];
                if (color != EmptyColor)
                    return color;
            }
            return 0;
        }

        private void AddObstacleNeighbor(HashSet<int> targets, int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            CellContent content = contents[x, y];
            if (content == CellContent.Bolt || content == CellContent.Motor || content == CellContent.FrozenGrease)
                targets.Add(Index(x, y));
        }

        private void ApplyObstacleHit(int x, int y)
        {
            switch (contents[x, y])
            {
                case CellContent.Bolt:
                    obstacleState[x, y] = Math.Max(0, obstacleState[x, y] - 1);
                    break;
                case CellContent.FrozenGrease:
                    obstacleState[x, y] = Math.Max(0, obstacleState[x, y] - 1);
                    if (obstacleState[x, y] == 0)
                        contents[x, y] = CellContent.Empty;
                    break;
                case CellContent.Motor:
                    HitMotor(x,y);
                    break;
            }
        }

        private void ApplyObstacleHits(HashSet<int> targets)
        {
            EnsureMotorGroups();
            var hitMotors=new HashSet<MotorGroup>();
            foreach (int target in targets)
            {
                int x = target % Width;
                int y = target / Width;
                if (contents[x, y] == CellContent.Motor)
                {
                    if(motorOwners.TryGetValue(target,out var group)&&!hitMotors.Add(group))continue;
                }
                ApplyObstacleHit(x, y);
            }
        }

        private bool HasReadyBolts()
        {
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
                if(contents[x,y]==CellContent.Bolt&&obstacleState[x,y]==0)return true;
            return false;
        }
        private void AppendBoltExitStep(List<ResolutionStep> steps,Action<Cell,SpecialKind,int> onPowerSwept)
        {
            var before=CaptureFrame();var exited=new List<Cell>();ExitReadyBolts(exited,onPowerSwept);
            steps.Add(new ResolutionStep(before,CaptureFrame(),new List<Cell>(),new List<Cell>(),exited,hasFall:false));
        }

        private void ExitReadyBolts(List<Cell> exited = null, Action<Cell, SpecialKind, int> onPowerSwept = null)
        {
            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (contents[x, y] != CellContent.Bolt || obstacleState[x, y] > 0)
                        continue;
                    contents[x, y] = CellContent.Empty;
                    collectedBolts++;
                    exited?.Add(new Cell(x, y));
                    for (int above = y + 1; above < Height; above++)
                    {
                        if (contents[x, above] == CellContent.Candy)
                        {
                            if (specials[x, above] != SpecialKind.None)
                            {
                                onPowerSwept?.Invoke(new Cell(x, above), specials[x, above], colors[x, above]);
                                if (onPowerSwept != null) continue;
                            }
                            contents[x, above] = CellContent.Empty;
                            colors[x, above] = EmptyColor;
                            specials[x, above] = SpecialKind.None;
                        }
                    }
                }
            }
        }

        private void ShuffleUntilPlayable()
        {
            List<int> candyColors = new();
            List<Cell> candyCells = new();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (IsNormalCandy(x, y))
                    {
                        candyCells.Add(new Cell(x, y));
                        candyColors.Add(colors[x, y]);
                    }

            for (int attempt = 0; attempt < 256; attempt++)
            {
                for (int i = candyColors.Count - 1; i > 0; i--)
                {
                    int other = NextInt(i + 1);
                    (candyColors[i], candyColors[other]) = (candyColors[other], candyColors[i]);
                }
                for (int i = 0; i < candyCells.Count; i++)
                    colors[candyCells[i].X, candyCells[i].Y] = candyColors[i];
                if (FindMatchGroups().Count == 0 && HasAnyValidSwap())
                    return;
            }

            for (int attempt = 0; attempt < 256; attempt++)
            {
                FillCandyCellsWithoutMatches();
                if (FindMatchGroups().Count == 0 && HasAnyValidSwap())
                    return;
            }
            throw new InvalidOperationException("Could not produce a playable obstacle board.");
        }

        private void EvaluateOutcome()
        {
            bool boltsDone = collectedBolts >= totalBolts;
            bool oilDone = CountOil() == 0 && CountFrozenOil() == 0;
            bool motorDone = !HasMotor || motorHits >= motorHitsRequired;
            bool hasObjectives = totalBolts > 0 || totalOil > 0 || totalFrozenOil > 0 || HasMotor || MoveGoal > 0;
            if (objectivesSealed && hasObjectives && boltsDone && oilDone && motorDone && (MoveGoal == 0 || ValidMovesMade >= MoveGoal))
            {
                State = RoundState.Won;
            }
            else if (MovesRemaining <= 0)
            {
                State = RoundState.Lost;
            }
            else
            {
                State = RoundState.Playing;
            }
        }

        private int CountFrozenOil()
        {
            int count = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (contents[x, y] == CellContent.FrozenGrease) count++;
            return count;
        }

        private int CountOil()
        {
            int count = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (oil[x, y]) count++;
            return count;
        }

        private int NextInt(int maximum)
        {
            randomState = unchecked(randomState * 1664525u + 1013904223u);
            return (int)((randomState >> 1) % (uint)maximum);
        }

        private void SwapCandy(Cell first, Cell second)
        {
            (colors[first.X, first.Y], colors[second.X, second.Y]) = (colors[second.X, second.Y], colors[first.X, first.Y]);
            (specials[first.X, first.Y], specials[second.X, second.Y]) = (specials[second.X, second.Y], specials[first.X, first.Y]);
        }

        private bool IsCandyColor(int x, int y, int color) => IsNormalCandy(x, y) && colors[x, y] == color;
        private bool IsNormalCandy(int x, int y) => contents[x, y] == CellContent.Candy && specials[x, y] == SpecialKind.None;
        private static bool IsFixed(CellContent content) => content == CellContent.Motor || content == CellContent.FrozenGrease;
        private bool InBounds(Cell cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        private static int ManhattanDistance(Cell first, Cell second) => Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);
        private int Index(int x, int y) => y * Width + x;
    }
}
