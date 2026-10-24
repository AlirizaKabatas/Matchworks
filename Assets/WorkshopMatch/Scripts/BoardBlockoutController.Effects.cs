using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private Material effectMaterial;
        private static MaterialPropertyBlock effectTintBlock;
        private readonly float[] toolRewardUntil = new float[3];
        private readonly int[] toolRewardAmount = new int[3];

        private Material EffectMaterial()
        {
            if (effectMaterial != null) return effectMaterial;
            effectMaterial = new Material(Shader.Find("WorkshopMatch/FlatPiece"))
            { name = "Runtime animation tint", mainTexture = Texture2D.whiteTexture };
            generatedMaterials.Add(effectMaterial);
            return effectMaterial;
        }

        private GameObject EffectRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(runtimeBoardRoot, false);
            transientEffects.Add(root);
            return root;
        }

        private void RemoveEffect(GameObject effect)
        {
            if (effect == null) return;
            effect.SetActive(false);
            transientEffects.Remove(effect);
            Destroy(effect);
        }

        private static void EffectTint(Renderer renderer, Color color)
        {
            var properties = effectTintBlock ??= new MaterialPropertyBlock();
            properties.Clear();
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }

        private Renderer EffectQuad(Transform parent, Texture2D texture, Rect uv, Vector2 size)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite fragment";
            quad.GetComponent<Collider>().enabled = false;
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = EffectMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", texture);
            properties.SetVector("_MainTex_ST", new Vector4(uv.width, uv.height, uv.x, uv.y));
            properties.SetColor("_BaseColor", Color.white);
            renderer.SetPropertyBlock(properties);
            return renderer;
        }

        private IEnumerator AnimateCleanBoltReveal(GameObject token)
        {
            // The clean silver sprite must read at its original cell before flight.
            GameObject shine=EffectRoot("M6 clean reveal sparkles");
            Vector3 center=CombinedBounds(token.GetComponentsInChildren<Renderer>(true)).center;
            shine.transform.localPosition=runtimeBoardRoot.InverseTransformPoint(center)+new Vector3(0,0,-.18f);
            Renderer[] rays=new Renderer[8];
            for(int i=0;i<4;i++)
            {
                float angle=(45f+i*90f)*Mathf.Deg2Rad;
                Vector3 position=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*CellPitch*.43f;
                for(int axis=0;axis<2;axis++)
                {
                    Renderer ray=EffectQuad(shine.transform,Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one);
                    ray.transform.localPosition=position;
                    rays[i*2+axis]=ray;
                }
            }
            Vector3 original=token.transform.localScale;
            const float duration=.48f;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/duration);
                token.transform.localScale=original*(1f+.08f*Mathf.Sin(t*Mathf.PI));
                for(int i=0;i<4;i++)
                {
                    float pulse=Mathf.Sin(Mathf.Clamp01((t-i*.045f)/.75f)*Mathf.PI);
                    for(int axis=0;axis<2;axis++)
                    {
                        Renderer ray=rays[i*2+axis];
                        ray.transform.localScale=new Vector3(CellPitch*(axis==0?.20f:.035f)*pulse,
                            CellPitch*(axis==0?.035f:.20f)*pulse,1);
                        EffectTint(ray,new Color(1f,.94f,.65f,pulse));
                    }
                }
                yield return null;
            }
            token.transform.localScale=original;
            RemoveEffect(shine);
        }

        private IEnumerator AnimateProductionBurst(Vector3 position, Texture2D texture, float size, float duration = .28f)
        {
            if (texture == null || runtimeBoardRoot == null) yield break;
            GameObject root = EffectRoot("Production VFX " + texture.name);
            root.transform.localPosition = position + new Vector3(0, 0, -1.6f);
            Renderer sprite = EffectQuad(root.transform, texture, new Rect(0, 0, 1, 1), Vector2.one);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                sprite.transform.localScale = Vector3.one * (size * Mathf.Lerp(.35f, 1.15f, t));
                EffectTint(sprite, new Color(1f, 1f, 1f, 1f - t));
                yield return null;
            }
            RemoveEffect(root);
        }

        private LineRenderer EffectLine(Transform parent, Color color, float width, int points = 2)
        {
            var line = new GameObject("Energy stroke").AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false;
            line.sharedMaterial = EffectMaterial();
            line.positionCount = points;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            EffectTint(line, color);
            return line;
        }

        private IEnumerator AnimateCombinedHammerStrike(Match3BoardModel.PowerEvent power, System.Action onImpact = null)
        {
            if(!views.TryGetValue(power.Cell,out CellView destination))yield break;
            Vector3 center=destination.Root.localPosition;
            void HideHammer(CellView cell)
            {
                if(cell.Model!=null)cell.Model.SetActive(false);
                if(cell.Marker!=null)cell.Marker.SetActive(false);
                cell.BaseRenderer.enabled=false;
            }
            HideHammer(destination);
            if(power.Secondary.HasValue&&views.TryGetValue(power.Secondary.Value,out CellView source))HideHammer(source);
            Texture2D texture=PieceTexture("hammer");
            if(texture==null)yield break;
            GameObject root=EffectRoot("Combined giant hammer");
            float height=CellPitch*3.8f;
            Renderer hammer=EffectQuad(root.transform,texture,new Rect(0,0,1,1),new Vector2(height,height));
            // Pivot around the hammer head, so the head lands precisely on the destination cell.
            hammer.transform.localPosition=new Vector3(0,-height*.28f,0);
            const float windup=.32f,strike=.18f;
            for(float elapsed=0;elapsed<windup+strike;elapsed+=Time.unscaledDeltaTime)
            {
                if(root==null)yield break;
                float lift=elapsed<windup?Mathf.SmoothStep(0,1,elapsed/windup):
                    1-Mathf.Pow(Mathf.Clamp01((elapsed-windup)/strike),3);
                root.transform.localPosition=center+new Vector3(-CellPitch*.6f*lift,CellPitch*1.8f*lift,-3f);
                root.transform.localRotation=Quaternion.Euler(0,0,55*lift);
                root.transform.localScale=Vector3.one*(elapsed<windup?Mathf.Lerp(.65f,1,elapsed/windup):1);
                yield return null;
            }
            root.transform.localPosition=center+new Vector3(0,0,-3f);
            root.transform.localRotation=Quaternion.identity;root.transform.localScale=Vector3.one;
            onImpact?.Invoke();
            if(uiCatalog?.HammerHit!=null)PlayProductionSound(uiCatalog.HammerHit);else PlayMachineImpact();
            StartCoroutine(AnimateProductionBurst(center,uiCatalog?.HammerImpact,CellPitch*3.5f));
            StartCoroutine(AnimateImpactRing(center,CellPitch*2.5f));
            yield return new WaitForSecondsRealtime(.08f);
            StartCoroutine(FadeCombinedHammer(root,hammer));
        }
        private IEnumerator FadeCombinedHammer(GameObject root,Renderer hammer)
        {
            for(float elapsed=0;elapsed<.18f;elapsed+=Time.unscaledDeltaTime)
            {
                if(root==null||hammer==null)yield break;
                EffectTint(hammer,new Color(1,1,1,1-elapsed/.18f));yield return null;
            }
            RemoveEffect(root);
        }

        private IEnumerator AnimateHammerStrike(CellView view, System.Action onImpact = null)
        {
            Transform hammer = view.Model.transform;
            Vector3 position = hammer.localPosition, scale = hammer.localScale;
            Quaternion rotation = hammer.localRotation;
            const float windup = .18f, strike = .10f;
            for (float elapsed = 0; elapsed < windup + strike; elapsed += Time.unscaledDeltaTime)
            {
                if (hammer == null) yield break;
                float lift = elapsed < windup ? Mathf.SmoothStep(0, 1, elapsed / windup)
                    : 1 - Mathf.Pow(Mathf.Clamp01((elapsed - windup) / strike), 2);
                hammer.localPosition = position + new Vector3(-.13f * lift, .38f * lift, -.7f);
                hammer.localRotation = rotation * Quaternion.Euler(0, 0, 48 * lift - 12 * (1 - lift));
                hammer.localScale = scale * (1 + .16f * lift);
                yield return null;
            }
            hammer.localPosition = position;
            hammer.localRotation = rotation;
            hammer.localScale = scale;
            onImpact?.Invoke();
            if (uiCatalog?.HammerHit != null) PlayProductionSound(uiCatalog.HammerHit);
            else PlayMachineImpact();
            StartCoroutine(AnimateProductionBurst(view.Root.localPosition, uiCatalog?.HammerImpact, CellPitch * 1.5f));
            StartCoroutine(AnimateImpactRing(view.Root.localPosition, CellPitch * 1.45f));
        }

        private IEnumerator AnimateImpactRing(Vector3 center, float radius)
        {
            GameObject root = EffectRoot("Hammer impact ring");
            center.z = -1.3f;
            LineRenderer ring = EffectLine(root.transform, new Color(1, .8f, .35f), .065f, 33);
            const float duration = .28f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                float r = Mathf.Lerp(.12f, radius, 1 - (1 - t) * (1 - t));
                for (int i = 0; i < 33; i++)
                {
                    float angle = i * Mathf.PI * 2 / 32;
                    ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * r);
                }
                EffectTint(ring, new Color(1, .8f, .35f, 1 - t));
                yield return null;
            }
            RemoveEffect(root);
        }

        private IEnumerator AnimateRocketPreparation(CellView view)
        {
            Transform rocket = view.Model.transform;
            Vector3 scale = rocket.localScale;
            for (float elapsed = 0; elapsed < .12f; elapsed += Time.unscaledDeltaTime)
            {
                if (rocket == null) yield break;
                rocket.localScale = scale * (1 + .13f * Mathf.Sin(elapsed / .12f * Mathf.PI));
                yield return null;
            }
            rocket.localScale = scale;
        }

        private sealed class RocketFlight
        {
            public GameObject Root;
            public readonly Transform[] Heads = new Transform[2];
            public readonly LineRenderer[] Trails = new LineRenderer[2];
            public readonly float[] Distances = new float[2];
            public Vector3 Origin, Axis;
        }

        private RocketFlight BeginRocketFlight(Match3BoardModel.PowerEvent power)
        {
            Texture2D texture = PieceTexture("rocket");
            if (texture == null) return null;
            Rect crop = PieceRect(texture);
            Rect uv = new Rect(crop.x / texture.width, crop.y / texture.height,
                crop.width / texture.width, crop.height / texture.height);
            bool vertical = power.Kind == SpecialKind.RayVertical;
            var flight = new RocketFlight
            {
                Root = EffectRoot("Rocket two-way flight"),
                Origin = views[power.Cell].HomePosition + Vector3.back * 1.3f,
                Axis = vertical ? Vector3.up : Vector3.right
            };
            float height = CellPitch * .66f, width = height * crop.width / crop.height;
            for (int i = 0; i < 2; i++)
            {
                flight.Heads[i] = EffectQuad(flight.Root.transform, texture, uv, new Vector2(width, height)).transform;
                flight.Heads[i].localPosition = flight.Origin;
                flight.Heads[i].localRotation = Quaternion.Euler(0, 0, (vertical ? 0 : -90) + i * 180);
                flight.Trails[i] = EffectLine(flight.Root.transform, new Color(1, .73f, .24f), .075f);
                flight.Trails[i].startWidth=.055f;flight.Trails[i].endWidth=.006f;
                float cells = vertical ? (i == 0 ? boardHeight - 1 - power.Cell.Y : power.Cell.Y)
                    : (i == 0 ? boardWidth - 1 - power.Cell.X : power.Cell.X);
                // Stop at the board's inner edge, including an edge-cell launch.
                flight.Distances[i] = (cells + .42f) * CellPitch;
            }
            if (views[power.Cell].Model != null) views[power.Cell].Model.SetActive(false);
            return flight;
        }

        private void UpdateRocketFlight(RocketFlight flight, float elapsed)
        {
            if (flight == null) return;
            float distance = elapsed / RocketCellSeconds * CellPitch;
            for (int i = 0; i < 2; i++)
            {
                Vector3 direction = flight.Axis * (i == 0 ? 1 : -1);
                float travel = Mathf.Min(distance, flight.Distances[i]);
                Vector3 head = flight.Origin + direction * travel;
                flight.Heads[i].localPosition = head;
                flight.Heads[i].gameObject.SetActive(distance + CellPitch * .33f < flight.Distances[i]);
                flight.Trails[i].SetPosition(0, head);
                flight.Trails[i].SetPosition(1, flight.Origin + direction * Mathf.Max(0, travel - .65f));
                float alpha = 1 - Mathf.Clamp01((distance - flight.Distances[i]) / (CellPitch*.40f));
                EffectTint(flight.Trails[i], new Color(1, .73f, .24f, alpha));
            }
        }

        private IEnumerator AnimateMagnetLinks(Match3BoardModel.ResolutionStep step)
        {
            if (step.MatchedCells.Count == 0) yield break;
            GameObject root = EffectRoot("Magnet target links");
            var lines = new List<LineRenderer>();
            var ends = new List<Vector3>();
            int colorIndex = step.Activation.Value.Color;
            Color energy = colorIndex >= 0 && colorIndex < Palette.Length
                ? Color.Lerp(Palette[colorIndex], Color.white, .35f) : new Color(.55f,.9f,1);
            Vector3 start = views[step.Activation.Value.Cell].HomePosition + Vector3.back * 1.3f;
            foreach (var cell in step.MatchedCells)
            {
                if (cell.Equals(step.Activation.Value.Cell)) continue;
                LineRenderer line = EffectLine(root.transform, energy, .026f);
                line.SetPosition(0, start); line.SetPosition(1, start);
                lines.Add(line);
                ends.Add(views[cell].HomePosition + Vector3.back * 1.3f);
            }
            const float duration = .32f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    float delay = .10f * i / Mathf.Max(1, lines.Count - 1);
                    float reach = Mathf.Clamp01((elapsed - delay) / .12f);
                    lines[i].SetPosition(1, Vector3.Lerp(start, ends[i], reach));
                    EffectTint(lines[i], new Color(energy.r, energy.g, energy.b,
                        Mathf.Clamp01((duration - elapsed) / .10f) * .8f));
                }
                yield return null;
            }
            RemoveEffect(root);
        }

        private IEnumerator AnimateConversionLink(Match3BoardModel.Cell source, Match3BoardModel.Cell target)
        {
            GameObject root=EffectRoot("Magnet conversion beam");
            LineRenderer line=EffectLine(root.transform,new Color(.55f,.9f,1),.035f);
            Vector3 start=views[source].HomePosition+Vector3.back*1.4f;
            Vector3 end=views[target].HomePosition+Vector3.back*1.4f;
            for(float elapsed=0;elapsed<.14f;elapsed+=Time.unscaledDeltaTime)
            {
                line.SetPosition(0,start); line.SetPosition(1,Vector3.Lerp(start,end,Mathf.Clamp01(elapsed/.10f)));
                yield return null;
            }
            RemoveEffect(root);
        }

        private IEnumerator AnimateMagnetOrbit(Match3BoardModel.ResolutionStep step)
        {
            GameObject root=EffectRoot("Magnet orbiting candies");
            var copies=new List<(Transform visual,Vector3 start,Vector3 scale,Quaternion rotation)>();
            Vector3 center=views[step.Activation.Value.Cell].HomePosition+Vector3.back*1.5f;
            foreach(var cell in step.MatchedCells)
            {
                if(cell.Equals(step.Activation.Value.Cell)) continue;
                CellView view=views[cell];
                if(view.Model==null || step.BeforeClear.GetContent(cell.X,cell.Y)!=CellContent.Candy) continue;
                GameObject copy=Instantiate(view.Model,root.transform);
                copy.transform.position=view.Model.transform.position;
                copy.transform.rotation=view.Model.transform.rotation;
                copy.transform.localScale=Vector3.Scale(view.Model.transform.localScale,view.Root.localScale);
                Vector3 start=copy.transform.localPosition;start.z=center.z;copy.transform.localPosition=start;
                copies.Add((copy.transform,start,copy.transform.localScale,copy.transform.localRotation));
                view.Model.SetActive(false);
            }
            for(float elapsed=0;elapsed<.65f;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/.65f);
                foreach(var item in copies)
                {
                    Vector3 offset=item.start-center;
                    // Pull inward first, then orbit close to the magnet to stay within the board.
                    float pull=Mathf.Clamp01(t/.45f);
                    Vector3 near=Vector3.Lerp(offset,offset.normalized*.32f,Mathf.SmoothStep(0,1,pull));
                    float angle=Mathf.Max(0,t-.4f)*Mathf.PI*3;
                    Vector3 orbit=new Vector3(near.x*Mathf.Cos(angle)-near.y*Mathf.Sin(angle),near.x*Mathf.Sin(angle)+near.y*Mathf.Cos(angle),0);
                    item.visual.localPosition=center+orbit*(1-Mathf.Clamp01((t-.8f)/.2f));
                    item.visual.localRotation=item.rotation*Quaternion.Euler(0,0,t*300);
                    item.visual.localScale=item.scale*(1-t*.85f);
                }
                yield return null;
            }
            RemoveEffect(root);
        }

        private IEnumerator AnimateHammerRocketLaunch(Match3BoardModel.PowerEvent power)
        {
            Texture2D texture=PieceTexture("hammer");if(texture==null)yield break;
            GameObject root=EffectRoot("Hammer launches three firework lanes");
            Vector3 center=views[power.Cell].HomePosition;
            Renderer hammer=EffectQuad(root.transform,texture,new Rect(0,0,1,1),Vector2.one*CellPitch*2.2f);
            for(float elapsed=0;elapsed<.27f;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/.27f),lift=1-Mathf.SmoothStep(0,1,t);
                root.transform.localPosition=center+new Vector3(-lift*.35f,lift*1.1f,-2f);
                root.transform.localRotation=Quaternion.Euler(0,0,lift*50);
                EffectTint(hammer,new Color(1,1,1,t>.8f?1-(t-.8f)/.2f:1));
                yield return null;
            }
            PlayProductionSound(uiCatalog?.HammerHit);RemoveEffect(root);
        }

        private IEnumerator AnimateMotorRun(CellView view, float duration)
        {
            if (view.Model == null) yield break;
            PlayProductionSound(uiCatalog?.MotorRunLoopDraft);
            Transform motor = view.Model.transform;
            Vector3 position = motor.localPosition;
            Quaternion rotation = motor.localRotation;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                if (motor == null) yield break;
                float t = elapsed / duration;
                float beat = Mathf.Sin(elapsed * 72) * Mathf.Sin(Mathf.PI * t);
                motor.localPosition = position + new Vector3(beat * .026f, Mathf.Abs(beat) * .035f, 0);
                motor.localRotation = rotation * Quaternion.Euler(0, 0, beat * 1.7f);
                yield return null;
            }
            motor.localPosition = position;
            motor.localRotation = rotation;
        }

        private IEnumerator AnimateMotorBurst(Match3BoardModel.ResolutionStep step)
        {
            CellView view = null;
            foreach (var item in views)
                if (item.Value.Model != null && item.Value.ModelKey.StartsWith("sprite_motor") &&
                    item.Value.Model.activeSelf &&
                    step.BeforeClear.GetContent(item.Key.X,item.Key.Y)==CellContent.Motor &&
                    step.AfterFall.GetContent(item.Key.X,item.Key.Y)!=CellContent.Motor)
                { view = item.Value; break; }
            if (view == null) { yield return AnimateClearAndHits(step); yield break; }
            yield return AnimateMotorRun(view, .30f);
            Texture2D texture = PieceTexture("motor");
            if (texture == null) { yield return AnimateClearAndHits(step); yield break; }
            Rect crop = PieceRect(texture);
            Transform motor = view.Model.transform;
            GameObject root = EffectRoot("Motor fifth-hit explosion");
            root.transform.localPosition = runtimeBoardRoot.InverseTransformPoint(motor.position) + Vector3.back * .3f;
            root.transform.localRotation = Quaternion.Inverse(runtimeBoardRoot.rotation) * motor.rotation;
            Vector3 localSize = new Vector3(motor.lossyScale.x / runtimeBoardRoot.lossyScale.x,
                motor.lossyScale.y / runtimeBoardRoot.lossyScale.y, 1);
            var parts = new Renderer[6];
            var origins = new Vector3[6];
            var velocities = new Vector3[6];
            for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++)
            {
                int i = y * 3 + x;
                Rect uv = new Rect((crop.x + crop.width * x / 3) / texture.width,
                    (crop.y + crop.height * y / 2) / texture.height,
                    crop.width / 3 / texture.width, crop.height / 2 / texture.height);
                parts[i] = EffectQuad(root.transform, texture, uv, new Vector2(localSize.x / 3, localSize.y / 2));
                origins[i] = new Vector3((x - 1) * localSize.x / 3, (y - .5f) * localSize.y / 2, 0);
                parts[i].transform.localPosition = origins[i];
                velocities[i] = new Vector3((x - 1) * 1.05f, (y - .5f) * 1.4f + .45f, 0);
            }
            view.Model.SetActive(false);
            gearBurstUntil = Time.unscaledTime + .82f;
            if (uiCatalog?.MotorBreak != null) PlayProductionSound(uiCatalog.MotorBreak);
            else PlayMachineImpact();
            StartCoroutine(AnimateProductionBurst(view.Root.localPosition, uiCatalog?.MotorBurst, CellPitch * 3.2f, .52f));
            const float duration = .48f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                foreach (var target in step.MatchedCells)
                {
                    float clear = Mathf.Clamp01(elapsed / ClearDuration);
                    views[target].Root.localScale = new Vector3(CellSize,CellSize,.34f) * Mathf.Lerp(1,.01f,clear*clear);
                }
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i].transform.localPosition = origins[i] + velocities[i] * t + Vector3.down * t * t * .4f;
                    parts[i].transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 1 : -1) * t * 24);
                    EffectTint(parts[i], new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, t)));
                }
                yield return null;
            }
            RemoveEffect(root);
        }
    }
}
