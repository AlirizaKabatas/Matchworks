namespace WorkshopMatch
{
    internal static class TrainingLevelPolicy
    {
        internal static int LevelNumber(string id)=>id!=null&&id.StartsWith("level_")&&int.TryParse(id.Substring(6),out int number)?number:20;
        internal static bool ToolsLocked(string id,bool previewEnabled,int unlockedLevel=0)=>previewEnabled&&System.Math.Max(LevelNumber(id),unlockedLevel)<4;
        internal static bool ToolLocked(string id,bool previewEnabled,ToolKind kind,int unlockedLevel=0)=>previewEnabled&&System.Math.Max(LevelNumber(id),unlockedLevel)<(kind==ToolKind.Ray?4:kind==ToolKind.Solvent?6:9);
        internal static void RecordProduced(string id,bool previewEnabled,ToolProgressTracker progress,SpecialKind special,int unlockedLevel=0)
        {
            ToolKind kind=special==SpecialKind.Magnet?ToolKind.Magnet:special==SpecialKind.Solvent?ToolKind.Solvent:ToolKind.Ray;
            if(!ToolLocked(id,previewEnabled,kind,unlockedLevel))progress?.RecordProduced(special);
        }
    }
}
