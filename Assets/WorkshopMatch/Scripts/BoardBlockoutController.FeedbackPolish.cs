using UnityEngine;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private AudioClip fireworkWhoosh,fireworkSoftPop;
        private void PlayFireworkSound(bool burst)
        {
            if(fireworkWhoosh==null)
            {
                fireworkWhoosh=MakeFireworkClip(false);
                fireworkSoftPop=MakeFireworkClip(true);
            }
            PlayProductionSound(burst?fireworkSoftPop:fireworkWhoosh);
        }
        private static AudioClip MakeFireworkClip(bool burst)
        {
            const int rate=22050;
            float duration=burst?.10f:.18f;
            var samples=new float[Mathf.RoundToInt(rate*duration)];
            var random=new System.Random(burst?804:402);
            float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,u=t/duration;
                filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,burst?.07f:.045f);
                float envelope=burst?Mathf.Min(1,t*1200)*Mathf.Exp(-t*48):Mathf.Sin(u*Mathf.PI)*Mathf.Sin(u*Mathf.PI);
                float low=burst?Mathf.Sin(t*125*Mathf.PI*2)*Mathf.Exp(-t*65)*.22f:0;
                samples[i]=(filtered+low)*envelope*(burst?.36f:.26f);
            }
            var clip=AudioClip.Create(burst?"Firework soft pop":"Firework short whoosh",samples.Length,1,rate,false);
            clip.SetData(samples,0);return clip;
        }
        private static void DrawQuietClose(Rect rect,bool held)
        {
            Color old=GUI.color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.77f,.55f,.21f,old.a),0,rect.width*.5f);
            Rect face=InsetRect(rect,rect.width*.065f);
            GUI.DrawTexture(face,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,
                held?new Color(.025f,.48f,.56f,old.a):new Color(.06f,.67f,.74f,old.a),0,face.width*.5f);
            Matrix4x4 matrix=GUI.matrix;
            for(int i=0;i<2;i++)
            {
                GUI.matrix=matrix;GUIUtility.RotateAroundPivot(i==0?45f:-45f,rect.center);
                GUI.DrawTexture(new Rect(rect.center.x-rect.width*.26f,rect.center.y-rect.height*.06f,rect.width*.52f,rect.height*.12f),
                    Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(1,.96f,.84f,old.a),0,rect.width*.035f);
            }
            GUI.matrix=matrix;GUI.color=old;
        }
    }
}
