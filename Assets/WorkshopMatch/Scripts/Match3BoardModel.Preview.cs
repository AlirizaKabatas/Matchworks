namespace WorkshopMatch
{
    public sealed partial class Match3BoardModel
    {
        // Exact isolated simulation: same RNG and state, no stock/progress callbacks.
        public Match3BoardModel CreatePreviewCopy() => new Match3BoardModel(this);
        private Match3BoardModel(Match3BoardModel source)
        {
            Width=source.Width; Height=source.Height; colorCount=source.colorCount;
            appearanceSeed=source.appearanceSeed;partVariants=(int[,])source.partVariants.Clone();
            colors=(int[,])source.colors.Clone(); contents=(CellContent[,])source.contents.Clone();
            oil=(bool[,])source.oil.Clone(); obstacleState=(int[,])source.obstacleState.Clone();
            specials=(SpecialKind[,])source.specials.Clone(); randomState=source.randomState;
            MovesRemaining=source.MovesRemaining; MoveGoal=source.MoveGoal; ValidMovesMade=source.ValidMovesMade; State=source.State; HasMotor=source.HasMotor;
            totalBolts=source.totalBolts; collectedBolts=source.collectedBolts; totalOil=source.totalOil; totalFrozenOil=source.totalFrozenOil;
            motorHits=source.motorHits; motorHitsRequired=source.motorHitsRequired;
            objectivesSealed=source.objectivesSealed; motorActivated=source.motorActivated;
            producedRays=source.producedRays; producedSolvents=source.producedSolvents; producedMagnets=source.producedMagnets;
            CopyMotorGroups(source);
            onSpecialProduced=null;
        }
    }
}
