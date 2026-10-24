using UnityEngine;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private bool homeMenuOpen,achievementsOpen;
        private float homeMenuSlide;
        private int homeMenuSlideFrame=-1;
        private bool HomeDrawerBlocking=>homeMenuOpen||homeMenuSlide>.001f||achievementsOpen;
        private Rect HomeMenuButtonRect(Rect portrait,Rect safe)=>
            HomeMenuLayout.MenuButton(portrait,safe);
        private void CloseHomeDrawer(){homeMenuOpen=false;homeMenuSlide=0;}
        private void DrawHomeMenuButton(Rect portrait,Rect safe,GUIStyle title)
        {
            Rect rect=HomeMenuButtonRect(portrait,safe);
            bool oldEnabled=GUI.enabled;
            GUI.enabled=HomeInputReady&&homeLesson==HomeLesson.Off&&!settingsOpen&&!helpOpen&&!wheelOpen&&!achievementsOpen;
            DrawPieceIcon(rect,"menu_button_v2");
            if(DrawHomeHitButton(rect)){homeMenuOpen=!homeMenuOpen;PlaySweetTone(false);}
            GUI.enabled=oldEnabled;
        }
        private static void DrawRoundedHomeControl(Rect rect,Color fill,float cornerRatio=.30f,Color? border=null)
        {
            float radius=rect.height*cornerRatio;
            GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,border??new Color(.60f,.35f,.12f),0,radius);
            GUI.DrawTexture(InsetRect(rect,rect.height*.045f),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,fill,0,radius*.88f);
            GUI.DrawTexture(new Rect(rect.x+rect.width*.08f,rect.y+rect.height*.08f,rect.width*.84f,rect.height*.025f),
                Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(1,.96f,.82f,.45f),0,rect.height*.015f);
        }
        private bool DrawHomeHitButton(Rect rect,Vector2 inputOffset=default)
        {
            bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
            if(GUI.enabled&&uiTouchReleaseFrame==Time.frameCount&&rect.Contains(uiTouchStart-inputOffset)&&rect.Contains(uiTouchEnd-inputOffset)&&Vector2.Distance(uiTouchStart,uiTouchEnd)<Screen.width*.02f)
            {clicked=true;uiTouchReleaseFrame=-1;}
            if(clicked)uiTouchReleaseFrame=-1;
            return clicked;
        }
        private void DrawHomeDrawer(Rect portrait,Rect safe,GUIStyle title)
        {
            if(homeMenuSlideFrame!=Time.frameCount)
            {homeMenuSlideFrame=Time.frameCount;homeMenuSlide=Mathf.MoveTowards(homeMenuSlide,homeMenuOpen?1:0,Time.unscaledDeltaTime/ .20f);}
            if(homeMenuSlide<=.001f)return;
            Color old=GUI.color;bool enabled=GUI.enabled;GUI.enabled=true;
            float ease=Mathf.SmoothStep(0,1,homeMenuSlide);
            GUI.DrawTexture(portrait,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.08f,.05f,.025f,.28f*ease),0,0);
            float width=portrait.width*.55f,height=portrait.width*.64f;
            Rect panel=new(portrait.xMax-portrait.width*.115f-width+(1-ease)*width,
                HomeMenuButtonRect(portrait,safe).yMax+portrait.width*.020f,width,height);
            // The drawer travels behind the right wood rail, inside the map aperture.
            Rect aperture=new(portrait.x+portrait.width*.10f,portrait.y,portrait.width*.80f,portrait.height);
            GUI.BeginGroup(aperture);
            Rect local=new(panel.position-aperture.position,panel.size);
            DrawSimplePopupPanel(local);
            GUI.enabled=homeMenuOpen&&homeMenuSlide>.95f;
            float inset=width*.060f,rowHeight=height*.19f;
            Rect row=new(local.x+inset,local.y+height*.045f,width-inset*2,rowHeight);
            if(DrawHomeMenuRow(row,"HOW TO PLAY","?",new Color(.91f,.73f,.44f),title,false,aperture.position))
            {CloseHomeDrawer();helpPage=0;helpAnimatedPage=-1;helpOpen=TryLoadHelpGuide();}
            row.y+=rowHeight+height*.018f;
            if(DrawHomeMenuRow(row,"ACHIEVEMENTS","*",new Color(.91f,.73f,.44f),title,false,aperture.position))
            {CloseHomeDrawer();OpenAchievementsPopup();}
            float dividerY=row.yMax+height*.038f;
            DrawHomeMenuDivider(local,dividerY);
            row.y=dividerY+height*.030f;
            if(DrawHomeMenuRow(row,"SETTINGS",null,new Color(.14f,.64f,.70f),title,false,aperture.position))
            {CloseHomeDrawer();settingsOpen=true;PlayProductionSound(uiCatalog?.SettingsToggle);}
            dividerY=row.yMax+height*.038f;
            DrawHomeMenuDivider(local,dividerY);
            row.y=dividerY+height*.030f;
            if(DrawHomeMenuRow(row,"DEVELOPER",null,new Color(.56f,.77f,.64f),title,true,aperture.position))
            {CloseHomeDrawer();OpenDeveloperPage();}
            GUI.EndGroup();
            Rect toggle=HomeMenuButtonRect(portrait,safe);
            bool touch=uiTouchReleaseFrame==Time.frameCount;
            Vector2 point=touch?uiTouchStart:Event.current.mousePosition;
            if(homeMenuOpen&&!panel.Contains(point)&&!toggle.Contains(point)&&MenuBackgroundTapped(portrait))
            {homeMenuOpen=false;uiTouchReleaseFrame=-1;if(Event.current.type==EventType.MouseUp)Event.current.Use();}
            GUI.enabled=enabled;GUI.color=old;
        }
        private static void DrawHomeMenuDivider(Rect panel,float y)
        {
            GUI.DrawTexture(new Rect(panel.x+panel.width*.09f,y,panel.width*.82f,panel.height*.0035f),
                Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.68f,.49f,.27f,.5f),0,0);
        }
        private bool DrawHomeMenuRow(Rect rect,string label,string glyph,Color fill,GUIStyle title,bool developerIcon,Vector2 inputOffset)
        {
            DrawRoundedHomeControl(rect,fill);
            Rect icon=new(rect.x+rect.height*.14f,rect.y+rect.height*.16f,rect.height*.68f,rect.height*.68f);
            GUIStyle style=new(title){fontSize=Mathf.RoundToInt(rect.height*.29f),normal={textColor=new Color(.30f,.17f,.07f)}};
            if(developerIcon)DrawDeveloperIcon(InsetRect(icon,icon.width*.05f));
            else if(label=="SETTINGS")DrawArt(icon,uiCatalog.TealSettingsButton??uiCatalog.SettingsIcon);
            else if(label=="ACHIEVEMENTS")DrawArt(icon,uiCatalog.StarFilled);
            else DrawUiLabel(icon,glyph,new GUIStyle(style){fontSize=Mathf.RoundToInt(icon.height*.68f)});
            DrawUiLabel(new Rect(icon.xMax+rect.height*.08f,rect.y,rect.xMax-icon.xMax-rect.height*.19f,rect.height),label,style,true);
            if(DrawHomeHitButton(rect,inputOffset)){PlaySweetTone(false);return true;}return false;
        }
    }
}
