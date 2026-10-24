using System;
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        [Serializable] private sealed class DeveloperLinkSettings { public string ItchIoUrl=""; }
        private void OpenDeveloperPage()
        {
            var config=Resources.Load<TextAsset>("WorkshopMatch/developer_link");
            string url=config==null?"":JsonUtility.FromJson<DeveloperLinkSettings>(config.text)?.ItchIoUrl?.Trim();
            if(string.IsNullOrEmpty(url))return; // Ali will provide the page URL later.
            if(Uri.TryCreate(url,UriKind.Absolute,out var uri)&&(uri.Scheme=="https"||uri.Scheme=="http"))
                Application.OpenURL(uri.AbsoluteUri);
        }

        private static void DrawDeveloperIcon(Rect rect)
        {
            float pulse=(1f-Mathf.Cos(Time.unscaledTime*Mathf.PI*2f/3.2f))*.5f;
            Rect r=InsetRect(rect,-rect.width*.045f*pulse);
            DrawRoundedHomeControl(r,new Color(.12f,.53f,.57f),.22f);
            Vector2 Point(float x,float y)=>new(r.x+x*r.width,r.y+y*r.height);
            void Stroke(float x1,float y1,float x2,float y2)
            {
                Vector2 a=Point(x1,y1),b=Point(x2,y2);
                Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;
                GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
                GUI.color=new Color(1f,.95f,.78f,color.a);
                GUI.DrawTexture(new Rect(a.x,a.y-r.height*.034f,Vector2.Distance(a,b),r.height*.068f),Texture2D.whiteTexture);
                GUI.matrix=matrix;GUI.color=color;
            }
            Stroke(.38f,.27f,.22f,.50f);Stroke(.22f,.50f,.38f,.73f);
            Stroke(.62f,.27f,.78f,.50f);Stroke(.78f,.50f,.62f,.73f);
            Stroke(.56f,.25f,.44f,.75f);
        }
    }
}
