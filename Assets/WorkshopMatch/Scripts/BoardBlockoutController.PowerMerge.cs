using System.Collections;
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private Match3BoardModel.ResolutionStep mergedPowerStep;
        private Match3BoardModel.Cell mergedPowerSource;
        private GameObject powerMergeVisual;

        private bool BeginPowerMerge(Match3BoardModel.Cell from, Match3BoardModel.Cell to,
            Match3BoardModel.SwapResult result)
        {
            mergedPowerStep=null;
            if(!result.Accepted||result.Steps.Count==0)return false;
            var step=result.Steps[0];
            if(!step.Activation.HasValue||step.Activation.Value.Shape==Match3BoardModel.PowerShape.Normal||
                !step.Activation.Value.Cell.Equals(to)||
                step.BeforeClear.GetSpecial(from.X,from.Y)==SpecialKind.None||
                step.BeforeClear.GetSpecial(to.X,to.Y)==SpecialKind.None)return false;
            mergedPowerStep=step;mergedPowerSource=from;
            return true;
        }

        private static void HideMergedCell(CellView view)
        {
            if(view.Model!=null)view.Model.SetActive(false);
            view.BaseRenderer.enabled=false;
            view.Marker.SetActive(false);
        }

        private IEnumerator AnimatePowerMerge(Match3BoardModel.Cell from,Match3BoardModel.Cell to)
        {
            var held=views[from];var destination=views[to];
            powerMergeVisual=EffectRoot("Tools merge at destination");
            Transform Copy(CellView view)
            {
                if(view.Model==null)return null;
                var copy=Instantiate(view.Model,powerMergeVisual.transform,true);
                copy.SetActive(true);
                foreach(var collider in copy.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                return copy.transform;
            }
            Transform front=Copy(held),back=Copy(destination);
            Vector3 frontScale=front!=null?front.localScale:Vector3.one;
            Vector3 backScale=back!=null?back.localScale:Vector3.one;
            HideMergedCell(held);HideMergedCell(destination);
            Vector3 target=destination.HomePosition;
            const float travel=.18f,absorb=.12f;
            for(float elapsed=0;elapsed<travel+absorb;elapsed+=Time.unscaledDeltaTime)
            {
                float move=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/travel));
                float merge=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-travel)/absorb));
                if(front!=null)
                {
                    front.localPosition=Vector3.Lerp(held.HomePosition,target,move)+Vector3.back*1.25f;
                    front.localScale=frontScale*(1+.10f*Mathf.Sin(merge*Mathf.PI));
                }
                if(back!=null)
                {
                    back.localPosition=target+Vector3.back*.85f;
                    back.localScale=backScale*Mathf.Lerp(1,.01f,merge);
                }
                yield return null;
            }
            if(front!=null){front.localPosition=target+Vector3.back*1.25f;front.localScale=frontScale;}
            if(back!=null)back.gameObject.SetActive(false);
            // Keep the foreground copy until the first effect takes ownership. A
            // snapshot refresh must not resurrect the partner at the old source.
        }

        private bool PrepareMergedPower(Match3BoardModel.ResolutionStep step)
        {
            if(!ReferenceEquals(step,mergedPowerStep))return false;
            HideMergedCell(views[mergedPowerSource]);
            var power=step.Activation.Value;
            var destination=views[power.Cell];
            UpdateCellModel(destination,power.Cell,CellContent.Candy,0,power.Kind);
            destination.BaseRenderer.enabled=false;destination.Marker.SetActive(false);
            if(destination.Model!=null)destination.Model.SetActive(true);
            RemoveEffect(powerMergeVisual);powerMergeVisual=null;mergedPowerStep=null;
            return true;
        }

        private IEnumerator AnimateMergedMagnetRelease(CellView view)
        {
            // Combination magnets release from one fixed center; do not spin the
            // two original tools away independently after conversion has started.
            var visual=view.Model.transform;
            Vector3 scale=visual.localScale;
            for(float elapsed=0;elapsed<.14f;elapsed+=Time.unscaledDeltaTime)
            {
                if(visual==null)yield break;
                float t=Mathf.Clamp01(elapsed/.14f);
                visual.localScale=scale*Mathf.Lerp(1,.01f,t*t);
                yield return null;
            }
            if(visual!=null){visual.localScale=scale;visual.gameObject.SetActive(false);}
        }
    }
}
