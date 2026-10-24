using UnityEngine;
using System.Collections.Generic;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private readonly Dictionary<Texture2D,Texture2D> milestoneGuiTextures=new();
        private void DrawMilestoneIcon(Rect rect,string key)
        {
            Texture2D source=PieceTexture(key);
            if(source==null)return;
            Texture2D art=source;
            if(QualitySettings.activeColorSpace==ColorSpace.Linear&&source.isReadable)
            {
                if(!milestoneGuiTextures.TryGetValue(source,out art))
                {
                    var pixels=source.GetPixels();
                    for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].linear;
                    art=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,true)
                        {name=key+" GUI color",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                    art.SetPixels(pixels);art.Apply(false,true);milestoneGuiTextures[source]=art;
                }
            }
            Rect crop=PieceRect(source);
            float fit=Mathf.Min(rect.width/crop.width,rect.height/crop.height);
            Vector2 size=crop.size*fit;
            GUI.DrawTextureWithTexCoords(new Rect(rect.center-size*.5f,size),art,
                new Rect(crop.x/source.width,crop.y/source.height,crop.width/source.width,crop.height/source.height),true);
        }
        private void DrawMapMilestone(Rect node,int index)
        {
            if(index!=9&&index!=19)return;
            float size=node.width*(index==9?.31f:.28f);
            // Nest the symbols into the curved disk's corners, not outside its bounding box.
            Rect marker=index==9
                ?new Rect(node.x+node.width*.02f,node.yMax-size*.65f,size,size)
                :new Rect(node.xMax-size*.70f,node.y+node.height*.08f-size*.25f,size,size);
            Color old=GUI.color;
            bool enabled=GUI.enabled;
            // These decorative milestone symbols stay legible independently of node state.
            GUI.enabled=true;
            GUI.color=new Color(1f,1f,1f,old.a);
            DrawMilestoneIcon(marker,index==9?"map_tutorial_complete_source":"map_demo_trophy_source");
            GUI.color=old;
            GUI.enabled=enabled;
        }

        private void DrawDemoCompletion(GUIStyle title,GUIStyle body)
        {
            Rect portrait=GetPortraitRect();
            Rect safe=GetSafeGuiRect(portrait);
            Color old=GUI.color;
            GUI.color=new Color(.08f,.05f,.025f,old.a*.55f);
            GUI.DrawTexture(portrait,Texture2D.whiteTexture);
            GUI.color=old;
            float width=portrait.width*.86f;
            float height=Mathf.Min(width*1.32f,safe.height*.82f);
            Rect panel=new(safe.center.x-width*.5f,safe.center.y-height*.5f,width,height);
            DrawDraftPanel(panel,uiCatalog?.ResultPanel);
            DrawPieceIcon(LayoutRect(panel,.33f,.12f,.34f,.28f),"demo_completion_trophy_source");
            GUIStyle heading=new(title){fontSize=Mathf.RoundToInt(width*.090f),normal={textColor=new Color(.32f,.17f,.065f)}};
            GUIStyle detail=new(body){fontSize=Mathf.RoundToInt(width*.054f),normal={textColor=new Color(.38f,.24f,.11f)}};
            GUIStyle thanks=new(detail){fontSize=Mathf.RoundToInt(width*.043f)};
            DrawUiLabel(LayoutRect(panel,.12f,.44f,.76f,.105f),"TEBRİKLER!",heading,true);
            DrawUiLabel(LayoutRect(panel,.12f,.57f,.76f,.075f),"Demoyu tamamladın.",detail,true);
            DrawUiLabel(LayoutRect(panel,.12f,.66f,.76f,.070f),"Oynadığın için teşekkürler.",thanks,true);
            GUIStyle action=new(heading){fontSize=Mathf.RoundToInt(width*.052f)};
            if(DrawTextureButton(LayoutRect(panel,.16f,.79f,.68f,.11f),new GUIContent("HARİTAYA DÖN"),
                uiCatalog?.ActionButton,null,null,action))ReturnToProgressHome();
            GUI.color=old;
        }
    }
}
