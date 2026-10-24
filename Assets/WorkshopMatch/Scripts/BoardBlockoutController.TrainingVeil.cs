using UnityEngine;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private Texture2D trainingVeil;
        private Rect tutorialRotationRect,trainingVeilHole;
        private float trainingVeilRadius;
        private float tutorialVisibleAt,tutorialFadeAt;
        private float TutorialFade=>Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-tutorialFadeAt)/TutorialPresentationRules.IntroFade));
        private void DrawTrainingVeil(Rect bounds,Rect hole,float radius)
        {
            Rect uv=new Rect((hole.x-bounds.x)/bounds.width,(hole.y-bounds.y)/bounds.height,hole.width/bounds.width,hole.height/bounds.height);
            if(trainingVeil==null||trainingVeilHole!=uv||!Mathf.Approximately(trainingVeilRadius,radius))
            {
                // Screen-independent distance in portrait-width units, with a broad
                // outward feather. There is no pale border or abrupt edge at the hole.
                const int width=384,height=768;
                if(trainingVeil==null){trainingVeil=new Texture2D(width,height,TextureFormat.RGBA32,false);trainingVeil.filterMode=FilterMode.Bilinear;trainingVeil.wrapMode=TextureWrapMode.Clamp;}
                trainingVeilHole=uv;trainingVeilRadius=radius;
                var pixels=new Color32[width*height];
                float aspect=bounds.height/bounds.width;
                Vector2 half=new Vector2(uv.width*.5f,uv.height*aspect*.5f);
                float r=Mathf.Min(radius/bounds.width,Mathf.Min(half.x,half.y));
                Vector2 center=new Vector2(uv.center.x,uv.center.y*aspect);
                const float feather=.022f;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    Vector2 q=new Vector2(Mathf.Abs((x+.5f)/width-center.x),Mathf.Abs((1-(y+.5f)/height)*aspect-center.y))-half+Vector2.one*r;
                    float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-r;
                    float alpha=hole.width>0?Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/feather)):1;
                    pixels[y*width+x]=new Color32(6,4,3,(byte)(alpha*178));
                }
                trainingVeil.SetPixels32(pixels);trainingVeil.Apply(false,false);
            }
            GUI.DrawTexture(bounds,trainingVeil);
        }
        private Texture2D tutorialFrameTexture;
        private void DrawTutorialFrame(Rect panel,float portraitWidth)
        {
            tutorialFrameTexture??=Resources.Load<Texture2D>("WorkshopMatch/tutorial_frame_teal_v1");
            if(tutorialFrameTexture==null){DrawDraftPanel(panel,null);return;}
            Rect source=TexturePixels(tutorialFrameTexture);
            // Equal source/destination corner widths and heights keep screws round
            // while the plain cream middle expands for each message.
            float sourceCorner=source.height*.22f;
            float corner=Mathf.Min(portraitWidth*.085f,Mathf.Min(panel.width,panel.height)*.30f);
            float[] dx={panel.x,panel.x+corner,panel.xMax-corner,panel.xMax};
            float[] dy={panel.y,panel.y+corner,panel.yMax-corner,panel.yMax};
            float[] sx={source.x,source.x+sourceCorner,source.xMax-sourceCorner,source.xMax};
            float[] sy={source.y,source.y+sourceCorner,source.yMax-sourceCorner,source.yMax};
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {
                Rect target=Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]);
                Rect sample=Rect.MinMaxRect(sx[x],sy[y],sx[x+1],sy[y+1]);
                float fx=sample.width/target.width,fy=sample.height/target.height;
                float left=x>0?.75f:0,right=x<2?.75f:0,top=y>0?.75f:0,bottom=y<2?.75f:0;
                DrawSource(Rect.MinMaxRect(target.x-left,target.y-top,target.xMax+right,target.yMax+bottom),tutorialFrameTexture,
                    Rect.MinMaxRect(sample.x-left*fx,sample.y-top*fy,sample.xMax+right*fx,sample.yMax+bottom*fy));
            }
        }
        private float TutorialPanelHeight(Rect portrait,int lines,bool prompt)=>portrait.width*TutorialPresentationRules.PanelHeight(lines,prompt);
        private void DrawTutorialPanelCopy(Rect panel,Rect portrait,string[] lines,GUIStyle title,bool prompt)
        {
            DrawTutorialFrame(panel,portrait.width);
            var style=new GUIStyle(title){font=uiCatalog?.UiFont,alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(portrait.width*.039f),normal={textColor=new Color(.42f,.23f,.08f)}};
            float pitch=portrait.width*TutorialPresentationRules.TextPitch;
            float top=panel.y+portrait.width*TutorialPresentationRules.TopPadding;
            for(int i=0;i<lines.Length;i++)DrawUiLabel(new Rect(panel.x+panel.width*.10f,top+i*pitch,panel.width*.80f,pitch),lines[i],style);
            if(prompt)
            {
                float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*3);
                Rect rect=new Rect(panel.x+panel.width*.12f,top+lines.Length*pitch+portrait.width*TutorialPresentationRules.PromptGap,panel.width*.76f,portrait.width*TutorialPresentationRules.PromptHeight);
                DrawUiLabel(rect,"DEVAM ICIN DOKUN",new GUIStyle(style){fontSize=Mathf.RoundToInt(portrait.width*(.024f+.001f*pulse))});
            }
        }
    }
}
