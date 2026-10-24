using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using WorkshopMatch;
using C = WorkshopMatch.Match3BoardModel.Cell;
partial class Program
{
    static Match3BoardModel TutorialBoard(int level,Action<SpecialKind> callback=null,int? seedOverride=null)
    {
        string path=Path.Combine("Assets/WorkshopMatch/Resources/WorkshopMatch/Levels",$"level_{level:000}.json");
        using var json=JsonDocument.Parse(File.ReadAllText(path).TrimStart('\uFEFF')); var d=json.RootElement;
        var b=new Match3BoardModel(d.GetProperty("Width").GetInt32(),d.GetProperty("Height").GetInt32(),d.GetProperty("ColorCount").GetInt32(),seedOverride??d.GetProperty("SpawnSeed").GetInt32(),d.GetProperty("Moves").GetInt32(),callback);
        foreach(var cell in d.GetProperty("Cells").EnumerateArray())
        {
            int x=cell.GetProperty("X").GetInt32(),y=cell.GetProperty("Y").GetInt32();
            var c=(CellContent)cell.GetProperty("Content").GetInt32(); int state=cell.GetProperty("ObstacleState").GetInt32();
            if(c==CellContent.Motor) {
                int orientation=cell.TryGetProperty("MotorOrientation",out var orient)?orient.GetInt32():0;
                bool rotated=orientation==2||(orientation==0&&d.TryGetProperty("MotorRotated",out var oldRot)&&oldRot.GetBoolean());
                b.PlaceMotor(new C(x,y),rotated?2:3,rotated?3:2,state);
            }
            else if(c==CellContent.Bolt||c==CellContent.FrozenGrease)b.PlaceObstacle(new C(x,y),c,state);
            else if(cell.GetProperty("CandyColor").GetInt32()>=0)b.DebugSetCandy(new C(x,y),cell.GetProperty("CandyColor").GetInt32());
            if(cell.GetProperty("Oil").GetBoolean())b.SetOil(new C(x,y));
        }
        if(level==1)b.ConfigureMoveGoal(10);
        b.SealObjectives();return b;
    }
    static void RunTutorialChecks()
    {
        Check(!TutorialPresentationRules.IsMeaningfulMapSwipe(25,1200,.8f),"tiny map drag cannot finish tutorial");
        Check(!TutorialPresentationRules.IsMeaningfulMapSwipe(300,1200,.1f),"brief accidental map flick cannot finish tutorial");
        Check(TutorialPresentationRules.IsMeaningfulMapSwipe(240,1200,.5f),"deliberate substantial map swipe advances tutorial");
        for(int lineCount=1;lineCount<=8;lineCount++)foreach(bool prompt in new[]{false,true})
        {
            float height=TutorialPresentationRules.PanelHeight(lineCount,prompt);
            float lastContent=TutorialPresentationRules.TopPadding+lineCount*TutorialPresentationRules.TextPitch+(prompt?TutorialPresentationRules.PromptGap+TutorialPresentationRules.PromptHeight:0);
            Check(height-lastContent>=.09f&&TutorialPresentationRules.TopPadding>.07f,"tutorial text and prompt clear inner frame for "+lineCount+" lines, prompt="+prompt);
        }
        var firstLevel=TutorialBoard(1);
        Check(firstLevel.MoveGoal==10&&firstLevel.MovesRemaining==10,"only first level has ten-move beginner goal");
        Check(!firstLevel.TrySwap(new C(0,0),new C(7,7)).Accepted&&firstLevel.ValidMovesMade==0,"invalid beginner swap does not count toward move goal");
        for(int turn=1;turn<=10;turn++)
        {
            Check(firstLevel.TryFindBestHintSwap(out var moveFrom,out var moveTo),"beginner round has valid move "+turn);
            Check(firstLevel.TrySwap(moveFrom,moveTo).Accepted&&firstLevel.ValidMovesMade==turn,"beginner goal counts accepted move once "+turn);
            Check(firstLevel.State==(turn==10?RoundState.Won:RoundState.Playing),"beginner round wins only after tenth resolved move "+turn);
        }
        var goalCopy=firstLevel.CreatePreviewCopy();Check(goalCopy.MoveGoal==10&&goalCopy.ValidMovesMade==10,"preview preserves beginner goal progress");
        Check(TutorialBoard(2).MoveGoal==0,"second level keeps ordinary objective rules");
        var oilGoals=Board();
        oilGoals.PlaceObstacle(new C(3,3),CellContent.FrozenGrease,1);
        oilGoals.PlaceObstacle(new C(6,6),CellContent.FrozenGrease,1);
        foreach(var cell in new[]{new C(3,3),new C(6,6),new C(0,0),new C(7,0)})oilGoals.SetOil(cell);
        oilGoals.SealObjectives();
        var initialOil=oilGoals.GetObjectives();
        Check(initialOil.TotalFrozenOil==2 && initialOil.RemainingFrozenOil==2 && initialOil.TotalOil==4 && initialOil.RemainingOil==4,"separate two frozen shells and four floor oil objectives");
        oilGoals.DebugResolveGroup(new C(2,3),new C(2,4),new C(1,3));
        var crackedOil=oilGoals.GetObjectives();
        Check(crackedOil.TotalFrozenOil==2 && crackedOil.RemainingFrozenOil==1 && crackedOil.RemainingOil==4,"breaking shell advances only frozen objective and retains underlying oil");
        var copiedOil=oilGoals.CreatePreviewCopy().GetObjectives();
        Check(copiedOil.TotalFrozenOil==2 && copiedOil.RemainingFrozenOil==1 && copiedOil.TotalOil==4,"preview preserves separate shell objective totals");
        var shellOnly=Board();shellOnly.PlaceObstacle(new C(3,3),CellContent.FrozenGrease,1);shellOnly.SealObjectives();
        shellOnly.DebugResolveGroup(new C(2,3),new C(2,4),new C(1,3));
        Check(shellOnly.State==RoundState.Won,"shell-only target level completes after last shell breaks");
        foreach(var lesson in new[]{(5,TutorialMovePlanner.Goal.Oil),(6,TutorialMovePlanner.Goal.Hammer),(7,TutorialMovePlanner.Goal.Frozen),(8,TutorialMovePlanner.Goal.Motor),(9,TutorialMovePlanner.Goal.Magnet)})
        {
            var lessonBoard=TutorialBoard(lesson.Item1);uint beforeHash=lessonBoard.SnapshotHash();
            Check(TutorialMovePlanner.Find(lessonBoard,lesson.Item2,out var start,out var end,out bool direct),"actual configured lesson move exists level "+lesson.Item1);
            Check(lessonBoard.SnapshotHash()==beforeHash,"configured lesson search preserves layout/RNG level "+lesson.Item1);
            Console.WriteLine($"LESSON {lesson.Item1} ({start.X},{start.Y})->({end.X},{end.Y}) direct={direct}");
            var action=lessonBoard.TrySwap(start,end);
            if(!direct){Check(TutorialMovePlanner.FindDirect(lessonBoard,lesson.Item2,out start,out end),"setup reaches direct target lesson "+lesson.Item1);action=lessonBoard.TrySwap(start,end);}
            Check(TutorialMovePlanner.Matches(action,lesson.Item2),"first match genuinely demonstrates lesson "+lesson.Item1);
            if(lesson.Item1==7)
            {
                Check(TutorialMovePlanner.FindActivation(lessonBoard,TutorialMovePlanner.Goal.Hammer,out start,out end),"frozen lesson creates a hammer for second action");
                var green=new[]{new C(start.X+1,start.Y),new C(start.X-1,start.Y),new C(start.X,start.Y+1),new C(start.X,start.Y-1)}.First(n=>lessonBoard.GetContent(n)==CellContent.Candy&&lessonBoard.GetColor(n.X,n.Y)==2);
                Check(lessonBoard.GetContent(green)==CellContent.Candy&&lessonBoard.GetColor(green.X,green.Y)==2,"hammer in frozen lesson has neighbouring green candy");
                var cleanup=lessonBoard.TrySwap(start,green);
                Check(cleanup.Accepted&&cleanup.Steps.Any(step=>step.Activation?.Kind==SpecialKind.Solvent),"second frozen lesson uses created hammer");
                Check(cleanup.Steps[0].AfterFall.Objectives.RemainingFrozenOil<cleanup.Steps[0].BeforeClear.Objectives.RemainingFrozenOil,"hammer in frozen lesson breaks surrounding shells");
            }
            if(lesson.Item1==6||lesson.Item1==9)
            {
                Check(TutorialMovePlanner.FindActivation(lessonBoard,lesson.Item2,out start,out end),"produced power remains usable level "+lesson.Item1);
                Console.WriteLine($"USE {lesson.Item1} ({start.X},{start.Y})->({end.X},{end.Y})");
                var used=lessonBoard.TrySwap(start,end);Check(used.Accepted&&used.Steps.Any(step=>step.Activation.HasValue),"produced power genuinely activates level "+lesson.Item1);
                Check(lessonBoard.State==RoundState.Playing,"power lesson leaves round open for toolbar unlock "+lesson.Item1);
            }
        }
        var rocketLesson=TutorialBoard(3);
        Check(TutorialMovePlanner.FindDirect(rocketLesson,TutorialMovePlanner.Goal.Rocket,out var rocketFrom,out var rocketTo),"third lesson direct rocket production");
        rocketLesson.TrySwap(rocketFrom,rocketTo);
        Check(TutorialMovePlanner.FindActivation(rocketLesson,TutorialMovePlanner.Goal.Rocket,out rocketFrom,out rocketTo)&&rocketLesson.TrySwap(rocketFrom,rocketTo).Accepted,"third lesson can activate the rocket it just produced");
        var stockRocketLesson=TutorialBoard(4);var stockPair=TutorialMovePlanner.Pairs(stockRocketLesson).First();
        Check(stockRocketLesson.TryUseTool(stockPair.a,SpecialKind.RayHorizontal).Accepted&&TutorialMovePlanner.FindActivation(stockRocketLesson,TutorialMovePlanner.Goal.Rocket,out rocketFrom,out rocketTo)&&stockRocketLesson.TrySwap(rocketFrom,rocketTo).Accepted,"fourth lesson places and activates chosen horizontal stock rocket");
        var comboLesson=TutorialBoard(10);var firstPair=TutorialMovePlanner.Pairs(comboLesson).First();int placementMoves=comboLesson.MovesRemaining;
        Check(comboLesson.TryUseTool(firstPair.a,SpecialKind.Magnet).Accepted&&comboLesson.TryUseTool(firstPair.b,SpecialKind.Solvent).Accepted,"level ten trial tools can be placed on neighbouring normal candies");
        Check(comboLesson.MovesRemaining==placementMoves,"level ten two placements do not spend moves");
        var combined=comboLesson.TrySwap(firstPair.a,firstPair.b);
        Check(combined.Accepted&&combined.Steps.Any(step=>step.Activation.HasValue&&step.Activation.Value.Shape==Match3BoardModel.PowerShape.Conversion),"level ten magnet plus hammer runs genuine conversion combo");
        Check(comboLesson.State==RoundState.Playing,"combo lesson leaves room for final explanation");
        var wheelGift=new ToolProgressTracker();Check(wheelGift.GrantWheelIntroGiftOnce()&&wheelGift.BankedMoves==10,"wheel tutorial grants one ten-move spin");
        var wheelReload=new ToolProgressTracker(wheelGift.Snapshot);Check(!wheelReload.GrantWheelIntroGiftOnce()&&wheelReload.BankedMoves==10,"wheel intro gift cannot be farmed on replay or reload");
        var oneGift=new ToolProgressTracker();Check(oneGift.GrantTutorialGiftOnce(ToolKind.Magnet)&&oneGift.Stock(ToolKind.Magnet)==1,"first magnet unlock gives one saved gift");
        var reloadedGift=new ToolProgressTracker(oneGift.Snapshot);Check(!reloadedGift.GrantTutorialGiftOnce(ToolKind.Magnet)&&reloadedGift.Stock(ToolKind.Magnet)==1,"replay/reload cannot farm unlock gift");
        Check(!TrainingLevelPolicy.ToolLocked("level_004",true,ToolKind.Ray)&&TrainingLevelPolicy.ToolLocked("level_005",true,ToolKind.Solvent)&&!TrainingLevelPolicy.ToolLocked("level_006",true,ToolKind.Solvent)&&TrainingLevelPolicy.ToolLocked("level_008",true,ToolKind.Magnet)&&!TrainingLevelPolicy.ToolLocked("level_009",true,ToolKind.Magnet),"individual tool unlock thresholds are four six nine");
        foreach(string level in new[]{"level_001","level_002","level_003"})
        {
            var progress=new ToolProgressTracker();
            for(int n=0;n<20;n++)foreach(var special in new[]{SpecialKind.Solvent,SpecialKind.RayVertical,SpecialKind.Magnet})
                TrainingLevelPolicy.RecordProduced(level,true,progress,special);
            Check(TrainingLevelPolicy.ToolsLocked(level,true) && progress.Stock(ToolKind.Solvent)==0 && progress.Stock(ToolKind.Ray)==0 && progress.Stock(ToolKind.Magnet)==0 && progress.Pieces(ToolKind.Solvent)==0,"first three levels lock tools and never grant stock or pieces "+level);
        }
        var savedProgress=new ToolProgressTracker(new ToolProgressData{SolventStock=7,SolventPieces=3});
        var gated=TutorialBoard(3,special=>TrainingLevelPolicy.RecordProduced("level_003",true,savedProgress,special),26092026);
        var trainingMove=gated.TrySwap(new C(2,4),new C(1,4));
        Check(trainingMove.Accepted && gated.ProducedRays>0 && savedProgress.Stock(ToolKind.Solvent)==7 && savedProgress.Pieces(ToolKind.Solvent)==3 && savedProgress.Stock(ToolKind.Ray)==0 && savedProgress.Pieces(ToolKind.Ray)==0,"training board produces working rocket while preserving saved inventory");
        var unlockedProgress=new ToolProgressTracker();
        for(int n=0;n<4;n++)TrainingLevelPolicy.RecordProduced("level_004",true,unlockedProgress,SpecialKind.Solvent);
        Check(!TrainingLevelPolicy.ToolsLocked("level_004",true) && unlockedProgress.Stock(ToolKind.Solvent)==0,"level four keeps hammer production reward locked until six");
        var disabledProgress=new ToolProgressTracker();
        for(int n=0;n<4;n++)TrainingLevelPolicy.RecordProduced("level_001",false,disabledProgress,SpecialKind.Solvent);
        Check(!TrainingLevelPolicy.ToolsLocked("level_001",false) && disabledProgress.Stock(ToolKind.Solvent)==1,"single preview flag restores normal tools and rewards");
        int callbacks=0; var board=TutorialBoard(3,_=>callbacks++,26092026);uint hash=board.SnapshotHash();int moves=board.MovesRemaining;
        var preview=board.CreatePreviewCopy();var swap=preview.TrySwap(new C(2,4),new C(1,4));
        Check(swap.Accepted && preview.ProducedRays>0,"level 3 screenshot red-left-purple swap creates rocket");
        Check(board.SnapshotHash()==hash && board.MovesRemaining==moves && callbacks==0,"tutorial simulation preserves live board RNG moves and stock callback");
        var actual=board.TrySwap(new C(2,4),new C(1,4));
        Check(actual.Accepted && callbacks>0 && board.SnapshotHash()==preview.SnapshotHash(),"preview and actual rocket move have identical random fall");
        foreach(int level in new[]{1,2,3})
        {
            board=TutorialBoard(level);var goal=level==1?TutorialMovePlanner.Goal.Match:level==2?TutorialMovePlanner.Goal.Bolt:TutorialMovePlanner.Goal.Rocket;
            hash=board.SnapshotHash();
            Check(TutorialMovePlanner.Find(board,goal,out var a,out var b,out var direct),"tutorial finds actionable move level "+level);
            Check(board.SnapshotHash()==hash,"tutorial planner is read only level "+level);
            var result=board.TrySwap(a,b);Check(result.Accepted,"tutorial suggested move valid level "+level);
            if(level==2)
            {
                Check(direct && result.Steps.Any(s=>s.HitTargets.Any(h=>s.BeforeClear.GetContent(h.X,h.Y)==CellContent.Bolt)),"M6 lesson has real adjacent rust hit");
                bool exited=result.Steps.Any(s=>s.ExitedBolts.Count>0);
                for(int turn=0;turn<8 && !exited;turn++)
                {
                    Check(TutorialMovePlanner.Find(board,goal,out a,out b,out direct),"M6 lesson next action exists");
                    result=board.TrySwap(a,b);exited=result.Steps.Any(s=>s.ExitedBolts.Count>0);
                }
                Check(exited,"M6 lesson reaches clean and exit without changing obstacles");
                preview=board.CreatePreviewCopy();
                Check(preview.HasMotor==board.HasMotor && preview.GetObjectives().MotorHits==board.GetObjectives().MotorHits,"preview preserves motor objective state");
            }
            if(level==3)Check(direct && board.ProducedRays>0,"third tutorial uses real four-match rocket");
        }
    }
}
