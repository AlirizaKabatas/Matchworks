using System;

namespace WorkshopMatch
{
    public enum AchievementDifficulty { Easy, Medium, Hard }
    public enum AchievementMetric { FirstLevel, FirstFive, FirstTen, AllLevels, Free6To10, Free11To15, Free16To20, AllFree,
        FirstRay, Wheel, ComboHammer, ComboMagnet, ComboRay, ComboHammerRay, ComboMagnetHammer, ComboMagnetRay,
        AllCombos, Variety, Motors, Parts, SpareMoves, LastMove, OnlyPowers, Magnets, RoundCombos }

    public sealed class AchievementDefinition
    {
        public AchievementDefinition(string id,string tr,string en,string bodyTr,string bodyEn,AchievementDifficulty difficulty,
            AchievementMetric metric,int target,string icon="star")
        { Id=id;TitleTr=tr;TitleEn=en;BodyTr=bodyTr;BodyEn=bodyEn;Difficulty=difficulty;Metric=metric;Target=target;Icon=icon; }
        public string Id { get; }
        public string TitleTr { get; }
        public string TitleEn { get; }
        public string BodyTr { get; }
        public string BodyEn { get; }
        public AchievementDifficulty Difficulty { get; }
        public AchievementMetric Metric { get; }
        public int Target { get; }
        public string Icon { get; }
    }

