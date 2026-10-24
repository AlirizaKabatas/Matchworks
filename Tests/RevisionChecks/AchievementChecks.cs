using System;
using System.Linq;
using System.Text.Json;
using WorkshopMatch;
partial class Program
{
    static AchievementDefinition Ach(string id)=>AchievementCatalog.All.Single(a=>a.Id==id);
    static void RunAchievementChecks()
    {
        var t=new AchievementTracker();
        Check(AchievementCatalog.All.Length==25&&AchievementCatalog.All.Select(a=>a.Id).Distinct().Count()==25,"25 achievement definitions have stable unique IDs");
        Check(Ach("magnet_hammer").TitleTr=="Manyetik Darbe"&&Ach("magnet_hammer").BodyTr.StartsWith("Mıknatıs"),"magnet hammer renamed and magnet first");
        Check(AchievementCatalog.All[7].Id=="hammer_ray"&&AchievementCatalog.All[8].Id=="magnet_hammer"&&AchievementCatalog.All[9].Id=="magnet_ray"&&AchievementCatalog.All[10].Id=="magnet_pair", "explosive strike eighth; magnetic strike ninth; magnet achievements consecutive");
        Check(Ach("first_ten").Difficulty==AchievementDifficulty.Easy&&Ach("parts").Difficulty==AchievementDifficulty.Easy&&Ach("free_16_20").Difficulty==AchievementDifficulty.Hard,"requested achievement difficulties");
        Check(AchievementCatalog.All.All(a=>t.Progress(a)==0)&&t.CompletedCount==0,"new achievements start empty");
        for(int i=1;i<=14;i++){t.BeginRound(i);t.ObserveSwap(true,SpecialKind.None,SpecialKind.None);t.CompleteRound(3);}
        Check(t.Progress(Ach("all_levels"))==14&&t.Progress(Ach("first_ten"))==10,"14 unique completed levels display 14 of 20");
        t.BeginRound(1);t.CompleteRound(3);
        Check(t.Progress(Ach("all_levels"))==14,"repeat level win does not add another completed level");
        for(int i=15;i<=20;i++){t.BeginRound(i);t.CompleteRound(3);}
        Check(t.Progress(Ach("all_levels"))==20&&t.Progress(Ach("all_free"))==20,"20 actual wins complete level sets");
        var imported=new AchievementTracker();imported.ImportCompletedLevels(14);
        Check(imported.Progress(Ach("all_levels"))==14&&imported.Progress(Ach("all_free"))==0&&imported.Progress(Ach("three_magnets"))==0,"legacy import never invents tool-free or production history");
        Check(imported.HasCompletedLevel(10)&&!imported.HasCompletedLevel(20),"map milestones use completed levels, not unlocked final node");
        var finalNode=new AchievementTracker();finalNode.ImportCompletedLevels(19);
        Check(!finalNode.HasCompletedLevel(20),"legacy highest unlocked level20 does not imply demo win");
        finalNode.BeginRound(20);finalNode.CompleteRound(2);
        var finalReload=new AchievementTracker(finalNode.Snapshot);
        Check(finalReload.HasCompletedLevel(20),"actual level20 win restores colored trophy from existing save");
        Check(!finalReload.HasCompletedLevel(0)&&!finalReload.HasCompletedLevel(21),"milestone lookup rejects non-catalog levels");
        t=new AchievementTracker();t.BeginRound(10);t.ObserveRoundTotals(0,0,2,0,0);
        Check(t.Progress(Ach("three_magnets"))==2&&t.Progress(Ach("all_levels"))==0,"partial magnet best persists without a win");
        t.ObserveRoundTotals(0,0,2,0,0);
        Check(t.Progress(Ach("three_magnets"))==2,"cumulative model totals do not double count observations");
        t.CompleteRound(1);t.BeginRound(10);t.ObserveRoundTotals(0,0,1,0,0);t.CompleteRound(1);
        Check(t.Progress(Ach("three_magnets"))==2,"2 then 1 magnets in separate attempts remains 2 of 3");
        var saved=t.Snapshot;saved.BestMagnets=99;
        Check(t.Progress(Ach("three_magnets"))==2,"snapshot mutations do not alter live tracker");
        string json=JsonSerializer.Serialize(t.Snapshot,new JsonSerializerOptions{IncludeFields=true});
        var reloaded=new AchievementTracker(JsonSerializer.Deserialize<AchievementProgressData>(json,new JsonSerializerOptions{IncludeFields=true}));
        Check(reloaded.Progress(Ach("three_magnets"))==2&&reloaded.Progress(Ach("all_levels"))==1,"save fields round-trip best and unique levels");
        reloaded.BeginRound(10);reloaded.ObserveRoundTotals(0,0,3,0,0);
        Check(reloaded.Progress(Ach("three_magnets"))==3,"single attempt reaches magnet target after reload");
        t=new AchievementTracker();t.BeginRound(8);t.ObserveRoundTotals(1,1,0,3,1);t.BeginRound(8);t.ObserveRoundTotals(0,0,1,2,1);
        Check(t.Progress(Ach("variety"))==2&&t.Progress(Ach("parts"))==3&&t.Progress(Ach("motors"))==1,"variety parts and motors do not combine separate attempts");
        t.ObserveRoundTotals(1,1,1,5,1);
        Check(t.Progress(Ach("variety"))==3&&t.Progress(Ach("parts"))==5&&t.Progress(Ach("motors"))==2,"current attempt cumulative totals reach targets");
        t=new AchievementTracker();t.BeginRound(6);t.ObserveStockPlacement(true);t.CompleteRound(5);t.CompleteRound(9);
        Check(t.Progress(Ach("free_6_10"))==0&&t.Progress(Ach("spare_moves"))==5,"used stock blocks tool-free win; duplicate finish is ignored");
        t.BeginRound(6);t.ObserveStockPlacement(false);t.ObserveSwap(true,SpecialKind.Solvent,SpecialKind.None);t.CompleteRound(0);
        Check(t.Progress(Ach("free_6_10"))==1&&t.Progress(Ach("only_powers"))==1&&t.Progress(Ach("last_move"))==1,"fresh retry clears stock use; natural powers and last move count");
        t=new AchievementTracker();t.BeginRound(5);t.ObserveSwap(false,SpecialKind.None,SpecialKind.None);t.ObserveSwap(true,SpecialKind.Magnet,SpecialKind.None);t.CompleteRound(1);
        Check(t.Progress(Ach("only_powers"))==1,"invalid normal swap does not break powers-only condition");
        t=new AchievementTracker();t.BeginRound(5);t.ObserveSwap(true,SpecialKind.None,SpecialKind.None);t.ObserveSwap(true,SpecialKind.Magnet,SpecialKind.None);t.CompleteRound(1);
        Check(t.Progress(Ach("only_powers"))==0,"accepted normal match blocks powers-only achievement");
        t=new AchievementTracker();t.BeginRound(5);t.CompleteRound(1);
        Check(t.Progress(Ach("only_powers"))==0,"zero moves cannot grant powers-only achievement");
        var pairs=new[]{(SpecialKind.Solvent,SpecialKind.Solvent),(SpecialKind.Magnet,SpecialKind.Magnet),(SpecialKind.RayHorizontal,SpecialKind.RayVertical),
            (SpecialKind.Solvent,SpecialKind.RayVertical),(SpecialKind.Magnet,SpecialKind.Solvent),(SpecialKind.Magnet,SpecialKind.RayHorizontal)};
        t=new AchievementTracker();
        foreach(var pair in pairs){t.BeginRound(9);var result=Combo(pair.Item1,pair.Item2);t.ObserveSwap(result.Accepted,pair.Item1,pair.Item2);}
        Check(t.Progress(Ach("all_combos"))==6&&t.Progress(Ach("three_combos"))==1,"real six model combinations tracked; separate attempts do not merge variety");
        t.BeginRound(9);t.ObserveSwap(true,SpecialKind.Solvent,SpecialKind.Magnet);t.ObserveSwap(true,SpecialKind.Magnet,SpecialKind.Solvent);
        Check(t.Progress(Ach("three_combos"))==1,"reversed same combination remains one type");
        t.ObserveSwap(true,SpecialKind.RayVertical,SpecialKind.Magnet);t.ObserveSwap(true,SpecialKind.RayHorizontal,SpecialKind.RayVertical);
        Check(t.Progress(Ach("three_combos"))==3,"three distinct combos in one attempt reach target");
        t=new AchievementTracker();t.BeginRound(9);t.ObserveSwap(false,SpecialKind.Magnet,SpecialKind.Solvent);
        Check(t.Progress(Ach("magnet_hammer"))==0,"rejected combination awards nothing");
        t.InvalidateRound();t.ObserveSwap(true,SpecialKind.Magnet,SpecialKind.Solvent);t.ObserveRoundTotals(5,5,5,5,5);t.CompleteRound(10);
        Check(t.CompletedCount==0,"invalidated debug rounds cannot award achievements");
        t.BeginRound(21);t.ObserveRoundTotals(1,1,3,5,2);t.CompleteRound(5);
        Check(t.CompletedCount==0,"non-catalog levels cannot award fixed 20-level achievements");
        t.RecordWheelSpin();t.RecordWheelSpin();
        Check(t.Progress(Ach("first_spin"))==1,"wheel achievement is one-time persistent flag");
    }
}
