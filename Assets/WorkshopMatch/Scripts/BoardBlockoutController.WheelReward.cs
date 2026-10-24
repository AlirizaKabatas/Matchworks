using System.Collections;
using UnityEngine;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private bool wheelRewardAnimating;
        private Rect wheelRewardRect;
        private string wheelRewardIcon;
        private float wheelRewardOpacity;
        private IEnumerator AnimateWheelRewardToInventory(Rect source,string icon)
        {
            // Stock was committed when the spin began. This is presentation only.
            wheelRewardAnimating=true;wheelRewardIcon=icon;wheelRewardRect=source;wheelRewardOpacity=1;
            Rect portrait=GetPortraitRect();Vector2 start=source.center;
            Vector2 end=new Vector2(homePlayRect.center.x,portrait.yMax+source.height*.6f);
            float elapsed=0;
            while(elapsed<1.25f)
            {
                elapsed+=Time.unscaledDeltaTime;
                float t=Mathf.Clamp01(elapsed/1.25f);
                float motion=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.12f)/.88f));
                Vector2 position=Vector2.Lerp(start,end,motion);
                position.x+=Mathf.Sin(motion*Mathf.PI)*portrait.width*.05f;
                Vector2 size=source.size*Mathf.Lerp(1,.28f,Mathf.SmoothStep(0,1,t));
                wheelRewardRect=new Rect(position-size*.5f,size);
                wheelRewardOpacity=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.83f)/.17f));
                yield return null;
            }
            wheelRewardAnimating=false;wheelRewardIcon=null;
            if(WheelTeaching){wheelOpen=false;SetHomeLesson(HomeLesson.WheelDone);}
        }
        private void DrawWheelRewardFlight()
        {
            if(!wheelRewardAnimating||wheelRewardIcon==null)return;
            Color old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*wheelRewardOpacity);
            DrawPieceIcon(wheelRewardRect,wheelRewardIcon);GUI.color=old;
        }
    }
}