    public static class AchievementCatalog
    {
        // Stable IDs are independent of display order, language and difficulty changes.
        public static readonly AchievementDefinition[] All={
            new("first_step","İlk Adım","First Step","İlk bölümü bitir.","Complete level 1.",AchievementDifficulty.Easy,AchievementMetric.FirstLevel,1,"achievement_first_step_v2"),
            new("first_five","İlk Beş","First Five","İlk 5 bölümü bitir.","Complete the first 5 levels.",AchievementDifficulty.Easy,AchievementMetric.FirstFive,5,"achievement_first_five_v2"),
            new("first_spark","İlk Kıvılcım","First Spark","Şeker eşleştirerek bir havai fişek üret.","Create a firework by matching candies.",AchievementDifficulty.Easy,AchievementMetric.FirstRay,1,"achievement_first_spark_v2"),
            new("first_spin","Çarkın Şansı","Wheel of Fortune","Çarkı ilk kez çevir.","Spin the wheel for the first time.",AchievementDifficulty.Easy,AchievementMetric.Wheel,1,"achievement_first_spin_v2"),
            new("first_ten","Eğitim Tamam","Training Complete","İlk 10 bölümü bitir.","Complete the first 10 levels.",AchievementDifficulty.Easy,AchievementMetric.FirstTen,10,"achievement_first_ten_v2"),
            new("hammer_pair","Çift Çekiç","Double Hammer","İki çekici birleştir.","Combine two hammers.",AchievementDifficulty.Medium,AchievementMetric.ComboHammer,1,"achievement_hammer_pair_v2"),
            new("ray_pair","Çift Fişek","Double Firework","İki havai fişeği birleştir.","Combine two fireworks.",AchievementDifficulty.Medium,AchievementMetric.ComboRay,1,"achievement_ray_pair_v2"),
            new("hammer_ray","Patlayıcı Darbe","Explosive Strike","Çekiç ve havai fişeği birleştir.","Combine a hammer and a firework.",AchievementDifficulty.Medium,AchievementMetric.ComboHammerRay,1,"achievement_hammer_ray_v2"),
            new("magnet_hammer","Manyetik Darbe","Magnetic Strike","Mıknatıs ve çekici birleştir.","Combine a magnet and a hammer.",AchievementDifficulty.Medium,AchievementMetric.ComboMagnetHammer,1,"achievement_magnet_hammer_v2"),
            new("magnet_ray","Manyetik Kıvılcım","Magnetic Spark","Mıknatıs ve havai fişeği birleştir.","Combine a magnet and a firework.",AchievementDifficulty.Medium,AchievementMetric.ComboMagnetRay,1,"achievement_magnet_ray_v2"),
            new("magnet_pair","Çift Mıknatıs","Double Magnet","İki mıknatısı birleştir.","Combine two magnets.",AchievementDifficulty.Medium,AchievementMetric.ComboMagnet,1,"achievement_magnet_pair_v2"),
            new("all_combos","Tam Koleksiyon","Full Collection","Altı farklı güç birleşimini de en az bir kez yap.","Perform all six power combinations at least once.",AchievementDifficulty.Medium,AchievementMetric.AllCombos,6,"achievement_all_combos_v2"),
            new("free_6_10","Araçsız Usta I","Tool-Free Master I","6–10. bölümleri stoktan ek araç yerleştirmeden bitir.","Complete levels 6–10 without placing an inventory tool.",AchievementDifficulty.Medium,AchievementMetric.Free6To10,5,"achievement_free_6_10_v2"),
            new("free_11_15","Araçsız Usta II","Tool-Free Master II","11–15. bölümleri stoktan ek araç yerleştirmeden bitir.","Complete levels 11–15 without placing an inventory tool.",AchievementDifficulty.Medium,AchievementMetric.Free11To15,5,"achievement_free_11_15_v2"),
            new("free_16_20","Araçsız Usta III","Tool-Free Master III","16–20. bölümleri stoktan ek araç yerleştirmeden bitir.","Complete levels 16–20 without placing an inventory tool.",AchievementDifficulty.Hard,AchievementMetric.Free16To20,5,"achievement_free_16_20_v2"),
            new("variety","Üretim Hattı","Production Line","Aynı bölümde eşleşmeyle bir mıknatıs, çekiç ve fişek üret.","Create a magnet, hammer and firework through matches in one level.",AchievementDifficulty.Medium,AchievementMetric.Variety,3,"achievement_variety_v2"),
            new("motors","Çifte Motor","Double Engine","Aynı bölümde iki motoru patlat.","Destroy two engines in one level.",AchievementDifficulty.Medium,AchievementMetric.Motors,2,"achievement_motors_v2"),
            new("parts","Pas Ustası","Rust Master","Aynı bölümde 5 vida veya somunu temizleyip çıkar.","Clear and collect 5 screws or nuts in one level.",AchievementDifficulty.Easy,AchievementMetric.Parts,5,"achievement_parts_v2"),
            new("spare_moves","Rahat Zafer","Comfortable Win","Bir bölümü en az 5 hamle bırakarak bitir.","Win a level with at least 5 moves left.",AchievementDifficulty.Medium,AchievementMetric.SpareMoves,5,"achievement_spare_moves_v2"),
            new("last_move","Son Hamle","Last Move","Son kalan hamlenle bir bölümü kazan.","Win a level on your last remaining move.",AchievementDifficulty.Hard,AchievementMetric.LastMove,1,"achievement_last_move_v2"),
            new("all_levels","Tamir Ustası","Workshop Master","20 bölümün tamamını bitir.","Complete all 20 levels.",AchievementDifficulty.Hard,AchievementMetric.AllLevels,20,"achievement_all_levels_v2"),
            new("all_free","Saf Strateji","Pure Strategy","20 bölümü stoktan ek araç yerleştirmeden bitir.","Complete all 20 levels without placing inventory tools.",AchievementDifficulty.Hard,AchievementMetric.AllFree,20,"achievement_all_free_v2"),
            new("only_powers","Yalnız Güç","Powers Only","Normal eşleştirme hamlesi yapmadan bir bölümü kazan. Otomatik eşleşmeler serbesttir.","Win without making a normal matching move. Automatic cascades are allowed.",AchievementDifficulty.Hard,AchievementMetric.OnlyPowers,1,"achievement_only_powers_v2"),
            new("three_magnets","Büyük Üretim","Mass Production","Aynı bölümde eşleşmeyle 3 mıknatıs üret.","Create 3 magnets through matches in one level.",AchievementDifficulty.Hard,AchievementMetric.Magnets,3,"achievement_three_magnets_v2"),
            new("three_combos","Birleşim Ustası","Combo Master","Aynı bölümde 3 farklı güç birleşimi yap.","Perform 3 different power combinations in one level.",AchievementDifficulty.Hard,AchievementMetric.RoundCombos,3,"achievement_three_combos_v2")
        };
    }

