using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private bool helpOpen, wheelOpen, movesBankedThisRound;
        private int helpPage;

        private Vector2 helpDragStart;
        private Texture2D wheelTexture;
        private Texture2D wheelHomeButton;
        private Texture2D wheelHomeRotor;
        private float wheelSpinAt = -100f, wheelFromAngle, wheelToAngle;
        private int wheelMotion;
        private bool wheelSpinning;
        private float wheelPointerAngle, wheelPointerVelocity;
        private Texture2D wheelPeg;
        private float WheelDuration => wheelMotion == 0 ? 2.8f : 4.4f;
        private float WheelAngle() => EvaluateWheelAngle(Time.unscaledTime-wheelSpinAt, wheelFromAngle, wheelToAngle, wheelMotion);

        // Presentation only: the awarded sector is already saved by ToolProgress.
        internal static float EvaluateWheelAngle(float elapsed, float from, float to, int motion)
        {
            if (motion == 0)
                return Mathf.Lerp(from,to,1-Mathf.Pow(1-Mathf.Clamp01(elapsed/2.8f),4));
            // The adjacent sector starts at +/-30 degrees. Stay within 1.2 degrees
            // of that pin while hesitating, never deep inside the losing sector.
            float direction = motion == 2 ? -1f : 1f;
            float sectorCenter=Mathf.Round(to/60f)*60f;
            float approach = sectorCenter + direction*29.2f;
            if (elapsed < 2.6f)
                return Mathf.Lerp(from,approach,1-Mathf.Pow(1-Mathf.Clamp01(elapsed/2.6f),4));
            if (elapsed < 3.35f)
            {
                float u = Mathf.Clamp01((elapsed-2.6f)/.75f);
                // One small lean toward the boundary and one release. No repeated
                // sign changes that make the wheel/pointer look mechanically stuck.
                float lean = Mathf.Sin(u*Mathf.PI);
                return approach + direction*.6f*lean*lean;
            }
            float settle = Mathf.Clamp01((elapsed-3.35f)/1.05f);
            return Mathf.Lerp(approach,to,Mathf.SmoothStep(0,1,settle));
        }
        private int wheelResult = -1;
        private bool wheelPrizeOpen;
        private float wheelPrizeAt;
        private int bankTransferTotal;
        private float bankTransferAt = -100f;
        private int BankTransferShown => Mathf.RoundToInt(bankTransferTotal * Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-bankTransferAt)/1.8f)));

        private System.Collections.IEnumerator AnimateBankTransferSound()
        {
            int last=0;
            while(Time.unscaledTime-bankTransferAt<1.8f)
            {
                if(BankTransferShown>last){last=BankTransferShown;PlayProductionSound(uiCatalog?.BoltPing,.85f+last*.015f);}
                yield return null;
            }
        }

        private void DrawOpenTopHomeFrame(Rect display)
        {
            Texture2D texture=Resources.Load<Texture2D>("WorkshopMatch/home_open_top_v3");
            if(texture==null)return;
            // The art has no top rail. Keep the bolts and bottom corners at their
            // native proportions; only repeat a plain section of each side rail.
            float scale=display.width/(941f-50f);
            float inset=25f*scale;
            float edge=111f*scale;
            float bottom=200f*scale;
            float bottomY=display.height-bottom;
            float topY=-100f*scale;
            float middleY=topY+820f*scale;
            float lowerY=bottomY-430f*scale;
            float topRailY=GetSafeGuiRect(display).y-display.y+2f*scale;
            GUI.BeginGroup(display);
            float left=-inset, right=display.width+inset-edge;
            // A slim crosspiece closes the frame just below the safe-area notch.
            // This straight, bolt-free strip comes from the same oak/brass art;
            // the side rails cover its ends for clean joins.
            DrawSource(new Rect(left+100f*scale,topRailY,741f*scale,56f*scale),texture,new Rect(100,1492,741,56));
            DrawSource(new Rect(left,topY,edge,820f*scale),texture,new Rect(0,0,111,820));
            DrawSource(new Rect(right,topY,edge,820f*scale),texture,new Rect(830,0,111,820));
            if(lowerY>middleY)
            {
                DrawVerticalWood(new Rect(left,middleY,edge,lowerY-middleY),texture,new Rect(0,820,111,180));
                DrawVerticalWood(new Rect(right,middleY,edge,lowerY-middleY),texture,new Rect(830,820,111,180));
            }
            DrawSource(new Rect(left,lowerY,edge,430f*scale),texture,new Rect(0,1000,111,430));
            DrawSource(new Rect(right,lowerY,edge,430f*scale),texture,new Rect(830,1000,111,430));
            DrawSource(new Rect(-inset,bottomY,941f*scale,bottom),texture,new Rect(0,1430,941,200));
            GUI.EndGroup();
        }

        private static Texture2D modalFrame;
        private static void DrawDraftPanel(Rect rect, Texture2D texture, bool whiteFill=false)
        {
            if (modalFrame == null) modalFrame = Resources.Load<Texture2D>("WorkshopMatch/modal_frame_b_gold_v1");
            if (modalFrame == null) { DrawPanel(rect,texture); return; }
            // Plain fill is independent of the artwork. All four square corner
            // blocks use one uniform scale; only undecorated rails stretch.
            float corner = Mathf.Min(rect.width*.14f,rect.height*.22f);
            Color old = GUI.color;
            GUI.color = whiteFill ? new Color(1f,.98f,.94f,old.a) : new Color(.98f,.92f,.79f,old.a);
            // The source frame's inner rail begins halfway through its corner cell.
            // Use this same border on both axes, independent of panel aspect ratio.
            GUI.DrawTexture(InsetRect(rect,corner*.50f),Texture2D.whiteTexture);
            GUI.color = old;
            float[] dx = {rect.x,rect.x+corner,rect.xMax-corner,rect.xMax};
            float[] dy = {rect.y,rect.y+corner,rect.yMax-corner,rect.yMax};
            float[] sx = {0,modalFrame.width*.20f,modalFrame.width*.80f,modalFrame.width};
            float[] sy = {0,modalFrame.height*.20f,modalFrame.height*.80f,modalFrame.height};
            for (int y=0;y<3;y++) for (int x=0;x<3;x++)
            {
                if(x==1 && y==1) continue;
                DrawSource(Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]),modalFrame,
                    Rect.MinMaxRect(sx[x],sy[y],sx[x+1],sy[y+1]));
            }
        }

        private Rect PopupCloseRect(Rect panel, bool woodenFrame=false)
        {
            float size=panel.width*.14f;
            float gap=panel.width*(woodenFrame?.085f:.025f);
            return new Rect(panel.xMax-gap-size,panel.y+gap,size,size);
        }

        private System.Collections.IEnumerator AnimateWheelAudio()
        {
            float lastAngle=WheelAngle();
            wheelPointerAngle=wheelPointerVelocity=0;
            while(Time.unscaledTime-wheelSpinAt<WheelDuration+.35f)
            {
                float angle=WheelAngle(), dt=Mathf.Clamp(Time.unscaledDeltaTime,.001f,.025f);
                float speed=(angle-lastAngle)/dt;
                // Pins are at each 30+60n sector boundary. Sound and spring
                // impulse come from the same crossing, including reverse motion.
                if(Mathf.FloorToInt((angle+30)/60)!=Mathf.FloorToInt((lastAngle+30)/60))
                {
                    PlayProductionSound(uiCatalog?.UiClick,Mathf.Lerp(.85f,1.2f,Mathf.Clamp01(Mathf.Abs(speed)/800)));
                }
                float pinDistance=Mathf.Abs(Mathf.Repeat(angle,60)-30);
                float target=-Mathf.Clamp(speed/90f,-1f,1f)*16f*Mathf.Clamp01(1-pinDistance/9f);
                // Critically damped contact response: slow boundary motion cannot
                // repeatedly flip the pointer, and release returns it smoothly.
                wheelPointerAngle=Mathf.SmoothDamp(wheelPointerAngle,target,ref wheelPointerVelocity,.09f,
                    240f,Mathf.Max(.001f,Time.unscaledDeltaTime));
                lastAngle=angle;
                yield return null;
            }
            PlayProductionSound(wheelResult==5?uiCatalog?.MapSelect:uiCatalog?.BoltPing,wheelResult==5?.8f:1.2f);
            wheelSpinning=false;
            wheelPrizeOpen=true;wheelPrizeAt=Time.unscaledTime;
        }

        private static void DrawSlimHomeFrame(Rect rect, Texture2D texture)
        {
            // Fixed-size corners and independently sized rails; do not stretch the
            // entire portrait image to each device's aspect ratio.
            float edge = rect.width * .075f, bottom = rect.width * .16f;
            float[] dx = { rect.x, rect.x + edge, rect.xMax - edge, rect.xMax };
            float[] dy = { rect.y, rect.y + edge, rect.yMax - bottom, rect.yMax };
            float[] sx = { 0, texture.width * .09f, texture.width * .91f, texture.width };
            float[] sy = { 0, texture.height * .045f, texture.height * .92f, texture.height };
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            {
                if (x == 1 && y == 1) continue;
                DrawSource(Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]),texture,
                    Rect.MinMaxRect(sx[x],sy[y],sx[x+1],sy[y+1]));
            }
        }

        private void DrawHomeUtilityButtons(Rect portrait, Rect safe, GUIStyle title)
        {
            if (toolProgress == null || uiCatalog == null) return;
            float size = portrait.width * .135f;
            Rect wheel = new(portrait.x + portrait.width * .127f,
                HomeMenuLayout.ControlsY(portrait,safe) + portrait.width * .008f, size, size);
            if (wheelHomeButton == null)
                wheelHomeButton = Resources.Load<Texture2D>("WorkshopMatch/wheel_home_button_v1");
            wheelHomeRotor ??= Resources.Load<Texture2D>("WorkshopMatch/wheel_home_rotor_v2");
            Texture2D staticWheel = wheelHomeRotor != null ? null : wheelHomeButton != null ? wheelHomeButton : WheelTexture();
            homeWheelRect=wheel;
            bool utilityEnabled=GUI.enabled;
            GUI.enabled=utilityEnabled&&!HomeDrawerBlocking&&HomeInputReady&&(homeLesson==HomeLesson.Off||homeLesson==HomeLesson.WheelOpen);
            if(WheelUnlocked){
            if (DrawTextureButton(wheel, GUIContent.none, staticWheel, null, null, GUIStyle.none)){wheelOpen = true;if(homeLesson==HomeLesson.WheelOpen)SetHomeLesson(HomeLesson.WheelSpin);}
            if(wheelHomeRotor != null && wheelHomeButton != null)
            {
                Matrix4x4 previousMatrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(Mathf.Repeat(Time.unscaledTime*10f,360f),wheel.center);
                DrawArt(wheel,wheelHomeRotor);
                GUI.matrix=previousMatrix;
                // Crop the original triangular pointer; it never rotates with the disk.
                DrawSource(new Rect(wheel.x+wheel.width*.395f,wheel.y,wheel.width*.21f,wheel.height*.205f),
                    wheelHomeButton,new Rect(wheelHomeButton.width*.395f,0,wheelHomeButton.width*.21f,wheelHomeButton.height*.205f));
            }
            }
            GUIStyle small = new(title) { fontSize = Mathf.RoundToInt(size * .27f),
                normal = { textColor = new Color(.30f,.17f,.07f) } };
            Rect bank = new(wheel.x,wheel.yMax-size*.015f,size,size*.36f);
            if(WheelUnlocked){
            DrawCommonButton(bank,false);
            DrawUiLabel(InsetRect(bank,size*.035f),toolProgress.BankedMoves.ToString(),small);
            }
            DrawHomeMenuButton(portrait,safe,title);
            GUI.enabled=utilityEnabled;
        }

        private Rect UtilityPopup(Rect portrait, float height, bool wheelStyle=false)
        {
            Color old = GUI.color;
            GUI.color = new Color(.12f,.075f,.04f,.65f);
            GUI.DrawTexture(portrait,Texture2D.whiteTexture);
            GUI.color = old;
            Rect panel = new(portrait.x+portrait.width*.07f,portrait.center.y-height*.5f,portrait.width*.86f,height);
            if (wheelStyle) DrawSimplePopupPanel(panel);
            else DrawDraftPanel(panel,uiCatalog.SettingsPanel);
            return panel;
        }

        private static void DrawSimplePopupPanel(Rect panel)
        {
            float radius=panel.width*.045f;
            GUI.DrawTexture(panel,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,
                new Color(.72f,.43f,.13f),0,radius);
            GUI.DrawTexture(InsetRect(panel,panel.width*.008f),Texture2D.whiteTexture,
                ScaleMode.StretchToFill,true,0,new Color(.99f,.94f,.82f),0,radius*.85f);
        }

        private Texture2D WheelTexture()
        {
            if(wheelTexture!=null)return wheelTexture;
            const int size=1024;
            wheelTexture=new Texture2D(size,size,TextureFormat.RGBA32,true);
            Color[] pixels=new Color[size*size];
            Color[] colors={new(.98f,.58f,.14f),new(.12f,.65f,.71f),new(.88f,.32f,.24f),new(.98f,.71f,.23f),new(.16f,.48f,.66f),new(.55f,.38f,.24f)};
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float radius=(size-1)*.5f;
                float dx=x-radius,dy=y-radius,r=Mathf.Sqrt(dx*dx+dy*dy)/radius;
                if(r>1)continue;
                float angle=Mathf.Repeat(Mathf.Atan2(dx,dy)*Mathf.Rad2Deg+30f,360f);
                int sector=Mathf.Min(5,(int)(angle/60));
                // Sculpted, broad brass rim: dark silhouette, bevel, highlight,
                // recessed inner lip. No thin sinusoidal rings at icon scale.
                float light=.65f;
                Color gold=new(1f,.78f,.16f), pale=new(1f,.96f,.57f), shadow=new(.49f,.28f,.065f);
                Color c=colors[sector];
                if(r>.97f)c=Color.Lerp(shadow,gold,light*.6f);
                else if(r>.925f)c=Color.Lerp(gold,pale,light);
                else if(r>.835f)c=Color.Lerp(shadow,gold,.35f+light*.65f);
                else if(r>.807f)c=Color.Lerp(shadow,pale,light*.5f);
                else
                {
                    float seam=Mathf.Min(angle%60,60-angle%60)*Mathf.Deg2Rad*r;
                    if(seam<.012f)c=Color.Lerp(shadow,gold,light);
                    // Gentle inset shine within each segment.
                    // Sector colors remain uniform while the disk turns.
                }
                if(r<.145f)c=r>.118f?shadow:Color.Lerp(gold,pale,light);
                c.a=Mathf.Clamp01((1-r)*radius);
                pixels[y*size+x]=c;
            }
            wheelTexture.SetPixels(pixels);wheelTexture.Apply(true,true);
            wheelTexture.filterMode=FilterMode.Trilinear;wheelTexture.wrapMode=TextureWrapMode.Clamp;
            return wheelTexture;
        }

        private void DrawWheelPopup(Rect portrait, GUIStyle title)
        {
            if (toolProgress == null || uiCatalog == null) return;
            if (wheelPrizeOpen) { DrawWheelPrizeCard(portrait,title); return; }
            bool enabled=GUI.enabled;GUI.enabled=true;
            Rect panel=UtilityPopup(portrait,Mathf.Min(portrait.height*.85f,portrait.width*1.55f),true);
            GUIStyle text=new(title){fontSize=Mathf.RoundToInt(panel.width*.052f),normal={textColor=new Color(.30f,.17f,.07f)}};
            DrawUiLabel(LayoutRect(panel,.08f,.09f,.84f,.08f),$"MOVE BANK: {toolProgress.BankedMoves}",text);
            GUI.enabled=!WheelTeaching&&!wheelSpinning;
            if(DrawTextureButton(PopupCloseRect(panel),GUIContent.none,uiCatalog.CloseButton,null,null,title))wheelOpen=false;
            GUI.enabled=true;
            Rect disc=LayoutRect(panel,.075f,.19f,.85f,.54f);
            float diameter=Mathf.Min(disc.width,disc.height);disc=new Rect(disc.center-Vector2.one*diameter*.5f,Vector2.one*diameter);
            float t=Mathf.Clamp01((Time.unscaledTime-wheelSpinAt)/WheelDuration);
            float angle=WheelAngle();
            Matrix4x4 matrix=GUI.matrix;GUIUtility.RotateAroundPivot(angle,disc.center);
            DrawArt(disc,WheelTexture());
            for(int pin=0;pin<6;pin++)
            {
                float pinAngle=(pin*60+30)*Mathf.Deg2Rad;
                Vector2 pinCenter=disc.center+new Vector2(Mathf.Sin(pinAngle),-Mathf.Cos(pinAngle))*diameter*.48f;
                float pinSize=diameter*.047f;
                GUI.DrawTexture(new Rect(pinCenter-Vector2.one*pinSize*.5f,Vector2.one*pinSize),WheelPegTexture());
            }
            string[] icons={"hammer","rocket","magnet","hammer","rocket",null};
            for(int i=0;i<6;i++)
            {
                GUI.matrix=matrix;
                GUIUtility.RotateAroundPivot(angle+i*60f,disc.center);
                Vector2 pos=disc.center+Vector2.up*(-diameter*.275f);
                Rect icon=new(pos-Vector2.one*diameter*.085f,Vector2.one*diameter*.17f);
                if(icons[i]!=null)
                {
                    DrawPieceIcon(icon,icons[i]);
                    DrawUiLabel(new Rect(icon.x,icon.yMax+diameter*.022f,icon.width,diameter*.055f),i==3||i==4?"+2":"+1",new GUIStyle(text){fontSize=Mathf.RoundToInt(diameter*.050f)});
                }
                else DrawUiLabel(new Rect(pos.x-diameter*.12f,pos.y-diameter*.045f,diameter*.24f,diameter*.09f),"TRY\nAGAIN",new GUIStyle(text){fontSize=Mathf.RoundToInt(diameter*.045f)});
            }
            GUI.matrix=matrix;
            Color pointerColor=GUI.color;GUI.color=new Color(1f,.78f,.32f);
            float pointerSize=diameter*.11f;
            Vector2 hinge=new(disc.center.x,disc.y-pointerSize*.55f);
            GUIUtility.RotateAroundPivot(wheelPointerAngle,hinge);
            for(int layer=0;layer<2;layer++) for(int row=0;row<32;row++)
            {
                GUI.color=layer==0?new Color(.02f,.30f,.38f):Color.Lerp(new Color(.20f,.86f,.92f),new Color(.02f,.56f,.69f),row/32f);
                float scale=layer==0?1f:.73f;
                float half=pointerSize*(1f-row/32f)*.5f*scale;
                GUI.DrawTexture(new Rect(disc.center.x-half,disc.y-pointerSize*.55f+row*pointerSize/32f*scale,
                    half*2,pointerSize/32f+1),Texture2D.whiteTexture);
            }
            GUI.color=pointerColor;
            GUI.matrix=matrix;
            float hingeSize=diameter*.044f;
            GUI.DrawTexture(new Rect(hinge-Vector2.one*hingeSize*.5f,Vector2.one*hingeSize),WheelPegTexture());
            string[] prizes={"+1 HAMMER","+1 FIREWORK","+1 MAGNET","+2 HAMMER","+2 FIREWORK","TRY AGAIN"};
            string result=t<1?"SPINNING":wheelResult>=0?prizes[wheelResult]:"10 MOVES = 1 SPIN";
            DrawUiLabel(LayoutRect(panel,.08f,.73f,.84f,.06f),result,text);
            GUI.enabled=HomeInputReady&&t>=1 && !wheelSpinning && !wheelPrizeOpen && toolProgress.BankedMoves>=ToolProgressTracker.WheelSpinCost;
            homeSpinRect=LayoutRect(panel,.18f,.81f,.64f,.10f);
            if(DrawTextureButton(homeSpinRect,new GUIContent("SPIN - 10 MOVES"),uiCatalog.ActionButton,null,null,text))
            {
                int sector=Random.Range(0,WheelTeaching?5:6);
                if(toolProgress.TrySpinWheel(sector))
                {
                    Achievements.RecordWheelSpin();AchievementProgressStore.Save(Achievements.Snapshot);
                    if(WheelTeaching)SetHomeLesson(HomeLesson.WheelPrize);
                    wheelResult=sector;wheelFromAngle=Mathf.Repeat(wheelToAngle,360);
                    float landing=Random.value<.65f ? (Random.value<.5f?-1f:1f)*Random.Range(17f,26f) : Random.Range(-15f,15f);
                    if(Mathf.Abs(landing)<4f)landing=landing<0?-4f:4f;
                    wheelToAngle=wheelFromAngle+1080+Mathf.Repeat(-sector*60+landing-wheelFromAngle,360);
                    wheelSpinAt=Time.unscaledTime;
                    wheelMotion=Random.Range(0,4);
                    wheelSpinning=true;
                    StartCoroutine(AnimateWheelAudio());
                }
            }
            GUI.enabled=enabled;
        }

        private void DrawWheelPrizeCard(Rect portrait, GUIStyle title)
        {
            bool enabled=GUI.enabled;GUI.enabled=true;
            Color old=GUI.color;
            GUI.color=new Color(.10f,.075f,.04f,.72f);
            GUI.DrawTexture(portrait,Texture2D.whiteTexture);
            GUI.color=old;
            Vector2 size=new Vector2(portrait.width*.64f,portrait.width*.74f);
            Rect card=new Rect(portrait.center-size*.5f,size);
            GUI.DrawTexture(card,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,
                new Color(.12f,.55f,.56f),0,card.width*.075f);
            GUI.DrawTexture(InsetRect(card,card.width*.012f),Texture2D.whiteTexture,
                ScaleMode.StretchToFill,true,0,new Color(1f,.96f,.86f),0,card.width*.065f);
            GUIStyle text=new GUIStyle(title){fontSize=Mathf.RoundToInt(card.width*.073f),
                normal={textColor=new Color(.35f,.23f,.10f)}};
            string[] icons={"hammer","rocket","magnet","hammer","rocket"};
            string[] prizes={"+1 HAMMER","+1 FIREWORK","+1 MAGNET","+2 HAMMER","+2 FIREWORK","TRY AGAIN"};
            DrawUiLabel(LayoutRect(card,.08f,.08f,.84f,.10f),wheelResult<5?"YOU WON!":"NO PRIZE",text);
            if(wheelResult>=0 && wheelResult<5)
                DrawPieceIcon(LayoutRect(card,.27f,.23f,.46f,.38f),icons[wheelResult]);
            DrawUiLabel(LayoutRect(card,.06f,.65f,.88f,.12f),prizes[Mathf.Clamp(wheelResult,0,5)],text);

            if(DrawTextureButton(LayoutRect(card,.20f,.85f,.60f,.10f),new GUIContent("CONTINUE"),
                uiCatalog.ActionButton,null,null,new GUIStyle(text){fontSize=Mathf.RoundToInt(text.fontSize*.75f)}))
                {
                    wheelPrizeOpen=false;wheelOpen=!WheelTeaching;
                    if(wheelResult>=0&&wheelResult<5)StartCoroutine(AnimateWheelRewardToInventory(LayoutRect(card,.27f,.23f,.46f,.38f),icons[wheelResult]));
                    else if(WheelTeaching)SetHomeLesson(HomeLesson.WheelDone);
                }
            GUI.color=old;GUI.enabled=enabled;
        }

        private Texture2D WheelPegTexture()
        {
            if(wheelPeg!=null)return wheelPeg;
            const int size=64;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x-31.5f)/31.5f,dy=(y-31.5f)/31.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
                Color c=r>.78f?new Color(.43f,.19f,.09f):new Color(.84f,.43f,.23f);
                c.a=Mathf.Clamp01((1-r)*31.5f);pixels[y*size+x]=c;
            }
            wheelPeg=new Texture2D(size,size,TextureFormat.RGBA32,true);
            wheelPeg.SetPixels(pixels);wheelPeg.Apply(true,true);return wheelPeg;
        }
    }
}
