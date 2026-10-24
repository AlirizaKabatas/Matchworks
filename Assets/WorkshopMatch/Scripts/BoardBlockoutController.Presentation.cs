using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private readonly Dictionary<Material, Material> toonMaterials = new();
        private AudioClip uiTick, candyDing, conversionAppear;
        private Texture2D rocketIcon;
        private GameObject rocketTemplate;
        private readonly List<GameObject> transientEffects = new();
        private void ClearTransientEffects()
        {
            chainShakeAt=-10f;
            mergedPowerStep=null;powerMergeVisual=null;
            foreach(GameObject effect in transientEffects) if(effect != null) Destroy(effect);
            transientEffects.Clear();
        }

        private void DrawFireworkIcon(Rect rect)
        {
            if (rocketIcon == null)
            {
                rocketIcon = new Texture2D(64,128,TextureFormat.RGBA32,false) { name="Firework icon", filterMode=FilterMode.Bilinear };
                Color[] pixels = new Color[64*128];
                for (int y=0;y<128;y++) for (int x=0;x<64;x++)
                {
                    float dx=Mathf.Abs(x-31.5f); Color c=Color.clear;
                    if (y<32 && dx<3) c=new Color(0.77f,0.43f,0.17f);
                    if (y>=27 && y<92 && dx<19) c=new Color(0.98f,0.22f,0.2f);
                    if (y>=32 && y<40 && dx<20) c=new Color(1f,0.81f,0.32f);
                    if (y>=88 && dx<(125-y)*0.81f) c=new Color(1f,0.78f,0.25f);
                    if (c.a>0) c*=new Color(0.78f+0.22f*Mathf.Cos((x-24)*0.035f),0.86f+0.14f*Mathf.Cos((x-24)*0.035f),1,1);
                    pixels[y*64+x]=c;
                }
                rocketIcon.SetPixels(pixels);rocketIcon.Apply();
            }
            GUI.DrawTexture(rect,rocketIcon,ScaleMode.ScaleToFit,true);
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void ApplyToon(Renderer[] renderers)
        {
            Shader shader = Shader.Find("WorkshopMatch/CandyToon");
            if (shader == null) return;
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.shader == shader) continue;
                    if (!toonMaterials.TryGetValue(source, out Material toon))
                    {
                        Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
                            source.HasProperty("_Color") ? source.color : Color.white;
                        toon = new Material(shader) { name = source.name + " Toon Glaze" };
                        toon.SetColor("_BaseColor", color);
                        toonMaterials[source] = toon;
                        generatedMaterials.Add(toon);
                    }
                    materials[i] = toon;
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void PlaySweetTone(bool candy)
        {
            AudioClip produced = candy ? uiCatalog?.CandyPopSound : uiCatalog?.UiClick;
            if (produced != null) { PlayProductionSound(produced, candy ? .78f : .72f); return; }
            if (uiAudio == null) { uiAudio = gameObject.AddComponent<AudioSource>(); uiAudio.playOnAwake = false; uiAudio.spatialBlend = 0; }
            AudioClip clip = candy ? candyDing : uiTick;
            if (clip == null)
            {
                const int rate = 22050;
                float duration = candy ? 0.20f : 0.075f;
                float[] data = new float[Mathf.RoundToInt(rate * duration)];
                float frequency = candy ? 380f : 260f;
                for (int i = 0; i < data.Length; i++)
                {
                    float t = i / (float)rate;
                    float envelope = Mathf.Min(1f, t * 1200f) * Mathf.Exp(-t * (candy ? 24f : 60f));
                    data[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2) + 0.22f * Mathf.Sin(t * frequency * Mathf.PI * 4)) * envelope * 0.14f;
                }
                clip = AudioClip.Create(candy ? "Temporary candy ding" : "Temporary UI tick", data.Length, 1, rate, false);
                clip.SetData(data, 0);
                if (candy) candyDing = clip; else uiTick = clip;
            }
            uiAudio.PlayOneShot(clip);
        }

        private IEnumerator AnimatePowerFeedback(Match3BoardModel.ResolutionStep step, System.Action onImpact = null)
        {
            PrepareMergedPower(step);
            // Present the captured conversion result one cell at a time before any queued power fires.
            // Stagger the shrink/appear phases, allowing nearby conversions to overlap.
            if(step.ConvertedCells.Count>0)
            {
                int count=step.ConvertedCells.Count;
                var changed=new bool[count];var linked=new bool[count];
                var scales=new Vector3[count];
                for(int i=0;i<count;i++)scales[i]=views[step.ConvertedCells[i]].Root.localScale;
                float duration=.30f+(count-1)*.045f;
                for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
                {
                    for(int i=0;i<count;i++)
                    {
                        float phase=elapsed-i*.045f;if(phase<0)continue;
                        var cell=step.ConvertedCells[i];var converted=views[cell];
                        if(!linked[i])
                        {
                            linked[i]=true;
                            if(step.Activation?.Shape==Match3BoardModel.PowerShape.Conversion)
                                StartCoroutine(AnimateConversionLink(step.Activation.Value.Cell,cell));
                        }
                        if(phase<.12f)converted.Root.localScale=scales[i]*Mathf.Lerp(1,.08f,Mathf.SmoothStep(0,1,phase/.12f));
                        else
                        {
                            if(!changed[i])
                            {
                                changed[i]=true;
                                UpdateCellModel(converted,cell,CellContent.Candy,step.AfterFall.GetColor(cell.X,cell.Y),step.AfterFall.GetSpecial(cell.X,cell.Y));
                                PlayConversionAppear(i);
                            }
                            float t=Mathf.Clamp01((phase-.12f)/.18f);
                            converted.Root.localScale=scales[i]*(Mathf.Lerp(.08f,1,t)+Mathf.Sin(t*Mathf.PI)*.10f);
                        }
                    }
                    yield return null;
                }
                for(int i=0;i<count;i++)views[step.ConvertedCells[i]].Root.localScale=scales[i];
            }
            if (step.MatchedCells.Count > 0) PlaySweetTone(true);
            if (!step.Activation.HasValue) yield break;
            Match3BoardModel.PowerEvent power=step.Activation.Value;
            if(power.Shape==Match3BoardModel.PowerShape.LargeHammer)
            {
                yield return AnimateCombinedHammerStrike(power,onImpact);
                yield break;
            }
            Texture2D productionEffect = power.Kind switch
            {
                SpecialKind.Magnet => uiCatalog?.MagnetGlint,
                SpecialKind.Solvent => uiCatalog?.HammerImpact,
                SpecialKind.RayHorizontal or SpecialKind.RayVertical => null,
                _ => null
            };
            if (views.TryGetValue(power.Cell, out CellView effectView))
                StartCoroutine(AnimateProductionBurst(effectView.Root.localPosition, productionEffect, CellPitch * 1.5f));
            if (power.Kind == SpecialKind.Magnet) PlayProductionSound(uiCatalog?.MagnetPull);
            else if (power.Kind is SpecialKind.RayHorizontal or SpecialKind.RayVertical)
                PlayProductionSound(uiCatalog?.RocketIgnite);
            if (power.Shape == Match3BoardModel.PowerShape.Conversion)
                PlayProductionSound(uiCatalog?.MagnetClose);
            if (!views.TryGetValue(power.Cell, out CellView view)) yield break;
            // A queued power can already have been cleared by the preceding explosion.
            // Recreate its own visual at its saved cell, never silently skip the activation.
            UpdateCellModel(view,power.Cell,CellContent.Candy,0,power.Kind);
            if (view.Model == null) yield break;
            view.Model.SetActive(true);view.BaseRenderer.enabled=false;
            if (power.Shape == Match3BoardModel.PowerShape.WideRay)
            {
                view.Model.SetActive(false);
                yield return AnimateHammerRocketLaunch(power);
            }
            if (power.Shape == Match3BoardModel.PowerShape.Normal)
            {
                if (power.Kind == SpecialKind.Solvent)
                {
                    yield return AnimateHammerStrike(view,onImpact);
                    yield break;
                }
                if (power.Kind is SpecialKind.RayHorizontal or SpecialKind.RayVertical)
                {
                    yield return AnimateRocketPreparation(view);
                    yield break;
                }
                if (power.Kind == SpecialKind.Magnet)
                {
                    yield return AnimateMagnetLinks(step);
                    yield return AnimateMagnetOrbit(step);
                }
            }
            if(power.Kind==SpecialKind.Magnet&&power.Shape!=Match3BoardModel.PowerShape.Normal)
            {
                yield return AnimateMergedMagnetRelease(view);
                yield break;
            }
            if (power.Kind != SpecialKind.Magnet)
            {
                yield return new WaitForSecondsRealtime(0.075f);
                yield break;
            }
            Transform visual = view.Model.transform;
            Vector3 origin = visual.localPosition, scale = visual.localScale;
            Quaternion rotation = visual.localRotation;
            for (float elapsed = 0; elapsed < 0.36f; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / 0.36f);
                visual.localPosition = origin + Vector3.back * t * 1.6f;
                visual.localRotation = rotation * Quaternion.Euler(0, 0, t * 540f);
                visual.localScale = scale * (t < 0.7f ? 1f + t * 0.5f : Mathf.Lerp(1.35f, 0.01f, (t - 0.7f) / 0.3f));
                yield return null;
            }
            visual.localPosition = origin; visual.localRotation = rotation; visual.localScale = scale;
            visual.gameObject.SetActive(false);
            GameObject puff=new("Magnet puff");puff.transform.SetParent(runtimeBoardRoot,false);
            puff.transform.localPosition=view.Root.localPosition+Vector3.back*0.6f;
            transientEffects.Add(puff);
            for(int i=0;i<7;i++)
            {
                GameObject dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.transform.SetParent(puff.transform,false);dot.GetComponent<Collider>().enabled=false;
                dot.GetComponent<Renderer>().sharedMaterial=magnetMaterial;
                dot.transform.localPosition=new Vector3(Mathf.Cos(i*Mathf.PI*2/7),Mathf.Sin(i*Mathf.PI*2/7),0)*0.16f;
                dot.transform.localScale=Vector3.one*0.11f;
            }
            for(float t=0;t<0.16f;t+=Time.unscaledDeltaTime)
            {
                float progress=t/0.16f;
                for(int i=0;i<puff.transform.childCount;i++)
                {
                    Transform dot=puff.transform.GetChild(i);
                    dot.localPosition=dot.localPosition.normalized*Mathf.Lerp(0.16f,0.6f,progress);
                    dot.localScale=Vector3.one*0.11f*(1-progress);
                }
                yield return null;
            }
            transientEffects.Remove(puff);Destroy(puff);
        }

        private void PlayConversionAppear(int index)
        {
            if(conversionAppear==null)
            {
                const int rate=24000;
                var samples=new float[2400];
                for(int i=0;i<samples.Length;i++)
                {
                    float t=i/(float)rate, u=i/(float)(samples.Length-1);
                    float envelope=Mathf.Sin(Mathf.PI*u);envelope*=envelope;
                    float phase=2*Mathf.PI*(520*t+2100*t*t);
                    samples[i]=.15f*envelope*(Mathf.Sin(phase)+.18f*Mathf.Sin(phase*2));
                }
                conversionAppear=AudioClip.Create("Magnet tool appear",samples.Length,1,rate,false);
                conversionAppear.SetData(samples,0);
            }
            PlayProductionSound(conversionAppear,.92f+Mathf.Min(index,9)*.025f);
        }

        // A firework silhouette, not the old rail cylinder. Root axis is +Y.
        private GameObject CreateRocket(Transform parent)
        {
            if (rocketTemplate != null)
            {
                GameObject copy=Instantiate(rocketTemplate,parent);copy.SetActive(true);return copy;
            }
            GameObject rocket = new("Firework Rocket");
            rocket.transform.SetParent(parent, false);
            Material MaterialFor(Color color)
            {
                Material material = new(Shader.Find("WorkshopMatch/CandyToon"));
                material.SetColor("_BaseColor", color); generatedMaterials.Add(material); return material;
            }
            Material red = MaterialFor(new Color(0.98f, 0.23f, 0.22f));
            Material gold = MaterialFor(new Color(1f, 0.75f, 0.22f));
            void Part(PrimitiveType type, Vector3 position, Vector3 scale, Material material)
            {
                GameObject part = GameObject.CreatePrimitive(type); part.transform.SetParent(rocket.transform, false);
                part.transform.localPosition = position; part.transform.localScale = scale;
                part.GetComponent<Collider>().enabled = false; part.GetComponent<Renderer>().sharedMaterial = material;
            }
            Part(PrimitiveType.Capsule, Vector3.zero, new Vector3(0.46f, 0.32f, 0.46f), red);
            Part(PrimitiveType.Cylinder, new Vector3(0,-0.20f,0), new Vector3(0.48f,0.065f,0.48f), gold);
            Part(PrimitiveType.Cube, new Vector3(0,-0.51f,0), new Vector3(0.07f,0.36f,0.07f), gold);
            Mesh cone = new(); List<Vector3> vertices = new(); List<int> triangles = new();
            for (int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8, b=(i+1)*Mathf.PI/8;
                int n=vertices.Count; vertices.Add(new Vector3(0,0.68f,0));
                vertices.Add(new Vector3(Mathf.Cos(a)*0.34f,0.24f,Mathf.Sin(a)*0.34f));
                vertices.Add(new Vector3(Mathf.Cos(b)*0.34f,0.24f,Mathf.Sin(b)*0.34f));
                triangles.Add(n); triangles.Add(n+2); triangles.Add(n+1);
            }
            cone.SetVertices(vertices); cone.SetTriangles(triangles,0); cone.RecalculateNormals();
            GameObject tip=new("Pointed cap"); tip.transform.SetParent(rocket.transform,false);
            tip.AddComponent<MeshFilter>().sharedMesh=cone; tip.AddComponent<MeshRenderer>().sharedMaterial=gold;
            rocketTemplate=Instantiate(rocket,transform);rocketTemplate.name="Firework template";rocketTemplate.SetActive(false);
            return rocket;
        }
    }
}
