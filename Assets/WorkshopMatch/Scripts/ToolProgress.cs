using System;
using UnityEngine;

namespace WorkshopMatch
{
    public enum ToolKind
    {
        Ray,
        Solvent,
        Magnet
    }

    [Serializable]
    public sealed class ToolProgressData
    {
        public int Version = 1;
        public int RayPieces;
        public int RayStock;
        public int SolventPieces;
        public int SolventStock;
        public int MagnetPieces;
        public int MagnetStock;
        public int BankedMoves;
        public bool FreeWheelSpin;
        public int LastWheelSector = -1;
        public int TutorialGiftMask;
        public int PendingRayCredits,PendingHammerCredits,PendingMagnetCredits;

        public ToolProgressData Copy()
        {
            return new ToolProgressData
            {
                Version = Version,
                RayPieces = RayPieces,
                RayStock = RayStock,
                SolventPieces = SolventPieces,
                SolventStock = SolventStock,
                MagnetPieces = MagnetPieces,
                MagnetStock = MagnetStock,
                BankedMoves = BankedMoves,
                FreeWheelSpin = FreeWheelSpin,
                LastWheelSector = LastWheelSector,
                TutorialGiftMask = TutorialGiftMask,
                PendingRayCredits=PendingRayCredits,PendingHammerCredits=PendingHammerCredits,PendingMagnetCredits=PendingMagnetCredits
            };
        }
    }

    public sealed class ToolProgressTracker
    {
        public const int WheelSpinCost = 10;
        public const int RayTarget = 4;
        public const int SolventTarget = 4;
        public const int MagnetTarget = 5;

        private readonly ToolProgressData data;

        public ToolProgressTracker(ToolProgressData loaded = null)
        {
            data = loaded?.Copy() ?? new ToolProgressData();
            Normalize();
        }

        public event Action<ToolProgressData> Changed;
        public ToolProgressData Snapshot => data.Copy();
        public int BankedMoves => data.BankedMoves;
        public bool FreeWheelSpin => data.FreeWheelSpin;

        public void BankRemainingMoves(int moves)
        {
            data.BankedMoves = (int)Math.Min(int.MaxValue, (long)data.BankedMoves + Math.Max(0, moves));
            Changed?.Invoke(Snapshot);
        }

        // Cost and reward are committed in the SAME existing inventory save. Closing
        // during the cosmetic spin cannot lose a paid reward or duplicate it.
        public bool TrySpinWheel(int sector)
        {
            if (sector < 0 || sector >= 6 || data.BankedMoves < WheelSpinCost) return false;
            data.BankedMoves -= WheelSpinCost;
            data.FreeWheelSpin = false;
            switch (sector)
            {
                case 0: data.SolventStock++; break;
                case 3: data.SolventStock += 2; break;
                case 1: data.RayStock++; break;
                case 4: data.RayStock += 2; break;
                case 2: data.MagnetStock++; break;
                case 5: break;
            }
            data.LastWheelSector = sector;
            Changed?.Invoke(Snapshot);
            return true;
        }

        public int Pieces(ToolKind kind) => kind switch
        {
            ToolKind.Ray => data.RayPieces,
            ToolKind.Solvent => data.SolventPieces,
            ToolKind.Magnet => data.MagnetPieces,
            _ => 0
        };

        public int Stock(ToolKind kind) => kind switch
        {
            ToolKind.Ray => data.RayStock,
            ToolKind.Solvent => data.SolventStock,
            ToolKind.Magnet => data.MagnetStock,
            _ => 0
        };

        public int Target(ToolKind kind) => kind == ToolKind.Magnet ? MagnetTarget : 4;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void EnsureMinimumStockForTesting(int minimum)
        {
            int amount = Math.Max(0, minimum);
            data.RayStock = Math.Max(data.RayStock, amount);
            data.SolventStock = Math.Max(data.SolventStock, amount);
            data.MagnetStock = Math.Max(data.MagnetStock, amount);
            Changed?.Invoke(Snapshot);
        }

        public void AddTestingStock(int amount)
        {
            amount = Math.Max(0, amount);
            data.RayStock += amount;
            data.SolventStock += amount;
            data.MagnetStock += amount;
            Changed?.Invoke(Snapshot);
        }

#endif

        public void RecordProduced(SpecialKind special)
        {
            switch (special)
            {
                case SpecialKind.RayHorizontal:
                case SpecialKind.RayVertical:
                    data.PendingRayCredits++;data.RayPieces++;
                    ConvertPiecesToStock(ref data.RayPieces, ref data.RayStock, RayTarget);
                    break;
                case SpecialKind.Solvent:
                    data.PendingHammerCredits++;data.SolventPieces++;
                    ConvertPiecesToStock(ref data.SolventPieces, ref data.SolventStock, SolventTarget);
                    break;
                case SpecialKind.Magnet:
                    data.PendingMagnetCredits++;data.MagnetPieces++;
                    ConvertPiecesToStock(ref data.MagnetPieces, ref data.MagnetStock, MagnetTarget);
                    break;
                default:
                    return;
            }
            Changed?.Invoke(Snapshot);
        }

