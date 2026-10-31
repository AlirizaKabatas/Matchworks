using System;
using System.Linq;
using WorkshopMatch;
using C = WorkshopMatch.Match3BoardModel.Cell;
partial class Program
{
    static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static Match3BoardModel Board()
    {
        var b = new Match3BoardModel(8,8,5,1234,30);
        for(int y=0;y<8;y++) for(int x=0;x<8;x++) b.DebugSetCandy(new C(x,y),(x+2*y)%5);
        return b;
    }
    static Match3BoardModel.SwapResult Combo(SpecialKind a,SpecialKind b)
    { var m=Board(); m.DebugPlaceSpecial(new C(3,3),a);m.DebugPlaceSpecial(new C(4,3),b);return m.TrySwap(new C(3,3),new C(4,3)); }
    static void RunPartAppearanceChecks()
    {
        var first=Board();var second=Board();var seen=new System.Collections.Generic.HashSet<int>();
        for(int y=0;y<8;y++)for(int x=0;x<8;x++)
        {
            first.PlaceObstacle(new C(x,y),CellContent.Bolt,2);
            second.PlaceObstacle(new C(x,y),CellContent.Bolt,2);
            seen.Add(first.GetPartVariant(x,y));
        }
        Check(seen.Count==4,"part appearance includes yellow/blue nuts and screws");
        Check(Enumerable.Range(0,64).All(i=>first.GetPartVariant(i%8,i/8)==second.GetPartVariant(i%8,i/8)),
            "same seed gives identical part appearances");
        var preview=first.CreatePreviewCopy();
        Check(Enumerable.Range(0,64).All(i=>preview.GetPartVariant(i%8,i/8)==first.GetPartVariant(i%8,i/8)),
            "preview preserves all part appearances");
        var falling=Board();falling.PlaceObstacle(new C(2,5),CellContent.Bolt,2);falling.SealObjectives();
        int variant=falling.GetPartVariant(2,5);
        var result=falling.DebugActivatePower(new C(0,0),SpecialKind.RayHorizontal);
        var step=result.Steps.First(s=>s.Falls.Any(f=>!f.Spawned && f.From.X==2 && f.From.Y==5));
        var move=step.Falls.First(f=>!f.Spawned && f.From.X==2 && f.From.Y==5);
        Check(move.To.Y<move.From.Y && step.AfterFall.GetContent(move.To.X,move.To.Y)==CellContent.Bolt &&
            step.AfterFall.GetPartVariant(move.To.X,move.To.Y)==variant,"fall preserves part appearance");
        Check(step.BeforeClear.GetPartVariant(2,5)==variant,"animation frame keeps independent appearance snapshot");
        var rusty=Board();rusty.PlaceObstacle(new C(3,3),CellContent.Bolt,2);rusty.SealObjectives();
        variant=rusty.GetPartVariant(3,3);
        result=rusty.DebugActivatePower(new C(0,3),SpecialKind.RayHorizontal);
        Check(result.Steps[0].AfterFall.GetObstacleState(3,3)==1 &&
            result.Steps[0].AfterFall.GetPartVariant(3,3)==variant,"rust hit preserves part color and family");
    }
    static void Main()
    {
        if(Environment.GetEnvironmentVariable("WORKSHOP_AUDIT_FALLS")=="1"){AuditHammerPace();return;}
        RunOpeningScreenChecks();
        RunLanguageChecks();
        RunHomeMenuInteractionChecks();
        if(Environment.GetEnvironmentVariable("WORKSHOP_FIND_MAGNET_SEED")=="1")
        {
            for(int seed=1;seed<=10000;seed++)
            {
                var search=TutorialBoard(9,null,seed);
                if(!TutorialMovePlanner.FindDirect(search,TutorialMovePlanner.Goal.Magnet,out var from,out var to))continue;
                var result=search.TrySwap(from,to);
                if(!TutorialMovePlanner.FindActivation(search,TutorialMovePlanner.Goal.Magnet,out var power,out var neighbour))continue;
                Console.WriteLine($"SEED={seed} SWAP=({from.X},{from.Y})->({to.X},{to.Y}) ACTIVATE=({power.X},{power.Y})->({neighbour.X},{neighbour.Y})");return;
            }
            throw new Exception("No direct usable magnet seed found");
        }
        RunGravityChecks();
        RunFallAnimationChecks();
        RunTutorialChecks();
        RunAchievementChecks();
        RunPartAppearanceChecks();
        for(int level=1;level<=20;level++) {
            var a=TutorialBoard(level);var b=TutorialBoard(level);
            Check(a.Snapshot()==b.Snapshot() && Enumerable.Range(0,64).All(i=>a.GetPartVariant(i%8,i/8)==b.GetPartVariant(i%8,i/8)),
                "final level deterministic layout and appearances "+level);
        }
        Check(!TrainingLevelPolicy.ToolLocked("level_001",true,ToolKind.Magnet,10),"unlocked magnet stays open on old level");
        Check(TrainingLevelPolicy.ToolLocked("level_001",true,ToolKind.Magnet,6),"magnet remains locked before reaching its unlock level");
        var oldReward=new ToolProgressTracker(new ToolProgressData());
        TrainingLevelPolicy.RecordProduced("level_001",true,oldReward,SpecialKind.Magnet,10);
        Check(oldReward.Pieces(ToolKind.Magnet)==1,"old level produces credits for permanently unlocked tool");
        var debugBoard = Board();
        debugBoard.DebugAdjustMoves(1);
        Check(debugBoard.MovesRemaining == 31, "debug adds one move");
        debugBoard.DebugAdjustMoves(-1);
        Check(debugBoard.MovesRemaining == 30, "debug removes one move");
        debugBoard.DebugAdjustMoves(-100);
        Check(debugBoard.MovesRemaining == 0 && debugBoard.State == RoundState.Lost, "debug zero moves triggers loss");
        debugBoard.DebugAdjustMoves(1);
        Check(debugBoard.MovesRemaining == 0, "debug does not reopen completed round");
        var r=Combo(SpecialKind.RayHorizontal,SpecialKind.RayVertical);
        for(int color=0;color<5;color++)
        {
            var magnetOil=Board();
            magnetOil.DebugPlaceSpecial(new C(3,3),SpecialKind.Magnet);
            magnetOil.DebugSetCandy(new C(4,3),color);
            magnetOil.DebugSetCandy(new C(6,6),color);
            magnetOil.SetOil(new C(6,6));
            magnetOil.PlaceObstacle(new C(0,0),CellContent.FrozenGrease,1);
            magnetOil.SetOil(new C(0,0));
            magnetOil.SealObjectives();
            var magnetic=magnetOil.TrySwap(new C(3,3),new C(4,3));
            Check(magnetic.Accepted && !magnetic.Steps[0].AfterFall.HasOil(6,6),"magnet clears selected candy oil color "+color);
            Check(magnetic.Steps[0].AfterFall.HasOil(0,0),"magnet preserves unselected frozen oil color "+color);
        }
        Check(r.Accepted && r.Steps[0].MatchedCells.Count==15,"rocket cross: 15 cells");
        r=Combo(SpecialKind.Solvent,SpecialKind.Solvent);
        Check(r.Steps[0].MatchedCells.Count==25,"hammer pair: 5x5");
        foreach(var ray in new[]{SpecialKind.RayVertical,SpecialKind.RayHorizontal})
        {
            r=Combo(SpecialKind.Solvent,ray);
            Check(r.Steps[0].MatchedCells.Count==24,"hammer rocket: 3 lanes " + ray);
        }
        r=Combo(SpecialKind.Magnet,SpecialKind.RayHorizontal);
        Check(r.Steps[0].ConvertedCells.Count==10,"magnet converts ten");
        r=Combo(SpecialKind.Magnet,SpecialKind.Magnet);
        Check(r.Accepted && r.Steps.Any(s=>s.Activation?.Shape==Match3BoardModel.PowerShape.WholeBoard),
            "magnet pair starts full-board sweep");
        Check(r.Steps.Where(s=>s.Activation?.Shape==Match3BoardModel.PowerShape.WholeBoard)
            .SelectMany(s=>s.MatchedCells).Distinct().Count()==64,"magnet pair targets each board cell");
        Check(r.Steps.Where(s=>s.Activation.HasValue).All(s=>s.AutomaticMatchGroups==0),
            "power activations do not count as automatic chain matches");
        r=Combo(SpecialKind.Magnet,SpecialKind.RayHorizontal);
        Check(r.Steps.Count(s=>s.Activation.HasValue)>=11,"converted powers fire");
        foreach(var partner in new[]{SpecialKind.RayHorizontal,SpecialKind.Solvent})
        {
            var combo=Combo(SpecialKind.Magnet,partner);
            var powers=combo.Steps.Select((s,i)=>(s,i)).Where(p=>p.s.Activation.HasValue && p.s.Activation.Value.Shape!=Match3BoardModel.PowerShape.Conversion).ToArray();
            Check(powers.Length==10,"ten converted powers activate exactly once: "+partner);
            Check(powers.All(p=>p.s.BeforeClear.GetSpecial(p.s.Activation.Value.Cell.X,p.s.Activation.Value.Cell.Y)==p.s.Activation.Value.Kind),
                "converted tool remains visible until its own activation: "+partner);
            Check(powers.All(p=>p.s.MatchedCells.All(c=>p.s.BeforeClear.GetSpecial(c.X,c.Y)==SpecialKind.None || c.Equals(p.s.Activation.Value.Cell))),
                "combo blast never consumes another waiting tool: "+partner);
            Check(powers.Skip(1).All(p=>p.s.TriggerStepIndex>=0 || combo.Steps[p.i-1].HasFall),"contact powers share wave; untouched combo powers refill first: "+partner);
            Check(powers.Skip(1).Where(p=>p.s.TriggerStepIndex<0).All(p=>Enumerable.Range(0,64).All(n=>p.s.BeforeClear.GetContent(n%8,n/8)!=CellContent.Empty)),"independent combo powers hit refilled board: "+partner);
            var falls=combo.Steps.Where(s=>s.HasFall).SelectMany(s=>s.Falls).ToArray();
            Check(falls.All(f=>f.From.X==f.To.X && f.From.Y>=f.To.Y && (!f.Spawned || f.From.Y>f.SegmentTop)),"fall provenance is vertical with spawns above segment: "+partner);
            Check(combo.Steps.Where(s=>s.HasFall).All(s=>s.Falls.Where(f=>!f.Spawned).All(f=>s.BeforeClear.GetColor(f.From.X,f.From.Y)==s.AfterFall.GetColor(f.To.X,f.To.Y) && s.BeforeClear.GetSpecial(f.From.X,f.From.Y)==s.AfterFall.GetSpecial(f.To.X,f.To.Y))),"fall retains candy and special identity: "+partner);
        }
        var model=Board();model.DebugPlaceSpecial(new C(2,2),SpecialKind.Solvent);
        model.DebugPlaceSpecial(new C(3,2),SpecialKind.RayVertical);
        r=model.DebugActivatePower(new C(2,2),SpecialKind.Solvent);
        Check(r.Steps.Any(s=>s.Activation?.Kind==SpecialKind.RayVertical),"area hit activates rocket");
        foreach(var trigger in new[]{SpecialKind.Solvent,SpecialKind.RayHorizontal})
        foreach(var targetKind in new[]{SpecialKind.Solvent,SpecialKind.RayVertical,SpecialKind.Magnet})
        {
            var chain=Board(); chain.DebugPlaceSpecial(new C(2,2),trigger);
            chain.DebugPlaceSpecial(new C(3,2),targetKind);
            var trace=chain.DebugActivatePower(new C(2,2),trigger);
            Check(trace.Steps[0].AfterFall.GetSpecial(3,2)==targetKind && !trace.Steps[0].MatchedCells.Contains(new C(3,2)) &&
                trace.Steps.Skip(1).Any(s=>s.Activation?.Kind==targetKind &&
                s.BeforeClear.GetSpecial(s.Activation.Value.Cell.X,s.Activation.Value.Cell.Y)==targetKind),
                "hit tool survives then activates visibly: "+trigger+" -> "+targetKind);
            Check(trace.Steps.Any(s=>s.TriggerStepIndex==0 && s.Activation?.Kind==targetKind) &&
                trace.Steps.ToList().FindIndex(s=>s.HasFall)>trace.Steps.ToList().FindIndex(s=>s.TriggerStepIndex==0),
                "contact activation precedes refill: "+trigger+" -> "+targetKind);
        }
        var second=Board(); second.DebugPlaceSpecial(new C(2,2),SpecialKind.Solvent);second.DebugPlaceSpecial(new C(3,2),SpecialKind.RayVertical);
        second.DebugActivatePower(new C(2,2),SpecialKind.Solvent);
        Check(model.Snapshot()==second.Snapshot(),"deterministic chain");
        model=Board();model.DebugPlaceSpecial(new C(3,3),SpecialKind.RayVertical);
        r=model.DebugActivatePower(new C(3,3),SpecialKind.Solvent);
        Check(r.Steps.Any(s=>s.Activation?.Kind==SpecialKind.RayVertical),"overlapping power preserves center rocket");
        model=Board();model.PlaceObstacle(new C(2,2),CellContent.Bolt,1);
        model.DebugPlaceSpecial(new C(2,5),SpecialKind.RayHorizontal);
        r=model.DebugActivatePower(new C(2,1),SpecialKind.RayVertical);
        int departure=r.Steps.ToList().FindIndex(s=>s.ExitedBolts.Count>0);
        Check(departure>0&&r.Steps[departure-1].HasFall,"bolt exits only after gravity and refill");
        var departed=r.Steps[departure].ExitedBolts.Single();
        Check(departed.Equals(new C(2,0)),"clean bolt falls into gap before departure");
        Check(Enumerable.Range(1,7).All(y=>r.Steps[departure].BeforeClear.GetContent(2,y)==CellContent.Candy),"bolt sweep starts with a fully refilled column");
        Check(r.Steps.Any(s=>s.Activation?.Kind==SpecialKind.RayHorizontal),"bolt sweep preserves power");
        var boltMatch=Board();boltMatch.PlaceObstacle(new C(3,3),CellContent.Bolt,1);
        boltMatch.DebugSetCandy(new C(3,0),1);boltMatch.DebugSetCandy(new C(3,1),1);
        boltMatch.DebugSetCandy(new C(3,2),0);boltMatch.DebugSetCandy(new C(4,2),1);
        boltMatch.DebugPlaceSpecial(new C(3,6),SpecialKind.RayHorizontal);
        var boltMatchResult=boltMatch.TrySwap(new C(4,2),new C(3,2));
        int matchedExit=boltMatchResult.Steps.ToList().FindIndex(s=>s.ExitedBolts.Count>0);
        Check(boltMatchResult.Accepted&&matchedExit>0&&boltMatchResult.Steps[matchedExit-1].HasFall,"normal match refills before cleaned bolt exits");
        Check(boltMatchResult.Steps[matchedExit].ExitedBolts.Contains(new C(3,0)),"matched column lets cleaned bolt fall before sweep");
        Check(boltMatchResult.Steps.Skip(matchedExit+1).Any(s=>s.Activation?.Kind==SpecialKind.RayHorizontal),"bolt sweep schedules fallen surviving power after refill");
        model=Board();model.PlaceObstacle(new C(3,3),CellContent.FrozenGrease,1);model.SetOil(new C(3,3));
        r=model.DebugActivatePower(new C(2,3),SpecialKind.Solvent);
        Check(r.Steps[0].AfterFall.HasOil(3,3),"frozen shell leaves oil layer");
        foreach(var ray in new[]{SpecialKind.RayHorizontal,SpecialKind.RayVertical})
        {
            model=Board();model.SetOil(new C(3,3));model.SealObjectives();
            model.DebugPlaceSpecial(new C(3,3),ray);
            var neighbor=ray==SpecialKind.RayHorizontal ? new C(4,3) : new C(3,4);
            model.DebugPlaceSpecial(neighbor,ray);
            r=model.TrySwap(new C(3,3),neighbor);
            Check(r.Accepted && !r.Steps[0].AfterFall.HasOil(3,3),"rocket removes exposed oil: " + ray);
        }
        foreach(var kind in new[]{SpecialKind.RayHorizontal,SpecialKind.RayVertical,SpecialKind.Solvent,SpecialKind.Magnet})
        {
            model=Board(); model.SetOil(new C(3,3)); model.SealObjectives();
            int moves=model.MovesRemaining, color=model.GetColor(3,3);
            r=model.TryUseTool(new C(3,3),kind);
            Check(r.Accepted && r.Removed==0 && r.Steps.Count==1 && !r.Steps[0].HasFall &&
                !r.Steps[0].Activation.HasValue && model.GetSpecial(3,3)==kind && model.GetColor(3,3)==color &&
                model.HasOil(3,3) && model.MovesRemaining==moves &&
                model.ProducedRays+model.ProducedSolvents+model.ProducedMagnets==0,"placement only: "+kind);
            string snapshot=model.Snapshot();
            Check(!model.TryUseTool(new C(3,3),SpecialKind.Magnet).Accepted && model.Snapshot()==snapshot,
                "existing power cannot be overwritten: "+kind);
            r=model.TrySwap(new C(3,3),new C(4,3));
            Check(r.Accepted && r.Steps.Any(s=>s.Activation?.Kind==kind) && model.MovesRemaining==moves-1,
                "placed power activates on later swap: "+kind);
        }
        model=Board();model.PlaceObstacle(new C(3,3),CellContent.Bolt,2);
        Check(!model.TryUseTool(new C(3,3),SpecialKind.Solvent).Accepted &&
            !model.TryUseTool(new C(-1,0),SpecialKind.Magnet).Accepted,"placement rejects obstacle and outside board");
        var attempt=new ToolProgressTracker(new ToolProgressData{RayPieces=3,SolventPieces=3,MagnetPieces=4,RayStock=2,SolventStock=2,MagnetStock=2});
        attempt.RecordProduced(SpecialKind.RayHorizontal);attempt.RecordProduced(SpecialKind.Solvent);attempt.RecordProduced(SpecialKind.Magnet);
        var recoveredAttempt=new ToolProgressTracker(attempt.Snapshot);recoveredAttempt.RollbackAttemptCredits();
        Check(recoveredAttempt.Pieces(ToolKind.Ray)==3&&recoveredAttempt.Stock(ToolKind.Ray)==2&&recoveredAttempt.Pieces(ToolKind.Solvent)==3&&
            recoveredAttempt.Stock(ToolKind.Solvent)==2&&recoveredAttempt.Pieces(ToolKind.Magnet)==4&&recoveredAttempt.Stock(ToolKind.Magnet)==2,
            "restart removes all earned credits including credits converted to tools across save reload");
        recoveredAttempt.RecordProduced(SpecialKind.Magnet);recoveredAttempt.CommitAttemptCredits();recoveredAttempt.RollbackAttemptCredits();
        Check(recoveredAttempt.Stock(ToolKind.Magnet)==3,"completed attempt keeps committed production reward");
        var damage=Board();damage.PlaceMotor(new C(2,2),3,2,1);damage.PlaceObstacle(new C(1,2),CellContent.Bolt,2);
        damage.PlaceObstacle(new C(5,2),CellContent.FrozenGrease,1);damage.SetOil(new C(5,2));damage.SealObjectives();
        var damageBurst=damage.DebugActivatePower(new C(3,6),SpecialKind.RayVertical).Steps.First(step=>step.MotorExplosion);
        Check(damageBurst.AfterFall.GetObstacleState(1,2)==1&&damageBurst.AfterFall.GetContent(5,2)==CellContent.Empty&&damageBurst.AfterFall.HasOil(5,2),
            "motor blast removes one rust layer and cracks frozen shell while preserving exposed oil");
        var mixedMotors=Board();mixedMotors.PlaceMotor(new C(0,0),3,2);mixedMotors.PlaceMotor(new C(5,0),2,3);mixedMotors.SealObjectives();
        Check(mixedMotors.GetContent(2,1)==CellContent.Motor&&mixedMotors.GetContent(6,2)==CellContent.Motor&&
            mixedMotors.GetContent(2,2)!=CellContent.Motor&&mixedMotors.GetContent(7,1)!=CellContent.Motor&&mixedMotors.GetObjectives().MotorRequired==10,
            "horizontal and vertical motors coexist with correct independent footprints");
        var twoMotors=Board();
        twoMotors.PlaceMotor(new C(0,0),3,2,5);twoMotors.PlaceMotor(new C(3,0),3,2,5);
        twoMotors.SetOil(new C(7,7));twoMotors.SetOil(new C(0,0));twoMotors.SetOil(new C(3,0));twoMotors.SealObjectives();
        Check(twoMotors.GetObjectives().MotorRequired==10,"two adjacent motor footprints remain separate objectives");
        var magnetMotors=Board();
        magnetMotors.PlaceMotor(new C(0,0),3,2,50);magnetMotors.PlaceMotor(new C(5,0),2,3,50);
        magnetMotors.DebugPlaceSpecial(new C(3,6),SpecialKind.Magnet);
        magnetMotors.DebugPlaceSpecial(new C(4,6),SpecialKind.Magnet);magnetMotors.SealObjectives();
        var magnetClear=magnetMotors.TrySwap(new C(3,6),new C(4,6)).Steps.First(s=>s.Activation?.Shape==Match3BoardModel.PowerShape.WholeBoard);
        Check(magnetClear.AfterFall.GetObstacleState(0,0)==44&&magnetClear.AfterFall.GetObstacleState(5,0)==40,
            "double magnet counts six and ten unique surrounding candies for independent motors");
        var chargedMotors=Board();chargedMotors.PlaceMotor(new C(0,0),3,2,5);chargedMotors.PlaceMotor(new C(5,0),2,3,5);
        chargedMotors.DebugPlaceSpecial(new C(3,6),SpecialKind.Magnet);
        chargedMotors.DebugPlaceSpecial(new C(4,6),SpecialKind.Magnet);chargedMotors.SealObjectives();
        var chargedResult=chargedMotors.TrySwap(new C(3,6),new C(4,6));
        Check(chargedResult.Steps.Count(s=>s.MotorExplosion)==2&&chargedMotors.GetObjectives().MotorActivated,
            "double magnet fully charges and explodes both motors in one combination");
        void HitLeft(Match3BoardModel board)
        {
            foreach(var cell in new[]{new C(0,2),new C(1,2),new C(2,2)})board.DebugSetCandy(cell,1);
            board.DebugResolveGroup(new C(0,2),new C(1,2),new C(2,2));
        }
        HitLeft(twoMotors);
        Check(twoMotors.GetObstacleState(0,0)==4&&twoMotors.GetObstacleState(3,0)==5&&twoMotors.GetObjectives().MotorHits==1,
            "one match hitting multiple cells charges only its own motor once");
        var motorCopy=twoMotors.CreatePreviewCopy();HitLeft(motorCopy);
        Check(motorCopy.GetObstacleState(0,0)==3&&twoMotors.GetObstacleState(0,0)==4,"motor preview progress is isolated");
        for(int i=0;i<3;i++)HitLeft(twoMotors);
        twoMotors.DebugSetCandy(new C(0,2),2);
        var separateBurst=twoMotors.DebugActivatePower(new C(0,2),SpecialKind.RayVertical).Steps.First(step=>step.MotorExplosion);
        Check(separateBurst.AfterFall.GetContent(0,0)==CellContent.Empty&&separateBurst.AfterFall.GetContent(3,0)==CellContent.Motor&&
            !separateBurst.AfterFall.Objectives.MotorActivated,"first motor explosion leaves adjacent uncharged motor intact");
        Check(!separateBurst.AfterFall.HasOil(0,0)&&separateBurst.AfterFall.HasOil(3,0)&&separateBurst.AfterFall.HasOil(7,7),
            "motor explosion clears only oil under its own footprint");
        Match3BoardModel PrimedMotor()
        {
            var m=Board();
            for(int y=3;y<=4;y++)for(int x=3;x<=5;x++)m.PlaceObstacle(new C(x,y),CellContent.Motor,5);
            m.SetOil(new C(3,2));m.SealObjectives();
            for(int i=0;i<4;i++)
            {
                m.DebugSetCandy(new C(2,3),1);m.DebugSetCandy(new C(2,4),1);m.DebugSetCandy(new C(1,3),1);
                m.DebugResolveGroup(new C(2,3),new C(2,4),new C(1,3));
            }
            m.DebugPlaceSpecial(new C(6,5),SpecialKind.RayVertical);
            return m;
        }
        model=PrimedMotor();
        Check(model.GetObjectives().MotorHits==4 && !model.GetObjectives().MotorActivated &&
            model.GetContent(3,3)==CellContent.Motor,"motor remains through fourth hit");
        r=model.DebugActivatePower(new C(0,3),SpecialKind.RayHorizontal);
        var burst=r.Steps.Single(s=>s.MotorExplosion);
        Check(burst.BeforeClear.Objectives.MotorHits==5 && burst.AfterFall.Objectives.MotorActivated,
            "motor explodes exactly on fifth hit");
        bool ring=true,body=true,outside=true;
        for(int y=0;y<8;y++)for(int x=0;x<8;x++)
        {
            if(x>=3&&x<=5&&y>=3&&y<=4)body &= burst.AfterFall.GetContent(x,y)==CellContent.Empty;
            else if(x>=2&&x<=6&&y>=2&&y<=5)
            { if(burst.BeforeClear.GetContent(x,y)==CellContent.Candy)
                ring &= burst.BeforeClear.GetSpecial(x,y)==SpecialKind.None
                    ? burst.AfterFall.GetContent(x,y)==CellContent.Empty
                    : burst.AfterFall.GetSpecial(x,y)==burst.BeforeClear.GetSpecial(x,y); }
            else outside &= burst.BeforeClear.GetContent(x,y)==burst.AfterFall.GetContent(x,y) &&
                burst.BeforeClear.GetColor(x,y)==burst.AfterFall.GetColor(x,y);
        }
        Check(body&&ring&&outside&&!burst.AfterFall.HasOil(3,2),"motor footprint and one-cell ring clean exposed oil");
        Check(r.Steps.SkipWhile(s=>!s.MotorExplosion).Skip(1).Any(s=>s.Activation?.Kind==SpecialKind.RayVertical &&
            s.BeforeClear.GetSpecial(s.Activation.Value.Cell.X,s.Activation.Value.Cell.Y)==SpecialKind.RayVertical),
            "motor border power chains after explosion");
        Check(r.Steps.Any(s=>s.HasFall)&&model.GetContent(3,3)==CellContent.Candy&&model.IsStable,
            "motor footprint refills and settles");
        second=PrimedMotor();second.DebugActivatePower(new C(0,3),SpecialKind.RayHorizontal);
        Check(second.Snapshot()==model.Snapshot(),"motor explosion remains deterministic");
        model=PrimedMotor();
        model.DebugSetCandy(new C(3,2),2);model.DebugSetCandy(new C(4,2),1);
        model.DebugSetCandy(new C(5,2),2);model.DebugSetCandy(new C(4,1),2);
        r=model.TrySwap(new C(4,1),new C(4,2));
        Check(r.Accepted&&r.Steps.Count(s=>s.MotorExplosion)==1&&model.GetContent(3,3)==CellContent.Candy&&model.IsStable,
            "ordinary match triggers fifth-hit explosion and refill");
        model=Board();
        for(int y=0;y<3;y++)for(int x=0;x<2;x++)model.PlaceObstacle(new C(x,y),CellContent.Motor,1);
        r=model.DebugActivatePower(new C(2,1),SpecialKind.Solvent);
        Check(r.Steps.Count(s=>s.MotorExplosion)==1&&model.GetContent(0,0)==CellContent.Candy,
            "rotated motor explosion clips safely at board corner");
        foreach(string file in System.IO.Directory.GetFiles("Assets/WorkshopMatch/Resources/WorkshopMatch/Levels","level_*.json"))
        {
            using var json=System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(file));var root=json.RootElement;
            bool rotated=root.TryGetProperty("MotorRotated",out var rot)&&rot.GetBoolean();
            Check(root.GetProperty("Height").GetInt32()==8 && root.GetProperty("Cells").EnumerateArray().All(c=>
                c.GetProperty("Y").GetInt32()+(c.GetProperty("Content").GetInt32()==2?(rotated?3:2):1)<=8),
                System.IO.Path.GetFileName(file)+" 8x8 bounds");
        }
        var wallet = new ToolProgressTracker();
        Check(!wallet.TrySpinWheel(0), "wheel rejects insufficient bank");
        wallet.BankRemainingMoves(9);
        Check(!wallet.TrySpinWheel(1) && wallet.BankedMoves==9,"wheel needs exactly ten moves");
        wallet.BankRemainingMoves(1);
        Check(wallet.TrySpinWheel(0) && wallet.BankedMoves==0 && wallet.Stock(ToolKind.Solvent)==1,"wheel atomically pays ten and awards hammer");
        for(int sector=0;sector<6;sector++)
        {
            var trial = new ToolProgressTracker(new ToolProgressData {BankedMoves=10});
            Check(trial.TrySpinWheel(sector) && trial.BankedMoves==0 &&
                (sector==5 ? !trial.FreeWheelSpin && trial.Stock(ToolKind.Ray)==0 && trial.Stock(ToolKind.Solvent)==0 && trial.Stock(ToolKind.Magnet)==0 : trial.Stock(sector==2?ToolKind.Magnet:sector==0||sector==3?ToolKind.Solvent:ToolKind.Ray)==(sector==3||sector==4?2:1)),
                "wheel sector reward "+sector);
        }
        wallet.BankRemainingMoves(10); wallet.TrySpinWheel(5);
        var restoredWallet = new ToolProgressTracker(wallet.Snapshot);
        Check(!restoredWallet.FreeWheelSpin && !restoredWallet.TrySpinWheel(2) && restoredWallet.BankedMoves==0 &&
            restoredWallet.Stock(ToolKind.Magnet)==0,"blank wheel sector gives no reward or free spin after restore");
        Check(!restoredWallet.TrySpinWheel(6) && !restoredWallet.TrySpinWheel(-1),"wheel rejects invalid sectors");
        restoredWallet.BankRemainingMoves(-10);
        Check(restoredWallet.BankedMoves==0,"negative win moves ignored");
        var saturated = new ToolProgressTracker(new ToolProgressData {BankedMoves=int.MaxValue});
        saturated.BankRemainingMoves(100);
        Check(saturated.BankedMoves==int.MaxValue,"move bank cannot overflow");
    }
}
