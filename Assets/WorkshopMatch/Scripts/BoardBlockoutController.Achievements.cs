using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private AchievementTracker achievementTracker;
        private AchievementTracker Achievements
        {
            get
            {
                if(achievementTracker!=null)return achievementTracker;
                bool existing=PlayerPrefs.HasKey(AchievementProgressStore.SaveKey);
                achievementTracker=new AchievementTracker(AchievementProgressStore.Load());
                // Legacy progression only proves the levels before the furthest unlocked node.
                // It contains no evidence about stock use, production or the final level's win.
                if(!existing&&PlayerPrefs.GetInt("WorkshopMatch.TestProgressOverride",0)==0)
                    achievementTracker.ImportCompletedLevels(highestUnlockedLevel);
                AchievementProgressStore.Save(achievementTracker.Snapshot);
                return achievementTracker;
            }
        }
        private int selectedAchievement;
        private int[] achievementDisplayOrder=Array.Empty<int>();
        private float achievementScroll,achievementRowPitch,achievementContentHeight;
        private Rect achievementListRect;
        private bool achievementDragging,achievementDragMoved;
        private Vector2 achievementDragStart;
        private float achievementDragStartScroll;
        private void OpenAchievementsPopup()
        {
            achievementsOpen=true;achievementScroll=0;achievementDragging=false;selectedAchievement=0;
            for(int i=0;i<AchievementCatalog.All.Length;i++)
                if(Achievements.Progress(AchievementCatalog.All[i])<AchievementCatalog.All[i].Target)
                {selectedAchievement=i;break;}
        }
        private void UpdateAchievementScroll()
        {
            if(!achievementsOpen){achievementDragging=false;return;}
            Vector2 pointer=default;bool pressed=false,held=false,released=false;
            if(Touchscreen.current!=null&&(Touchscreen.current.primaryTouch.press.isPressed||Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                var touch=Touchscreen.current.primaryTouch;pointer=touch.position.ReadValue();
                pressed=touch.press.wasPressedThisFrame;held=touch.press.isPressed;released=touch.press.wasReleasedThisFrame;
            }
            else if(Mouse.current!=null)
            {
                pointer=Mouse.current.position.ReadValue();pressed=Mouse.current.leftButton.wasPressedThisFrame;
                held=Mouse.current.leftButton.isPressed;released=Mouse.current.leftButton.wasReleasedThisFrame;
                Vector2 guiPointer=new(pointer.x,Screen.height-pointer.y);
                if(achievementListRect.Contains(guiPointer))achievementScroll-=Mouse.current.scroll.ReadValue().y*.55f;
            }
            pointer.y=Screen.height-pointer.y;
            if(pressed&&achievementListRect.Contains(pointer))
            {achievementDragging=true;achievementDragMoved=false;achievementDragStart=pointer;achievementDragStartScroll=achievementScroll;}
            if(achievementDragging&&(held||released))
            {
                if(Vector2.Distance(pointer,achievementDragStart)>Screen.width*.015f)achievementDragMoved=true;
                if(achievementDragMoved)achievementScroll=achievementDragStartScroll+achievementDragStart.y-pointer.y;
                if(released)
                {
                    if(!achievementDragMoved&&achievementListRect.Contains(pointer)&&achievementRowPitch>0)
                    {
                        int index=Mathf.FloorToInt((pointer.y-achievementListRect.y+achievementScroll)/achievementRowPitch);
                        if(index>=0&&index<achievementDisplayOrder.Length){selectedAchievement=achievementDisplayOrder[index];PlaySweetTone(false);}
                    }
                    achievementDragging=false;uiTouchReleaseFrame=-1;
                }
            }
            achievementScroll=Mathf.Clamp(achievementScroll,0,Mathf.Max(0,achievementContentHeight-achievementListRect.height));
        }
        private void BeginAchievementRound()
        {Achievements.BeginRound(Array.IndexOf(LevelRepository.PrototypeLevelIds,activeLevel?.Id)+1);}
        private void RecordAchievementTurn(Match3BoardModel.SwapResult result,SpecialKind first=SpecialKind.None,SpecialKind second=SpecialKind.None,bool stockPlacement=false)
        {
            if(!result.Accepted)return;
            if(stockPlacement)Achievements.ObserveStockPlacement(true);
            else Achievements.ObserveSwap(true,first,second);
            int motors=0;
            foreach(var step in result.Steps)if(step.MotorExplosion)motors++;
            Achievements.ObserveRoundTotals(model.ProducedRays,model.ProducedSolvents,model.ProducedMagnets,model.GetObjectives().CollectedBolts,motors);
            if(model.State==RoundState.Won)Achievements.CompleteRound(model.MovesRemaining);
            AchievementProgressStore.Save(Achievements.Snapshot);
        }
        private void DrawAchievementsPopup(Rect portrait,GUIStyle title)
        {
            bool enabled=GUI.enabled;GUI.enabled=true;
            Color color=GUI.color;
            GUI.DrawTexture(portrait,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.10f,.07f,.04f,.65f),0,0);
            Rect safe=GetSafeGuiRect(portrait);
            float width=portrait.width*.90f,height=Mathf.Min(safe.height-portrait.width*.045f,width*1.85f);
            Rect panel=new(safe.center.x-width*.5f,safe.center.y-height*.5f,width,height);
            DrawSimplePopupPanel(panel);
            GUIStyle heading=new(title){fontSize=Mathf.RoundToInt(width*.049f),normal={textColor=new Color(.30f,.17f,.07f)}};
            GUIStyle text=new(heading){fontSize=Mathf.RoundToInt(width*.036f)};
            DrawUiLabel(LayoutRect(panel,.08f,.035f,.70f,.055f),"ACHIEVEMENTS",heading);
            if(DrawTextureButton(new Rect(panel.xMax-width*.14f,panel.y+width*.022f,width*.115f,width*.115f),GUIContent.none,uiCatalog.CloseButton,null,null,title))achievementsOpen=false;
            int total=AchievementCatalog.All.Length,done=Achievements.CompletedCount;
            DrawUiLabel(LayoutRect(panel,.075f,.105f,.22f,.035f),$"{done} / {total}",text);
            DrawAchievementBar(LayoutRect(panel,.31f,.108f,.60f,.025f),(float)done/total);
            selectedAchievement=Mathf.Clamp(selectedAchievement,0,total-1);
            if(achievementDisplayOrder.Length!=total)achievementDisplayOrder=new int[total];
            int displayIndex=0;
            // Keep catalog order inside both groups; completed entries always follow unfinished ones.
            for(int group=0;group<2;group++)
                for(int index=0;index<total;index++)
                {
                    var definition=AchievementCatalog.All[index];
                    bool complete=Achievements.Progress(definition)>=definition.Target;
                    if(complete==(group==1))achievementDisplayOrder[displayIndex++]=index;
                }
            Rect list=LayoutRect(panel,.075f,.160f,.825f,.605f);
            float rowHeight=width*.18f;
            achievementListRect=list;achievementRowPitch=rowHeight;achievementContentHeight=total*rowHeight;
            achievementScroll=Mathf.Clamp(achievementScroll,0,Mathf.Max(0,achievementContentHeight-list.height));
            GUI.BeginGroup(list);
            try
            {
                for(int row=0;row<total;row++)
                {
                    int index=achievementDisplayOrder[row];
                    float y=row*rowHeight-achievementScroll;
                    if(y+rowHeight<0||y>list.height)continue;
                    var item=AchievementCatalog.All[index];int progress=Achievements.Progress(item);
                    Rect card=new(0,y,list.width,rowHeight*.92f);
                    bool selected=index==selectedAchievement;
                    GUI.DrawTexture(card,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,
                        selected?new Color(.04f,.46f,.53f):new Color(.81f,.66f,.43f),0,rowHeight*.13f);
                    GUI.DrawTexture(InsetRect(card,width*(selected?.008f:.003f)),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,
                        selected?new Color(.70f,.92f,.94f):progress==item.Target?new Color(.88f,.96f,.85f):new Color(1,.96f,.85f),0,rowHeight*.12f);
                    Rect icon=new(card.x+rowHeight*.045f,card.y+rowHeight*.045f,rowHeight*.82f,rowHeight*.82f);
                    DrawAchievementIcon(icon,item);
                    float labelX=icon.xMax+rowHeight*.08f;
                    DrawUiLabel(new Rect(labelX,card.y+card.height*.04f,card.xMax-labelX-width*.03f,card.height*.57f),
                        GameLanguage.English?item.TitleEn:item.TitleTr,new GUIStyle(text){alignment=TextAnchor.MiddleLeft,fontSize=Mathf.RoundToInt(width*.047f),normal={textColor=selected?new Color(.03f,.32f,.39f):new Color(.30f,.17f,.07f)}});
                    DrawUiLabel(new Rect(card.xMax-card.width*.25f,card.y+card.height*.55f,card.width*.23f,card.height*.24f),$"{progress}/{item.Target}",
                        new GUIStyle(text){fontSize=Mathf.RoundToInt(width*.033f)});
                    DrawAchievementBar(new Rect(labelX,card.y+card.height*.81f,card.xMax-labelX-width*.025f,card.height*.07f),(float)progress/item.Target);
                }
            }
            finally{GUI.EndGroup();}
            Rect track=LayoutRect(panel,.915f,.160f,.009f,.605f);
            GUI.DrawTexture(track,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.82f,.71f,.54f),0,track.width*.5f);
            float thumbHeight=track.height*Mathf.Min(1,list.height/achievementContentHeight);
            float scrollFraction=achievementScroll/Mathf.Max(1,achievementContentHeight-list.height);
            GUI.DrawTexture(new Rect(track.x,track.y+(track.height-thumbHeight)*scrollFraction,track.width,thumbHeight),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.17f,.72f,.74f),0,track.width*.5f);
            Rect detail=LayoutRect(panel,.065f,.790f,.87f,.170f);
            DrawRoundedHomeControl(detail,new Color(1,.95f,.82f),.12f);
            var chosen=AchievementCatalog.All[selectedAchievement];int value=Achievements.Progress(chosen);
            float detailIconSize=Mathf.Min(detail.height*.91f,detail.width*.265f);
            DrawAchievementIcon(new Rect(detail.x+detail.width*.025f,detail.center.y-detailIconSize*.5f,detailIconSize,detailIconSize),chosen);
            DrawUiLabel(LayoutRect(detail,.30f,.035f,.65f,.24f),GameLanguage.English?chosen.TitleEn:chosen.TitleTr,
                new GUIStyle(text){fontSize=Mathf.RoundToInt(width*.045f),alignment=TextAnchor.MiddleLeft});
            string body=WrapHelpCopy(GameLanguage.English?chosen.BodyEn:chosen.BodyTr,26);
            int lines=body.Split('\n').Length;
            DrawUiLabel(LayoutRect(detail,.30f,.30f,.65f,.46f),body,
                new GUIStyle(text){fontSize=Mathf.RoundToInt(Mathf.Min(width*.040f,detail.height*.46f/Mathf.Max(1,lines)/1.25f)),alignment=TextAnchor.MiddleLeft});
            DrawAchievementBar(LayoutRect(detail,.30f,.82f,.47f,.065f),(float)value/chosen.Target);
            DrawUiLabel(LayoutRect(detail,.79f,.76f,.17f,.20f),$"{value}/{chosen.Target}",new GUIStyle(text){fontSize=Mathf.RoundToInt(width*.040f)});
            GUI.enabled=enabled;GUI.color=color;
        }
        private void DrawAchievementIcon(Rect rect,AchievementDefinition item)
        {
            if(item.Icon=="star")DrawArt(rect,uiCatalog.StarFilled);
            else DrawPieceIcon(rect,item.Icon);
        }
        private static string DifficultyText(AchievementDifficulty difficulty)=>GameLanguage.English
            ?difficulty==AchievementDifficulty.Easy?"EASY":difficulty==AchievementDifficulty.Medium?"MEDIUM":"HARD"
            :difficulty==AchievementDifficulty.Easy?"KOLAY":difficulty==AchievementDifficulty.Medium?"ORTA":"ZOR";
        private static void DrawAchievementBar(Rect rect,float fraction)
        {
            GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.70f,.56f,.34f),0,rect.height*.5f);
            Rect inner=InsetRect(rect,Mathf.Max(1,rect.height*.13f));
            GUI.DrawTexture(inner,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.88f,.79f,.60f),0,rect.height*.5f);
            inner.width*=Mathf.Clamp01(fraction);
            if(inner.width>0)GUI.DrawTexture(inner,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,new Color(.17f,.72f,.74f),0,rect.height*.5f);
        }
    }
}