        public void RollbackAttemptCredits()
        {
            if(data.PendingRayCredits==0&&data.PendingHammerCredits==0&&data.PendingMagnetCredits==0)return;
            void Undo(ref int pieces,ref int stock,int count,int target)
            {
                long units=Math.Max(0L,(long)stock*target+pieces-count);
                stock=(int)(units/target);pieces=(int)(units%target);
            }
            Undo(ref data.RayPieces,ref data.RayStock,data.PendingRayCredits,RayTarget);
            Undo(ref data.SolventPieces,ref data.SolventStock,data.PendingHammerCredits,SolventTarget);
            Undo(ref data.MagnetPieces,ref data.MagnetStock,data.PendingMagnetCredits,MagnetTarget);
            CommitAttemptCredits();
        }
        public void CommitAttemptCredits()
        {
            data.PendingRayCredits=data.PendingHammerCredits=data.PendingMagnetCredits=0;
            Changed?.Invoke(Snapshot);
        }

        public bool GrantWheelIntroGiftOnce()
        {
            const int bit=1<<3;
            if((data.TutorialGiftMask&bit)!=0)return false;
            data.TutorialGiftMask|=bit;
            data.BankedMoves=(int)Math.Min(int.MaxValue,(long)data.BankedMoves+WheelSpinCost);
            Changed?.Invoke(Snapshot);return true;
        }

        public bool GrantTutorialGiftOnce(ToolKind kind)
        {
            int bit=1<<(int)kind;
            if((data.TutorialGiftMask&bit)!=0)return false;
            data.TutorialGiftMask|=bit;
            switch(kind){case ToolKind.Ray:data.RayStock++;break;case ToolKind.Solvent:data.SolventStock++;break;case ToolKind.Magnet:data.MagnetStock++;break;}
            Changed?.Invoke(Snapshot);return true;
        }

        public bool TrySpend(ToolKind kind)
        {
            if (Stock(kind) <= 0)
                return false;
            switch (kind)
            {
                case ToolKind.Ray: data.RayStock--; break;
                case ToolKind.Solvent: data.SolventStock--; break;
                case ToolKind.Magnet: data.MagnetStock--; break;
            }
            Changed?.Invoke(Snapshot);
            return true;
        }

        private void Normalize()
        {
            data.Version = 1;
            data.PendingRayCredits=Math.Max(0,data.PendingRayCredits);data.PendingHammerCredits=Math.Max(0,data.PendingHammerCredits);data.PendingMagnetCredits=Math.Max(0,data.PendingMagnetCredits);
            data.RayPieces = Math.Max(0, data.RayPieces);
            data.SolventPieces = Math.Max(0, data.SolventPieces);
            data.MagnetPieces = Math.Max(0, data.MagnetPieces);
            data.RayStock = Math.Max(0, data.RayStock);
            data.SolventStock = Math.Max(0, data.SolventStock);
            data.MagnetStock = Math.Max(0, data.MagnetStock);
            data.BankedMoves = Math.Max(0, data.BankedMoves);
            data.FreeWheelSpin = false; // Retired rule; never grant a free spin from an old save.
            ConvertPiecesToStock(ref data.RayPieces, ref data.RayStock, RayTarget);
            ConvertPiecesToStock(ref data.SolventPieces, ref data.SolventStock, SolventTarget);
            ConvertPiecesToStock(ref data.MagnetPieces, ref data.MagnetStock, MagnetTarget);
        }

        private static void ConvertPiecesToStock(ref int pieces, ref int stock, int target)
        {
            if (pieces < target)
                return;
            stock += pieces / target;
            pieces %= target;
        }
    }

    public static class ToolProgressStore
    {
        private const string SaveKey = "WorkshopMatch.ToolProgress.v1";

        public static ToolProgressData Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            return Deserialize(json);
        }

        public static void Save(ToolProgressData data)
        {
            PlayerPrefs.SetString(SaveKey, Serialize(data));
            PlayerPrefs.Save();
        }

        internal static string Serialize(ToolProgressData data) => JsonUtility.ToJson(data ?? new ToolProgressData());

        internal static ToolProgressData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ToolProgressData();
            try
            {
                ToolProgressData loaded = JsonUtility.FromJson<ToolProgressData>(json);
                return loaded ?? new ToolProgressData();
            }
            catch (ArgumentException)
            {
                return new ToolProgressData();
            }
        }
    }
}
