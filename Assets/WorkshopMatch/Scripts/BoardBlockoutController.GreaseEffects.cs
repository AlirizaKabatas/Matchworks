using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private AudioClip greaseSquelch, frozenSquelch;
        private bool[,] visibleImpactOil;

        private System.Action<float> CreatePowerImpactUpdater(Match3BoardModel.ResolutionStep step)
        {
            if(!step.Activation.HasValue)return null;
            var pending=new List<(Match3BoardModel.Cell cell,bool frozen,bool oil,bool bolt,float delay)>();
            for(int y=0;y<boardHeight;y++)for(int x=0;x<boardWidth;x++)
            {
                bool frozen=step.BeforeClear.GetContent(x,y)==CellContent.FrozenGrease && step.AfterFall.GetContent(x,y)!=CellContent.FrozenGrease;
                bool oil=step.BeforeClear.HasOil(x,y) && !step.AfterFall.HasOil(x,y);
                bool bolt=step.BeforeClear.GetContent(x,y)==CellContent.Bolt && step.AfterFall.GetContent(x,y)==CellContent.Bolt &&
                    step.BeforeClear.GetObstacleState(x,y)!=step.AfterFall.GetObstacleState(x,y);
                if(!frozen&&!oil&&!bolt)continue;
                var cell=new Match3BoardModel.Cell(x,y);
                pending.Add((cell,frozen,oil,bolt,PowerContactDelay(step.Activation.Value,cell)));
            }
            bool sounded=false;
            return elapsed=>
            {
                bool oilChanged=false;
                for(int i=pending.Count-1;i>=0;i--)
                {
                    var hit=pending[i];if(elapsed<hit.delay)continue;
                    pending.RemoveAt(i);
                    var view=views[hit.cell];
                    if(hit.frozen)
                    {
                        if(view.Model!=null)view.Model.SetActive(false);
                        view.BaseRenderer.enabled=false;view.Marker.SetActive(false);
                        if(view.TileRenderer!=null)view.TileRenderer.sharedMaterial=cellNormalMaterial??frameMaterial;
                    }
                    if(hit.bolt)
                        UpdateCellModel(view,hit.cell,CellContent.Bolt,-1,SpecialKind.None,step.AfterFall.GetObstacleState(hit.cell.X,hit.cell.Y));
                    if(hit.oil)
                    {
                        view.OilTile.SetActive(false);
                        if(visibleImpactOil!=null)visibleImpactOil[hit.cell.X,hit.cell.Y]=false;
                        oilChanged=true;
                    }
                    if(hit.frozen||hit.oil)
                    {
                        if(!sounded){PlayGreaseSquelch(hit.frozen);sounded=true;}
                        StartCoroutine(AnimateProductionBurst(view.HomePosition+Vector3.back*1.25f,
                            hit.frozen?uiCatalog?.FrozenOilShards:uiCatalog?.OilSplash,CellPitch*.85f));
                    }
                }
                if(oilChanged)RefreshJoinedOil(step.BeforeClear,visibleImpactOil);
            };
        }

        private IEnumerator AnimateGreaseRemoval(Match3BoardModel.ResolutionStep step)
        {
            // Power hits already updated at their travelling contact/hammer impact.
            if(step.Activation.HasValue)yield break;
            var cleared = new List<(Vector3 point, bool frozen)>();
            bool hasFrozen = false;
            for (int y=0; y<boardHeight; y++) for (int x=0; x<boardWidth; x++)
            {
                bool frozen = step.BeforeClear.GetContent(x,y) == CellContent.FrozenGrease &&
                    step.AfterFall.GetContent(x,y) != CellContent.FrozenGrease;
                bool oil = step.BeforeClear.HasOil(x,y) && !step.AfterFall.HasOil(x,y);
                if (!frozen && !oil) continue;
                var view = views[new Match3BoardModel.Cell(x,y)];
                cleared.Add((view.HomePosition + Vector3.back*1.25f, frozen));
                hasFrozen |= frozen;
                if (frozen)
                {
                    if (view.Model != null) view.Model.SetActive(false);
                    view.BaseRenderer.enabled = false;
                    view.Marker.SetActive(false);
                }
                if (oil) view.OilTile.SetActive(false);
            }
            if (cleared.Count == 0) yield break;
            RefreshJoinedOil(step.AfterFall);
            PlayGreaseSquelch(hasFrozen);
            foreach (var clearedCell in cleared)
                StartCoroutine(AnimateProductionBurst(clearedCell.point,
                    clearedCell.frozen ? uiCatalog?.FrozenOilShards : uiCatalog?.OilSplash, CellPitch * .85f));
            GameObject root = EffectRoot("Grease droplets");
            int count = cleared.Count*3;
            var particles = new Renderer[count];
            var velocities = new Vector3[count];
            var colors = new Color[count];
            for (int i=0; i<count; i++)
            {
                var source = cleared[i/3];
                particles[i] = EffectQuad(root.transform, Texture2D.whiteTexture, new Rect(0,0,1,1), new Vector2(.11f,.15f));
                particles[i].transform.localPosition = source.point;
                var properties = new MaterialPropertyBlock();
                particles[i].GetPropertyBlock(properties);
                properties.SetFloat("_CornerRadius", source.frozen ? .2f : .5f);
                particles[i].SetPropertyBlock(properties);
                float angle = (i%3)*Mathf.PI*2/3 + (i/3)*.31f;
                velocities[i] = new Vector3(Mathf.Cos(angle)*.43f, Mathf.Sin(angle)*.32f+.2f,0);
                colors[i] = source.frozen ? new Color(1,.78f,.36f) : new Color(.83f,.56f,.19f);
                EffectTint(particles[i],colors[i]);
            }
            const float duration = .26f;
            for (float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime)
            {
                float t=elapsed/duration;
                for (int i=0; i<count; i++)
                {
                    particles[i].transform.localPosition = cleared[i/3].point+velocities[i]*t+Vector3.down*t*t*.23f;
                    particles[i].transform.localRotation = Quaternion.Euler(0,0,t*(i%2==0?80:-80));
                    particles[i].transform.localScale = new Vector3(.11f,.15f,1)*(1-t*.8f);
                    Color color=colors[i]; color.a=1-t;
                    EffectTint(particles[i],color);
                }
                yield return null;
            }
            RemoveEffect(root);
        }

        private void PlayGreaseSquelch(bool frozen)
        {
            AudioClip produced = frozen ? uiCatalog?.FrozenOilCrack : uiCatalog?.OilSquish;
            if (produced != null) { PlayProductionSound(produced); return; }
            if (uiAudio==null) { uiAudio=gameObject.AddComponent<AudioSource>(); uiAudio.playOnAwake=false; uiAudio.spatialBlend=0; }
            AudioClip clip = frozen ? frozenSquelch : greaseSquelch;
            if (clip==null)
            {
                const int rate=22050;
                float duration=frozen?.19f:.23f;
                var samples=new float[Mathf.RoundToInt(rate*duration)];
                var noise=new System.Random(frozen?73:29);
                float phase=0, filtered=0;
                for (int i=0; i<samples.Length; i++)
                {
                    float t=i/(float)samples.Length;
                    float hz=Mathf.Lerp(frozen?1550:950,130,t*t)*(1+.12f*Mathf.Sin(t*28));
                    phase+=2*Mathf.PI*hz/rate;
                    filtered=Mathf.Lerp(filtered,(float)noise.NextDouble()*2-1,.2f);
                    float envelope=Mathf.Sin(Mathf.PI*t)*Mathf.Exp(-t*2.5f);
                    samples[i]=(Mathf.Sin(phase)*.10f+filtered*(frozen?.17f:.10f))*envelope;
                }
                clip=AudioClip.Create(frozen?"Frozen grease pop":"Grease squish",samples.Length,1,rate,false);
                clip.SetData(samples,0);
                if(frozen)frozenSquelch=clip;else greaseSquelch=clip;
            }
            uiAudio.PlayOneShot(clip);
        }
    }
}
