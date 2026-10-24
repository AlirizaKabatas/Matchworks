using UnityEngine;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private readonly float[] toolLockAt={-100f,-100f,-100f};
        private AudioClip lockedToolSound;
        private bool LockedToolbarPointer(Vector2 point)
        {
            for(int i=0;i<3;i++)if(IsTrainingToolLocked((ToolKind)i)&&tutorialToolRects[i].Contains(point))return true;
            return false;
        }
        private void TapLockedTool(int index)
        {
            if(Time.unscaledTime-toolLockAt[index]<.22f)return;
            toolLockAt[index]=Time.unscaledTime;
            if(lockedToolSound==null)
            {
                const int rate=22050;const float duration=.19f;
                var samples=new float[Mathf.RoundToInt(rate*duration)];
                for(int i=0;i<samples.Length;i++)
                {
                    float t=(float)i/rate;
                    float envelope=Mathf.Sin(Mathf.PI*t/duration)*Mathf.Exp(-t*7);
                    float buzz=Mathf.Sin(t*2*Mathf.PI*155)+.32f*Mathf.Sin(t*2*Mathf.PI*310);
                    samples[i]=buzz*envelope*.18f;
                }
                lockedToolSound=AudioClip.Create("Locked tool rattle",samples.Length,1,rate,false);lockedToolSound.SetData(samples,0);
            }
            PlayProductionSound(lockedToolSound);
        }
        private void DrawToolLock(Rect well,float size,int index)
        {
            float elapsed=Time.unscaledTime-toolLockAt[index];
            float envelope=elapsed<.36f?1-elapsed/.36f:0;
            Rect art=new Rect(well.center.x-size*.23f+Mathf.Sin(elapsed*65)*size*.035f*envelope,well.center.y-size*.12f,size*.46f,size*.46f);
            Matrix4x4 matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Sin(elapsed*58)*12*envelope,art.center);
            DrawArt(art,uiCatalog?.LockIcon);GUI.matrix=matrix;
        }
    }
}