    [Serializable]
    public sealed class AchievementProgressData
    {
        public int Version=1;
        public int CompletedLevels,ToolFreeLevels,Combinations;
        public int BestMagnets,BestVariety,BestMotors,BestParts,BestRoundCombos,BestSpareMoves;
        public bool FirstRay,WheelSpun,LastMoveWin,OnlyPowersWin;
    }

    public sealed class AchievementTracker
    {
        private readonly AchievementProgressData data;
        private int roundLevel,roundNormalMoves,roundStockUses,roundPowerMoves,roundComboMask,roundMotors;
        private bool roundActive,roundFinished;
        public AchievementTracker(AchievementProgressData saved=null) { data=saved??new AchievementProgressData(); }
        public AchievementProgressData Snapshot=>new() {Version=1,CompletedLevels=data.CompletedLevels,ToolFreeLevels=data.ToolFreeLevels,
            Combinations=data.Combinations,BestMagnets=data.BestMagnets,BestVariety=data.BestVariety,BestMotors=data.BestMotors,
            BestParts=data.BestParts,BestRoundCombos=data.BestRoundCombos,BestSpareMoves=data.BestSpareMoves,
            FirstRay=data.FirstRay,WheelSpun=data.WheelSpun,LastMoveWin=data.LastMoveWin,OnlyPowersWin=data.OnlyPowersWin};
        public void BeginRound(int level)
        {
            roundLevel=level;roundActive=level>=1&&level<=20;roundFinished=false;
            roundNormalMoves=roundStockUses=roundPowerMoves=roundComboMask=roundMotors=0;
        }
        public void InvalidateRound() { roundActive=false; }
        public void ImportCompletedLevels(int count)
        {count=Math.Max(0,Math.Min(20,count));data.CompletedLevels|=(1<<count)-1;}
        public bool HasCompletedLevel(int level)=>level>=1&&level<=20&&(data.CompletedLevels&(1<<(level-1)))!=0;
        public void ObserveSwap(bool accepted,SpecialKind first,SpecialKind second)
        {
            if(!accepted||!roundActive||roundFinished)return;
            if(first==SpecialKind.None&&second==SpecialKind.None)roundNormalMoves++;
            else roundPowerMoves++;
            int combination=Combination(first,second);
            if(combination>=0){data.Combinations|=1<<combination;roundComboMask|=1<<combination;}
            data.BestRoundCombos=Math.Max(data.BestRoundCombos,CountBits(roundComboMask));
        }
        public void ObserveStockPlacement(bool accepted)
        {if(accepted&&roundActive&&!roundFinished)roundStockUses++;}
        // These are totals within the current model, not deltas across retries.
        public void ObserveRoundTotals(int rays,int hammers,int magnets,int collectedParts,int newMotorExplosions)
        {
            if(!roundActive||roundFinished)return;
            if(rays>0)data.FirstRay=true;
            data.BestMagnets=Math.Max(data.BestMagnets,Math.Max(0,magnets));
            int variety=(rays>0?1:0)+(hammers>0?1:0)+(magnets>0?1:0);
            data.BestVariety=Math.Max(data.BestVariety,variety);
            data.BestParts=Math.Max(data.BestParts,Math.Max(0,collectedParts));
            roundMotors+=Math.Max(0,newMotorExplosions);
            data.BestMotors=Math.Max(data.BestMotors,roundMotors);
        }
        public void CompleteRound(int remainingMoves)
        {
            if(!roundActive||roundFinished)return;
            roundFinished=true;
            data.CompletedLevels|=1<<(roundLevel-1);
            if(roundStockUses==0)data.ToolFreeLevels|=1<<(roundLevel-1);
            data.BestSpareMoves=Math.Max(data.BestSpareMoves,Math.Max(0,remainingMoves));
            if(remainingMoves==0)data.LastMoveWin=true;
            if(roundNormalMoves==0&&roundPowerMoves>0)data.OnlyPowersWin=true;
        }
        public void RecordWheelSpin() {data.WheelSpun=true;}
        public int Progress(AchievementDefinition item)
        {
            int value=item.Metric switch {
                AchievementMetric.FirstLevel=>data.CompletedLevels&1,
                AchievementMetric.FirstFive=>CountRange(data.CompletedLevels,1,5),
                AchievementMetric.FirstTen=>CountRange(data.CompletedLevels,1,10),
                AchievementMetric.AllLevels=>CountRange(data.CompletedLevels,1,20),
                AchievementMetric.Free6To10=>CountRange(data.ToolFreeLevels,6,10),
                AchievementMetric.Free11To15=>CountRange(data.ToolFreeLevels,11,15),
                AchievementMetric.Free16To20=>CountRange(data.ToolFreeLevels,16,20),
                AchievementMetric.AllFree=>CountRange(data.ToolFreeLevels,1,20),
                AchievementMetric.FirstRay=>data.FirstRay?1:0,AchievementMetric.Wheel=>data.WheelSpun?1:0,
                AchievementMetric.ComboHammer=>(data.Combinations>>0)&1,AchievementMetric.ComboMagnet=>(data.Combinations>>1)&1,
                AchievementMetric.ComboRay=>(data.Combinations>>2)&1,AchievementMetric.ComboHammerRay=>(data.Combinations>>3)&1,
                AchievementMetric.ComboMagnetHammer=>(data.Combinations>>4)&1,AchievementMetric.ComboMagnetRay=>(data.Combinations>>5)&1,
                AchievementMetric.AllCombos=>CountBits(data.Combinations&63),AchievementMetric.Variety=>data.BestVariety,
                AchievementMetric.Motors=>data.BestMotors,AchievementMetric.Parts=>data.BestParts,
                AchievementMetric.SpareMoves=>data.BestSpareMoves,AchievementMetric.LastMove=>data.LastMoveWin?1:0,
                AchievementMetric.OnlyPowers=>data.OnlyPowersWin?1:0,AchievementMetric.Magnets=>data.BestMagnets,
                AchievementMetric.RoundCombos=>data.BestRoundCombos,_=>0};
            return Math.Min(item.Target,Math.Max(0,value));
        }
        public int CompletedCount {get {int n=0;foreach(var item in AchievementCatalog.All)if(Progress(item)==item.Target)n++;return n;}}
        private static bool Ray(SpecialKind kind)=>kind==SpecialKind.RayHorizontal||kind==SpecialKind.RayVertical;
        private static int Combination(SpecialKind a,SpecialKind b)
        {
            if(a==SpecialKind.None||b==SpecialKind.None)return -1;
            if(a==SpecialKind.Solvent&&b==SpecialKind.Solvent)return 0;
            if(a==SpecialKind.Magnet&&b==SpecialKind.Magnet)return 1;
            if(Ray(a)&&Ray(b))return 2;
            if(a==SpecialKind.Magnet||b==SpecialKind.Magnet)return a==SpecialKind.Solvent||b==SpecialKind.Solvent?4:5;
            return 3;
        }
        private static int CountRange(int mask,int first,int last)
        {int n=0;for(int i=first-1;i<last;i++)if((mask&(1<<i))!=0)n++;return n;}
        private static int CountBits(int value)
        {int n=0;for(int i=0;i<32;i++)if((value&(1<<i))!=0)n++;return n;}
    }
}
