using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        [Serializable] private sealed class TutorialPreviewConfig { public bool Enabled=true; }
        private enum TutorialStage { Off, ReplayPrompt, Message, Move, Place, SelectTool, Rotate, Feedback, Unlock, Outro }
        private enum LessonAction { Message, Match, Activate, Unlock, SelectTool, Rotate, Place, Combine, Outro }
        private sealed class LessonStep
        {
            internal LessonAction Action;
            internal string Text;
            internal TutorialMovePlanner.Goal Goal;
            internal ToolKind Tool;
            internal LessonStep(LessonAction action,string text,TutorialMovePlanner.Goal goal=TutorialMovePlanner.Goal.Match,ToolKind tool=ToolKind.Ray)
            {Action=action;Text=text;Goal=goal;Tool=tool;}
        }
        private TutorialStage tutorialStage;
        private int tutorialLevel,tutorialStepIndex;
        private bool tutorialDirect,tutorialAdvancePending,tutorialOwnToolRevealed;
        private float tutorialHintAt;
        private Match3BoardModel.Cell tutorialFrom,tutorialTo,tutorialComboFirst,tutorialComboSecond;
        private readonly List<Match3BoardModel.Cell> tutorialLitCells=new();
        private readonly List<LessonStep> tutorialSteps=new();
        private readonly int[] tutorialTrialStock=new int[3];
        private readonly Rect[] tutorialToolRects=new Rect[3];
        private Texture2D tutorialHand;
        private bool tutorialPreviewEnabled;
        private int tutorialConsumedPointerFrame=-1;
        private LessonStep CurrentLesson=>tutorialStepIndex>=0&&tutorialStepIndex<tutorialSteps.Count?tutorialSteps[tutorialStepIndex]:null;
        private bool TutorialRunning=>screen==AppScreen.Playing&&tutorialStage!=TutorialStage.Off;
        private bool TrainingToolsLocked=>TrainingLevelPolicy.ToolsLocked(activeLevel?.Id,tutorialPreviewEnabled,highestUnlockedLevel+1);
        private bool IsTrainingToolLocked(ToolKind kind)
        {
            if(TrainingLevelPolicy.ToolLocked(activeLevel?.Id,tutorialPreviewEnabled,kind,highestUnlockedLevel+1))return true;
            return TutorialRunning&&!tutorialOwnToolRevealed&&((tutorialLevel==5&&kind==ToolKind.Solvent)||(tutorialLevel==8&&kind==ToolKind.Magnet));
        }
        private int TutorialToolStock(ToolKind kind)=>toolProgress.Stock(kind)+tutorialTrialStock[(int)kind];
        private bool SpendTutorialTool(ToolKind kind)
        {if(tutorialTrialStock[(int)kind]>0){tutorialTrialStock[(int)kind]--;return true;}return toolProgress.TrySpend(kind);}
        private bool TutorialAllowsToolSelection(ToolKind kind)=>(!TutorialRunning||Time.unscaledTime>=tutorialFadeAt+TutorialPresentationRules.IntroFade)&&tutorialConsumedPointerFrame!=Time.frameCount&&(!TutorialRunning||((tutorialStage==TutorialStage.Place||tutorialStage==TutorialStage.SelectTool)&&CurrentLesson.Tool==kind));
        private void ReadTutorialPreviewConfig()
        {
            var config=Resources.Load<TextAsset>("WorkshopMatch/tutorial_preview");
            tutorialPreviewEnabled=config!=null&&JsonUtility.FromJson<TutorialPreviewConfig>(config.text).Enabled;
        }
        private const string TutorialDoneKey="WorkshopMatch.CompletedTutorials";
        private float tutorialGiftAt=-100f;
        private ToolKind tutorialGiftKind;
        private bool IsTutorialGiftPanel=>CurrentLesson!=null&&
            ((tutorialStage==TutorialStage.Unlock&&CurrentLesson.Text.Contains("kredisi birik"))||
             (tutorialLevel==9&&tutorialStepIndex==0&&tutorialStage==TutorialStage.Message));
        private void DrawTutorialGiftIcons(Rect panel,Rect portrait,int lineCount,GUIStyle title)
        {
            float elapsed=Time.unscaledTime-tutorialGiftAt;
            if(elapsed<0||elapsed>=3.2f)return;
            bool pair=tutorialLevel==9&&tutorialStepIndex==0;
            float fade=Mathf.Clamp01((elapsed-2f)/1.2f);
            Color old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*(1-Mathf.SmoothStep(0,1,fade)));
            float size=portrait.width*.085f, group=portrait.width*.16f, gap=portrait.width*.045f;
            float width=pair?group*2+gap:group;
            float y=panel.y+portrait.width*(TutorialPresentationRules.TopPadding+lineCount*TutorialPresentationRules.TextPitch+
                TutorialPresentationRules.PromptGap+TutorialPresentationRules.PromptHeight+.025f)-fade*portrait.width*.015f;
            for(int i=0;i<(pair?2:1);i++)
            {
                ToolKind kind=pair?(i==0?ToolKind.Solvent:ToolKind.Magnet):tutorialGiftKind;
                Rect icon=new Rect(panel.center.x-width*.5f+i*(group+gap),y,size,size);
                DrawPieceIcon(icon,kind==ToolKind.Ray?"rocket":kind==ToolKind.Solvent?"hammer":"magnet");
                DrawUiLabel(new Rect(icon.xMax+portrait.width*.007f,icon.y+size*.08f,group-size,size*.55f),"+1",
                    new GUIStyle(title){fontSize=Mathf.RoundToInt(size*.45f),normal={textColor=new Color(.42f,.23f,.08f)}});
            }
            GUI.color=old;
        }
        private void MarkTutorialCompleted(){if(tutorialLevel>=0&&tutorialLevel<10){PlayerPrefs.SetInt(TutorialDoneKey,PlayerPrefs.GetInt(TutorialDoneKey,0)|(1<<tutorialLevel));PlayerPrefs.Save();}}
        private void BeginTutorialPreview(bool replay=false)
        {
            tutorialStage=TutorialStage.Off;boardPointer.Reset();tutorialSteps.Clear();Array.Clear(tutorialTrialStock,0,3);
            tutorialLevel=activeLevel==null?-1:Array.IndexOf(LevelRepository.PrototypeLevelIds,activeLevel.Id);
            tutorialOwnToolRevealed=false;tutorialStepIndex=0;tutorialAdvancePending=false;
            if(!tutorialPreviewEnabled||tutorialLevel<0||tutorialLevel>9)return;
            if(!replay&&((PlayerPrefs.GetInt(TutorialDoneKey,0)&(1<<tutorialLevel))!=0||tutorialLevel<highestUnlockedLevel))
            {tutorialStage=TutorialStage.ReplayPrompt;return;}
            tutorialVisibleAt=Time.unscaledTime+TutorialPresentationRules.IntroDelay;
            BuildTutorialCourse();EnterTutorialStep();
        }
        private void BuildTutorialCourse()
        {
            void Say(string text)=>tutorialSteps.Add(new LessonStep(LessonAction.Message,text));
            void Match(TutorialMovePlanner.Goal goal,string text)=>tutorialSteps.Add(new LessonStep(LessonAction.Match,text,goal));
            void Use(TutorialMovePlanner.Goal goal,string text)=>tutorialSteps.Add(new LessonStep(LessonAction.Activate,text,goal));
            void Unlock(ToolKind tool,string text)=>tutorialSteps.Add(new LessonStep(LessonAction.Unlock,text,tool:tool));
            void Place(ToolKind tool,string text)=>tutorialSteps.Add(new LessonStep(LessonAction.Place,text,tool:tool));
            switch(tutorialLevel+1)
            {
                case 1:
                    Say("Aynı renkten üç şekeri eşleştir.\nYan yana duran şekerleri değiştirerek\nbir sıra oluştur.");Match(TutorialMovePlanner.Goal.Match,"Bu şekeri yanındaki şekere kaydır.\nAynı renkleri yan yana getir.");break;
                case 2:
                    Say("Makine parçalarında iki kat pas var.\nYanlarında iki kez eşleştirerek\nparçaları temizle.");Match(TutorialMovePlanner.Goal.Bolt,"Parçanın yanında\nüç şekeri eşleştir.");Say("Pas tamamen temizlenince\nparça yukarı çıkar ve\nsütunundaki şekerleri temizler.");break;
                case 3:
                    Say("Aynı renkten dört şekeri\ndüz bir sırada eşleştirerek\nhavai fişek oluşturabilirsin.");Match(TutorialMovePlanner.Goal.Rocket,"Dört şekeri\naynı sırada birleştir.");Say("Fişek hazır! Yanındaki bir şekerle\nyerini değiştirerek çalıştır.\nYönündeki satır veya sütunu temizler.");Use(TutorialMovePlanner.Goal.Rocket,"Fişeği yanındaki şekere kaydırman,\nyönündeki satırı veya sütunu temizler.");break;
                case 4:
                    Unlock(ToolKind.Ray,"Fişek aracı artık açık!\nTahtada oluşturduğun her fişek\nsana 1 fişek kredisi kazandırır.");Unlock(ToolKind.Ray,"4 fişek kredisi biriktiğinde\nkullanabileceğin 1 fişek kazanırsın.\nİlk fişeğin ücretsiz.");tutorialSteps.Add(new LessonStep(LessonAction.SelectTool,"Alttaki fişek aracına dokun.\nTahtaya bir fişek yerleştireceğiz.",tool:ToolKind.Ray));tutorialSteps.Add(new LessonStep(LessonAction.Rotate,"Sarı döndürme tuşuna dokun.\nFişeğin yönü yatay veya dikey olur.",tool:ToolKind.Ray));Place(ToolKind.Ray,"Şimdi tahtadaki herhangi bir\nnormal şekere dokunarak\nfişeği oraya yerleştir.");Say("Yerleştirmek hamle harcamaz.\nÇalıştırmak için yanındaki şekere kaydır.");Use(TutorialMovePlanner.Goal.Rocket,"Yerleştirdiğin fişeği\nyanındaki şekere kaydır.");break;
                case 5:
                    Say("Yağ, şekerlerin altındaki\nzeminde kalır. Temizlemek için\ntam üstünde eşleştirme yap.");Match(TutorialMovePlanner.Goal.Oil,"Yağın üstündeki\nşekerleri eşleştir.");break;
                case 6:
                    Say("Aynı renkten dört şekeri\n2x2 kare halinde birleştir.\nTahtada bir çekiç oluşur.");Match(TutorialMovePlanner.Goal.Hammer,"Bu şekeri kaydır.\nDört şekeri 2x2 karede birleştir.");Say("Çekiç hazır! Soldaki şekerle\nyerini değiştirerek çalıştır.");Use(TutorialMovePlanner.Goal.Hammer,"Çekici yanındaki şekere kaydırman,\n3x3 alanı temizler.");Unlock(ToolKind.Solvent,"Çekiç aracı artık açık!\nTahtada oluşturduğun her çekiç\nsana 1 çekiç kredisi kazandırır.");Unlock(ToolKind.Solvent,"4 çekiç kredisi biriktiğinde\nkullanabileceğin 1 çekiç kazanırsın.\nİlk çekicin ücretsiz.");break;
                case 7:
                    Say("Yağ bloğunun kabuğunu kırmak\niçin yanında eşleştirme yap.\nAltındaki yağı ayrıca temizlemelisin.");Match(TutorialMovePlanner.Goal.Frozen,"Yağ bloğunun yanında\nşekerleri eşleştir.");Say("Kabuk kırılınca altındaki yağ ortaya çıkar.");Match(TutorialMovePlanner.Goal.Oil,"Kalan yağın üstünde\neşleştirme yap.");break;
                case 8:
                    Say("Motorun yanındaki her eşleşme\ndolum çubuğunu bir adım ilerletir.\nÇubuğu doldurup motoru çalıştır.");Match(TutorialMovePlanner.Goal.Motor,"Motorun yanında\nşekerleri eşleştir.");Say("Çubuk dolduğunda motor patlar\nve çevresindeki şekerleri temizler.\nBölümü bitirmek için diğer\nhedefleri de tamamlamalısın.");break;
                case 9:
                    Say("Aynı renkten beş şekeri\ndüz bir sırada birleştirerek\nbir mıknatıs oluştur.");Match(TutorialMovePlanner.Goal.Magnet,"Beş şekeri\naynı sırada birleştir.");Say("Mıknatısı yanındaki bir şekere kaydırman,\ntahtadaki aynı renkteki tüm şekerleri temizler.");Use(TutorialMovePlanner.Goal.Magnet,"Mıknatısı bu şekere kaydır.\nTemizlenecek rengi sen seçersin.");Unlock(ToolKind.Magnet,"Mıknatıs aracı artık açık!\nOluşturduğun her mıknatıs\nsana 1 mıknatıs kredisi verir.");Unlock(ToolKind.Magnet,"5 mıknatıs kredisi biriktiğinde\nkullanabileceğin 1 mıknatıs kazanırsın.\nİlk mıknatısın ücretsiz.");break;
                case 10:
                    tutorialOwnToolRevealed=true;tutorialTrialStock[(int)ToolKind.Magnet]=1;tutorialTrialStock[(int)ToolKind.Solvent]=1;
                    Say("İki gücü birbiriyle değiştirerek\nbirlikte çalıştırabilirsin.\nBu deneme için sana bir mıknatıs\nve bir çekiç verdik.");Place(ToolKind.Magnet,"Mıknatıs aracına dokun.\nİşaretli şekere yerleştir.");Place(ToolKind.Solvent,"Çekiç aracına dokun.\nMıknatısın yanına yerleştir.");tutorialSteps.Add(new LessonStep(LessonAction.Combine,"Mıknatısı çekice kaydırman,\nbir renkteki şekerleri çekiçlere dönüştürür\nve hepsini birlikte çalıştırır."));Say("Fişek, çekiç ve mıknatısı\nbirleştirerek daha güçlü hamleler yap.\nDiğer birleşimleri de dene!");break;
            }
            if(tutorialLevel==0)tutorialSteps.Add(new LessonStep(LessonAction.Outro,"Bölümü bitirmek için\n10 hamle yap."));
            else if(tutorialLevel==1)tutorialSteps.Add(new LessonStep(LessonAction.Outro,"Üstteki hedefleri tamamla,\nsonraki bölümü aç."));
        }
        private void SkipLevelTutorial()
        {
            tutorialConsumedPointerFrame=Time.frameCount;uiTouchReleaseFrame=-1;
            MarkTutorialCompleted();EndTutorialPreview();
        }
        private void EndTutorialPreview()
        {
            tutorialStage=TutorialStage.Off;boardPointer.Reset();tutorialOwnToolRevealed=true;Array.Clear(tutorialTrialStock,0,3);
            activeTool=null;ClearSelection();ResetHintTimer();
        }
        private void AdvanceTutorialStep(){tutorialStepIndex++;EnterTutorialStep();}
        private void EnterTutorialStep()
        {
            if(CurrentLesson==null){MarkTutorialCompleted();EndTutorialPreview();return;}
            tutorialFadeAt=Mathf.Max(Time.unscaledTime,tutorialVisibleAt);
            tutorialAdvancePending=false;boardPointer.Reset();ClearSelection();tutorialLitCells.Clear();if(CurrentLesson.Action!=LessonAction.Rotate&&CurrentLesson.Action!=LessonAction.Place)activeTool=null;
            switch(CurrentLesson.Action)
            {
                case LessonAction.Message:
                    if(tutorialLevel==9&&tutorialStepIndex==0)tutorialGiftAt=tutorialFadeAt+TutorialPresentationRules.IntroFade;
                    tutorialStage=TutorialStage.Message;return;
                case LessonAction.Outro:tutorialStage=TutorialStage.Outro;return;
                case LessonAction.Unlock:
                    tutorialOwnToolRevealed=true;
                    tutorialGiftAt=-100f;
                    if(CurrentLesson.Text.Contains("kredisi birik")&&toolProgress.GrantTutorialGiftOnce(CurrentLesson.Tool)){tutorialGiftAt=tutorialFadeAt+TutorialPresentationRules.IntroFade;tutorialGiftKind=CurrentLesson.Tool;}
                    tutorialStage=TutorialStage.Unlock;return;
                case LessonAction.SelectTool:tutorialStage=TutorialStage.SelectTool;return;
                case LessonAction.Rotate:tutorialStage=TutorialStage.Rotate;return;
                case LessonAction.Place:
                    if(TutorialToolStock(CurrentLesson.Tool)==0)tutorialTrialStock[(int)CurrentLesson.Tool]=1;
                    if(tutorialLevel==9)
                    {
                        if(CurrentLesson.Tool==ToolKind.Magnet)
                        {
                            bool found=false;foreach(var pair in TutorialMovePlanner.Pairs(model)){tutorialComboFirst=pair.a;tutorialComboSecond=pair.b;found=true;break;}
                            if(!found){ShowTutorialUnavailable();return;}
                        }
                        tutorialFrom=CurrentLesson.Tool==ToolKind.Magnet?tutorialComboFirst:tutorialComboSecond;
                    }
                    else
                    {
                        bool found=false;foreach(var pair in TutorialMovePlanner.Pairs(model)){tutorialFrom=pair.a;found=true;break;}
                        if(!found){ShowTutorialUnavailable();return;}
                    }
                    tutorialTo=tutorialFrom;tutorialLitCells.Add(tutorialFrom);tutorialStage=TutorialStage.Place;return;
                default:PrepareTutorialMove();return;
            }
        }
        private void ShowTutorialUnavailable()
        {
            Debug.LogWarning($"Tutorial example unavailable: {activeLevel?.Id} step {tutorialStepIndex}. Board preserved.");
            tutorialSteps.RemoveRange(tutorialStepIndex,tutorialSteps.Count-tutorialStepIndex);
            tutorialSteps.Add(new LessonStep(LessonAction.Outro,"Uygun hamleleri keşfet,\nhedefleri tamamla!"));tutorialStage=TutorialStage.Outro;
        }
        private void PrepareTutorialMove()
        {
            if(tutorialLevel==6&&CurrentLesson.Action==LessonAction.Match&&CurrentLesson.Goal==TutorialMovePlanner.Goal.Oil&&TutorialMovePlanner.FindActivation(model,TutorialMovePlanner.Goal.Hammer,out _,out _))
            {CurrentLesson.Action=LessonAction.Activate;CurrentLesson.Goal=TutorialMovePlanner.Goal.Hammer;CurrentLesson.Text="Oluşan çekici yanındaki şekere kaydır.\n3x3 alandaki şekerleri ve açık\nyağı temizler, yağ kabuklarını kırar.";}
            bool found;
            if(CurrentLesson.Action==LessonAction.Combine)
            {tutorialFrom=tutorialComboFirst;tutorialTo=tutorialComboSecond;tutorialDirect=true;found=model.GetSpecial(tutorialFrom.X,tutorialFrom.Y)==SpecialKind.Magnet&&model.GetSpecial(tutorialTo.X,tutorialTo.Y)==SpecialKind.Solvent;}
            else if(CurrentLesson.Action==LessonAction.Activate)
            {tutorialDirect=true;found=TutorialMovePlanner.FindActivation(model,CurrentLesson.Goal,out tutorialFrom,out tutorialTo);}
            else found=TutorialMovePlanner.Find(model,CurrentLesson.Goal,out tutorialFrom,out tutorialTo,out tutorialDirect);
            if(!found){ShowTutorialUnavailable();return;}
            if(tutorialLevel==6&&CurrentLesson.Action==LessonAction.Activate)
            {
                foreach(var neighbour in new[]{new Match3BoardModel.Cell(tutorialFrom.X+1,tutorialFrom.Y),new Match3BoardModel.Cell(tutorialFrom.X-1,tutorialFrom.Y),new Match3BoardModel.Cell(tutorialFrom.X,tutorialFrom.Y+1),new Match3BoardModel.Cell(tutorialFrom.X,tutorialFrom.Y-1)})
                    if(neighbour.X>=0&&neighbour.Y>=0&&neighbour.X<model.Width&&neighbour.Y<model.Height&&model.GetContent(neighbour)==CellContent.Candy&&model.GetSpecial(neighbour.X,neighbour.Y)==SpecialKind.None&&model.GetColor(neighbour.X,neighbour.Y)==2){tutorialTo=neighbour;break;}
            }
            tutorialLitCells.Clear();tutorialLitCells.Add(tutorialFrom);tutorialLitCells.Add(tutorialTo);
            if(CurrentLesson.Action==LessonAction.Match)
            {
                var result=model.CreatePreviewCopy().TrySwap(tutorialFrom,tutorialTo);
                if(result.Steps.Count>0)
                {
                    foreach(var cell in result.Steps[0].MatchedCells)if(!tutorialLitCells.Contains(cell)&&tutorialLitCells.Count<12)tutorialLitCells.Add(cell);
                    foreach(var cell in result.Steps[0].HitTargets)if(!tutorialLitCells.Contains(cell)&&tutorialLitCells.Count<14)tutorialLitCells.Add(cell);
                }
            }
            tutorialStage=TutorialStage.Move;tutorialHintAt=Time.unscaledTime+.55f;boardPointer.Reset();ClearSelection();ResetHintTimer();
        }
        private void RecordTutorialMove(Match3BoardModel.SwapResult result)
        {
            if(!TutorialRunning||tutorialStage!=TutorialStage.Move||!result.Accepted)return;
            tutorialAdvancePending=CurrentLesson.Action!=LessonAction.Match||TutorialMovePlanner.Matches(result,CurrentLesson.Goal);
            tutorialStage=TutorialStage.Feedback;boardPointer.Reset();
        }
        private void RecordTutorialToolSelected(){if(TutorialRunning&&tutorialStage==TutorialStage.SelectTool)AdvanceTutorialStep();}
        private void RecordTutorialRotation(){if(TutorialRunning&&tutorialStage==TutorialStage.Rotate)AdvanceTutorialStep();}
        private void RecordTutorialPlacement(Match3BoardModel.Cell cell)
        {if(TutorialRunning&&tutorialStage==TutorialStage.Place&&(tutorialLevel==3||cell.Equals(tutorialFrom))){tutorialFrom=cell;tutorialAdvancePending=true;tutorialStage=TutorialStage.Feedback;}}
        private bool TutorialAllowsCell(Match3BoardModel.Cell cell)
        {
            if(tutorialConsumedPointerFrame==Time.frameCount||(TutorialRunning&&Time.unscaledTime<tutorialFadeAt+TutorialPresentationRules.IntroFade))return false;
            if(!TutorialRunning)return true;
            return tutorialStage==TutorialStage.Place?(tutorialLevel==3?model.GetContent(cell)==CellContent.Candy&&model.GetSpecial(cell.X,cell.Y)==SpecialKind.None:cell.Equals(tutorialFrom))&&activeTool==CurrentLesson.Tool:
                tutorialStage==TutorialStage.Move&&(cell.Equals(tutorialFrom)||cell.Equals(tutorialTo));
        }
        private void UpdateTutorialPreviewControls()
        {
            if(!TutorialRunning||tutorialStage==TutorialStage.ReplayPrompt||settingsOpen||Time.unscaledTime<tutorialFadeAt+TutorialPresentationRules.IntroFade)return;
            if(tutorialStage==TutorialStage.Feedback&&!resolving)
            {tutorialConsumedPointerFrame=Time.frameCount;if(tutorialAdvancePending)AdvanceTutorialStep();else PrepareTutorialMove();return;}
            bool pressed=false;Vector2 pointer=Vector2.zero;
            if(Touchscreen.current!=null&&(Touchscreen.current.primaryTouch.press.isPressed||Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {var touch=Touchscreen.current.primaryTouch;pointer=touch.position.ReadValue();pressed=touch.press.wasPressedThisFrame;}
            else if(Mouse.current!=null){pointer=Mouse.current.position.ReadValue();pressed=Mouse.current.leftButton.wasPressedThisFrame;}
            pointer.y=Screen.height-pointer.y;Rect portrait=GetPortraitRect();
            if(pressed&&!GameplaySettingsButtonRect(portrait).Contains(pointer)&&(tutorialStage==TutorialStage.Message||tutorialStage==TutorialStage.Unlock||tutorialStage==TutorialStage.Outro)&&!LockedToolbarPointer(pointer))
            {if(Time.unscaledTime<tutorialGiftAt+2f&&IsTutorialGiftPanel)return;
                tutorialConsumedPointerFrame=Time.frameCount;if(tutorialStage==TutorialStage.Outro){MarkTutorialCompleted();EndTutorialPreview();}else AdvanceTutorialStep();return;}
            if(tutorialStage!=TutorialStage.Move||resolving||hintAnimating||tutorialConsumedPointerFrame==Time.frameCount)return;
            if(!pressed&&!boardPointer.Tracking&&selectedCell==null&&Time.unscaledTime>=tutorialHintAt)
            {
                tutorialHintAt=Time.unscaledTime+4.2f;
                StartCoroutine(AnimateHint(tutorialFrom,tutorialTo));
            }
        }
        private Rect TutorialCellRect(Match3BoardModel.Cell cell,bool home=false)
        {
            if(!views.TryGetValue(cell,out var view)||boardCamera==null)return Rect.zero;
            Vector3 position=home?view.Root.parent.TransformPoint(view.HomePosition):view.Root.position;
            Vector3 p=boardCamera.WorldToScreenPoint(position);
            float size=GameplayLayout(GetPortraitRect()).Aperture.width/boardWidth;
            return new Rect(p.x-size*.5f,Screen.height-p.y-size*.5f,size,size);
        }
        private static string TutorialFontText(string text) => text.Replace('Ü','U').Replace('ü','u').Replace('Ö','O').Replace('ö','o')
            .Replace('Ç','C').Replace('ç','c').Replace('Ş','S').Replace('ş','s').Replace('Ğ','G').Replace('ğ','g').Replace('İ','I').Replace('ı','i');
        private static string WrapTutorialText(string text)
        {
            var lines=new List<string>();
            foreach(string paragraph in TutorialFontText(GameLanguage.Text(text).Replace('\n',' ')).Split('\n'))
            {
                string line="";foreach(string word in paragraph.Split(' '))
                {if(line.Length>0&&line.Length+word.Length+1>31){lines.Add(line);line="";}line+=(line.Length==0?"":" ")+word;}
                lines.Add(line);
            }
            return string.Join("\n",lines);
        }
        private void DrawTutorialReplayPrompt(Rect portrait,GUIStyle title)
        {
            DrawTrainingVeil(portrait,Rect.zero,0);
            string[] lines=WrapTutorialText("Öğreticiyi tekrar görmek ister misin?").Split('\n');
            float height=TutorialPanelHeight(portrait,lines.Length,false)+portrait.width*.13f;
            Rect panel=new Rect(portrait.center.x-portrait.width*.38f,GetSafeGuiRect(portrait).center.y-height*.5f,portrait.width*.76f,height);
            DrawTutorialPanelCopy(panel,portrait,lines,title,false);
            float y=panel.y+portrait.width*(TutorialPresentationRules.TopPadding+lines.Length*TutorialPresentationRules.TextPitch+.02f);
            var style=new GUIStyle(title){fontSize=Mathf.RoundToInt(portrait.width*.035f)};
            if(DrawTextureButton(new Rect(panel.x+panel.width*.13f,y,panel.width*.33f,portrait.width*.075f),new GUIContent("EVET"),uiCatalog?.ActionButton,null,null,style))
            {tutorialConsumedPointerFrame=Time.frameCount;BeginTutorialPreview(true);}
            if(DrawTextureButton(new Rect(panel.x+panel.width*.54f,y,panel.width*.33f,portrait.width*.075f),new GUIContent("HAYIR"),uiCatalog?.ActionButton,null,null,style))
            {tutorialConsumedPointerFrame=Time.frameCount;EndTutorialPreview();}
        }
        private void DrawTutorialPreview(Rect portrait,GUIStyle title,GUIStyle body)
        {
            if(tutorialStage==TutorialStage.ReplayPrompt&&TutorialRunning){DrawTutorialReplayPrompt(portrait,title);return;}
            if(!TutorialRunning||tutorialStage==TutorialStage.Feedback||resolving||CurrentLesson==null||Time.unscaledTime<tutorialFadeAt)return;
            Color tutorialColor=GUI.color;GUI.color=new Color(tutorialColor.r,tutorialColor.g,tutorialColor.b,tutorialColor.a*TutorialFade);
            bool move=tutorialStage==TutorialStage.Move||tutorialStage==TutorialStage.Place;
            bool action=move||tutorialStage==TutorialStage.SelectTool||tutorialStage==TutorialStage.Rotate;
            bool focusTool=tutorialStage==TutorialStage.Unlock||tutorialStage==TutorialStage.Place||tutorialStage==TutorialStage.SelectTool||tutorialStage==TutorialStage.Rotate;
            Rect focus=focusTool?tutorialToolRects[(int)CurrentLesson.Tool]:Rect.zero;
            if(tutorialStage==TutorialStage.Rotate)focus=tutorialRotationRect;
            if(tutorialStage==TutorialStage.Place&&tutorialLevel==3)focus=GameplayLayout(portrait).Aperture;
            DrawTrainingVeil(portrait,focusTool?focus:Rect.zero,portrait.width*.035f);
            Rect aperture=GameplayLayout(portrait).Aperture;
            Rect litBounds=move?TutorialCellRect(tutorialFrom,true):Rect.zero;
            if(move||tutorialStage==TutorialStage.SelectTool||tutorialStage==TutorialStage.Rotate)
            {
                foreach(var cell in tutorialLitCells)
                {
                    if(!views.TryGetValue(cell,out var view))continue;
                    var content=model.GetContent(cell);if(content==CellContent.Motor)continue;
                    Rect home=TutorialCellRect(cell,true);litBounds=Rect.MinMaxRect(Mathf.Min(litBounds.x,home.x),Mathf.Min(litBounds.y,home.y),Mathf.Max(litBounds.xMax,home.xMax),Mathf.Max(litBounds.yMax,home.yMax));
                    Rect art=TutorialCellRect(cell);float scale=view.Root.localScale.x/CellSize;art=new Rect(art.center-art.size*.47f*scale,art.size*.94f*scale);
                    var special=model.GetSpecial(cell.X,cell.Y);
                    string key=content==CellContent.FrozenGrease?"frozen":content==CellContent.Bolt?(view.PartVariant>=2?"bolt_screw":"bolt_nut"):
                        special==SpecialKind.Magnet?"magnet":special==SpecialKind.Solvent?"hammer":special==SpecialKind.RayHorizontal||special==SpecialKind.RayVertical?"rocket":"candy_"+model.GetColor(cell.X,cell.Y);
                    
                    if(content==CellContent.Bolt)
                    {
                        int rust=model.GetObstacleState(cell.X,cell.Y);bool screw=view.PartVariant>=2;
                        Texture2D bolt=PartTexture(view.PartVariant,rust)??(screw?(rust>=2?uiCatalog?.ScrewRust2:rust==1?uiCatalog?.ScrewRust1:uiCatalog?.ScrewClean):(rust>=2?uiCatalog?.NutRust2:rust==1?uiCatalog?.NutRust1:uiCatalog?.NutClean));
                        DrawArt(art,bolt);continue;
                    }
                    Matrix4x4 matrix=GUI.matrix;if(special==SpecialKind.RayHorizontal)GUIUtility.RotateAroundPivot(90,art.center);DrawPieceIcon(art,key);GUI.matrix=matrix;
                }
                if(CurrentLesson.Goal==TutorialMovePlanner.Goal.Motor&&CurrentLesson.Action==LessonAction.Match)
                {
                    Rect motor=Rect.zero;
                    for(int yCell=0;yCell<model.Height;yCell++)for(int xCell=0;xCell<model.Width;xCell++)if(model.GetContent(xCell,yCell)==CellContent.Motor)
                    {
                        Rect cell=TutorialCellRect(new Match3BoardModel.Cell(xCell,yCell),true);
                        if(motor.width==0)motor=cell;else motor=Rect.MinMaxRect(Mathf.Min(motor.x,cell.x),Mathf.Min(motor.y,cell.y),Mathf.Max(motor.xMax,cell.xMax),Mathf.Max(motor.yMax,cell.yMax));
                    }
                    if(motor.width>0)
                    {
                        DrawPieceIcon(motor,"motor");
                        var objective=model.GetObjectives();float fill=Mathf.Clamp01((float)objective.MotorHits/Mathf.Max(1,objective.MotorRequired));
                        Color color=GUI.color;GUI.color=new Color(1,.73f,.16f);GUI.DrawTexture(LayoutRect(motor,.33f,.302f,.34f*fill,.035f),Texture2D.whiteTexture);GUI.color=color;
                        litBounds=Rect.MinMaxRect(Mathf.Min(litBounds.x,motor.x),Mathf.Min(litBounds.y,motor.y),Mathf.Max(litBounds.xMax,motor.xMax),Mathf.Max(litBounds.yMax,motor.yMax));
                    }
                }
                tutorialHand??=Resources.Load<Texture2D>("WorkshopMatch/tutorial_hand_v1");
                if(tutorialHand!=null)
                {
                    Vector2 target=tutorialStage==TutorialStage.Rotate||tutorialStage==TutorialStage.SelectTool||(tutorialStage==TutorialStage.Place&&!activeTool.HasValue)?focus.center:TutorialCellRect(tutorialFrom).center;
                    if(tutorialStage==TutorialStage.Move&&CurrentLesson.Action!=LessonAction.Match&&!hintAnimating)
                    {
                        float cycle=Mathf.Repeat(Time.unscaledTime,2.4f);float progress=cycle<1.2f?Mathf.SmoothStep(0,1,cycle/1.2f):0;
                        target=Vector2.Lerp(TutorialCellRect(tutorialFrom).center,TutorialCellRect(tutorialTo).center,progress);
                    }
                    // Keep the palm outside the highlighted match; the fingertip stays on the moving candy.
                    int horizontalWeight=0;
                    if(tutorialStage==TutorialStage.Move&&CurrentLesson.Action==LessonAction.Match)
                        foreach(var cell in tutorialLitCells)horizontalWeight+=cell.X-tutorialFrom.X;
                    bool mirror=horizontalWeight>0;
                    Matrix4x4 handMatrix=GUI.matrix;
                    if(mirror)GUI.matrix=handMatrix*Matrix4x4.TRS(new Vector3(target.x*2,0,0),Quaternion.identity,new Vector3(-1,1,1));
                    float size=portrait.width*.21f;GUI.DrawTexture(new Rect(target.x-size*.17f,target.y-size*.14f,size,size),tutorialHand,ScaleMode.ScaleToFit,true);
                    GUI.matrix=handMatrix;
                }
            }
            string text=!tutorialDirect&&tutorialStage==TutorialStage.Move&&CurrentLesson.Action==LessonAction.Match?"Önce bu şekeri\nyanındaki şekere kaydır.":CurrentLesson.Text;
            string[] lines=WrapTutorialText(text).Split('\n');
            bool giftPanel=IsTutorialGiftPanel;
            float height=TutorialPanelHeight(portrait,lines.Length,!action)+(giftPanel?portrait.width*.14f:0);float y=aperture.y+aperture.height*.16f;
            if(move){y=litBounds.y-height-portrait.width*.035f;if(y<GetSafeGuiRect(portrait).y+portrait.width*.06f)y=litBounds.yMax+portrait.width*.035f;}
            if(focusTool&&!move)y=focus.y-height-portrait.width*.05f;
            float panelWidth=portrait.width*(giftPanel?.80f:.74f);
            Rect panel=new Rect(portrait.center.x-panelWidth*.5f,Mathf.Clamp(y,GetSafeGuiRect(portrait).y+portrait.width*.04f,portrait.yMax-height-portrait.width*.10f),panelWidth,height);
            DrawTutorialPanelCopy(panel,portrait,lines,title,!action);
            if(giftPanel)DrawTutorialGiftIcons(panel,portrait,lines.Length,title);
            GUI.color=tutorialColor;
        }
    }
}
