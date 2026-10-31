using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private enum HomeLesson { Off, SelectTwo, OpenMap, SwipeMap, Back, Play, WheelOpen, WheelSpin, WheelPrize, WheelDone }
        private HomeLesson homeLesson;
        private const string HomeLessonDoneKey="WorkshopMatch.HomeLessonDone";
        private const string WheelUnlockedKey="WorkshopMatch.WheelUnlocked";
        private const string WheelLessonDoneKey="WorkshopMatch.WheelLessonDone";
        private bool nextLevelTransition;
        private int newlyUnlockedLevel=-1;
        private float homeLessonAt;
        private int transitionLockedLevel=-1,homeLessonFrame=-1;
        private readonly Rect[] homeNodeRects=new Rect[20];
        private Rect homePlayRect,homeMapRect,homeBackRect,homeWheelRect,homeSpinRect,homeRouteRect;
        private readonly MenuMapTapGesture homeMapTap = new();
        private bool WheelUnlocked=>PlayerPrefs.GetInt(WheelUnlockedKey,0)==1;
        private bool WheelTeaching=>homeLesson>=HomeLesson.WheelOpen;
        private bool HomeInputReady=>!nextLevelTransition&&!wheelRewardAnimating&&homeLessonFrame!=Time.frameCount&&(homeLesson==HomeLesson.Off||Time.unscaledTime-homeLessonAt>=.4f);
        private bool HomeAllowsNode(int index)=>HomeInputReady&&(homeLesson==HomeLesson.Off||(homeLesson==HomeLesson.SelectTwo&&index==1));
        private void SetHomeLesson(HomeLesson lesson){if(lesson==HomeLesson.SwipeMap){tutorialMapSwipes=0;mapSwipe.Reset();}homeLesson=lesson;homeLessonAt=Time.unscaledTime;homeLessonFrame=Time.frameCount;uiTouchReleaseFrame=-1;}
        private void UpdateHomeMapLessonInput()
        {
            if(screen!=AppScreen.Menu || (homeLesson!=HomeLesson.OpenMap&&homeLesson!=HomeLesson.SelectTwo) || !HomeInputReady ||
                settingsOpen || helpOpen || wheelOpen || HomeDrawerBlocking)
            { homeMapTap.Reset(); return; }
            var touch=Touchscreen.current?.primaryTouch;
            bool touchActive=touch!=null&&(touch.press.isPressed||touch.press.wasReleasedThisFrame);
            Vector2 point;
            bool pressed,held,released;
            if(touchActive)
            {
                point=touch.position.ReadValue();pressed=touch.press.wasPressedThisFrame;
                held=touch.press.isPressed;released=touch.press.wasReleasedThisFrame;
            }
            else if(Mouse.current!=null)
            {
                point=Mouse.current.position.ReadValue();pressed=Mouse.current.leftButton.wasPressedThisFrame;
                held=Mouse.current.leftButton.isPressed;released=Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else { homeMapTap.Reset(); return; }
            point.y=Screen.height-point.y;
            Rect target=homeLesson==HomeLesson.SelectTwo?homeNodeRects[1]:homeMapRect;
            if(homeMapTap.Sample(target,point,pressed,held,released,GetPortraitRect().width*.02f))
            {
                uiTouchReleaseFrame=-1;
                if(homeLesson==HomeLesson.SelectTwo)SelectMenuLevel(1);else OpenMap();
            }
        }
        private void ReturnToProgressHome()
        {
            int completed=activeLevel==null?-1:Array.IndexOf(LevelRepository.PrototypeLevelIds,activeLevel.Id);
            if(completed==0){pendingMenuLevelIndex=-1;ReturnToMenu();SelectMenuLevel(0);homeVisualFocus=0;homeFocusVelocity=0;
                if(PlayerPrefs.GetInt(HomeLessonDoneKey,0)==0)SetHomeLesson(HomeLesson.SelectTwo);}
            else if(completed==9){pendingMenuLevelIndex=-1;ReturnToMenu();SelectMenuLevel(9);BeginWheelLesson();}
            else ReturnToMenu();
        }
        private void BeginWheelLesson()
        {
            if(!WheelUnlocked||PlayerPrefs.GetInt(WheelLessonDoneKey,0)==1)return;
            toolProgress.GrantWheelIntroGiftOnce();
            SetHomeLesson(HomeLesson.WheelOpen);
        }
        private void NextLevelFromResult(int next)
        {
            if(next==1||next==10){ReturnToProgressHome();return;}
            StartCoroutine(AnimateNextLevelHome(next));
        }
        private IEnumerator AnimateNextLevelHome(int next)
        {
            // ReturnToMenu stops coroutines; perform its cleanup before starting this routine.
            toolProgress?.RollbackAttemptCredits();
            ClearTransientEffects();ClearSelection();activeTool=null;settingsOpen=helpOpen=wheelOpen=false;
            resolving=hintAnimating=false;pendingMenuLevelIndex=-1;homeLesson=HomeLesson.Off;
            bool newlyOpened=next==newlyUnlockedLevel;
            nextLevelTransition=true;transitionLockedLevel=newlyOpened?next:-1;
            SelectMenuLevel(next-1);homeVisualFocus=next-1;homeFocusVelocity=0;
            screen=AppScreen.Menu;SetPresentationMode();
            if(newlyOpened)
            {
                yield return new WaitForSecondsRealtime(.75f);
                transitionLockedLevel=-1;PlayProductionSound(uiCatalog?.BoltPing);
                yield return new WaitForSecondsRealtime(1.0f);
            }
            else yield return new WaitForSecondsRealtime(.15f);
            newlyUnlockedLevel=-1;
            SelectMenuLevel(next);PlayProductionSound(uiCatalog?.MapSelect);
            float started=Time.unscaledTime;
            while(Time.unscaledTime-started<1.25f||Mathf.Abs(homeVisualFocus-next)>.018f)yield return null;
            yield return new WaitForSecondsRealtime(.45f);
            nextLevelTransition=false;uiTouchReleaseFrame=-1;
            // Once onboarding is over, leave the next level selected for the player's Play tap.
            if(next<10)StartLevel();
        }
        private void CompleteHomeLesson()
        {
            PlayerPrefs.SetInt(HomeLessonDoneKey,1);PlayerPrefs.Save();SetHomeLesson(HomeLesson.Off);
        }
        private void DrawHomeLesson(Rect portrait,GUIStyle title)
        {
            if(homeLesson==HomeLesson.Off||nextLevelTransition)return;
            Rect focus=homeLesson switch
            {
                HomeLesson.SelectTwo=>homeNodeRects[1],HomeLesson.OpenMap=>homeMapRect,
                HomeLesson.SwipeMap=>new Rect(portrait.x+portrait.width*.2f,portrait.y+portrait.height*.34f,portrait.width*.6f,portrait.height*.32f),
                HomeLesson.Back=>homeBackRect,HomeLesson.Play=>homePlayRect,
                HomeLesson.WheelOpen=>homeWheelRect,HomeLesson.WheelSpin=>homeSpinRect,_=>Rect.zero
            };
            if(homeLesson==HomeLesson.WheelPrize)return; // The prize card carries the explanation.
            Color original=GUI.color;GUI.color=new Color(original.r,original.g,original.b,original.a*Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-homeLessonAt)/.4f)));
            DrawTrainingVeil(portrait,focus,portrait.width*.04f);
            string text=homeLesson switch
            {
                HomeLesson.SelectTwo=>"İlk bölüm tamamlandı!\nİkinci bölümün kilidi açıldı.\nSeçmek için 2'ye dokun.",
                HomeLesson.OpenMap=>"Diğer bölümlere bakmak için\nharitanın boş bir yerine dokun.",
                HomeLesson.SwipeMap=>"Haritayı yukarı veya aşağı\nkaydırabilirsin.",
                HomeLesson.Back=>"Geri tuşuna dokunduğunda\nana ekrana dönersin.",
                HomeLesson.Play=>"İkinci bölüm seçili.\nBaşlamak için PLAY'e dokun.",
                HomeLesson.WheelOpen=>"Ödül çarkına dokun.",
                HomeLesson.WheelSpin=>"Kalan hamlelerin kasada birikir.\n10 hamleyle çarkı çevir ve araç kazan.",
                _=>"Öğretici tamamlandı!\nOynamaya devam edebilirsin."
            };
            string[] lines=WrapTutorialText(text).Split('\n');float h=TutorialPanelHeight(portrait,lines.Length,false);
            float y=focus.y-h-portrait.width*.04f;
            if(y<GetSafeGuiRect(portrait).y+portrait.width*.02f)y=focus.yMax+portrait.width*.04f;
            if(homeLesson==HomeLesson.OpenMap||homeLesson==HomeLesson.SwipeMap)y=GetSafeGuiRect(portrait).y+portrait.width*.24f;
            Rect panel=new Rect(portrait.center.x-portrait.width*.38f,Mathf.Clamp(y,portrait.y+portrait.width*.06f,portrait.yMax-h-portrait.width*.10f),portrait.width*.76f,h);
            if(homeLesson==HomeLesson.WheelDone)
                panel.y=GetSafeGuiRect(portrait).center.y-(h+portrait.width*.13f)*.5f;
            DrawTutorialPanelCopy(panel,portrait,lines,title,false);
            var textStyle=new GUIStyle(title){fontSize=Mathf.RoundToInt(portrait.width*.041f),alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.42f,.23f,.08f)}};
            tutorialHand??=Resources.Load<Texture2D>("WorkshopMatch/tutorial_hand_v1");
            Vector2 target=focus.center;
            if(homeLesson==HomeLesson.OpenMap)target=new Vector2(focus.x+focus.width*.12f,focus.y+focus.height*.68f);
            if(homeLesson==HomeLesson.SwipeMap)target.y+=Mathf.Sin(Time.unscaledTime*2.4f)*focus.height*.3f;
            float size=portrait.width*.21f;
            if(tutorialHand!=null&&focus.width>0)GUI.DrawTexture(new Rect(target.x-size*.17f,target.y-size*.14f,size,size),tutorialHand,ScaleMode.ScaleToFit,true);
            if(homeLesson==HomeLesson.WheelDone)
            {
                Rect done=new Rect(panel.x+panel.width*.12f,panel.yMax+portrait.width*.02f,panel.width*.76f,portrait.width*.09f);
                if(DrawTextureButton(done,new GUIContent("CONTINUE"),uiCatalog?.ActionButton,null,null,textStyle))
                {PlayerPrefs.SetInt(WheelLessonDoneKey,1);PlayerPrefs.Save();SetHomeLesson(HomeLesson.Off);}
            }
            GUI.color=original;
        }
    }
}
