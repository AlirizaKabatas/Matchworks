using System;
using System.Text;
using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        [Serializable] private sealed class HelpGuideData { public HelpGuidePage[] pages=Array.Empty<HelpGuidePage>(); }
        [Serializable] private sealed class HelpGuidePage
        {
            public string demo="",titleTr="",titleEn="",bodyTr="",bodyEn="";
            public string[] stepsTr=Array.Empty<string>(),stepsEn=Array.Empty<string>();
        }
        private HelpGuideData helpGuide;
        private int helpAnimatedPage=-1;
        private float helpAnimationAt;
        private bool helpSwipeStarted;
        private const float HelpLoopSeconds=7f;
        private string HelpText(string tr,string en)=>GameLanguage.English?en:tr;

        private bool TryLoadHelpGuide()
        {
            // Unity hot reload can restore this nested object with an empty pages array.
            // A non-null cache alone does not prove that it contains a usable guide.
            if(IsValidHelpGuide(helpGuide))return true;
            helpGuide=null;
            try
            {
                TextAsset asset=Resources.Load<TextAsset>("WorkshopMatch/help_guide");
                if(asset==null)throw new InvalidOperationException("Missing help_guide resource");
                HelpGuideData loaded=JsonUtility.FromJson<HelpGuideData>(asset.text);
                if(!IsValidHelpGuide(loaded))throw new InvalidOperationException("Empty or invalid help guide");
                helpAnimatedPage=-1;
                helpGuide=loaded;
                return true;
            }
            catch(Exception error)
            {
                // A failed load must never leave an invisible modal blocking the map.
                helpOpen=false;
                Debug.LogError("Workshop Match help guide could not open: "+error.Message);
                return false;
            }
        }

        private static bool IsValidHelpGuide(HelpGuideData data)
        {
            if(data?.pages==null||data.pages.Length==0)return false;
            foreach(HelpGuidePage page in data.pages)
                if(page==null||string.IsNullOrEmpty(page.demo)||page.stepsTr==null||page.stepsEn==null||page.stepsTr.Length!=3||page.stepsEn.Length!=3)
                    return false;
            return true;
        }

        private void DrawHelpPopup(Rect portrait,GUIStyle title)
        {
            bool enabled=GUI.enabled;
            Color color=GUI.color;
            Matrix4x4 matrix=GUI.matrix;
            try { DrawHelpPopupContent(portrait,title); }
            catch(ExitGUIException) { throw; }
            catch(Exception error)
            {
                helpOpen=false;
                helpSwipeStarted=false;
                Debug.LogException(error);
            }
            finally { GUI.enabled=enabled;GUI.color=color;GUI.matrix=matrix; }
        }

        private void DrawHelpPopupContent(Rect portrait,GUIStyle title)
        {
            if(!TryLoadHelpGuide())return;
            bool enabled=GUI.enabled;GUI.enabled=true;
            Color old=GUI.color;
            GUI.color=new Color(.12f,.075f,.04f,.68f);GUI.DrawTexture(portrait,Texture2D.whiteTexture);GUI.color=old;
            Rect safe=GetSafeGuiRect(portrait);
            float width=portrait.width*.90f,height=Mathf.Min(safe.height-portrait.width*.055f,width*1.62f);
            Rect panel=new(safe.center.x-width*.5f,safe.center.y-height*.5f,width,height);
            DrawSimplePopupPanel(panel);
            helpPage=Mathf.Clamp(helpPage,0,helpGuide.pages.Length-1);
            if(helpAnimatedPage!=helpPage){helpAnimatedPage=helpPage;helpAnimationAt=Time.unscaledTime;helpSwipeStarted=false;}
            HelpGuidePage page=helpGuide.pages[helpPage];
            float time=Mathf.Repeat(Time.unscaledTime-helpAnimationAt,HelpLoopSeconds);
            float outcome=page.demo=="Motor"?3.5f:page.demo=="Rust"?4.7f:page.demo=="Frozen"?3.7f:page.demo=="Wheel"?4.8f:page.demo=="Credits"?3.5f:page.demo=="Attempt"?4.5f:page.demo.EndsWith("Create")?2.88f:page.demo.StartsWith("ComboConvert")?4.6f:page.demo=="ComboMixed"?3.15f:2.3f;
            int stage=time<1.2f?0:time<outcome?1:2;
            GUIStyle heading=new(title){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(width*.048f),normal={textColor=new Color(.30f,.17f,.07f)}};
            GUIStyle text=new(heading){fontSize=Mathf.RoundToInt(width*.035f)};
            DrawUiLabel(LayoutRect(panel,.17f,.060f,.66f,.070f),HelpText(page.titleTr,page.titleEn),heading);
            DrawUiLabel(LayoutRect(panel,.08f,.135f,.84f,.035f),HelpText("NASIL OYNANIR","HOW TO PLAY"),new GUIStyle(text){fontSize=Mathf.RoundToInt(width*.024f)});
            if(DrawTextureButton(new Rect(panel.xMax-width*.17f,panel.y+width*.025f,width*.145f,width*.145f),GUIContent.none,uiCatalog.CloseButton,null,null,title))helpOpen=false;
            Rect preview=LayoutRect(panel,.065f,.195f,.87f,.335f);
            GUI.DrawTexture(preview,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.35f,.23f,.13f),0,width*.025f);
            GUI.BeginGroup(preview);
            try { DrawHelpDemo(InsetRect(new Rect(Vector2.zero,preview.size),width*.018f),page.demo,time,text); }
            finally { GUI.EndGroup(); }
            string[] steps=GameLanguage.English?page.stepsEn:page.stepsTr;
            DrawUiLabel(LayoutRect(panel,.08f,.555f,.84f,.060f),WrapHelpCopy(steps[Mathf.Min(stage,steps.Length-1)],38),text);
            for(int i=0;i<3;i++)
            {
                float dot=width*.013f;
                Rect r=new(panel.center.x+(i-1)*width*.025f-dot*.5f,panel.y+panel.height*.538f,dot,dot);
                GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,i==stage?new Color(.11f,.65f,.70f):new Color(.78f,.68f,.49f),0,dot*.5f);
            }
            string copy=WrapHelpCopy(HelpText(page.bodyTr,page.bodyEn),43);
            int lines=copy.Split('\n').Length;
            GUIStyle copyStyle=new(text){fontSize=Mathf.RoundToInt(Mathf.Min(width*.035f,panel.height*.207f/Mathf.Max(1,lines)/1.25f)),alignment=TextAnchor.UpperCenter};
            DrawUiLabel(LayoutRect(panel,.09f,.635f,.82f,.207f),copy,copyStyle);
            GUI.enabled=helpPage>0;
            if(DrawTextureButton(LayoutRect(panel,.08f,.915f,.26f,.055f),new GUIContent("BACK"),uiCatalog.ActionButton,null,null,text))ChangeHelpPage(-1);
            GUI.enabled=helpPage<helpGuide.pages.Length-1;
            if(DrawTextureButton(LayoutRect(panel,.66f,.915f,.26f,.055f),new GUIContent("NEXT"),uiCatalog.ActionButton,null,null,text))ChangeHelpPage(1);
            GUI.enabled=true;
            DrawUiLabel(LayoutRect(panel,.38f,.923f,.24f,.035f),$"{helpPage+1} / {helpGuide.pages.Length}",text);
            // Swipes start only in the illustration, never on a navigation button.
            if(Event.current.type==EventType.MouseDown&&preview.Contains(Event.current.mousePosition))
            {helpDragStart=Event.current.mousePosition;helpSwipeStarted=true;}
            if(Event.current.type==EventType.MouseUp&&helpSwipeStarted)
            {
                float delta=Event.current.mousePosition.x-helpDragStart.x;
                if(Mathf.Abs(delta)>panel.width*.16f){ChangeHelpPage(delta<0?1:-1);uiTouchReleaseFrame=-1;Event.current.Use();}
                helpSwipeStarted=false;
            }
            if(uiTouchReleaseFrame==Time.frameCount&&preview.Contains(uiTouchStart)&&panel.Contains(uiTouchEnd))
            {
                float delta=uiTouchEnd.x-uiTouchStart.x;
                if(Mathf.Abs(delta)>panel.width*.16f){ChangeHelpPage(delta<0?1:-1);uiTouchReleaseFrame=-1;}
            }
            GUI.enabled=enabled;GUI.color=old;
        }
        private void ChangeHelpPage(int delta)
        {
            helpPage=Mathf.Clamp(helpPage+delta,0,helpGuide.pages.Length-1);
            helpAnimatedPage=-1;
        }
        private static string WrapHelpCopy(string copy,int columns)
        {
            StringBuilder result=new();int count=0;
            foreach(string word in copy.Split(new[]{' ','\n'},StringSplitOptions.RemoveEmptyEntries))
            {
                if(count>0&&count+word.Length+1>columns){result.Append('\n');count=0;}
                if(count>0){result.Append(' ');count++;}
                result.Append(word);count+=word.Length;
            }
            return result.ToString();
        }
        private void DrawHelpDemo(Rect area,string demo,float time,GUIStyle text)
        {
            float swap=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,2.3f,time));
            float effect=Mathf.Clamp01((time-2.3f)/.6f);
            float fade=1-Mathf.Clamp01((time-6.3f)/.7f);
            if(demo=="Unlock"){DrawHelpUnlock(area,time,text);return;}
            if(demo=="Wheel"){DrawHelpWheel(area,time,text);return;}
            if(demo=="Map"){DrawHelpMap(area,time,text);return;}
            if(demo=="Credits"||demo=="Attempt"){DrawHelpCredits(area,time,demo=="Attempt",text);return;}
            if(demo=="Goals"||demo=="Tutorial"){DrawHelpMenuDemo(area,time,demo=="Tutorial",text);return;}
            bool create=demo=="Match"||demo.EndsWith("Create")||demo=="Oil";
            bool combo=demo.StartsWith("Combo");
            int[,] colors=new int[6,5];
            for(int y=0;y<5;y++)for(int x=0;x<6;x++)colors[x,y]=(x*2+y*3+1)%5;
            Vector2Int from=new(1,3),to=new(1,2);
            int n=demo=="MagnetCreate"?5:demo=="RayCreate"?4:3;
            if(create)
            {
                for(int x=0;x<n;x++)colors[x,2]=0;
                colors[1,2]=1;colors[1,3]=0;
                if(n<6)colors[n,2]=1;
            }
            if(demo=="HammerCreate")
            {
                colors[1,1]=colors[2,1]=colors[2,2]=colors[1,3]=0;colors[1,2]=1;
                colors[3,1]=1;colors[1,0]=1;colors[2,0]=2;
            }
            if(!create&&demo!="HammerCreate"){from=new Vector2Int(2,2);to=new Vector2Int(3,2);}
            if(demo=="MagnetUse")colors[3,2]=2;
            if(demo=="Rust")
            {from=new Vector2Int(3,3);to=new Vector2Int(3,2);colors[2,2]=colors[4,2]=colors[3,3]=0;colors[3,2]=1;colors[1,2]=colors[5,2]=1;}
            if(demo=="Frozen")
            {from=new Vector2Int(2,0);to=new Vector2Int(2,1);colors[1,1]=colors[3,1]=colors[2,0]=0;colors[2,1]=1;colors[0,1]=colors[4,1]=1;}
            if(demo=="Motor")
            {from=new Vector2Int(2,0);to=new Vector2Int(2,1);colors[1,1]=colors[3,1]=colors[2,0]=0;colors[2,1]=1;colors[0,1]=colors[4,1]=1;}
            bool swapMatch=create||demo=="HammerCreate"||demo=="Rust"||demo=="Frozen"||demo=="Motor";
            bool rustSecond=demo=="Rust"&&time>=3.1f&&time<3.8f;
            float motorPhase=Mathf.Repeat(time,.7f);
            float movement=demo=="Motor"?Mathf.SmoothStep(0,1,Mathf.Clamp01(motorPhase/.42f)):rustSecond?Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.1f,3.8f,time)):swap;
            bool swapped=demo=="Motor"?motorPhase>=.42f:time>=2.3f&&!rustSecond;
            float cell=Mathf.Min(area.width/6,area.height/5);
            Rect grid=new(area.center.x-cell*3,area.center.y-cell*2.5f,cell*6,cell*5);
            Rect Cell(int x,int y)=>new(grid.x+x*cell,grid.y+y*cell,cell,cell);
            Vector2 Point(Vector2Int c)=>Cell(c.x,c.y).center;
            bool convert=demo=="ComboConvertRay"||demo=="ComboConvertHammer";
            string converted=demo=="ComboConvertRay"?"rocket":"hammer";
            // Paint the entire floor first: a piece moving into a later row must
            // never be covered by that row's background during the same repaint.
            for(int y=0;y<5;y++)for(int x=0;x<6;x++)
            {
                Rect r=Cell(x,y);
                Color tile=(x+y)%2==0?new Color(.47f,.32f,.19f):new Color(.43f,.29f,.17f);
                GUI.DrawTexture(InsetRect(r,cell*.035f),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,tile,0,cell*.08f);
            }
            Rect draggedArt=default;
            string draggedKey=null;
            bool draggedHorizontal=false;
            for(int y=0;y<5;y++)for(int x=0;x<6;x++)
            {
                Rect r=Cell(x,y);
                bool isFrom=x==from.x&&y==from.y,isTo=x==to.x&&y==to.y;
                bool selected=create?(y==2&&x<n)||(isFrom):demo=="HammerCreate"?((x==1||x==2)&&(y==1||y==2))||isFrom:(isFrom||isTo);
                if(selected&&time<2.3f)
                    GUI.DrawTexture(InsetRect(r,cell*.02f),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.30f,.93f,.95f,.25f+.09f*Mathf.Sin(time*5)),0,cell*.08f);
                if((demo=="Oil"&&x==1&&y==2&&time<2.3f)||(demo=="Frozen"&&x==2&&y==2&&time<5.3f)||(demo=="Motor"&&y==1&&x>=4&&time<3.5f))
                    DrawPieceIcon(InsetRect(r,cell*.08f),"oil");
                if(demo=="Frozen"&&x==2&&y==2)
                {if(time<2.3f)DrawPieceIcon(InsetRect(r,cell*.06f),"frozen");else if(time<3.1f)DrawArt(InsetRect(r,-cell*.10f),uiCatalog.FrozenOilShards);else if(time>3.1f&&time<5.3f)DrawPieceIcon(InsetRect(r,cell*.10f),time<4.8f?"candy_1":"candy_0");continue;}
                if(demo=="Rust"&&x==3&&y==1)continue;
                if(demo=="Motor"&&x>=2&&x<=4&&(y==2||y==3))continue;
                if(combo&&(isFrom||isTo))continue;
                if(!swapMatch&&time>=2.3f&&(isFrom||isTo)&&demo!="Place"&&demo!="Rotate")continue;
                float clearAt=HelpDemoClearAt(demo,x,y,n,colors,time);
                if(convert&&colors[x,y]==0&&!isFrom&&!isTo)
                {
                    float phase=time-(2.45f+(y*6+x)*.028f);
                    float launch=3.55f+(y*6+x)*.032f;
                    if(phase>=0)
                    {
                        if(time>=launch)continue;
                        if(phase<.30f)DrawHelpMagnetLink(Point(to),r.center,cell,phase);
                        float size=phase<.14f?Mathf.Lerp(1,.06f,Mathf.SmoothStep(0,1,phase/.14f)):
                            Mathf.Lerp(.06f,1,Mathf.SmoothStep(0,1,(phase-.14f)/.20f));
                        string item=phase<.14f?"candy_0":converted;
                        DrawHelpPower(new Rect(r.center-Vector2.one*cell*.43f*size,Vector2.one*cell*.86f*size),item,converted=="rocket"&&x%2==0);
                        continue;
                    }
                }
                if(time>=clearAt)
                {
                    float progress=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-clearAt)/.23f));
                    if(progress<1)
                    {
                        Vector2 point=r.center;
                        if(demo=="MagnetUse")point=Vector2.Lerp(point,Point(to),progress);
                        float size=cell*.86f*(1-progress);
                        DrawHelpPower(new Rect(point-Vector2.one*size*.5f,Vector2.one*size),"candy_"+colors[x,y]);
                    }
                    // The new tool appears only after the matched candies have disappeared.
                    if(create&&demo!="Match"&&demo!="Oil"&&isTo&&time>2.62f)
                    {
                        float scale=Mathf.SmoothStep(.05f,1,Mathf.InverseLerp(2.62f,2.88f,time));
                        float size=cell*.88f*scale;
                        DrawHelpPower(new Rect(r.center-Vector2.one*size*.5f,Vector2.one*size),demo=="RayCreate"?"rocket":demo=="HammerCreate"?"hammer":"magnet",demo=="RayCreate");
                    }
                    continue;
                }
                int color=colors[x,y];
                if(swapMatch&&swapped&&(isFrom||isTo))color=colors[isFrom?to.x:from.x,isFrom?to.y:from.y];
                if(demo=="Frozen"&&time>3.1f&&y==2&&(x==1||x==3))color=0;
                if(demo=="Frozen"&&time>3.1f&&x==2&&y==3)color=time<4.8f?0:1;
                string key="candy_"+color;
                if(!swapMatch&&isFrom)key=HelpFirstPower(demo);
                if(combo&&isTo)key=demo=="ComboHammer"||demo=="ComboConvertHammer"?"hammer":demo=="ComboMagnet"?"magnet":"rocket";
                if((demo=="Place"||demo=="Rotate")&&isFrom)key="candy_"+colors[x,y];
                if((demo=="Place"||demo=="Rotate")&&isTo&&time>2.3f)key="rocket";
                Vector2 center=r.center;
                bool moving=(time<2.3f||rustSecond||demo=="Motor"&&time<3.5f)&&(swapMatch||combo||demo.EndsWith("Use"))&&(isFrom||isTo);
                if(moving)
                    center=Vector2.Lerp(Point(isFrom?from:to),Point(isFrom?to:from),movement);
                Rect art=new(center-Vector2.one*cell*.43f,Vector2.one*cell*.86f);
                bool horizontal=(key=="rocket"&&demo!="Rotate")||(demo=="Rotate"&&time>1.8f);
                if(moving&&isFrom)
                {
                    // The hand-held piece is in front of its swap partner in both directions.
                    draggedArt=art;draggedKey=key;draggedHorizontal=horizontal;
                }
                else DrawHelpPower(art,key,horizontal);
            }
            if(draggedKey!=null)DrawHelpPower(draggedArt,draggedKey,draggedHorizontal);
            if(combo&&time<2.45f)
            {
                string held=HelpFirstPower(demo);
                string partner=demo=="ComboHammer"||demo=="ComboConvertHammer"?"hammer":demo=="ComboMagnet"?"magnet":"rocket";
                float absorb=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.3f,2.45f,time));
                float backSize=cell*.86f*(1-absorb);
                if(backSize>0)DrawHelpPower(new Rect(Point(to)-Vector2.one*backSize*.5f,Vector2.one*backSize),partner,partner=="rocket");
                float frontSize=cell*.86f*(1+.10f*Mathf.Sin(absorb*Mathf.PI));
                Vector2 point=Vector2.Lerp(Point(from),Point(to),swap);
                DrawHelpPower(new Rect(point-Vector2.one*frontSize*.5f,Vector2.one*frontSize),held,held=="rocket");
            }
            if(demo=="MagnetUse"&&time>=2.3f&&time<3.1f)
            {
                float pulse=1+.07f*Mathf.Sin((time-2.3f)*18);
                float size=cell*.86f*pulse;
                DrawHelpPower(new Rect(Point(to)-Vector2.one*size*.5f,Vector2.one*size),"magnet");
            }
            if(demo=="Frozen"&&time>5.3f&&time<5.9f)DrawArt(InsetRect(Cell(2,2),-cell*.12f),uiCatalog.OilSplash);
            if(demo=="Rust")
            {
                int rust=time<2.3f?2:time<3.8f?1:0;
                Rect part=InsetRect(Cell(3,1),cell*.06f);
                if(time>4.7f)part.y-=cell*3*Mathf.Clamp01((time-4.7f)/1.1f);
                if(time<5.8f)DrawArt(part,PartTexture(2,rust));
                if(time>4.7f&&time<5.8f)DrawArt(new Rect(Cell(3,0).x,Cell(3,0).y,cell,cell*4),uiCatalog.RocketSparks);
            }
            if(demo=="Motor"&&time<3.5f)
            {
                Rect motor=new(Cell(2,2).position,new Vector2(cell*3,cell*2));DrawPieceIcon(motor,"motor");
                int hits=Mathf.Min(5,Mathf.FloorToInt(time/.7f));
                GUI.DrawTexture(LayoutRect(motor,.33f,.302f,.34f*hits/5f,.035f),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(1,.73f,.16f),0,0);
                DrawHelpBadge(new Rect(motor.x,motor.yMax-cell*.1f,motor.width,cell*.4f),$"{hits} / 5",text);
            }
            if(demo=="Motor"&&time>=3.5f&&time<4.2f)DrawArt(new Rect(Cell(1,1).position,new Vector2(cell*5,cell*4)),uiCatalog.MotorBurst);
            if(demo=="HammerUse"||demo=="ComboHammer"||demo=="ComboMixed")
            {
                if(time>2.3f&&time<3.15f)
                {
                    float strike=Mathf.Clamp01((time-2.3f)/.35f);
                    float size=cell*(demo=="ComboHammer"?3.3f:2.1f);
                    Vector2 target=Point(to)+Vector2.up*cell*1.6f*(1-strike);
                    Rect hammer=new(target-Vector2.one*size*.5f,Vector2.one*size);
                    Matrix4x4 matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Lerp(-55,20,strike),target);DrawPieceIcon(hammer,"hammer");GUI.matrix=matrix;
                    if(time>2.65f&&time<2.90f)
                    {
                        Color saved=GUI.color;GUI.color=new Color(1,.76f,.28f,1-Mathf.InverseLerp(2.65f,2.90f,time));
                        DrawPieceIcon(new Rect(Point(to)-Vector2.one*cell*.22f,Vector2.one*cell*.44f),"star");GUI.color=saved;
                    }
                }
            }
            if(demo=="RayUse"||demo=="ComboRay")
            {
                DrawHelpRocketFlight(grid,cell,Point(to),2.3f,time,true);
                if(demo=="ComboRay")DrawHelpRocketFlight(grid,cell,Point(to),2.3f,time,false);
            }
            if(demo=="ComboMixed")
                for(int lane=-1;lane<=1;lane++)DrawHelpRocketFlight(grid,cell,Point(to)+Vector2.up*lane*cell,2.70f,time,true);
            if(convert)
            {
                for(int y=0;y<5;y++)for(int x=0;x<6;x++)
                {
                    if(colors[x,y]!=0||(x==2||x==3)&&y==2)continue;
                    float launch=3.55f+(y*6+x)*.032f;
                    if(converted=="rocket")DrawHelpRocketFlight(grid,cell,Cell(x,y).center,launch,time,x%2==0);
                    else DrawHelpConvertedHammer(Cell(x,y).center,cell,time-launch);
                }
            }
            if(demo=="Place"||demo=="Rotate")
            {
                Rect slot=new(grid.x+cell*.15f,grid.yMax-cell*.95f,cell*.8f,cell*.8f);
                DrawArt(slot,uiCatalog.ToolSlotSelected);DrawHelpPower(InsetRect(slot,cell*.07f),"rocket",demo=="Rotate"&&time>1.8f);
                DrawHelpBadge(new Rect(slot.xMax,slot.y+cell*.2f,cell*2.6f,cell*.4f),HelpText("HAMLE: 10","MOVES: 10"),text);
                if(time>1.2f&&time<2.3f)DrawHelpHand(Vector2.Lerp(slot.center,Point(to),swap),cell);
            }
            else if(demo=="Motor"&&time<3.5f)DrawHelpHand(Vector2.Lerp(Point(from),Point(to),movement),cell);
            else if(rustSecond)DrawHelpHand(Vector2.Lerp(Point(from),Point(to),movement),cell);
            else if(time>1.2f&&time<2.3f)DrawHelpHand(Vector2.Lerp(Point(from),Point(to),swap),cell);
            if(demo=="Frozen"&&time>3.7f&&time<4.8f)DrawHelpHand(Vector2.Lerp(Cell(2,3).center,Cell(2,2).center,Mathf.InverseLerp(3.7f,4.8f,time)),cell);
            // Fade into the next cycle rather than abruptly resetting a cleared board.
            if(fade<1)GUI.DrawTexture(area,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.35f,.23f,.13f,1-fade),0,0);
        }
        private void DrawHelpConvertedHammer(Vector2 target,float cell,float elapsed)
        {
            const float windup=.10f,strike=.08f,linger=.15f;
            if(elapsed<0||elapsed>=windup+strike+linger)return;
            float lift=elapsed<windup?Mathf.SmoothStep(0,1,elapsed/windup):
                1-Mathf.Pow(Mathf.Clamp01((elapsed-windup)/strike),2);
            float size=cell*1.2f*(1+.16f*lift);
            Vector2 head=target+new Vector2(-.13f,-.38f)*cell*lift;
            // Keep the head at the impact point and swing the handle about it.
            Rect art=new(head+new Vector2(0,size*.28f)-Vector2.one*size*.5f,Vector2.one*size);
            Matrix4x4 matrix=GUI.matrix;
            Color color=GUI.color;
            if(elapsed>=windup+strike)
                GUI.color=new Color(color.r,color.g,color.b,color.a*(1-(elapsed-windup-strike)/linger));
            GUIUtility.RotateAroundPivot(-48*lift+12*(1-lift),head);
            DrawPieceIcon(art,"hammer");
            GUI.matrix=matrix;GUI.color=color;
            if(elapsed>=windup+strike)
            {
                float alpha=1-(elapsed-windup-strike)/linger;
                GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);
                DrawArt(new Rect(target-Vector2.one*cell*.9f,Vector2.one*cell*1.8f),uiCatalog?.HammerImpact);
                GUI.color=color;
            }
        }
        private float HelpDemoClearAt(string demo,int x,int y,int n,int[,] colors,float time)
        {
            if(demo=="RayUse")return y==2?2.3f+Mathf.Abs(x-3)*.065f:float.PositiveInfinity;
            if(demo=="ComboRay")return y==2||x==3?2.3f+(y==2?Mathf.Abs(x-3):Mathf.Abs(y-2))*.065f:float.PositiveInfinity;
            if(demo=="ComboMixed")return Mathf.Abs(y-2)<=1?2.70f+Mathf.Abs(x-3)*.065f:float.PositiveInfinity;
            if(demo=="MagnetUse")return colors[x,y]==2?2.55f+(Mathf.Abs(x-3)+Mathf.Abs(y-2))*.045f:float.PositiveInfinity;
            if(demo.StartsWith("ComboConvert"))
            {
                if((x==2||x==3)&&y==2)return 2.3f;
                float earliest=float.PositiveInfinity;
                for(int sy=0;sy<5;sy++)for(int sx=0;sx<6;sx++)
                {
                    if(colors[sx,sy]!=0||(sx==2||sx==3)&&sy==2)continue;
                    bool horizontal=sx%2==0;
                    bool hit=demo=="ComboConvertHammer"?Mathf.Abs(x-sx)<=1&&Mathf.Abs(y-sy)<=1:horizontal?y==sy:x==sx;
                    if(hit)earliest=Mathf.Min(earliest,3.55f+(sy*6+sx)*.032f+(demo=="ComboConvertHammer"?.18f:(horizontal?Mathf.Abs(x-sx):Mathf.Abs(y-sy))*.065f));
                }
                return earliest;
            }
            if(demo=="ComboMagnet")return 2.3f+(y*6+(y%2==0?x:5-x))*.065f;
            if(demo=="HammerUse"||demo=="ComboHammer")
                return Mathf.Abs(x-3)<=(demo=="ComboHammer"?2:1)&&Mathf.Abs(y-2)<=(demo=="ComboHammer"?2:1)?2.65f:float.PositiveInfinity;
            if(!HelpDemoAffected(demo,x,y,n,colors[x,y],time))return float.PositiveInfinity;
            if(demo=="Motor")return time>=3.5f?3.5f:Mathf.Floor(time/.7f)*.7f+.42f;
            if(demo=="Rust")return time>=3.8f?3.8f:2.3f;
            if(demo=="Frozen")return time>=5.3f?5.3f:2.3f;
            return 2.3f;
        }
        private void DrawHelpMagnetLink(Vector2 from,Vector2 to,float cell,float phase)
        {
            Vector2 end=Vector2.Lerp(from,to,Mathf.SmoothStep(0,1,phase/.14f));
            Vector2 delta=end-from;
            Matrix4x4 matrix=GUI.matrix;Color saved=GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);
            GUI.color=new Color(.18f,.80f,.88f,1-Mathf.Clamp01((phase-.14f)/.16f));
            GUI.DrawTexture(new Rect(from.x,from.y-cell*.012f,delta.magnitude,cell*.024f),Texture2D.whiteTexture);
            GUI.matrix=matrix;GUI.color=saved;
        }
        private void DrawHelpRocketFlight(Rect grid,float cell,Vector2 origin,float launch,float time,bool horizontal)
        {
            if(time<launch)return;
            float travel=(time-launch)/.065f*cell;
            foreach(int direction in new[]{-1,1})
            {
                float distance=horizontal?(direction>0?grid.xMax-origin.x:origin.x-grid.x):(direction>0?grid.yMax-origin.y:origin.y-grid.y);
                if(travel>=distance+cell*.35f)continue;
                Vector2 axis=horizontal?Vector2.right:Vector2.up;
                Vector2 point=origin+axis*direction*Mathf.Min(travel,distance);
                Color saved=GUI.color;
                float alpha=1-Mathf.Clamp01((travel-distance)/(cell*.35f));
                GUI.color=new Color(1,.76f,.30f,alpha*.75f);
                Vector2 tail=point-axis*direction*Mathf.Min(travel,cell*.55f);
                Rect trail=horizontal?Rect.MinMaxRect(Mathf.Min(point.x,tail.x),point.y-cell*.018f,Mathf.Max(point.x,tail.x),point.y+cell*.018f):
                    Rect.MinMaxRect(point.x-cell*.018f,Mathf.Min(point.y,tail.y),point.x+cell*.018f,Mathf.Max(point.y,tail.y));
                GUI.DrawTexture(trail,Texture2D.whiteTexture);
                GUI.color=saved;
                if(travel<distance-cell*.20f)
                {
                    Matrix4x4 matrix=GUI.matrix;
                    GUIUtility.RotateAroundPivot(horizontal?(direction>0?-90:90):(direction>0?180:0),point);
                    DrawPieceIcon(new Rect(point-Vector2.one*cell*.32f,Vector2.one*cell*.64f),"rocket");GUI.matrix=matrix;
                }
            }
        }
        private static string HelpFirstPower(string demo)
        {
            if(demo.Contains("Magnet")||demo.StartsWith("ComboConvert"))return "magnet";
            if(demo.Contains("Hammer")||demo=="ComboMixed")return "hammer";
            if(demo=="RayUse"||demo=="ComboRay")return "rocket";
            return "candy_0";
        }
        private static bool HelpDemoAffected(string demo,int x,int y,int n,int color,float time)
        {
            if(demo=="Match"||demo=="Oil"||demo=="RayCreate"||demo=="MagnetCreate")return y==2&&x<n;
            if(demo=="HammerCreate")return (x==1||x==2)&&(y==1||y==2);
            if(demo=="RayUse")return y==2;
            if(demo=="HammerUse")return time>2.65f&&Mathf.Abs(x-3)<=1&&Mathf.Abs(y-2)<=1;
            if(demo=="MagnetUse")return color==2||(x==2||x==3)&&y==2;
            if(demo=="ComboRay")return y==2||x==3;
            if(demo=="ComboHammer")return time>2.65f&&Mathf.Abs(x-3)<=2&&Mathf.Abs(y-2)<=2;
            if(demo=="ComboMixed")return Mathf.Abs(y-2)<=1;
            if(demo=="ComboMagnet")return time>2.3f+(y*6+(y%2==0?x:5-x))*.065f;
            if(demo=="Rust")return y==2&&x>=2&&x<=4&&((time>=2.3f&&time<3.1f)||(time>=3.8f&&time<4.7f));
            if(demo=="Frozen")return (y==1&&x>=1&&x<=3&&time<3.1f)||(y==2&&x>=1&&x<=3&&time>=5.3f);
            if(demo.StartsWith("ComboConvert"))return (x==2||x==3)&&y==2;
            if(demo=="Motor")return time>=3.5f?x>=1&&x<=5&&y>=1:y==1&&x>=1&&x<=3&&Mathf.Repeat(time,.7f)>=.42f;
            return false;
        }
        private void DrawHelpPower(Rect art,string key,bool horizontal=false)
        {
            Matrix4x4 matrix=GUI.matrix;
            if(key=="rocket"&&horizontal)GUIUtility.RotateAroundPivot(90,art.center);
            DrawPieceIcon(art,key);GUI.matrix=matrix;
        }
        private void DrawHelpHand(Vector2 point,float cell)
        {
            tutorialHand??=Resources.Load<Texture2D>("WorkshopMatch/tutorial_hand_v1");
            if(tutorialHand==null)return;
            float size=cell*1.6f;
            GUI.DrawTexture(new Rect(point.x-size*.17f,point.y-size*.14f,size,size),tutorialHand,ScaleMode.ScaleToFit,true);
        }
        private void DrawHelpBadge(Rect area,string caption,GUIStyle style)
        {
            GUI.DrawTexture(area,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.98f,.93f,.80f),0,area.height*.25f);
            DrawUiLabel(area,caption,new GUIStyle(style){fontSize=Mathf.RoundToInt(area.height*.58f),normal={textColor=new Color(.30f,.17f,.07f)}});
        }
        private void DrawHelpUnlock(Rect area,float time,GUIStyle text)
        {
            string[] keys={"rocket","hammer","magnet"};int[] levels={4,6,9};
            for(int i=0;i<3;i++)
            {
                float size=area.height*.40f;
                Rect icon=new(area.x+area.width*(.11f+i*.30f),area.y+area.height*.20f,size,size);
                DrawPieceIcon(icon,keys[i]);
                if(time<1.2f+i*.9f)DrawArt(InsetRect(icon,size*.24f),uiCatalog.LockIcon);
                else DrawArt(new Rect(icon.xMax-size*.27f,icon.yMax-size*.27f,size*.25f,size*.25f),uiCatalog.CheckIcon);
                DrawHelpBadge(new Rect(icon.x,icon.yMax+size*.10f,icon.width,area.height*.15f),HelpText("BÖLÜM ","LEVEL ")+levels[i],text);
            }
        }
        private void DrawHelpWheel(Rect area,float time,GUIStyle text)
        {
            float size=Mathf.Min(area.height*.83f,area.width*.64f);
            Rect wheel=new(area.center.x-size*.5f,area.center.y-size*.5f,size,size);
            float angle=time<1.2f?0:time<4.8f?Mathf.Lerp(0,1080,1-Mathf.Pow(1-Mathf.InverseLerp(1.2f,4.8f,time),4)):1080;
            Matrix4x4 matrix=GUI.matrix;GUIUtility.RotateAroundPivot(angle,wheel.center);DrawArt(wheel,WheelTexture());
            string[] wheelIcons={"hammer","rocket","magnet","hammer","rocket",null};
            for(int i=0;i<6;i++)
            {
                GUI.matrix=matrix;GUIUtility.RotateAroundPivot(angle+i*60,wheel.center);
                Vector2 point=wheel.center+Vector2.up*(-size*.27f);
                Rect icon=new(point-Vector2.one*size*.075f,Vector2.one*size*.15f);
                if(wheelIcons[i]!=null)
                {DrawPieceIcon(icon,wheelIcons[i]);DrawUiLabel(new Rect(icon.x,icon.yMax,icon.width,size*.055f),i==3||i==4?"+2":"+1",new GUIStyle(text){fontSize=Mathf.RoundToInt(size*.045f)});}
                else DrawUiLabel(icon,"-",text);
            }
            GUI.matrix=matrix;
            DrawUiLabel(new Rect(wheel.center.x-size*.1f,wheel.y-size*.08f,size*.2f,size*.13f),"V",text);
            DrawHelpBadge(new Rect(area.x,area.y,area.width*.28f,area.height*.13f),HelpText("KASA: ","BANK: ")+(time<1.2f?30:20),text);
            if(time>4.8f){DrawPieceIcon(new Rect(area.xMax-size*.24f,area.center.y-size*.12f,size*.24f,size*.24f),"hammer");DrawUiLabel(new Rect(area.xMax-size*.25f,area.center.y+size*.15f,size*.25f,size*.13f),"+1",text);}
            if(time>1.2f&&time<2f)DrawHelpHand(wheel.center,area.height*.18f);
        }
        private void DrawHelpMap(Rect area,float time,GUIStyle text)
        {
            float drift=time<2.3f?Mathf.Sin(time/2.3f*Mathf.PI)*area.height*.25f:0;
            for(int i=0;i<3;i++)
            {
                Vector2 point=new(area.x+area.width*(.25f+.25f*i),area.y+area.height*(.70f-.25f*i)+drift);
                float width=area.height*.24f;
                Rect disk=new(point-Vector2.one*width*.5f,Vector2.one*width);
                DrawArt(disk,FlatMapNode(true,i==1&&time>2.3f));DrawUiLabel(disk,(11+i).ToString(),text);
            }
            Rect play=new(area.xMax-area.width*.32f,area.yMax-area.height*.20f,area.width*.29f,area.height*.16f);
            DrawTextureButtonPreview(play,HelpText("OYNA","PLAY"),text);
            Vector2 hand=time<2.3f?new Vector2(area.center.x,area.center.y+drift):time<4.8f?new Vector2(area.center.x,area.center.y):play.center;
            DrawHelpHand(hand,area.height*.18f);
        }
        private void DrawTextureButtonPreview(Rect rect,string label,GUIStyle text)
        {DrawCommonButton(rect);DrawUiLabel(rect,label,text);}
        private void DrawHelpCredits(Rect area,float time,bool rollback,GUIStyle text)
        {
            int credits=time<2.3f?3:time<3.5f?4:rollback&&time>=4.5f?3:0;
            int stock=time>=3.5f&&(!rollback||time<4.5f)?1:0;
            Rect icon=new(area.x+area.width*.12f,area.center.y-area.height*.26f,area.height*.52f,area.height*.52f);
            DrawPieceIcon(icon,"rocket");
            DrawHelpBadge(new Rect(area.center.x,area.y+area.height*.22f,area.width*.40f,area.height*.18f),HelpText("KREDİ: ","CREDIT: ")+$"{credits} / 4",text);
            DrawHelpBadge(new Rect(area.center.x,area.y+area.height*.48f,area.width*.40f,area.height*.18f),HelpText("STOK: ","STOCK: ")+stock,text);
            if(rollback&&time>4.5f)DrawUiLabel(new Rect(area.x,area.yMax-area.height*.20f,area.width,area.height*.13f),HelpText("YENİDEN BAŞLAT","RESTART"),text);
            if(!rollback&&time>3.5f)DrawUiLabel(new Rect(icon.x,icon.yMax,icon.width,area.height*.14f),"+1",text);
        }
        private void DrawHelpMenuDemo(Rect area,float time,bool tutorial,GUIStyle text)
        {
            if(tutorial)
            {
                Rect card=InsetRect(area,area.height*.12f);DrawSimplePopupPanel(card);
                if(time>=4.8f)
                {
                    DrawUiLabel(LayoutRect(card,.08f,.10f,.84f,.18f),"SETTINGS",text);
                    DrawArt(LayoutRect(card,.40f,.31f,.20f,.22f),uiCatalog.TealSettingsButton);
                    Rect skip=LayoutRect(card,.10f,.62f,.80f,.20f);
                    DrawTextureButtonPreview(skip,HelpText("ÖĞRETİCİYİ ATLA","SKIP TUTORIAL"),text);
                    DrawHelpHand(skip.center,area.height*.17f);return;
                }
                DrawUiLabel(LayoutRect(card,.08f,.10f,.84f,.30f),WrapHelpCopy(HelpText("Öğreticiyi tekrar görmek ister misin?","Would you like to see the tutorial again?"),30),text);
                Rect yes=LayoutRect(card,.10f,.56f,.35f,.22f),no=LayoutRect(card,.55f,.56f,.35f,.22f);
                DrawTextureButtonPreview(yes,HelpText("EVET","YES"),text);DrawTextureButtonPreview(no,HelpText("HAYIR","NO"),text);
                if(time>1.2f&&time<4.8f)DrawHelpHand(yes.center,area.height*.17f);
            }
            else
            {
                string[] keys={"bolt_nut","oil","motor"};
                for(int i=0;i<3;i++)
                {
                    Rect icon=new(area.x+area.width*(.10f+i*.30f),area.y+area.height*.14f,area.width*.19f,area.height*.40f);
                    DrawPieceIcon(icon,keys[i]);
                    int max=i==0?2:i==1?4:5;int shown=Mathf.Clamp(Mathf.FloorToInt((time-1.2f)/.6f)-i,0,max);
                    DrawHelpBadge(new Rect(icon.x,area.y+area.height*.62f,icon.width,area.height*.15f),$"{shown} / {max}",text);
                }
                if(time>5.4f)DrawUiLabel(new Rect(area.x,area.yMax-area.height*.14f,area.width,area.height*.12f),HelpText("HEDEFLER TAMAM!","ALL GOALS COMPLETE!"),text);
            }
        }
    }
}
