using UnityEngine;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private static Texture2D rotationButtonArt;
        private static Texture2D RotationButtonArt()
        {
            if(rotationButtonArt != null) return rotationButtonArt;
            const int size=96;
            var pixels=new Color[size*size];
            bool Triangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
            {
                float Cross(Vector2 u,Vector2 v)=>u.x*v.y-u.y*v.x;
                float u=Cross(b-a,p-a),v=Cross(c-b,p-b),w=Cross(a-c,p-c);
                return (u>=0 && v>=0 && w>=0)||(u<=0 && v<=0 && w<=0);
            }
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                Color sum=Color.clear;
                for(int sample=0;sample<4;sample++)
                {
                    Vector2 p=new Vector2((x+(sample%2)*.5f+.25f)/size,(y+(sample/2)*.5f+.25f)/size);
                    Vector2 q=new Vector2(Mathf.Abs(p.x-.5f),Mathf.Abs(p.y-.5f))-new Vector2(.32f,.32f);
                    float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-.15f;
                    if(distance>0) continue;
                    Color color=Color.Lerp(new Color(.96f,.56f,.035f),new Color(1f,.89f,.24f),p.y);
                    if(distance>-.025f)color=new Color(.86f,.45f,.015f);
                    else if(distance>-.047f)color=new Color(1f,.94f,.50f);
                    else if(p.y>.69f)color=Color.Lerp(color,Color.white,.24f);
                    Vector2 v=p-new Vector2(.5f,.5f);
                    float angle=Mathf.Repeat(Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg,360f);
                    bool arrow=Mathf.Abs(v.magnitude-.255f)<.034f && ((angle>=20f && angle<=155f)||(angle>=200f && angle<=335f));
                    for(int head=0;head<2;head++)
                    {
                        float a=(20+head*180)*Mathf.Deg2Rad;
                        Vector2 radial=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                        Vector2 tangent=new Vector2(radial.y,-radial.x);
                        Vector2 center=new Vector2(.5f,.5f)+radial*.255f;
                        arrow |= Triangle(p,center+tangent*.085f,center-tangent*.055f+radial*.075f,center-tangent*.055f-radial*.075f);
                    }
                    if(arrow)color=Color.white;
                    sum+=color*.25f;
                }
                pixels[y*size+x]=sum;
            }
            rotationButtonArt=new Texture2D(size,size,TextureFormat.RGBA32,false);
            rotationButtonArt.name="Yellow square rotation button";
            rotationButtonArt.SetPixels(pixels);rotationButtonArt.Apply(false,true);
            rotationButtonArt.filterMode=FilterMode.Bilinear;
            rotationButtonArt.wrapMode=TextureWrapMode.Clamp;
            return rotationButtonArt;
        }
    }
}
