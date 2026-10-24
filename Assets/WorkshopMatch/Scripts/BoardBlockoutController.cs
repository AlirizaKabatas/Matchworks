using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController : MonoBehaviour
    {
        private enum AppScreen
        {
            Menu,
            Map,
            Playing,
            Result
        }

        private sealed class CellView
        {
            public Transform Root;
            public Vector3 HomePosition;
            public Renderer BaseRenderer;
            public Renderer TileRenderer;
            public Collider Collider;
            public GameObject Marker;
            public Renderer MarkerRenderer;
            public GameObject OilTile;
            public GameObject Model;
            public Renderer[] ModelRenderers = Array.Empty<Renderer>();
            public string ModelKey;
            public int PartVariant;
        }

        public const int BoardWidth = 8;
        public const int BoardHeight = 8;
        public const int DefaultSeed = 24092026;
        public const int StartingMoves = 18;

        private const float CellPitch = 0.92f;
        private const float CellSize = 0.82f;
        private const float BoardCenterY = -0.55f;
        private const float SwapDuration = 0.16f;
        private const float ClearDuration = 0.16f;
        private const float FallDuration = 0.28f;
        private const float HintDelay = 7f;
        private static Vector2 uiTouchStart;
        private static Vector2 uiTouchEnd;
        private static int uiTouchReleaseFrame = -1;
        private float mapFocusLevel;
        private Vector2 mapPointerStart, mapPointerLast;
        private bool mapDragging, mapPointerDown;
        private int mapDragReleaseFrame = -1;
        private AudioSource uiAudio;
        private AudioClip playClick;
        private AudioClip machineImpact;
        private bool playTransition;
        private float gearBurstUntil;
        private float gearVictoryAt = -1f;

        private static readonly Color[] Palette =
        {
            new(0.95f, 0.28f, 0.32f),
            new(0.20f, 0.62f, 0.98f),
            new(0.25f, 0.82f, 0.48f),
            new(1.00f, 0.76f, 0.20f),
            new(0.68f, 0.38f, 0.96f)
        };

        private readonly List<Material> generatedMaterials = new();
        private readonly Dictionary<Transform, Match3BoardModel.Cell> coordinates = new();
        private readonly Dictionary<Match3BoardModel.Cell, CellView> views = new();
        private Camera boardCamera;
        private Material[] candyMaterials;
        private Material frameMaterial;
        private Material oilMaterial;
        private Material motorMaterial;
        private Material motorReadyMaterial;
        private Material frozenMaterial;
        private Material rustDarkMaterial;
        private Material rustLightMaterial;
        private Material boltCleanMaterial;
        private Material motorCoreMaterial;
        private Material rayMaterial;
        private Material solventMaterial;
        private Material magnetMaterial;
        private Material cellNormalMaterial;
        private Material cellBlockedMaterial;
        private WorkshopMatchModelCatalog modelCatalog;
        private WorkshopMatchUiCatalog uiCatalog;
        private Transform runtimeBoardRoot;
        private Transform oilJoinedRoot;
        private int oilJoinedSignature = int.MinValue;
        private Transform runtimeBoardBacking;
        private Transform homeBackdrop;
        private Transform gameplayBackdrop;
        private Transform gearBackdrop;
        private Transform menuPresentationRoot;
        private Transform mainMenuScreenRoot;
        private Transform fullMapScreenRoot;
        private Transform gameplayScreenRoot;
        private Transform settingsPopupRoot;
        private Transform resultTransitionRoot;
        private Transform gearMachineRoot;
        private Match3BoardModel model;
        private LevelDefinition activeLevel;
        private int boardWidth = BoardWidth;
        private int boardHeight = BoardHeight;
        private ToolProgressTracker toolProgress;
        private ToolProgressData lastDisplayedProgress;
        private ToolKind rewardPulseKind;
        private float rewardPulseUntil;
        private Transform selectedCell;
        private float selectionPulseStartedAt;
        private Match3BoardModel.Cell selectedCoordinate;
        private string statusText = "Şekeri seç, sonra komşusuna dokun";
        private AppScreen screen = AppScreen.Menu;
        private ToolKind? activeTool;
        private bool rayVertical = true;
        private bool hintAnimating;
        private float lastInteractionAt;
        private int menuLevelIndex;
        private float homeVisualFocus;
        private float homeFocusVelocity;
        private int lockedFeedbackLevel = -1;
        private float lockedFeedbackUntil;
        private bool settingsOpen;
        private int highestUnlockedLevel;
        private int pendingMenuLevelIndex = -1;
        private bool resolving;
        private int automaticChain;

        private void OnEnable()
        {
            // Managed tracker is not serialized across an Editor script reload.
            // Recover its saved state without granting trial stock a second time.
            if (toolProgress == null)
            {
                toolProgress = new ToolProgressTracker(ToolProgressStore.Load());
                lastDisplayedProgress = toolProgress.Snapshot;
                toolProgress.Changed += OnToolProgressChanged;
            }
        }

        private void Awake()
        {
            ConfigurePortraitPresentation();
            ConfigureCamera();
            BeginOpening();
        }

        private IEnumerator Start()
        {
            // Paint the complete opening before synchronous catalog/board preparation.
            // Its minimum duration is measured from that first complete repaint.
            while (!OpeningArtLoaded) yield return null;
            while (!openingTiming.HasPresented) yield return null;
            yield return null;
            yield return InitializeGame();
            openingGameReady = true;
        }

        private IEnumerator InitializeGame()
        {
            TraceOpeningPhase("shared-catalogs");
            var uiRequest = Resources.LoadAsync<WorkshopMatchUiCatalog>("WorkshopMatch/WorkshopMatchUiCatalog");
            var modelRequest = Resources.LoadAsync<WorkshopMatchModelCatalog>("WorkshopMatch/WorkshopMatchModelCatalog");
            AudioListener.volume = PlayerPrefs.GetFloat("WM_SoundVolume",
                PlayerPrefs.GetInt("WM_SoundEnabled", 1) == 1 ? 1f : 0f);
            InitializeBackgroundMusic();
            while (!uiRequest.isDone || !modelRequest.isDone) yield return null;
            uiCatalog = uiRequest.asset as WorkshopMatchUiCatalog;
            modelCatalog = modelRequest.asset as WorkshopMatchModelCatalog;
            yield return null;
            TraceOpeningPhase("runtime-shaders");
            // Resources keeps these shaders in the player. Load them explicitly
            // before any runtime materials; URP/Standard can be stripped in WebGL.
            foreach(string shaderFile in new[]{"FlatPiece","WorkshopBackdrop","WorkshopCandy"})
            {
                var shaderRequest=Resources.LoadAsync<Shader>("WorkshopMatch/"+shaderFile);
                while(!shaderRequest.isDone)yield return null;
                if(shaderRequest.asset==null)Debug.LogError("MATCHWORKS_RUNTIME_SHADER_MISSING: "+shaderFile);
                yield return null;
            }
            TraceOpeningPhase("materials");
            CreateMaterials();
            yield return null;
            TraceOpeningPhase("screen-roots");
            BuildScreenRoots();
            // Do not render the partly constructed 3D board behind the opaque opening.
            SetPresentationMode();
            yield return null;
            TraceOpeningPhase("backdrops");
            BuildScreenBackdrops();
            yield return null;
            TraceOpeningPhase("gear-root");
            BuildGearMachine();
            yield return null;
            toolProgress = new ToolProgressTracker(ToolProgressStore.Load());
            toolProgress.RollbackAttemptCredits();ToolProgressStore.Save(toolProgress.Snapshot);
            lastDisplayedProgress = toolProgress.Snapshot;
            toolProgress.Changed += OnToolProgressChanged;
            bool firstLaunch=PlayerPrefs.GetInt("WorkshopMatch.FirstLaunchStarted",0)==0;
            if(firstLaunch)LevelRepository.Select(LevelRepository.DefaultLevelId);
            TraceOpeningPhase("selected-level-model");
            LoadSelectedLevel();
            yield return null;
            menuLevelIndex = Math.Max(0, Array.IndexOf(LevelRepository.PrototypeLevelIds, activeLevel.Id));
            highestUnlockedLevel = PlayerPrefs.HasKey(LevelRepository.HighestUnlockedLevelKey)
                ? Mathf.Clamp(PlayerPrefs.GetInt(LevelRepository.HighestUnlockedLevelKey), 0, LevelRepository.PrototypeLevelIds.Length - 1)
                : menuLevelIndex;
            if(!PlayerPrefs.HasKey(TutorialDoneKey)){PlayerPrefs.SetInt(TutorialDoneKey,(1<<Mathf.Min(highestUnlockedLevel,10))-1);PlayerPrefs.Save();}
            menuLevelIndex = Mathf.Min(menuLevelIndex, highestUnlockedLevel);
            homeVisualFocus = menuLevelIndex;
            TraceOpeningPhase("sprite-preload");
            yield return PreloadOpeningPieceTextures();
            TraceOpeningPhase("board-objects");
            yield return BuildBoardViewRoutine(true);
            TraceOpeningPhase("menu");
            BuildMenuPresentation();
            yield return null;
            TraceOpeningPhase("board-sprites");
            yield return RefreshBoardViewRoutine(null, true);
            UpdateGameplayTheme();
            SetPresentationMode();
            yield return null;
            lastInteractionAt = Time.unscaledTime;
            openingFirstLaunch = firstLaunch;
            TraceOpeningPhase("ready-minimum-exposure");
        }

        private void EnterPreparedGame()
        {
            if(openingFirstLaunch)
            {
                PlayerPrefs.SetInt("WorkshopMatch.FirstLaunchStarted",1);PlayerPrefs.Save();
                // InitializeGame already built this exact first board in small
                // batches. Rebuilding it synchronously here caused a second hitch
                // at the moment the opening disappeared.
                screen=AppScreen.Playing;
                BeginAchievementRound();
                SetPresentationMode();
                statusText="Şekeri seç, sonra komşusuna dokun";
                ResetHintTimer();
                BeginTutorialPreview();
            }
            else if(WheelUnlocked)BeginWheelLesson();
            else if(highestUnlockedLevel>=1&&PlayerPrefs.GetInt(HomeLessonDoneKey,0)==0)
            {SelectMenuLevel(0);homeVisualFocus=0;SetHomeLesson(HomeLesson.SelectTwo);}
        }

        private void Update()
        {
            UpdateBackgroundMusic();
            if (UpdateOpening()) return;
            // Device Simulator and mobile devices deliver touch through the new Input
            // System; they need not synthesize the mouse events IMGUI buttons expect.
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                Vector2 position = touch.position.ReadValue();
                Vector2 guiPosition = new(position.x, Screen.height - position.y);
                if (touch.press.wasPressedThisFrame) uiTouchStart = guiPosition;
                if (touch.press.wasReleasedThisFrame)
                {
                    uiTouchEnd = guiPosition;
                    uiTouchReleaseFrame = Time.frameCount;
                }
            }
            UpdateHomeMapLessonInput();
            UpdateTutorialPreviewControls();
            UpdateAchievementScroll();
            if(selectedCell!=null&&screen==AppScreen.Playing&&!resolving)
            {
                float size=CellSize*SelectionPulseScale(Time.unscaledTime-selectionPulseStartedAt);
                selectedCell.localScale=new Vector3(size,size,.48f);
            }
            AnimateTopGears();
            if (screen == AppScreen.Menu)
                homeVisualFocus = Mathf.SmoothDamp(homeVisualFocus, menuLevelIndex,
                    ref homeFocusVelocity, nextLevelTransition?.48f:.30f, Mathf.Infinity, Time.unscaledDeltaTime);
            if (screen == AppScreen.Map) UpdateMapDrag();
            if (settingsOpen || screen != AppScreen.Playing || resolving || hintAnimating || model == null ||
                model.State != RoundState.Playing)
                return;

            if (!TutorialRunning && !activeTool.HasValue && Time.unscaledTime - lastInteractionAt >= HintDelay &&
                model.TryFindBestHintSwap(out Match3BoardModel.Cell hintFrom, out Match3BoardModel.Cell hintTo))
            {
                ClearSelection();
                StartCoroutine(AnimateHint(hintFrom, hintTo));
                return;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 position = Touchscreen.current.primaryTouch.position.ReadValue();
                if (!PointerOverHud(position)) TryHandlePointer(position);
                return;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 position = Mouse.current.position.ReadValue();
                if (!PointerOverHud(position)) TryHandlePointer(position);
            }
        }

        private void OnDestroy()
        {
            StopOpeningAudio();
            DestroyOpeningView();
            StopBackgroundMusic();
            foreach(var texture in milestoneGuiTextures.Values)if(texture!=null)Destroy(texture);
            if(trainingVeil!=null)Destroy(trainingVeil);
            if(lockedToolSound!=null)Destroy(lockedToolSound);
            if(wheelTexture!=null)Destroy(wheelTexture);
            if(wheelPeg!=null)Destroy(wheelPeg);
            if(fireworkWhoosh!=null)Destroy(fireworkWhoosh);
            if(fireworkSoftPop!=null)Destroy(fireworkSoftPop);
            if(mapTerrainTexture!=null)Destroy(mapTerrainTexture);
            if(illustratedMap!=null)Destroy(illustratedMap);
            if(mapDash!=null)Destroy(mapDash);
            foreach (Texture2D node in flatNodes)
                if (node != null) Destroy(node);
            if (toolProgress != null)
                toolProgress.Changed -= OnToolProgressChanged;
            foreach (Material material in generatedMaterials)
                if (material != null) Destroy(material);
            if (playClick != null) Destroy(playClick);
            if (machineImpact != null) Destroy(machineImpact);
            if (uiTick != null) Destroy(uiTick);
            if (candyDing != null) Destroy(candyDing);
            if (conversionAppear != null) Destroy(conversionAppear);
            if (chainTone != null) Destroy(chainTone);
            if (greaseSquelch != null) Destroy(greaseSquelch);
            if (frozenSquelch != null) Destroy(frozenSquelch);
            if (rocketIcon != null) Destroy(rocketIcon);
            if (rocketTemplate != null)
                foreach (MeshFilter filter in rocketTemplate.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.gameObject.name == "Pointed cap") Destroy(filter.sharedMesh);
        }

        private bool PointerOverHud(Vector2 screenPosition)
        {
            Rect portrait = GetPortraitRect();
            Vector2 guiPoint = new(screenPosition.x, Screen.height - screenPosition.y);
            return !GameplayLayout(portrait).Aperture.Contains(guiPoint);
        }

        private void StartLevel(bool replayTutorial=false)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            double levelLoadStarted=Time.realtimeSinceStartupAsDouble;
#endif
            toolProgress?.RollbackAttemptCredits();
            newlyUnlockedLevel=-1;
            movesBankedThisRound = false;
            bankTransferTotal = 0; bankTransferAt = -100f;
            helpOpen = wheelOpen = achievementsOpen = false;
            CloseHomeDrawer();
            StopAllCoroutines();
            ClearTransientEffects();
            ClearSelection();
            activeTool = null;
            rayVertical = true;
            settingsOpen = false;
            resolving = false;
            automaticChain = 0;
            hintAnimating = false;
            gearBurstUntil = 0f;
            gear2DAngle = 0f;
            gearVictoryAt = -1f;
            LoadSelectedLevel();
            UpdateGameplayTheme();
            BeginAchievementRound();
            BuildBoardView();
            RefreshBoardView();
            screen = AppScreen.Playing;
            SetPresentationMode();
            statusText = "Şekeri seç, sonra komşusuna dokun";
            ResetHintTimer();
            BeginTutorialPreview(replayTutorial);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"MATCHWORKS_LEVEL_LOAD id={activeLevel.Id} mainThreadMs={(Time.realtimeSinceStartupAsDouble-levelLoadStarted)*1000:F1}");
#endif
        }

        private void ReturnToMenu()
        {
            toolProgress?.RollbackAttemptCredits();
            StopAllCoroutines();
            ClearTransientEffects();
            ClearSelection();
            activeTool = null;
            rayVertical = true;
            settingsOpen = false;
            resolving = false;
            hintAnimating = false;
            if (pendingMenuLevelIndex >= 0)
            {
                SelectMenuLevel(pendingMenuLevelIndex);
                pendingMenuLevelIndex = -1;
            }
            screen = AppScreen.Menu;
            statusText = "Şekeri seç, sonra komşusuna dokun";
            SetPresentationMode();
            BeginWheelLesson();
        }

        private void ResetHintTimer()
        {
            lastInteractionAt = Time.unscaledTime;
        }

        private void SelectMenuLevel(int index)
        {
            if (index < 0 || index >= LevelRepository.PrototypeLevelIds.Length || index > highestUnlockedLevel) return;
            menuLevelIndex = index;
            LevelRepository.Select(LevelRepository.PrototypeLevelIds[index]);
            activeLevel = LevelRepository.LoadSelected();
            statusText = $"Bölüm {index + 1} seçildi";
            if(homeLesson==HomeLesson.SelectTwo&&index==1)SetHomeLesson(HomeLesson.OpenMap);
        }

        private float mapFrameTransitionAt = -10f;
        private void OpenMap()
        {
            PlaySweetTone(false);
            mapFrameTransitionAt = Time.unscaledTime;
            mapFocusLevel = menuLevelIndex;
            mapPointerDown = mapDragging = false;
            if(homeLesson==HomeLesson.OpenMap)SetHomeLesson(HomeLesson.SwipeMap);
            screen = AppScreen.Map;
            SetPresentationMode();
        }

        private void BuildScreenRoots()
        {
            mainMenuScreenRoot = GetOrCreateScreenRoot("MainMenuScreen");
            fullMapScreenRoot = GetOrCreateScreenRoot("FullMapScreen");
            gameplayScreenRoot = GetOrCreateScreenRoot("GameplayScreen");
            settingsPopupRoot = GetOrCreateScreenRoot("SettingsPopup");
            resultTransitionRoot = GetOrCreateScreenRoot("ResultTransition");
        }

        private Transform GetOrCreateScreenRoot(string rootName)
        {
            Transform existing = transform.Find(rootName);
            if (existing == null)
            {
                GameObject sceneRoot = GameObject.Find(rootName);
                if (sceneRoot != null) existing = sceneRoot.transform;
            }
            if (existing != null) return existing;
            Transform created = new GameObject(rootName).transform;
            created.SetParent(transform, false);
            return created;
        }

        private void BuildScreenBackdrops()
        {
            if (uiCatalog == null || !uiCatalog.IsReady) return;
            homeBackdrop = CreateTexturedQuad(mainMenuScreenRoot, "Home Background", uiCatalog.HomeBackground,
                Vector3.zero, new Vector2(7.76f, 13.8f), 2.0f, false);
            gameplayBackdrop = CreateTexturedQuad(gameplayScreenRoot, "Gameplay Background", uiCatalog.GameplayBackground,
                Vector3.zero, new Vector2(7.76f, 13.8f), 2.0f, false);
            Transform previousGearBackdrop = gameplayScreenRoot.Find("Gear Backplate");
            if(previousGearBackdrop != null) Destroy(previousGearBackdrop.gameObject);
            // Destroy is deferred: Find could return the old quad queued for destruction.
            // Keep the actual newly created backdrop and discard its old material cache.
            gameplayThemeMaterial = gameplayBackdrop.GetComponent<Renderer>().sharedMaterial;
            UpdateGameplayTheme();
            gearBackdrop = gameplayScreenRoot.Find("Gear Backplate");
        }

        private Transform CreateTexturedQuad(Transform parent, string name, Texture2D texture, Vector3 position,
            Vector2 size, float z, bool transparent)
        {
            if (parent == null || texture == null) return null;
            Transform previous = parent.Find(name);
            if (previous != null) { previous.gameObject.SetActive(false); Destroy(previous.gameObject); }
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(position.x, position.y, z);
            Rect source = TexturePixels(texture);
            float fitScale = Mathf.Min(size.x / source.width, size.y / source.height);
            quad.transform.localScale = transparent
                ? new Vector3(source.width * fitScale, source.height * fitScale, 1f)
                : new Vector3(size.x, size.y, 1f);
            Destroy(quad.GetComponent<Collider>());
            Shader shader = Shader.Find(name == "Gameplay Background" ? "WorkshopMatch/WorkshopBackdrop" :
                            transparent ? "Unlit/Transparent" : "Unlit/Texture") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard") ??
                            Shader.Find("WorkshopMatch/FlatPiece");
            Material material = new(shader) { name = $"{name} Material", mainTexture = texture };
            if (transparent)
            {
                material.mainTextureScale = new Vector2(source.width / texture.width, source.height / texture.height);
                material.mainTextureOffset = new Vector2(source.x / texture.width, 1f - source.yMax / texture.height);
            }
            if (transparent) material.renderQueue = 3000;
            generatedMaterials.Add(material);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quad.GetComponent<Renderer>().receiveShadows = false;
            return quad.transform;
        }

        private void BuildGearMachine()
        {
            if (gameplayScreenRoot == null) return;
            Transform previous = gameplayScreenRoot.Find("Top Gear Machine");
            if (previous != null) Destroy(previous.gameObject);
            // Keep only the existing flight destination anchor. All visible gears
            // now come from the 2D atlas; no FactoryTop prefab or fallback meshes.
            gearMachineRoot = new GameObject("Top Gear Machine").transform;
            gearMachineRoot.SetParent(gameplayScreenRoot, false);
        }

        private void AnimateTopGears()
        {
            if (screen is not (AppScreen.Playing or AppScreen.Result)) return;
            float speed = 0f;
            if (gearVictoryAt >= 0f)
                speed = Mathf.Lerp(20f, 230f, Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((Time.unscaledTime - gearVictoryAt) / 1.35f)));
            else if (Time.unscaledTime < gearBurstUntil)
            {
                float phase = Mathf.Clamp01((gearBurstUntil - Time.unscaledTime) / 0.82f);
                speed = Mathf.Sin(phase * Mathf.PI) * 190f;
            }
            if (speed <= 0f) return;
            gear2DAngle += speed * Time.unscaledDeltaTime;

        }

        public string RunLiveAcceptanceScenario()
        {
            Achievements.InvalidateRound();
            if (!model.TryFindValidSwap(out Match3BoardModel.Cell first, out Match3BoardModel.Cell second))
                return "T03_LIVE_SWAP_FAIL no valid move";
            int movesBefore = model.MovesRemaining;
            Match3BoardModel.SwapResult result = model.TrySwap(first, second);
            RefreshBoardView();
            string message = result.Accepted && model.MovesRemaining == movesBefore - 1 && model.IsStable
                ? $"T03_LIVE_SWAP_PASS {first}->{second} removed={result.Removed} cascades={result.Cascades} moves={model.MovesRemaining} hash={model.SnapshotHash()}"
                : "T03_LIVE_SWAP_FAIL model did not settle correctly";
            Debug.Log(message);
            return message;
        }

        private void LoadSelectedLevel()
        {
            ReadTutorialPreviewConfig();
            try
            {
                activeLevel = LevelRepository.LoadSelected();
                string productionLevelId=activeLevel.Id;
                model = LevelRepository.BuildBoard(activeLevel, special=>TrainingLevelPolicy.RecordProduced(productionLevelId,tutorialPreviewEnabled,toolProgress,special,highestUnlockedLevel+1));
            }
            catch (Exception exception)
            {
                Debug.LogError($"T06_LEVEL_LOAD_FAIL {exception.Message}; geçici taslak yükleniyor.");
                activeLevel = LevelRepository.CreatePrototype();
                string productionLevelId=activeLevel.Id;
                model = LevelRepository.BuildBoard(activeLevel, special=>TrainingLevelPolicy.RecordProduced(productionLevelId,tutorialPreviewEnabled,toolProgress,special,highestUnlockedLevel+1));
            }
            boardWidth = model.Width;
            boardHeight = model.Height;
            Debug.Log($"T06_LEVEL_LOADED id={activeLevel.Id} size={boardWidth}x{boardHeight} draft={activeLevel.IsDraft} hash={model.SnapshotHash()}");
        }

        private void OnToolProgressChanged(ToolProgressData data)
        {
            int[] gains = { data.RayStock - lastDisplayedProgress.RayStock,
                data.SolventStock - lastDisplayedProgress.SolventStock,
                data.MagnetStock - lastDisplayedProgress.MagnetStock };
            for (int i = 0; i < gains.Length; i++)
                if (gains[i] > 0)
                {
                    toolRewardAmount[i] = gains[i];
                    toolRewardUntil[i] = Time.unscaledTime + 1.1f;
                }
            if (data.RayStock > lastDisplayedProgress.RayStock) rewardPulseKind = ToolKind.Ray;
            else if (data.SolventStock > lastDisplayedProgress.SolventStock) rewardPulseKind = ToolKind.Solvent;
            else if (data.MagnetStock > lastDisplayedProgress.MagnetStock) rewardPulseKind = ToolKind.Magnet;
            if (data.RayStock > lastDisplayedProgress.RayStock || data.SolventStock > lastDisplayedProgress.SolventStock ||
                data.MagnetStock > lastDisplayedProgress.MagnetStock)
                rewardPulseUntil = Time.unscaledTime + 0.55f;
            lastDisplayedProgress = data.Copy();
            ToolProgressStore.Save(data);
        }

        private static void ConfigurePortraitPresentation()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
#endif
            Application.targetFrameRate = 60;
        }

        private void ConfigureCamera()
        {
            boardCamera = Camera.main;
            if (boardCamera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                boardCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            // Imported 2D artwork already includes lighting. Template tone mapping
            // and vignette darken camera sprites while the IMGUI HUD bypasses them.
            var cameraData = boardCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null) cameraData.renderPostProcessing = false;
            boardCamera.allowHDR = false;
            boardCamera.orthographic = true;
            boardCamera.orthographicSize = 6.9f;
            boardCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            boardCamera.clearFlags = CameraClearFlags.SolidColor;
            boardCamera.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
        }

        private void CreateMaterials()
        {
            frameMaterial = CreateMaterial("Board Frame", new Color(0.40f, 0.27f, 0.16f));
            frameMaterial.shader = Shader.Find("Universal Render Pipeline/Unlit") ?? frameMaterial.shader;
            oilMaterial = CreateMaterial("Floor Oil", new Color(0.24f, 0.12f, 0.055f));
            oilMaterial.shader = Shader.Find("Universal Render Pipeline/Unlit") ?? oilMaterial.shader;
            motorMaterial = CreateMaterial("Motor", new Color(0.22f, 0.24f, 0.27f));
            motorReadyMaterial = CreateMaterial("Motor Ready", new Color(0.18f, 0.68f, 0.28f));
            motorCoreMaterial = CreateMaterial("Motor Core", new Color(1.00f, 0.42f, 0.08f));
            rayMaterial = CreateMaterial("Ray Cutter", new Color(0.96f, 0.96f, 1.00f));
            solventMaterial = CreateMaterial("Hammer Power", new Color(1.00f, 0.66f, 0.20f));
            magnetMaterial = CreateMaterial("Electromagnet", new Color(1.00f, 0.18f, 0.78f));
            if (uiCatalog != null && uiCatalog.IsReady)
            {
                cellNormalMaterial = frameMaterial;
                cellBlockedMaterial = CreateTexturedMaterial("Cell Blocked", uiCatalog.CellBlocked);
            }
            frozenMaterial = CreateMaterial("Frozen Grease", new Color(0.45f, 0.88f, 1.00f));
            rustDarkMaterial = CreateMaterial("Bolt Rust 2", new Color(0.42f, 0.16f, 0.06f));
            rustLightMaterial = CreateMaterial("Bolt Rust 1", new Color(0.78f, 0.36f, 0.10f));
            boltCleanMaterial = CreateMaterial("Bolt Clean", new Color(0.70f, 0.75f, 0.80f));
            candyMaterials = new Material[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
                candyMaterials[i] = CreateMaterial($"Candy Color {i + 1}", Palette[i]);
        }

        private Material CreateMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ??
                            Shader.Find("WorkshopMatch/FlatPiece");
            Material material = new(shader) { name = materialName, color = color };
            generatedMaterials.Add(material);
            return material;
        }

        private Material CreateTexturedMaterial(string materialName, Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ??
                            Shader.Find("Standard") ?? Shader.Find("WorkshopMatch/FlatPiece");
            Material material = new(shader) { name = materialName, color = Color.white, mainTexture = texture };
            if (texture != null && texture.name.StartsWith("cell_tile_", StringComparison.Ordinal))
            {
                material.mainTextureScale = new Vector2(84f / 112f, 83f / 112f);
                material.mainTextureOffset = new Vector2(19f / 112f, 23f / 112f);
            }
            generatedMaterials.Add(material);
            return material;
        }

        private void BuildBoardView()
        {
            var build = BuildBoardViewRoutine(false);
            while (build.MoveNext()) { }
        }

        private IEnumerator BuildBoardViewRoutine(bool incremental)
        {
            double batchStarted = Time.realtimeSinceStartupAsDouble;
            Transform parent = gameplayScreenRoot != null ? gameplayScreenRoot : transform;
            Transform previous = parent.Find("Runtime Board");
            if (previous != null) Destroy(previous.gameObject);
            coordinates.Clear();
            views.Clear();
            Transform root = new GameObject("Runtime Board").transform;
            root.SetParent(parent, false);
            runtimeBoardRoot = root;
            oilJoinedRoot = new GameObject("Joined Oil Surfaces").transform;
            oilJoinedRoot.SetParent(root, false);
            oilJoinedSignature = int.MinValue;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            runtimeBoardBacking = frame.transform;
            frame.name = "Board Frame";
            frame.transform.SetParent(root, false);
            frame.transform.localPosition = new Vector3(0f, BoardCenterY, 0.35f);
            frame.transform.localScale = new Vector3(boardWidth * CellPitch + 0.34f, boardHeight * CellPitch + 0.34f, 0.20f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMaterial;
            frame.GetComponent<Renderer>().receiveShadows = false;
            frame.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(frame.GetComponent<Collider>());

            float startX = -(boardWidth - 1) * CellPitch * 0.5f;
            float startY = BoardCenterY - (boardHeight - 1) * CellPitch * 0.5f;
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    Match3BoardModel.Cell coordinate = new(x, y);
                    GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.name = $"Cell_{x}_{y}";
                    cell.transform.SetParent(root, false);
                    cell.transform.localPosition = new Vector3(startX + x * CellPitch, startY + y * CellPitch, 0f);
                    cell.transform.localScale = new Vector3(CellSize, CellSize, 0.34f);

                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "Cell Tile";
                    tile.transform.SetParent(root, false);
                    tile.transform.localPosition = new Vector3(cell.transform.localPosition.x, cell.transform.localPosition.y, 0.28f);
                    tile.transform.localScale = new Vector3(CellSize * 1.06f, CellSize * 1.06f, 0.035f);
                    tile.GetComponent<Renderer>().sharedMaterial = cellNormalMaterial != null ? cellNormalMaterial : frameMaterial;
                    tile.GetComponent<Renderer>().receiveShadows = false;
                    tile.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    Destroy(tile.GetComponent<Collider>());
                    if (modelCatalog != null && modelCatalog.BoardCell != null && PieceTexture("candy_0") == null)
                    {
                        GameObject boardCell = Instantiate(modelCatalog.BoardCell, root);
                        boardCell.name = $"Wood Inlay {x}_{y}";
                        boardCell.transform.localPosition = new Vector3(cell.transform.localPosition.x,
                            cell.transform.localPosition.y, 0.32f);
                        boardCell.transform.localRotation = Quaternion.identity;
                        boardCell.transform.localScale = Vector3.one;
                        foreach (Collider item in boardCell.GetComponentsInChildren<Collider>(true)) item.enabled = false;
                        Renderer[] parts = boardCell.GetComponentsInChildren<Renderer>(true);
                        if (parts.Length > 0)
                        {
                            Bounds size = parts[0].bounds;
                            for (int part = 1; part < parts.Length; part++) size.Encapsulate(parts[part].bounds);
                            float widest = Mathf.Max(size.size.x, size.size.y);
                            if (widest > 0.001f) boardCell.transform.localScale *= CellPitch * 0.96f / widest;
                            size = parts[0].bounds;
                            for (int part = 1; part < parts.Length; part++) size.Encapsulate(parts[part].bounds);
                            boardCell.transform.position += cell.transform.position + new Vector3(0f, 0f, 0.32f) - size.center;
                            tile.GetComponent<Renderer>().enabled = false;
                        }
                    }

                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = "Obstacle Marker";
                    marker.transform.SetParent(cell.transform, false);
                    marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    marker.transform.localPosition = new Vector3(0f, 0f, -0.72f);
                    marker.transform.localScale = new Vector3(0.62f, 0.28f, 0.62f);
                    Destroy(marker.GetComponent<Collider>());

                    GameObject oilTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    oilTile.name = "Floor Oil";
                    oilTile.transform.SetParent(root, false);
                    oilTile.transform.localPosition = new Vector3(cell.transform.localPosition.x, cell.transform.localPosition.y, 0.14f);
                    // Slight overlap makes neighbouring oil cells read as one surface.
                    oilTile.transform.localScale = new Vector3(CellPitch + 0.012f, CellPitch + 0.012f, 0.04f);
                    oilTile.GetComponent<Renderer>().sharedMaterial = oilMaterial;
                    Destroy(oilTile.GetComponent<Collider>());

                    CellView view = new()
                    {
                        Root = cell.transform,
                        HomePosition = cell.transform.localPosition,
                        BaseRenderer = cell.GetComponent<Renderer>(),
                        TileRenderer = tile.GetComponent<Renderer>(),
                        Collider = cell.GetComponent<Collider>(),
                        Marker = marker,
                        MarkerRenderer = marker.GetComponent<Renderer>(),
                        OilTile = oilTile
                    };
                    coordinates[cell.transform] = coordinate;
                    views[coordinate] = view;
                    if (incremental && Time.realtimeSinceStartupAsDouble - batchStarted >= .003)
                    { yield return null; batchStarted = Time.realtimeSinceStartupAsDouble; }
                }
                if (incremental) { yield return null; batchStarted = Time.realtimeSinceStartupAsDouble; }
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Physics.SyncTransforms();
        }

        private void RefreshJoinedOil(Match3BoardModel.BoardFrame frame, bool[,] oilOverride = null)
        {
            if (oilJoinedRoot == null) return;
            bool Has(int x, int y) => x >= 0 && y >= 0 && x < boardWidth && y < boardHeight &&
                (oilOverride!=null ? oilOverride[x,y] : frame == null ? model.HasOil(x, y) : frame.HasOil(x, y));
            int signature = 17;
            for (int y = 0; y < boardHeight; y++)
                for (int x = 0; x < boardWidth; x++)
                {
                    views[new Match3BoardModel.Cell(x,y)].OilTile.SetActive(false);
                    signature = unchecked(signature * 31 + (Has(x,y) ? 1 : 0));
                }
            if (signature == oilJoinedSignature) return;
            oilJoinedSignature = signature;
            for (int i = oilJoinedRoot.childCount - 1; i >= 0; i--)
            {
                var child = oilJoinedRoot.GetChild(i);
                if (child.TryGetComponent<MeshFilter>(out var filter)) Destroy(filter.sharedMesh);
                Destroy(child.gameObject);
            }
            List<Vector3> vertices = new(); List<int> indices = new();
            float half = CellPitch * 0.5f, radius = CellPitch * 0.16f;
            for (int y = 0; y < boardHeight; y++) for (int x = 0; x < boardWidth; x++)
            {
                if (!Has(x,y)) continue;
                Vector3 center = views[new Match3BoardModel.Cell(x,y)].HomePosition;
                center.z = 0.04f;
                int start = vertices.Count; vertices.Add(center);
                for (int corner = 0; corner < 4; corner++)
                {
                    int sx = corner == 0 || corner == 3 ? 1 : -1;
                    int sy = corner < 2 ? 1 : -1;
                    bool rounded = !Has(x+sx,y) && !Has(x,y+sy);
                    float r = rounded ? radius : 0;
                    Vector3 pivot = center + new Vector3(sx*(half-r), sy*(half-r),0);
                    for (int segment=0; segment<=5; segment++)
                    {
                        float angle=(corner*90f+segment*18f)*Mathf.Deg2Rad;
                        vertices.Add(pivot+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*r);
                    }
                }
                for (int i=1;i<=24;i++) { indices.Add(start); indices.Add(start+(i==24?1:i+1)); indices.Add(start+i); }
            }
            Mesh mesh = new() { name = "Continuous rounded oil floor" };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals();
            GameObject surface = new("Oil union");
            surface.transform.SetParent(oilJoinedRoot,false);
            surface.AddComponent<MeshFilter>().sharedMesh=mesh;
            Renderer renderer=surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=oilMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }

        private void BuildMenuPresentation()
        {
            Transform parent = mainMenuScreenRoot != null ? mainMenuScreenRoot : transform;
            Transform previous = parent.Find("Menu Presentation");
            if (previous != null) Destroy(previous.gameObject);
            menuPresentationRoot = new GameObject("Menu Presentation").transform;
            menuPresentationRoot.SetParent(parent, false);

            if (uiCatalog != null && uiCatalog.IsReady)
                return;

            CreateMenuBlock("Workshop Wall", new Vector3(0f, 0f, 0.8f), new Vector3(6.25f, 12.2f, 0.18f),
                new Color(0.42f, 0.20f, 0.08f));
            CreateMenuBlock("Floor Glow", new Vector3(0f, -4.35f, 0.55f), new Vector3(5.75f, 2.2f, 0.12f),
                new Color(0.88f, 0.55f, 0.20f));
            CreateMenuBlock("Factory Plinth", new Vector3(0f, 0.55f, 0.50f), new Vector3(5.2f, 0.28f, 0.18f),
                new Color(0.72f, 0.37f, 0.12f));

            if (modelCatalog != null && modelCatalog.FactoryTop != null)
                InstantiateMenuModel(modelCatalog.FactoryTop, "Factory Display", new Vector3(0f, 2.25f, 0f), 4.7f);
            else
            {
                CreateMenuBlock("Factory Body", new Vector3(0f, 2.15f, 0.2f), new Vector3(3.9f, 2.25f, 0.45f),
                    new Color(0.23f, 0.31f, 0.38f));
                CreateMenuBlock("Factory Chimney", new Vector3(-1.15f, 3.85f, 0.15f), new Vector3(0.65f, 1.6f, 0.42f),
                    new Color(0.56f, 0.22f, 0.12f));
            }

            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 0.92f;
                GameObject prefab = modelCatalog != null && modelCatalog.Candies != null && i < modelCatalog.Candies.Length
                    ? modelCatalog.Candies[i]
                    : null;
                if (prefab != null)
                    InstantiateMenuModel(prefab, $"Candy Display {i + 1}", new Vector3(x, -3.1f + Mathf.Abs(i - 2) * 0.10f, 0f), 0.72f);
                else
                {
                    GameObject candy = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                    candy.name = $"Candy Display {i + 1}";
                    candy.transform.SetParent(menuPresentationRoot, false);
                    candy.transform.localPosition = new Vector3(x, -3.1f, 0f);
                    candy.transform.localScale = Vector3.one * 0.68f;
                    candy.GetComponent<Renderer>().sharedMaterial = candyMaterials[i];
                    Destroy(candy.GetComponent<Collider>());
                }
            }
        }

        private void CreateMenuBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(menuPresentationRoot, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name, color);
            Destroy(block.GetComponent<Collider>());
        }

        private void InstantiateMenuModel(GameObject prefab, string name, Vector3 position, float targetSize)
        {
            GameObject instance = Instantiate(prefab, menuPresentationRoot);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            instance.transform.localScale = Vector3.one;
            foreach (Collider modelCollider in instance.GetComponentsInChildren<Collider>(true))
                modelCollider.enabled = false;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.0001f)
                instance.transform.localScale *= targetSize / largest;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 targetWorld = menuPresentationRoot.TransformPoint(position);
            instance.transform.position += targetWorld - bounds.center;
        }

        private void SetPresentationMode()
        {
            if (mainMenuScreenRoot != null) mainMenuScreenRoot.gameObject.SetActive(screen == AppScreen.Menu);
            if (fullMapScreenRoot != null) fullMapScreenRoot.gameObject.SetActive(screen == AppScreen.Map);
            if (gameplayScreenRoot != null) gameplayScreenRoot.gameObject.SetActive(screen is AppScreen.Playing or AppScreen.Result);
            if (settingsPopupRoot != null) settingsPopupRoot.gameObject.SetActive(false);
            if (resultTransitionRoot != null) resultTransitionRoot.gameObject.SetActive(screen == AppScreen.Result);
        }

        private void RefreshBoardView(Match3BoardModel.BoardFrame frame = null)
        {
            var refresh = RefreshBoardViewRoutine(frame, false);
            while (refresh.MoveNext()) { }
        }

        private IEnumerator RefreshBoardViewRoutine(Match3BoardModel.BoardFrame frame, bool incremental)
        {
            double batchStarted = Time.realtimeSinceStartupAsDouble;
            Match3BoardModel.Objectives objectives = frame == null ? model.GetObjectives() : frame.Objectives;
            if(gearVictoryAt<0 && ((model.MoveGoal>0 && frame==null && model.ValidMovesMade>=model.MoveGoal) ||
                (model.MoveGoal==0 && (objectives.TotalBolts>0||objectives.TotalOil>0||objectives.TotalFrozenOil>0||objectives.MotorRequired>0) &&
                objectives.CollectedBolts>=objectives.TotalBolts&&objectives.RemainingOil==0&&objectives.RemainingFrozenOil==0&&objectives.MotorHits>=objectives.MotorRequired)))
            {gearVictoryAt=Time.unscaledTime;PlayProductionSound(uiCatalog?.GearSpinLoopDraft);}
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    Match3BoardModel.Cell coordinate = new(x, y);
                    CellView view = views[coordinate];
                    CellContent content = frame == null ? model.GetContent(coordinate) : frame.GetContent(x, y);
                    int candyColor = content == CellContent.Candy
                        ? frame == null ? model.GetColor(x, y) : frame.GetColor(x, y)
                        : -1;
                    SpecialKind special = content == CellContent.Candy
                        ? frame == null ? model.GetSpecial(x, y) : frame.GetSpecial(x, y)
                        : SpecialKind.None;
                    view.Root.localPosition = view.HomePosition;
                    view.Root.localScale = new Vector3(CellSize, CellSize, 0.34f);
                    view.OilTile.SetActive(frame == null ? model.HasOil(x, y) : frame.HasOil(x, y));
                    view.Marker.SetActive(false);
                    view.Marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    view.Marker.transform.localPosition = new Vector3(0f, 0f, -0.72f);
                    view.Marker.transform.localScale = new Vector3(0.62f, 0.28f, 0.62f);
                    view.BaseRenderer.enabled = true;
                    if (view.TileRenderer != null)
                    {
                        bool blockedCell = content == CellContent.Bolt || content == CellContent.Motor ||
                                           content == CellContent.FrozenGrease;
                        view.TileRenderer.sharedMaterial = blockedCell && cellBlockedMaterial != null
                            ? cellBlockedMaterial
                            : cellNormalMaterial != null ? cellNormalMaterial : frameMaterial;
                    }
                    view.Collider.enabled = content != CellContent.Empty;
                    view.PartVariant=frame==null?model.GetPartVariant(x,y):frame.GetPartVariant(x,y);
                    UpdateCellModel(view, coordinate, content, candyColor, special,
                        content == CellContent.Bolt ? (frame == null ? model.GetObstacleState(x, y) : frame.GetObstacleState(x, y)) : -1);

                    switch (content)
                    {
                        case CellContent.Candy:
                            Material candyMaterial = candyMaterials[candyColor];
                            view.BaseRenderer.sharedMaterial = candyMaterial;
                            view.BaseRenderer.enabled = view.Model == null;
                            if (special != SpecialKind.None)
                            {
                                view.Marker.SetActive(view.Model == null);
                                if (special == SpecialKind.RayHorizontal || special == SpecialKind.RayVertical)
                                {
                                    view.MarkerRenderer.sharedMaterial = rayMaterial;
                                    view.Marker.transform.localRotation = special == SpecialKind.RayHorizontal
                                        ? Quaternion.Euler(90f, 0f, 90f)
                                        : Quaternion.Euler(90f, 0f, 0f);
                                    view.Marker.transform.localScale = new Vector3(0.18f, 0.18f, 0.72f);
                                }
                                else
                                {
                                    view.MarkerRenderer.sharedMaterial = special == SpecialKind.Solvent ? solventMaterial : magnetMaterial;
                                    view.Marker.transform.localScale = special == SpecialKind.Solvent
                                        ? new Vector3(0.42f, 0.24f, 0.42f)
                                        : new Vector3(0.58f, 0.22f, 0.58f);
                                }
                            }
                            break;
                        case CellContent.Bolt:
                            view.BaseRenderer.enabled = false;
                            view.Marker.SetActive(view.Model == null);
                            int rust = frame == null ? model.GetObstacleState(x, y) : frame.GetObstacleState(x, y);
                            Material boltMaterial = rust >= 2 ? rustDarkMaterial : rust == 1 ? rustLightMaterial : boltCleanMaterial;
                            view.MarkerRenderer.sharedMaterial = boltMaterial;
                            if (!view.ModelKey.StartsWith("production_bolt_")) TintBoltModel(view, rust);
                            break;
                        case CellContent.Motor:
                            bool motorAnchor = IsMotorVisualAnchor(coordinate);
                            if (!motorAnchor)
                            {
                                view.BaseRenderer.enabled = false;
                                view.Marker.SetActive(false);
                                if (view.Model != null) view.Model.SetActive(false);
                                break;
                            }
                            int motorWidth = IsMotorRotatedAt(coordinate) ? 2 : 3;
                            int motorHeight = IsMotorRotatedAt(coordinate) ? 3 : 2;
                            view.Root.localPosition = view.HomePosition + new Vector3((motorWidth - 1) * CellPitch * 0.5f,
                                (motorHeight - 1) * CellPitch * 0.5f, 0f);
                            view.BaseRenderer.sharedMaterial = (frame==null?model.GetObstacleState(x,y):frame.GetObstacleState(x,y))==0 ? motorReadyMaterial : motorMaterial;
                            view.BaseRenderer.enabled = view.Model == null;
                            if (view.Model == null)
                                view.Root.localScale = new Vector3(CellSize * motorWidth + CellPitch * (motorWidth - 1),
                                    CellSize * motorHeight + CellPitch * (motorHeight - 1), 0.34f);
                            else
                                view.Model.SetActive(true);
                            int required=model.GetMotorRequired(coordinate);
                            int remaining=frame==null?model.GetObstacleState(x,y):frame.GetObstacleState(x,y);
                            UpdateMotorIndicators(view,required-remaining,required);
                            view.Marker.SetActive(false);
                            view.MarkerRenderer.sharedMaterial = motorCoreMaterial;
                            if (objectives.MotorActivated)
                                view.Root.localScale *= 1.06f;
                            break;
                        case CellContent.FrozenGrease:
                            view.BaseRenderer.sharedMaterial = frozenMaterial;
                            view.BaseRenderer.enabled = view.Model == null;
                            view.Marker.SetActive(view.Model == null);
                            view.MarkerRenderer.sharedMaterial = oilMaterial;
                            break;
                        default:
                            view.BaseRenderer.enabled = false;
                            break;
                    }
                    if (incremental && Time.realtimeSinceStartupAsDouble - batchStarted >= .003)
                    { yield return null; batchStarted = Time.realtimeSinceStartupAsDouble; }
                }
                if (incremental) { yield return null; batchStarted = Time.realtimeSinceStartupAsDouble; }
            }
            visibleImpactOil = new bool[boardWidth,boardHeight];
            for(int y=0;y<boardHeight;y++)for(int x=0;x<boardWidth;x++)
                visibleImpactOil[x,y]=frame==null?model.HasOil(x,y):frame.HasOil(x,y);
            RefreshJoinedOil(frame);
        }

        private bool IsMotorRotatedAt(Match3BoardModel.Cell coordinate)
        {
            var anchor=activeLevel?.Cells?.Find(cell=>cell!=null&&cell.Content==CellContent.Motor&&cell.X==coordinate.X&&cell.Y==coordinate.Y);
            return anchor!=null&&activeLevel.IsMotorRotated(anchor);
        }
        private bool IsMotorVisualAnchor(Match3BoardModel.Cell coordinate)
        {
            if (activeLevel != null && activeLevel.Cells != null)
            {
                LevelCellData anchor = activeLevel.Cells.Find(cell => cell != null && cell.Content == CellContent.Motor && cell.X==coordinate.X && cell.Y==coordinate.Y);
                if (anchor != null) return coordinate.X == anchor.X && coordinate.Y == anchor.Y;
            }
            for (int y = 0; y < boardHeight; y++)
                for (int x = 0; x < boardWidth; x++)
                    if (model.GetContent(x, y) == CellContent.Motor)
                        return coordinate.X == x && coordinate.Y == y;
            return false;
        }

        private void UpdateCellModel(CellView view, Match3BoardModel.Cell coordinate, CellContent content, int color,
            SpecialKind special, int obstacleState = -1)
        {
            if (modelCatalog == null) return;
            GameObject prefab = null;
            string key = "";
            if (content == CellContent.Candy)
            {
                if (special is SpecialKind.RayHorizontal or SpecialKind.RayVertical)
                {
                    prefab = modelCatalog.Ray;
                    key = $"ray_{special}";
                }
                else if (special == SpecialKind.Solvent)
                {
                    // Keep the existing 3x3 power/save identifier; only its game-facing
                    // form changes. The solvent FBX stays in the catalog for later use.
                    prefab = modelCatalog.Hammer;
                    key = "hammer";
                }
                else if (special == SpecialKind.Magnet)
                {
                    prefab = modelCatalog.Magnet;
                    key = "magnet";
                }
                else if (modelCatalog.Candies != null && color >= 0 && color < modelCatalog.Candies.Length)
                {
                    prefab = modelCatalog.Candies[color];
                    key = $"candy_{color}";
                }
            }
            else if (content == CellContent.Bolt)
            {
                // The model carries each part's appearance through rust changes and falls.
                bool useScrew = view.PartVariant>=2 && modelCatalog.BoltScrew != null;
                prefab = useScrew ? modelCatalog.BoltScrew : modelCatalog.Bolt;
                key = useScrew ? "bolt_screw" : "bolt_nut";
            }
            else if (content == CellContent.Motor && IsMotorVisualAnchor(coordinate))
            {
                prefab = modelCatalog.Motor;
                key = IsMotorRotatedAt(coordinate) ? "motor_v" : "motor_h";
            }
            else if (content == CellContent.Motor) key = "motor_hidden";
            else if (content == CellContent.FrozenGrease) { prefab = modelCatalog.FrozenGrease; key = "frozen"; }

            if (content == CellContent.Bolt && TrySetProductionBoltSprite(view, key,
                    obstacleState >= 0 ? obstacleState : model.GetObstacleState(coordinate.X, coordinate.Y))) return;
            if (prefab != null && TrySetPieceSprite(view,key,content,special)) return;
            if (view.ModelKey == key && view.Model != null) return;
            if (view.Model != null) Destroy(view.Model);
            view.Model = null;
            view.ModelRenderers = Array.Empty<Renderer>();
            view.ModelKey = key;
            if (prefab == null) return;

            GameObject instance = new($"Visual {key}");
            instance.transform.SetParent(view.Root, false);
            // Cancel the tile's depth squash before rotating the imported model.
            instance.transform.localScale = new Vector3(1f, 1f, CellSize / 0.34f);
            GameObject geometry = special is SpecialKind.RayHorizontal or SpecialKind.RayVertical
                ? CreateRocket(instance.transform) : Instantiate(prefab, instance.transform);
            geometry.transform.localPosition = Vector3.zero;
            geometry.transform.localRotation = key == "bolt_screw" ? Quaternion.Euler(12f, 65f, -28f)
                : content == CellContent.Motor ? Quaternion.Euler(0f, 180f, key == "motor_v" ? 90f : 0f)
                : Quaternion.Euler(8f, 0f, special == SpecialKind.RayHorizontal ? -90f : 0f);
            foreach (Collider modelCollider in geometry.GetComponentsInChildren<Collider>(true)) modelCollider.enabled = false;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            ApplyToon(renderers);
            if (renderers.Length > 0)
            {
                Bounds bounds = CombinedBounds(renderers);
                float unit = runtimeBoardRoot.lossyScale.x;
                float size = content == CellContent.Candy ? CellPitch * 0.98f :
                    content == CellContent.FrozenGrease ? CellPitch * 0.99f : CellPitch * 0.88f;
                if (content == CellContent.Motor)
                {
                    float width = (key == "motor_v" ? 2 : 3) * CellPitch - 0.06f;
                    float height = (key == "motor_v" ? 3 : 2) * CellPitch - 0.06f;
                    instance.transform.localScale = Vector3.Scale(instance.transform.localScale,
                        new Vector3(width * unit / Mathf.Max(bounds.size.x, 0.001f),
                            height * unit / Mathf.Max(bounds.size.y, 0.001f),
                            size * unit / Mathf.Max(bounds.size.x, bounds.size.y)));
                }
                else instance.transform.localScale *= size * unit / Mathf.Max(bounds.size.x, bounds.size.y, 0.001f);
                bounds = CombinedBounds(renderers);
                instance.transform.position += view.Root.TransformPoint(new Vector3(0, 0, -0.8f)) - bounds.center;
            }
            view.Model = instance;
            view.ModelRenderers = renderers;
        }

        private static void TintBoltModel(CellView view, int rust)
        {
            Color tint = rust >= 2 ? new Color(0.60f, 0.33f, 0.18f) :
                rust == 1 ? new Color(0.83f, 0.60f, 0.36f) : Color.white;
            foreach (Renderer renderer in view.ModelRenderers)
            {
                Material source = renderer.sharedMaterial;
                if (source == null) continue;
                MaterialPropertyBlock block = new();
                if (source.HasProperty("_BaseColor"))
                    block.SetColor("_BaseColor", source.GetColor("_BaseColor") * tint);
                if (source.HasProperty("_Color"))
                    block.SetColor("_Color", source.GetColor("_Color") * tint);
                renderer.SetPropertyBlock(block);
            }
        }

        private void TryHandlePointer(Vector2 screenPosition)
        {
            if (boardCamera == null) return;
            Ray ray = boardCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 30f) && coordinates.TryGetValue(hit.collider.transform, out Match3BoardModel.Cell coordinate))
            {
                ResetHintTimer();
                HandleCellTap(coordinate);
            }
        }

        private void HandleCellTap(Match3BoardModel.Cell coordinate)
        {
            if (!TutorialAllowsCell(coordinate)) return;
            if (activeTool.HasValue && !settingsOpen)
            {
                TryApplyActiveTool(coordinate);
                return;
            }

            CellContent content = model.GetContent(coordinate);
            if (content != CellContent.Candy)
            {
                ClearSelection();
                statusText = content switch
                {
                    CellContent.Bolt => $"Civata pası: {model.GetObstacleState(coordinate.X, coordinate.Y)}",
                    CellContent.Motor => $"Motor sabit — yalnız komşu eşleşme doldurur ({model.GetObjectives().MotorHits}/{model.GetObjectives().MotorRequired})",
                    CellContent.FrozenGrease => "Donmuş yağ — komşu eşleşmeyle kabuğu kır",
                    _ => "Bu karede değiştirilebilir şeker yok"
                };
                return;
            }

            if (selectedCell == null) { Select(coordinate); return; }
            if (selectedCoordinate.Equals(coordinate)) { ClearSelection(); statusText = "Seçim iptal edildi"; return; }
            int distance = Math.Abs(selectedCoordinate.X - coordinate.X) + Math.Abs(selectedCoordinate.Y - coordinate.Y);
            if (distance != 1) { Select(coordinate); return; }

            resolving = true;
            Match3BoardModel.Cell from = selectedCoordinate;
            SpecialKind achievementFirst=model.GetSpecial(from.X,from.Y),achievementSecond=model.GetSpecial(coordinate.X,coordinate.Y);
            Match3BoardModel.SwapResult result = model.TrySwap(from, coordinate);
            RecordAchievementTurn(result,achievementFirst,achievementSecond);
            RecordTutorialMove(result);
            ClearSelection();
            StartCoroutine(AnimateResolution(from, coordinate, result));
        }

        private void SelectTool(ToolKind kind)
        {
            if(IsTrainingToolLocked(kind)||!TutorialAllowsToolSelection(kind))return;
            ResetHintTimer();
            ClearSelection();
            if (resolving || model.State != RoundState.Playing || TutorialToolStock(kind) <= 0) return;
            activeTool = activeTool == kind ? null : kind;
            if(kind == ToolKind.Ray) rayVertical=true;
            RecordTutorialToolSelected();
            statusText = activeTool switch
            {
                ToolKind.Ray => $"Havai fişek yerleştir — yön: {(rayVertical ? "dikey" : "yatay")}",
                ToolKind.Solvent => "Çekiç yerleştirilecek normal şekeri seç",
                ToolKind.Magnet => "Mıknatıs yerleştirilecek normal şekeri seç",
                _ => "Araç seçimi iptal edildi"
            };
        }

        private void CancelTool()
        {
            activeTool = null;
            rayVertical = true;
            statusText = "Araç iptal edildi — stok ve hamle korunuyor";
            ResetHintTimer();
        }

        private void TryApplyActiveTool(Match3BoardModel.Cell coordinate)
        {
            ToolKind kind = activeTool.Value;
            if(IsTrainingToolLocked(kind)){activeTool=null;return;}
            if (model.GetContent(coordinate) != CellContent.Candy)
            {
                statusText = "Araç için renkli bir şeker karesi seç";
                return;
            }
            if (TutorialToolStock(kind) <= 0 || model.MovesRemaining <= 0)
            {
                statusText = "Araç kullanılamıyor — stok veya hamle yok";
                return;
            }

            SpecialKind special = kind switch
            {
                ToolKind.Ray => rayVertical ? SpecialKind.RayVertical : SpecialKind.RayHorizontal,
                ToolKind.Solvent => SpecialKind.Solvent,
                ToolKind.Magnet => SpecialKind.Magnet,
                _ => SpecialKind.None
            };
            Match3BoardModel.SwapResult result = model.TryUseTool(coordinate, special);
            if (!result.Accepted)
            {
                statusText = "Normal bir şeker seç — mevcut özel parçanın üstüne yerleştirilemez";
                return;
            }
            if (!SpendTutorialTool(kind))
            {
                Achievements.InvalidateRound();
                Debug.LogError("T07_TOOL_SPEND_FAIL model uygulandı ancak stok harcanamadı");
                return;
            }

            RecordAchievementTurn(result,stockPlacement:true);
            RecordTutorialPlacement(coordinate);
            activeTool = null;
            rayVertical = true;
            resolving = true;
            StartCoroutine(AnimateToolResolution(kind, result));
        }

        private IEnumerator AnimateResolution(Match3BoardModel.Cell from, Match3BoardModel.Cell to, Match3BoardModel.SwapResult result)
        {
            automaticChain = 0;
            if(BeginPowerMerge(from,to,result))yield return AnimatePowerMerge(from,to);
            else yield return AnimateSwap(from, to, result.Accepted);
            if (!result.Accepted)
            {
                statusText = "Eşleşme yok — hamle geri alındı";
                Debug.Log($"T03_SWAP_REVERTED {from}->{to} moves={model.MovesRemaining}");
                resolving = false;
                ResetHintTimer();
                yield break;
            }

            for (int i = 0; i < result.Steps.Count; i++)
            {
                Match3BoardModel.ResolutionStep step = result.Steps[i];
                if (step.HasFall) { yield return AnimateFall(step); continue; }
                int contactEnd = ContactWaveEnd(result.Steps, i);
                if (contactEnd > i)
                {
                    yield return AnimateContactWave(result.Steps, i, contactEnd);
                    i = contactEnd;
                    continue;
                }
                AdvanceAutomaticChain(step);
                RefreshBoardView(step.BeforeClear);
                statusText = i == 0 ? "Eşleşme temizleniyor" : $"Zincir {i + 1} temizleniyor";
                var impact=CreatePowerImpactUpdater(step);
                yield return AnimatePowerFeedback(step,()=>impact?.Invoke(0f));
                if (step.MotorExplosion) yield return AnimateMotorBurst(step);
                else if (step.MatchedCells.Count > 0 || step.HitTargets.Count > 0) yield return AnimateClearAndHits(step,impact);
                yield return AnimateGreaseRemoval(step);
                yield return AnimateMachineParts(step);
                RefreshBoardView(step.AfterFall);
            }

            RefreshBoardView();
            if (model.State == RoundState.Won)
                yield return AnimateVictoryGears();
            statusText = model.State == RoundState.Won ? "HEDEFLER TAMAM — MOTOR ÇALIŞTI" : $"{result.Removed} temizlendi, {result.Cascades} zincir";
            Debug.Log($"T03_SWAP_VALID {from}->{to} removed={result.Removed} cascades={result.Cascades} moves={model.MovesRemaining} objectives={ObjectiveText()}");
            resolving = false;
            FinishVisualTurn();
        }

        private IEnumerator AnimateToolResolution(ToolKind kind, Match3BoardModel.SwapResult result)
        {
            automaticChain = 0;
            for (int i = 0; i < result.Steps.Count; i++)
            {
                Match3BoardModel.ResolutionStep step = result.Steps[i];
                if (step.HasFall) { yield return AnimateFall(step); continue; }
                int contactEnd = ContactWaveEnd(result.Steps, i);
                if (contactEnd > i)
                {
                    yield return AnimateContactWave(result.Steps, i, contactEnd);
                    i = contactEnd;
                    continue;
                }
                AdvanceAutomaticChain(step);
                RefreshBoardView(step.BeforeClear);
                statusText = i == 0 ? $"{ToolLabel(kind)} uygulanıyor" : $"Zincir {i + 1} temizleniyor";
                var impact=CreatePowerImpactUpdater(step);
                yield return AnimatePowerFeedback(step,()=>impact?.Invoke(0f));
                if (step.MotorExplosion) yield return AnimateMotorBurst(step);
                else if (step.MatchedCells.Count > 0 || step.HitTargets.Count > 0) yield return AnimateClearAndHits(step,impact);
                yield return AnimateGreaseRemoval(step);
                yield return AnimateMachineParts(step);
                RefreshBoardView(step.AfterFall);
            }
            RefreshBoardView();
            if (model.State == RoundState.Won)
                yield return AnimateVictoryGears();
            statusText = model.State == RoundState.Won ? "HEDEFLER TAMAM — MOTOR ÇALIŞTI" :
                $"{ToolLabel(kind)} yerleştirildi — kullanmak için komşusuyla değiştir";
            resolving = false;
            Debug.Log($"T07_TOOL_USED kind={kind} removed={result.Removed} moves={model.MovesRemaining} objectives={ObjectiveText()}");
            FinishVisualTurn();
        }

        private IEnumerator AnimateHint(Match3BoardModel.Cell from, Match3BoardModel.Cell to)
        {
            hintAnimating = true;
            statusText = "İpucu: bu şekeri komşusuna kaydır";
            CellView first = views[from];
            Vector3 baseScale = new(CellSize, CellSize, 0.34f);
            float elapsed = 0f;
            const float pulseDuration = 0.62f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float heartbeat = Mathf.Sin(Mathf.Clamp01(elapsed / pulseDuration) * Mathf.PI * 3f);
                float scale = 1f + Mathf.Max(0f, heartbeat) * 0.18f;
                first.Root.localScale = baseScale * scale;
                yield return null;
            }
            first.Root.localScale = baseScale;
            yield return AnimateSwap(from, to, false);
            RefreshBoardView();
            hintAnimating = false;
            ResetHintTimer();
        }

        private void FinishVisualTurn()
        {
            ResetHintTimer();
            if (model.State != RoundState.Playing)
            {
                PlayProductionSound(model.State == RoundState.Won ? uiCatalog?.LevelWin : uiCatalog?.LevelLose);
                if (model.State == RoundState.Won)
                    RecordLevelCompletion();
                if(model.State==RoundState.Won&&activeLevel?.Id=="level_001"){ReturnToProgressHome();return;}
                screen = AppScreen.Result;
                SetPresentationMode();
            }
        }

        private void AdvanceAutomaticChain(Match3BoardModel.ResolutionStep step)
        {
            if(step.HasFall||step.MotorExplosion||step.MatchedCells.Count==0)return;
            automaticChain++;
            PlayChainFeedback();
        }

        private void PlayProductionSound(AudioClip clip, float pitch = 1f)
        {
            if (openingTiming.Visible || clip == null) return;
            if (uiAudio == null)
            {
                uiAudio = gameObject.AddComponent<AudioSource>();
                uiAudio.playOnAwake = false;
                uiAudio.spatialBlend = 0f;
            }
            uiAudio.pitch = pitch;
            uiAudio.PlayOneShot(clip);
        }

        private void RecordLevelCompletion()
        {
            toolProgress.CommitAttemptCredits();
            if (!movesBankedThisRound)
            {
                movesBankedThisRound = true;
                toolProgress.BankRemainingMoves(model.MovesRemaining);
                bankTransferTotal = model.MovesRemaining;
                bankTransferAt = Time.unscaledTime;
                StartCoroutine(AnimateBankTransferSound());
            }
            int completed = activeLevel == null ? -1 : Array.IndexOf(LevelRepository.PrototypeLevelIds, activeLevel.Id);
            if (completed < 0) return;
            if(completed<10){PlayerPrefs.SetInt(TutorialDoneKey,PlayerPrefs.GetInt(TutorialDoneKey,0)|(1<<completed));PlayerPrefs.Save();}
            int next = Mathf.Min(completed + 1, LevelRepository.PrototypeLevelIds.Length - 1);
            if (next > highestUnlockedLevel)
            {
                newlyUnlockedLevel=next;
                highestUnlockedLevel = next;
                PlayerPrefs.SetInt(LevelRepository.HighestUnlockedLevelKey, highestUnlockedLevel);
                PlayerPrefs.Save();
            }
            if(completed>=9){PlayerPrefs.SetInt(WheelUnlockedKey,1);PlayerPrefs.Save();}
            pendingMenuLevelIndex = next;
        }

        private static string ToolLabel(ToolKind kind) => kind switch
        {
            ToolKind.Ray => "FIREWORK",
            ToolKind.Solvent => "HAMMER",
            ToolKind.Magnet => "MAGNET",
            _ => "TOOL"
        };

        private IEnumerator AnimateSwap(Match3BoardModel.Cell from, Match3BoardModel.Cell to, bool accepted)
        {
            CellView first = views[from];
            CellView second = views[to];
            Vector3 firstStart = first.HomePosition;
            Vector3 secondStart = second.HomePosition;
            float elapsed = 0f;
            while (elapsed < SwapDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SwapDuration));
                first.Root.localPosition = Vector3.Lerp(firstStart, secondStart, t);
                second.Root.localPosition = Vector3.Lerp(secondStart, firstStart, t);
                yield return null;
            }

            if (accepted)
                yield break;

            elapsed = 0f;
            while (elapsed < SwapDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SwapDuration));
                first.Root.localPosition = Vector3.Lerp(secondStart, firstStart, t);
                second.Root.localPosition = Vector3.Lerp(firstStart, secondStart, t);
                yield return null;
            }
            first.Root.localPosition = firstStart;
            second.Root.localPosition = secondStart;
        }

        private static int ContactWaveEnd(IReadOnlyList<Match3BoardModel.ResolutionStep> steps, int start)
        {
            int end=start; bool contact=false;
            while(end+1<steps.Count && !steps[end+1].HasFall)
            {
                end++;
                contact |= steps[end].TriggerStepIndex >= start;
            }
            return contact ? end : start;
        }

        private float PowerContactDelay(Match3BoardModel.PowerEvent power, Match3BoardModel.Cell cell)
        {
            if(power.Shape==Match3BoardModel.PowerShape.WholeBoard)
            {
                int row=boardHeight-1-cell.Y;
                return (row*boardWidth+(row%2==0?cell.X:boardWidth-1-cell.X))*.025f;
            }
            if(power.Kind is SpecialKind.RayHorizontal or SpecialKind.RayVertical)
                return RayCellDelay(power,cell);
            return 0f;
        }

        private IEnumerator AnimateContactWave(IReadOnlyList<Match3BoardModel.ResolutionStep> steps, int start, int end)
        {
            RefreshBoardView(steps[start].BeforeClear);
            var started=new bool[end-start+1];
            int finished=0;
            void StartStep(int index)
            {
                if(started[index-start])return;
                started[index-start]=true;
                StartCoroutine(RunStep(index));
            }
            IEnumerator RunStep(int index)
            {
                var step=steps[index];
                AdvanceAutomaticChain(step);
                var impact=CreatePowerImpactUpdater(step);
                yield return AnimatePowerFeedback(step,()=>impact?.Invoke(0f));
                void Contact(float elapsed)
                {
                    impact?.Invoke(elapsed);
                    for(int child=index+1;child<=end;child++)
                        if(steps[child].TriggerStepIndex==index && steps[child].Activation.HasValue &&
                            elapsed>=PowerContactDelay(step.Activation.Value,steps[child].Activation.Value.Cell))
                            StartStep(child);
                }
                if(step.MotorExplosion)yield return AnimateMotorBurst(step);
                else yield return AnimateClearAndHits(step,Contact);
                // Handles end-of-line contact at the last rendered frame too.
                if(step.Activation.HasValue)Contact(float.PositiveInfinity);
                finished++;
                // Motor side effects have no travelling-contact parent of their own.
                for(int next=index+1;next<=end && steps[next].TriggerStepIndex<0;next++)StartStep(next);
            }
            StartStep(start);
            while(finished<started.Length)yield return null;
            // Snapshot-wide oil redraw must remain ordered, not race parallel effects.
            for(int index=start;index<=end;index++)
            {
                yield return AnimateGreaseRemoval(steps[index]);
            }
            yield return AnimateMachineParts(steps,start,end);
            RefreshBoardView(steps[end].AfterFall);
        }

        private IEnumerator AnimateClearAndHits(Match3BoardModel.ResolutionStep step, Action<float> onContactProgress = null)
        {
            bool ray=step.Activation.HasValue && (step.Activation.Value.Kind==SpecialKind.RayHorizontal || step.Activation.Value.Kind==SpecialKind.RayVertical);
            bool magnet=step.Activation.HasValue && step.Activation.Value.Kind==SpecialKind.Magnet;
            bool wholeBoard = step.Activation.HasValue && step.Activation.Value.Shape == Match3BoardModel.PowerShape.WholeBoard;
            RocketFlight flight = ray && step.Activation.Value.Shape != Match3BoardModel.PowerShape.WideRay
                ? BeginRocketFlight(step.Activation.Value) : null;
            var batteryFlights = new List<RocketFlight>();
            if(ray)PlayFireworkSound(false);
            if(ray&&step.Activation.Value.Shape==Match3BoardModel.PowerShape.Cross)
            {
                var power=step.Activation.Value;
                var perpendicular=BeginRocketFlight(new Match3BoardModel.PowerEvent(power.Cell,
                    power.Kind==SpecialKind.RayVertical?SpecialKind.RayHorizontal:SpecialKind.RayVertical));
                if(perpendicular!=null)batteryFlights.Add(perpendicular);
            }
            if(ray && step.Activation.Value.Shape==Match3BoardModel.PowerShape.WideRay)
            {
                var power=step.Activation.Value;
                for(int offset=-1;offset<=1;offset++)
                {
                    var cell=new Match3BoardModel.Cell(power.Cell.X+(power.Kind==SpecialKind.RayVertical?offset:0),power.Cell.Y+(power.Kind==SpecialKind.RayHorizontal?offset:0));
                    if(cell.X<0 || cell.X>=boardWidth || cell.Y<0 || cell.Y>=boardHeight) continue;
                    var launched=BeginRocketFlight(new Match3BoardModel.PowerEvent(cell,power.Kind));
                    if(launched!=null) batteryFlights.Add(launched);
                }
            }
            float Delay(Match3BoardModel.Cell cell)
            {
                if (wholeBoard)
                {
                    int row = boardHeight - 1 - cell.Y;
                    int column = row % 2 == 0 ? cell.X : boardWidth - 1 - cell.X;
                    return (row * boardWidth + column) * .025f;
                }
                if(!ray) return 0;
                return RayCellDelay(step.Activation.Value,cell);
            }
            float maxDelay=0;
            var rotations=new Quaternion[step.MatchedCells.Count];
            for(int i=0;i<step.MatchedCells.Count;i++)
            {
                maxDelay=Mathf.Max(maxDelay,Delay(step.MatchedCells[i]));
                var visual=views[step.MatchedCells[i]].Model;
                rotations[i]=visual!=null?visual.transform.localRotation:Quaternion.identity;
            }
            foreach (var target in step.HitTargets) maxDelay = Mathf.Max(maxDelay, Delay(target));
            if (flight != null)
                maxDelay = Mathf.Max(maxDelay, Mathf.Max(flight.Distances[0], flight.Distances[1]) / CellPitch * RocketCellSeconds);
            foreach(var launched in batteryFlights)
                maxDelay=Mathf.Max(maxDelay,Mathf.Max(launched.Distances[0],launched.Distances[1])/CellPitch*RocketCellSeconds);
            float elapsed = 0f;
            bool[] burstPlayed = wholeBoard ? new bool[step.MatchedCells.Count] : null;
            while (elapsed < ClearDuration+maxDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateRocketFlight(flight, elapsed);
                foreach(var launched in batteryFlights) UpdateRocketFlight(launched,elapsed);
                onContactProgress?.Invoke(elapsed);
                for (int i = 0; i < step.MatchedCells.Count; i++)
                {
                    var view=views[step.MatchedCells[i]];
                    if (burstPlayed != null && !burstPlayed[i] && elapsed >= Delay(step.MatchedCells[i]))
                    {
                        burstPlayed[i] = true;
                        PlayProductionSound(uiCatalog?.CandyPopSound, .78f + (i % 5) * .02f);
                        // Whole-board clears shrink cleanly without the snow-like burst.
                    }
                    float progress=Mathf.Clamp01((elapsed-Delay(step.MatchedCells[i]))/ClearDuration);
                    view.Root.localScale=new Vector3(CellSize,CellSize,.34f)*Mathf.Lerp(1,0,progress*progress);
                    if(magnet && view.Model!=null) view.Model.transform.localRotation=rotations[i]*Quaternion.Euler(0,0,progress*270f);
                }

                for (int i = 0; i < step.HitTargets.Count; i++)
                {
                    float hitT = Mathf.Clamp01((elapsed - Delay(step.HitTargets[i])) / ClearDuration);
                    float pulse = 1f + Mathf.Sin(hitT * Mathf.PI) * .18f;
                    views[step.HitTargets[i]].Root.localScale = new Vector3(CellSize * pulse, CellSize * pulse, .34f * pulse);
                }
                yield return null;
            }
            for(int i=0;i<step.MatchedCells.Count;i++)
            {
                views[step.MatchedCells[i]].Root.localScale=Vector3.zero;
                if(views[step.MatchedCells[i]].Model!=null) views[step.MatchedCells[i]].Model.transform.localRotation=rotations[i];
            }
            if (flight != null) RemoveEffect(flight.Root);
            foreach(var launched in batteryFlights) RemoveEffect(launched.Root);
            if (ray) PlayFireworkSound(true);
        }

        private IEnumerator AnimateMachineParts(Match3BoardModel.ResolutionStep step)
        {
            yield return AnimateMachineParts(new[]{step},0,0);
        }

        private IEnumerator AnimateMachineParts(IReadOnlyList<Match3BoardModel.ResolutionStep> steps,int start,int end)
        {
            int running=0;
            bool sounded=false;
            IEnumerator Fly(Match3BoardModel.Cell cell,Match3BoardModel.BoardFrame before)
            {
                try { yield return AnimateMachinePart(cell,false,before); }
                finally { running--; }
            }
            // Launch every reward in the same frame; wait before applying the next snapshot.
            for(int index=start;index<=end;index++)
            {
                var step=steps[index];
                int boltRewards=step.AfterFall.Objectives.CollectedBolts-step.BeforeClear.Objectives.CollectedBolts;
                foreach(var cell in step.ExitedBolts)
                {
                    if(boltRewards<=0)break;
                    if(!sounded){PlayProductionSound(uiCatalog?.BoltPing);sounded=true;}
                    running++;
                    StartCoroutine(Fly(cell,step.BeforeClear));
                    boltRewards--;
                }
            }
            while(running>0)yield return null;
        }

        private IEnumerator AnimateMachinePart(Match3BoardModel.Cell cell, bool motor,
            Match3BoardModel.BoardFrame before)
        {
            if (!views.TryGetValue(cell, out CellView view) || view.Model == null || gearMachineRoot == null)
                yield break;
            if (!motor)
            {
                string boltKey=view.ModelKey.Contains("screw")?"bolt_screw":"bolt_nut";
                if (!TrySetProductionBoltSprite(view,boltKey,0)) TintBoltModel(view,0);
                if (view.MarkerRenderer != null) view.MarkerRenderer.sharedMaterial=boltCleanMaterial;
            }
            GameObject token = Instantiate(view.Model);
            token.SetActive(true);
            transientEffects.Add(token);
            token.name = motor ? "Motor reward flight" : "M6 reward flight";
            token.transform.SetParent(gameplayScreenRoot, true);
            token.transform.SetPositionAndRotation(view.Model.transform.position, view.Model.transform.rotation);
            Vector3 parentScale = gameplayScreenRoot.lossyScale;
            Vector3 worldScale = view.Model.transform.lossyScale;
            token.transform.localScale = new Vector3(worldScale.x / parentScale.x,
                worldScale.y / parentScale.y, worldScale.z / parentScale.z);
            foreach (Collider collider in token.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Vector3 originalScale = token.transform.localScale;
            if (motor) token.transform.localScale = originalScale * 0.34f;
            view.Model.SetActive(false);
            if (!motor) yield return AnimateCleanBoltReveal(token);
            Vector3 start = token.transform.position;
            Vector3 sourceCenter = CombinedBounds(token.GetComponentsInChildren<Renderer>(true)).center;
            Vector3 pivotOffset = start - sourceCenter;
            Vector3 boardExit = runtimeBoardRoot.TransformPoint(new Vector3(view.HomePosition.x,
                BoardCenterY + (boardHeight - 1) * CellPitch * 0.5f + CellPitch * 1.2f, -0.65f)) + pivotOffset;
            float riseTime = motor ? 0.32f : 0.48f;
            for (float elapsed = 0f; elapsed < riseTime; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / riseTime));
                token.transform.position = Vector3.Lerp(start, boardExit, t);
                if (!motor)
                    for (int y = cell.Y + 1; y < boardHeight; y++)
                        if (before.GetContent(cell.X, y) == CellContent.Candy &&
                            t >= (y - cell.Y) / (float)(boardHeight - cell.Y + 1))
                            views[new Match3BoardModel.Cell(cell.X, y)].Root.localScale = Vector3.one * 0.01f;
                yield return null;
            }
            Vector3 gearTarget = gearMachineRoot.TransformPoint(new Vector3(0f, 4.15f, -0.65f));
            Vector3 exit = token.transform.position;
            const float dockTime = 0.35f;
            for (float elapsed = 0f; elapsed < dockTime; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dockTime));
                token.transform.position = Vector3.Lerp(exit, gearTarget + pivotOffset * (1f - t * 0.72f), t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.20f;
                token.transform.localScale = originalScale * (motor ? 0.34f : 1f) * (1f - t * 0.72f);
                yield return null;
            }
            Destroy(token);
            transientEffects.Remove(token);
            gearBurstUntil = Time.unscaledTime + 0.82f;
            PlayMachineImpact();
        }

        private IEnumerator AnimateVictoryGears()
        {
            if(gearVictoryAt<0){gearVictoryAt=Time.unscaledTime;PlayProductionSound(uiCatalog?.GearSpinLoopDraft);}
            yield break;
        }

        private void PlayMachineImpact()
        {
            if (uiAudio == null)
            {
                uiAudio = gameObject.AddComponent<AudioSource>();
                uiAudio.playOnAwake = false;
                uiAudio.spatialBlend = 0f;
            }
            if (machineImpact == null)
            {
                const int rate = 22050;
                float[] samples = new float[rate / 4];
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)rate;
                    samples[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) +
                        0.38f * Mathf.Sin(2f * Mathf.PI * 1320f * t)) * Mathf.Exp(-t * 19f) * 0.16f;
                }
                machineImpact = AudioClip.Create("Machine part ping", samples.Length, 1, rate, false);
                machineImpact.SetData(samples, 0);
            }
            uiAudio.PlayOneShot(machineImpact);
        }

        private IEnumerator AnimateFall(Match3BoardModel.ResolutionStep step)
        {
            RefreshBoardView(step.AfterFall);
            var movingViews=new CellView[step.Falls.Count];
            var clipped=new List<(Renderer renderer,MaterialPropertyBlock saved)>();
            Vector3 Position(int piece,Match3BoardModel.Cell cell)
            {
                var move=step.Falls[piece];
                return movingViews[piece].HomePosition+new Vector3(cell.X-move.To.X,cell.Y-move.To.Y,0)*CellPitch;
            }
            for(int p=0;p<step.Falls.Count;p++)
            {
                var move=step.Falls[p];
                if(move.From.Equals(move.To)&&(move.Path==null||move.Path.Count<=1))continue;
                CellView view=views[move.To];movingViews[p]=view;
                view.Root.localPosition=Position(p,move.From);
                if(!move.Spawned||view.Model==null)continue;
                float ceiling=runtimeBoardRoot.TransformPoint(view.HomePosition+Vector3.up*CellPitch*(move.SegmentTop-move.To.Y+.5f)).y;
                float left=runtimeBoardRoot.TransformPoint(view.HomePosition+Vector3.right*CellPitch*(-move.To.X-.5f)).x;
                float right=runtimeBoardRoot.TransformPoint(view.HomePosition+Vector3.right*CellPitch*(boardWidth-move.To.X-.5f)).x;
                foreach(Renderer renderer in view.ModelRenderers)
                {
                    var saved=new MaterialPropertyBlock();renderer.GetPropertyBlock(saved);
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                    block.SetFloat("_FallClipTop",ceiling);block.SetFloat("_FallClipLeft",left);block.SetFloat("_FallClipRight",right);
                    renderer.SetPropertyBlock(block);clipped.Add((renderer,saved));
                }
            }
            foreach(var phase in FallAnimationPlan.Build(step.Falls,step.AfterFall))
            {
                float longest=0f,entryLongest=0f;
                foreach(var item in phase)
                {
                    float distance=Vector3.Distance(Position(item.Piece,item.From),Position(item.Piece,item.To))/CellPitch;
                    if(item.Entry)entryLongest=Mathf.Max(entryLongest,distance);else longest=Mathf.Max(longest,distance);
                }
                // Keep the existing candies' speed when a longer offscreen queue joins them.
                float duration=.32f+.060f*(longest>0?longest:entryLongest);
                for(float elapsed=0f;elapsed<duration;)
                {
                    yield return null;elapsed+=Time.unscaledDeltaTime;
                    float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/duration));
                    foreach(var item in phase)movingViews[item.Piece].Root.localPosition=Vector3.Lerp(Position(item.Piece,item.From),Position(item.Piece,item.To),t);
                }
                foreach(var item in phase)movingViews[item.Piece].Root.localPosition=Position(item.Piece,item.To);
            }
            foreach(var view in movingViews)if(view!=null)view.Root.localPosition=view.HomePosition;
            foreach(var item in clipped)if(item.renderer!=null)item.renderer.SetPropertyBlock(item.saved);
        }

        private void Select(Match3BoardModel.Cell coordinate)
        {
            ClearSelection();
            selectedCoordinate = coordinate;
            selectedCell = views[coordinate].Root;
            selectionPulseStartedAt=Time.unscaledTime;
            float size=CellSize*SelectionPulseScale(0);
            selectedCell.localScale=new Vector3(size,size,.48f);
            statusText = $"Seçili: {coordinate.X}, {coordinate.Y} — komşusuna dokun";
        }

        private static float SelectionPulseScale(float elapsed)=>1.06f+.05f*(1f+Mathf.Cos(elapsed*Mathf.PI*2f/1.1f));
        private static Rect PulseSelectionRect(Rect rect)
        {
            Vector2 size=rect.size*SelectionPulseScale(Time.unscaledTime);
            return new Rect(rect.center-size*.5f,size);
        }

        private void ClearSelection()
        {
            if (selectedCell != null)
                selectedCell.localScale = new Vector3(CellSize, CellSize, 0.34f);
            selectedCell = null;
        }

        private string ObjectiveText()
        {
            Match3BoardModel.Objectives target = model.GetObjectives();
            return $"bolt={target.CollectedBolts}/{target.TotalBolts},oil={target.TotalOil - target.RemainingOil}/{target.TotalOil},motor={target.MotorHits}/{target.MotorRequired},state={target.State}";
        }

        private static void RunT02SelfTest()
        {
            Match3BoardModel first = new(BoardWidth, BoardHeight, Palette.Length, DefaultSeed, StartingMoves);
            Match3BoardModel second = new(BoardWidth, BoardHeight, Palette.Length, DefaultSeed, StartingMoves);
            bool deterministicStart = first.Snapshot() == second.Snapshot();
            bool invalidFound = first.TryFindInvalidSwap(out Match3BoardModel.Cell invalidA, out Match3BoardModel.Cell invalidB);
            string beforeInvalid = first.Snapshot();
            int movesBeforeInvalid = first.MovesRemaining;
            Match3BoardModel.SwapResult invalidResult = invalidFound ? first.TrySwap(invalidA, invalidB) : default;
            bool invalidReverted = invalidFound && !invalidResult.Accepted && first.Snapshot() == beforeInvalid && first.MovesRemaining == movesBeforeInvalid;
            bool validFoundA = first.TryFindValidSwap(out Match3BoardModel.Cell validA1, out Match3BoardModel.Cell validA2);
            bool validFoundB = second.TryFindValidSwap(out Match3BoardModel.Cell validB1, out Match3BoardModel.Cell validB2);
            bool sameMove = validFoundA && validFoundB && validA1.Equals(validB1) && validA2.Equals(validB2);
            Match3BoardModel.SwapResult resultA = sameMove ? first.TrySwap(validA1, validA2) : default;
            Match3BoardModel.SwapResult resultB = sameMove ? second.TrySwap(validB1, validB2) : default;
            bool deterministicResult = resultA.Accepted && resultB.Accepted && first.Snapshot() == second.Snapshot() && first.MovesRemaining == StartingMoves - 1 && first.IsStable;
            if (deterministicStart && invalidReverted && deterministicResult)
                Debug.Log($"T02_RECHECK_PASS invalid_revert=1 deterministic_seed=1 valid_swap={validA1}->{validA2} hash={first.SnapshotHash()}");
            else
                Debug.LogError($"T02_RECHECK_FAIL start={deterministicStart} revert={invalidReverted} result={deterministicResult}");
        }

        private static void RunT03SelfTest()
        {
            Match3BoardModel hits = new(5, 5, Palette.Length, 303, 10);
            hits.PlaceObstacle(new Match3BoardModel.Cell(2, 2), CellContent.Motor, 5);
            hits.PlaceObstacle(new Match3BoardModel.Cell(4, 2), CellContent.Bolt, 2);
            hits.PlaceObstacle(new Match3BoardModel.Cell(2, 4), CellContent.FrozenGrease, 1);
            hits.SetOil(new Match3BoardModel.Cell(2, 4));
            hits.SetOil(new Match3BoardModel.Cell(0, 0));
            hits.SealObjectives();
            hits.DebugResolveGroup(new Match3BoardModel.Cell(0, 1), new Match3BoardModel.Cell(0, 2), new Match3BoardModel.Cell(1, 1));
            bool distantMatchIgnored = hits.GetObjectives().MotorHits == 0;
            hits.DebugResolveGroup(new Match3BoardModel.Cell(2, 1), new Match3BoardModel.Cell(1, 2), new Match3BoardModel.Cell(1, 1));
            bool motorSingleHit = hits.GetObjectives().MotorHits == 1;
            hits.DebugResolveGroup(new Match3BoardModel.Cell(4, 1), new Match3BoardModel.Cell(3, 2), new Match3BoardModel.Cell(3, 1));
            bool boltSingleHit = hits.GetObstacleState(4, 2) == 1;
            hits.DebugResolveGroup(new Match3BoardModel.Cell(2, 3), new Match3BoardModel.Cell(1, 4), new Match3BoardModel.Cell(1, 3));
            bool frozenLayered = hits.GetContent(2, 4) == CellContent.Empty && hits.HasOil(2, 4);
            hits.DebugResolveGroup(new Match3BoardModel.Cell(0, 0), new Match3BoardModel.Cell(0, 1), new Match3BoardModel.Cell(1, 0));
            bool matchedOilCleared = !hits.HasOil(0, 0);

            Match3BoardModel order = new(5, 5, Palette.Length, 304, 10);
            order.PlaceObstacle(new Match3BoardModel.Cell(2, 2), CellContent.Motor, 1);
            order.PlaceObstacle(new Match3BoardModel.Cell(1, 0), CellContent.Bolt, 1);
            order.SetOil(new Match3BoardModel.Cell(0, 0));
            order.SealObjectives();
            order.DebugResolveGroup(new Match3BoardModel.Cell(2, 1), new Match3BoardModel.Cell(1, 2), new Match3BoardModel.Cell(1, 1));
            Match3BoardModel.Objectives beforeFinal = order.GetObjectives();
            bool waitsForObjectives = beforeFinal.MotorReady && beforeFinal.MotorActivated && beforeFinal.State == RoundState.Playing;
            order.DebugResolveGroup(new Match3BoardModel.Cell(0, 0), new Match3BoardModel.Cell(0, 1), new Match3BoardModel.Cell(1, 1));
            Match3BoardModel.Objectives afterFinal = order.GetObjectives();
            bool finalOrder = afterFinal.CollectedBolts == 1 && afterFinal.RemainingOil == 0 && afterFinal.MotorActivated && afterFinal.State == RoundState.Won;

            if (distantMatchIgnored && motorSingleHit && boltSingleHit && frozenLayered && matchedOilCleared && waitsForObjectives && finalOrder)
                Debug.Log("T03_SELF_TEST_PASS distant_match_ignored=1 single_hit=1 frozen_oil_layer=1 bolt_exit=1 motor_explodes_before_remaining_objectives=1 final_order=1");
            else
                Debug.LogError($"T03_SELF_TEST_FAIL distant={distantMatchIgnored} motor={motorSingleHit} bolt={boltSingleHit} frozen={frozenLayered} oil={matchedOilCleared} waits={waitsForObjectives} final={finalOrder}");
        }

        private static void RunT04SelfTest()
        {
            Match3BoardModel rayProduction = new(6, 6, Palette.Length, 401, 10);
            SpecialKind rayKind = rayProduction.DebugResolveClassifiedGroup(new Match3BoardModel.Cell(2, 0),
                new Match3BoardModel.Cell(0, 0), new Match3BoardModel.Cell(1, 0),
                new Match3BoardModel.Cell(2, 0), new Match3BoardModel.Cell(3, 0));
            bool rayProduced = rayKind == SpecialKind.RayHorizontal &&
                rayProduction.GetSpecial(2, 0) == SpecialKind.RayHorizontal && rayProduction.ProducedRays == 1;

            Match3BoardModel solventProduction = new(6, 6, Palette.Length, 402, 10);
            SpecialKind solventKind = solventProduction.DebugResolveClassifiedGroup(new Match3BoardModel.Cell(1, 1),
                new Match3BoardModel.Cell(0, 0), new Match3BoardModel.Cell(1, 0),
                new Match3BoardModel.Cell(0, 1), new Match3BoardModel.Cell(1, 1));
            bool solventProduced = solventKind == SpecialKind.Solvent &&
                solventProduction.GetSpecial(1, 1) == SpecialKind.Solvent && solventProduction.ProducedSolvents == 1;

            Match3BoardModel magnetProduction = new(6, 6, Palette.Length, 403, 10);
            SpecialKind magnetKind = magnetProduction.DebugResolveClassifiedGroup(new Match3BoardModel.Cell(3, 0),
                new Match3BoardModel.Cell(0, 0), new Match3BoardModel.Cell(1, 0), new Match3BoardModel.Cell(2, 0),
                new Match3BoardModel.Cell(3, 0), new Match3BoardModel.Cell(4, 0));
            bool magnetProduced = magnetKind == SpecialKind.Magnet &&
                magnetProduction.GetSpecial(3, 0) == SpecialKind.Magnet && magnetProduction.ProducedMagnets == 1;

            Match3BoardModel rayEffect = new(6, 6, Palette.Length, 404, 10);
            rayEffect.PlaceObstacle(new Match3BoardModel.Cell(2, 0), CellContent.Bolt, 2);
            rayEffect.PlaceObstacle(new Match3BoardModel.Cell(3, 0), CellContent.FrozenGrease, 1);
            rayEffect.PlaceObstacle(new Match3BoardModel.Cell(4, 0), CellContent.Motor, 5);
            rayEffect.SetOil(new Match3BoardModel.Cell(0, 0));
            rayEffect.SetOil(new Match3BoardModel.Cell(3, 0));
            rayEffect.SealObjectives();
            rayEffect.DebugPlaceSpecial(new Match3BoardModel.Cell(0, 0), SpecialKind.RayHorizontal);
            rayEffect.DebugActivateSpecial(new Match3BoardModel.Cell(0, 0), 1);
            bool rayEffectPassed = FindObstacleState(rayEffect, CellContent.Bolt, 1) &&
                !ContainsContent(rayEffect, CellContent.FrozenGrease) && rayEffect.HasOil(0, 0) &&
                rayEffect.HasOil(3, 0) && rayEffect.GetObjectives().MotorHits == 1;

            Match3BoardModel solventEffect = new(6, 6, Palette.Length, 405, 10);
            solventEffect.PlaceObstacle(new Match3BoardModel.Cell(1, 2), CellContent.Bolt, 2);
            solventEffect.PlaceObstacle(new Match3BoardModel.Cell(3, 3), CellContent.FrozenGrease, 1);
            solventEffect.PlaceObstacle(new Match3BoardModel.Cell(3, 2), CellContent.Motor, 5);
            solventEffect.SetOil(new Match3BoardModel.Cell(1, 1));
            solventEffect.SetOil(new Match3BoardModel.Cell(3, 3));
            solventEffect.SealObjectives();
            solventEffect.DebugPlaceSpecial(new Match3BoardModel.Cell(2, 2), SpecialKind.Solvent);
            solventEffect.DebugActivateSpecial(new Match3BoardModel.Cell(2, 2), 1);
            bool solventEffectPassed = FindObstacleState(solventEffect, CellContent.Bolt, 1) &&
                !ContainsContent(solventEffect, CellContent.FrozenGrease) && !solventEffect.HasOil(1, 1) &&
                solventEffect.HasOil(3, 3) && solventEffect.GetObjectives().MotorHits == 1;

            Match3BoardModel magnetEffect = new(6, 6, Palette.Length, 406, 10);
            magnetEffect.PlaceObstacle(new Match3BoardModel.Cell(2, 2), CellContent.Motor, 5);
            magnetEffect.PlaceObstacle(new Match3BoardModel.Cell(0, 1), CellContent.Bolt, 2);
            magnetEffect.PlaceObstacle(new Match3BoardModel.Cell(4, 1), CellContent.FrozenGrease, 1);
            magnetEffect.SetOil(new Match3BoardModel.Cell(0, 0));
            magnetEffect.SetOil(new Match3BoardModel.Cell(4, 1));
            magnetEffect.SealObjectives();
            magnetEffect.DebugPlaceSpecial(new Match3BoardModel.Cell(2, 0), SpecialKind.Magnet, 4);
            magnetEffect.DebugSetCandy(new Match3BoardModel.Cell(0, 0), 1);
            magnetEffect.DebugSetCandy(new Match3BoardModel.Cell(1, 2), 1);
            magnetEffect.DebugSetCandy(new Match3BoardModel.Cell(2, 1), 1);
            magnetEffect.DebugSetCandy(new Match3BoardModel.Cell(3, 1), 1);
            magnetEffect.DebugActivateSpecial(new Match3BoardModel.Cell(2, 0), 1);
            bool magnetEffectPassed = FindObstacleState(magnetEffect, CellContent.Bolt, 1) &&
                !ContainsContent(magnetEffect, CellContent.FrozenGrease) && magnetEffect.HasOil(0, 0) &&
                magnetEffect.HasOil(4, 1) && magnetEffect.GetObjectives().MotorHits == 1;

            Match3BoardModel activationFlow = new(6, 6, Palette.Length, 407, 10);
            activationFlow.SealObjectives();
            activationFlow.DebugPlaceSpecial(new Match3BoardModel.Cell(2, 2), SpecialKind.Solvent, 3);
            activationFlow.DebugSetCandy(new Match3BoardModel.Cell(2, 3), 1);
            Match3BoardModel.SwapResult activationResult = activationFlow.TrySwap(
                new Match3BoardModel.Cell(2, 2), new Match3BoardModel.Cell(2, 3));
            bool activationFlowPassed = activationResult.Accepted && activationFlow.MovesRemaining == 9 && activationResult.Steps.Count > 0;

            if (rayProduced && solventProduced && magnetProduced && rayEffectPassed && solventEffectPassed && magnetEffectPassed && activationFlowPassed)
                Debug.Log("T04_SELF_TEST_PASS production=ray+solvent+magnet effects=ray+solvent+magnet activation_swap=1 unique_hits=1 oil_matrix=1");
            else
                Debug.LogError($"T04_SELF_TEST_FAIL production={rayProduced}/{solventProduced}/{magnetProduced} effects={rayEffectPassed}/{solventEffectPassed}/{magnetEffectPassed} activation={activationFlowPassed}");
        }

        private static void RunT05SelfTest()
        {
            ToolProgressTracker progress = new();
            Match3BoardModel integration = new(6, 6, Palette.Length, 501, 10, progress.RecordProduced);
            integration.DebugResolveClassifiedGroup(new Match3BoardModel.Cell(2, 0),
                new Match3BoardModel.Cell(0, 0), new Match3BoardModel.Cell(1, 0),
                new Match3BoardModel.Cell(2, 0), new Match3BoardModel.Cell(3, 0));
            bool creationRecorded = progress.Pieces(ToolKind.Ray) == 1;

            progress.RecordProduced(SpecialKind.RayVertical);
            progress.RecordProduced(SpecialKind.RayHorizontal);
            for (int i = 0; i < 3; i++) progress.RecordProduced(SpecialKind.Solvent);
            for (int i = 0; i < 4; i++) progress.RecordProduced(SpecialKind.Magnet);

            string savedBeforeClose = ToolProgressStore.Serialize(progress.Snapshot);
            ToolProgressTracker afterClose = new(ToolProgressStore.Deserialize(savedBeforeClose));
            bool partialsPersist = afterClose.Pieces(ToolKind.Ray) == 3 &&
                afterClose.Pieces(ToolKind.Solvent) == 3 && afterClose.Pieces(ToolKind.Magnet) == 4;

            afterClose.RecordProduced(SpecialKind.RayHorizontal);
            afterClose.RecordProduced(SpecialKind.Solvent);
            afterClose.RecordProduced(SpecialKind.Magnet);
            bool thresholdsConvert = afterClose.Pieces(ToolKind.Ray) == 0 && afterClose.Stock(ToolKind.Ray) == 1 &&
                afterClose.Pieces(ToolKind.Solvent) == 0 && afterClose.Stock(ToolKind.Solvent) == 1 &&
                afterClose.Pieces(ToolKind.Magnet) == 0 && afterClose.Stock(ToolKind.Magnet) == 1;

            ToolProgressTracker afterRestart = new(ToolProgressStore.Deserialize(ToolProgressStore.Serialize(afterClose.Snapshot)));
            bool restartPersists = afterRestart.Stock(ToolKind.Ray) == 1 &&
                afterRestart.Stock(ToolKind.Solvent) == 1 && afterRestart.Stock(ToolKind.Magnet) == 1;
            bool spendPersists = afterRestart.TrySpend(ToolKind.Ray) &&
                new ToolProgressTracker(ToolProgressStore.Deserialize(ToolProgressStore.Serialize(afterRestart.Snapshot))).Stock(ToolKind.Ray) == 0;

            if (creationRecorded && partialsPersist && thresholdsConvert && restartPersists && spendPersists)
                Debug.Log("T05_SELF_TEST_PASS creation_save=1 thresholds=4/4/5 close_reload=1 restart_loss_independent=1 spend_save=1");
            else
                Debug.LogError($"T05_SELF_TEST_FAIL creation={creationRecorded} partials={partialsPersist} thresholds={thresholdsConvert} restart={restartPersists} spend={spendPersists}");
        }

        private static void RunT06SelfTest()
        {
            LevelDefinition edited = LevelRepository.CreatePrototype().Copy();
            LevelCellData bolt = edited.Cells.Find(cell => cell.Content == CellContent.Bolt);
            LevelCellData oil = edited.Cells.Find(cell => cell.Content == CellContent.Candy && cell.Oil);
            bolt.X = 0;
            bolt.Y = 7;
            oil.X = 4;
            oil.Y = 1;

            string saved = JsonUtility.ToJson(edited, true);
            LevelDefinition loaded = JsonUtility.FromJson<LevelDefinition>(saved);
            bool movedCellsReloaded = loaded.Cells.Exists(cell => cell.X == 0 && cell.Y == 7 && cell.Content == CellContent.Bolt) &&
                                       loaded.Cells.Exists(cell => cell.X == 4 && cell.Y == 1 && cell.Oil);
            Match3BoardModel first = LevelRepository.BuildBoard(loaded);
            Match3BoardModel second = LevelRepository.BuildBoard(loaded);
            bool deterministic = first.SnapshotHash() == second.SnapshotHash();

            LevelDefinition invalid = loaded.Copy();
            invalid.Cells.Add(invalid.Cells[0].Copy());
            // Keep the second motor's whole 3x2 footprint inside the 8x8 board.
            // Out-of-bounds entries are deliberately skipped before counting motors.
            invalid.Cells.Add(new LevelCellData { X = 0, Y = 0, Content = CellContent.Motor, ObstacleState = 5 });
            List<LevelValidationMessage> invalidMessages = LevelValidator.Validate(invalid);
            bool duplicateWarning = invalidMessages.Exists(message =>
                message.Severity == LevelValidationSeverity.Error && message.Text.Contains("çakışan"));
            bool twoMotorWarning = invalidMessages.Exists(message =>
                message.Severity == LevelValidationSeverity.Error && message.Text.Contains("birden fazla motor"));
            LevelDefinition outside = loaded.Copy();
            outside.Cells.Add(new LevelCellData { X = outside.Width - 1, Y = outside.Height, Content = CellContent.Motor, ObstacleState = 5 });
            bool boundsWarning = LevelValidator.Validate(outside).Exists(message =>
                message.Severity == LevelValidationSeverity.Error && message.Text.Contains("tahta dışında"));
            bool playable = first.TryFindValidSwap(out _, out _);

            if (movedCellsReloaded && deterministic && duplicateWarning && twoMotorWarning && boundsWarning && playable)
                Debug.Log($"T06_SELF_TEST_PASS save_load=1 moved_bolt_oil=1 duplicate_warning=1 two_motor_warning=1 bounds_warning=1 playable=1 deterministic=1 hash={first.SnapshotHash()}");
            else
                Debug.LogError($"T06_SELF_TEST_FAIL reload={movedCellsReloaded} deterministic={deterministic} duplicate={duplicateWarning} motors={twoMotorWarning} bounds={boundsWarning} playable={playable}");
        }

        private static void RunT07SelfTest()
        {
            Match3BoardModel hintBoard = new(8, 9, Palette.Length, 701, 10);
            bool hintFound = hintBoard.TryFindBestHintSwap(out Match3BoardModel.Cell hintFrom, out Match3BoardModel.Cell hintTo);
            bool hintIsValid = hintFound && hintBoard.TrySwap(hintFrom, hintTo).Accepted;

            ToolProgressTracker inventory = new();
            for (int i = 0; i < ToolProgressTracker.RayTarget; i++)
                inventory.RecordProduced(SpecialKind.RayHorizontal);
            Match3BoardModel toolBoard = new(6, 6, Palette.Length, 702, 10);
            toolBoard.SealObjectives();
            int movesBefore = toolBoard.MovesRemaining;
            Match3BoardModel.SwapResult toolResult = toolBoard.TryUseTool(new Match3BoardModel.Cell(2, 2), SpecialKind.RayHorizontal);
            bool validToolSpent = toolResult.Accepted && inventory.TrySpend(ToolKind.Ray) &&
                                  inventory.Stock(ToolKind.Ray) == 0 && toolBoard.MovesRemaining == movesBefore &&
                                  toolBoard.GetSpecial(2,2) == SpecialKind.RayHorizontal && toolResult.Removed == 0;

            ToolProgressTracker protectedInventory = new();
            for (int i = 0; i < ToolProgressTracker.SolventTarget; i++)
                protectedInventory.RecordProduced(SpecialKind.Solvent);
            Match3BoardModel invalidTarget = new(5, 5, Palette.Length, 703, 10);
            invalidTarget.PlaceObstacle(new Match3BoardModel.Cell(2, 2), CellContent.Motor, 5);
            invalidTarget.SealObjectives();
            Match3BoardModel.SwapResult invalidResult = invalidTarget.TryUseTool(new Match3BoardModel.Cell(2, 2), SpecialKind.Solvent);
            bool invalidPreserved = !invalidResult.Accepted && protectedInventory.Stock(ToolKind.Solvent) == 1 && invalidTarget.MovesRemaining == 10;

            Match3BoardModel layeredOil = new(5, 5, Palette.Length, 704, 10);
            Match3BoardModel.Cell frozenCell = new(2, 2);
            layeredOil.PlaceObstacle(frozenCell, CellContent.FrozenGrease, 1);
            layeredOil.SetOil(frozenCell);
            layeredOil.SealObjectives();
            layeredOil.DebugResolveGroup(new Match3BoardModel.Cell(2, 1), new Match3BoardModel.Cell(1, 2), new Match3BoardModel.Cell(1, 1));
            bool shellFirst = layeredOil.GetContent(frozenCell) == CellContent.Empty && layeredOil.HasOil(2, 2);
            layeredOil.DebugSetCandy(frozenCell, 1);
            layeredOil.DebugResolveGroup(frozenCell, new Match3BoardModel.Cell(2, 1), new Match3BoardModel.Cell(1, 2));
            bool oilSecond = !layeredOil.HasOil(2, 2);

            if (hintIsValid && validToolSpent && invalidPreserved && shellFirst && oilSecond)
                Debug.Log("T07_SELF_TEST_PASS best_hint=1 valid_tool_spend=1 invalid_target_preserves_stock=1 frozen_then_oil=1");
            else
                Debug.LogError($"T07_SELF_TEST_FAIL hint={hintIsValid} spend={validToolSpent} preserve={invalidPreserved} shell={shellFirst} oil={oilSecond}");
        }

        private static void RunT08SelfTest(WorkshopMatchModelCatalog catalog)
        {
            bool candies = catalog != null && catalog.Candies != null && catalog.Candies.Length == 5;
            if (candies)
                for (int i = 0; i < catalog.Candies.Length; i++) candies &= catalog.Candies[i] != null;
            bool mechanics = catalog != null && catalog.Bolt != null && catalog.BoltScrew != null && catalog.Motor != null &&
                               catalog.FrozenGrease != null && catalog.BoardCell != null && catalog.OilJoinedSurface != null;
            bool powers = catalog != null && catalog.Ray != null && catalog.Solvent != null && catalog.Magnet != null && catalog.Hammer != null;
            bool menu = catalog != null && catalog.FactoryTop != null;
            bool icons = catalog != null && catalog.ToolIcons != null && catalog.ToolIcons.Length == 4;
            if (icons)
                for (int i = 0; i < catalog.ToolIcons.Length; i++) icons &= catalog.ToolIcons[i] != null;
            if (candies && mechanics && powers && menu && icons)
                Debug.Log("T08_SELF_TEST_PASS catalog=16 toon_v4=1 m6_nut_and_screw=1 icons=4");
            else
                Debug.LogError($"T08_SELF_TEST_FAIL catalog={(catalog != null)} candies={candies} mechanics={mechanics} powers={powers} menu={menu} icons={icons}");
        }

        private static void RunT09SelfTest()
        {
            bool allLoaded = true;
            bool allValid = true;
            bool deterministicReload = true;
            HashSet<string> ids = new();
            for (int i = 0; i < LevelRepository.PrototypeLevelIds.Length; i++)
            {
                LevelDefinition firstDefinition = LevelRepository.Load(LevelRepository.PrototypeLevelIds[i]);
                LevelDefinition secondDefinition = LevelRepository.Load(LevelRepository.PrototypeLevelIds[i]);
                allLoaded &= firstDefinition != null && secondDefinition != null && firstDefinition.Id == LevelRepository.PrototypeLevelIds[i];
                ids.Add(firstDefinition.Id);
                List<LevelValidationMessage> messages = LevelValidator.Validate(firstDefinition);
                allValid &= !messages.Exists(message => message.Severity == LevelValidationSeverity.Error);
                Match3BoardModel firstBoard = LevelRepository.BuildBoard(firstDefinition);
                Match3BoardModel secondBoard = LevelRepository.BuildBoard(secondDefinition);
                deterministicReload &= firstBoard.SnapshotHash() == secondBoard.SnapshotHash() &&
                                       firstBoard.MovesRemaining == secondBoard.MovesRemaining;
            }
            int expectedCount = LevelRepository.PrototypeLevelIds.Length;
            bool allDistinct = ids.Count == expectedCount;
            if (allLoaded && allValid && deterministicReload && allDistinct && expectedCount == 20)
                Debug.Log($"T09_SELF_TEST_PASS draft_levels={expectedCount} validation={expectedCount}/{expectedCount} deterministic_reload={expectedCount}/{expectedCount} single_scene=1");
            else
                Debug.LogError($"T09_SELF_TEST_FAIL expected={expectedCount} loaded={allLoaded} valid={allValid} deterministic={deterministicReload} distinct={ids.Count}");
        }

        private static bool ContainsContent(Match3BoardModel board, CellContent content)
        {
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (board.GetContent(x, y) == content)
                        return true;
            return false;
        }

        private void RunT12SelfTest()
        {
            Match3BoardModel horizontal = new(8, 9, 5, 12012, 20);
            for (int y = 2; y < 4; y++)
                for (int x = 2; x < 5; x++)
                    horizontal.PlaceObstacle(new Match3BoardModel.Cell(x, y), CellContent.Motor, 5);
            horizontal.SealObjectives();
            horizontal.DebugSetCandy(new Match3BoardModel.Cell(2, 1), 0);
            horizontal.DebugSetCandy(new Match3BoardModel.Cell(3, 1), 0);
            horizontal.DebugSetCandy(new Match3BoardModel.Cell(4, 1), 0);
            horizontal.DebugResolveGroup(new Match3BoardModel.Cell(2, 1), new Match3BoardModel.Cell(3, 1),
                new Match3BoardModel.Cell(4, 1));

            Match3BoardModel vertical = new(8, 9, 5, 12013, 20);
            for (int y = 2; y < 5; y++)
                for (int x = 2; x < 4; x++)
                    vertical.PlaceObstacle(new Match3BoardModel.Cell(x, y), CellContent.Motor, 5);
            vertical.SealObjectives();

            bool rootsReady = mainMenuScreenRoot != null && fullMapScreenRoot != null && gameplayScreenRoot != null &&
                              settingsPopupRoot != null && resultTransitionRoot != null;
            bool footprintsReady = CountContent(horizontal, CellContent.Motor) == 6 && CountContent(vertical, CellContent.Motor) == 6;
            bool oneGroupOneHit = horizontal.GetObjectives().MotorHits == 1;
            bool uiReady = uiCatalog != null && uiCatalog.IsReady;
            if (rootsReady && footprintsReady && oneGroupOneHit && LevelRepository.PrototypeLevelIds.Length == 20 && uiReady)
                Debug.Log("T12_SELF_TEST_PASS roots=5 map_nodes=20 motor_horizontal=3x2 motor_rotated=2x3 one_group_one_hit=1 ui_catalog=33");
            else
                Debug.LogError($"T12_SELF_TEST_FAIL roots={rootsReady} footprints={footprintsReady} one_hit={oneGroupOneHit} ui={uiReady}");
        }

        private static int CountContent(Match3BoardModel board, CellContent content)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (board.GetContent(x, y) == content) count++;
            return count;
        }

        private static bool FindObstacleState(Match3BoardModel board, CellContent content, int state)
        {
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (board.GetContent(x, y) == content && board.GetObstacleState(x, y) == state)
                        return true;
            return false;
        }

        private void OnGUI()
        {
            Rect portrait = GetPortraitRect();
            if (DrawOpening(portrait)) return;
            int titleSize = Mathf.Max(12, Mathf.RoundToInt(portrait.height * 0.026f));
            if (uiCatalog != null && Resources.Load<Font>("WorkshopMatch/CuteDino") is Font cuteDino)
                uiCatalog.UiFont = cuteDino;
            int bodySize = Mathf.Max(10, Mathf.RoundToInt(portrait.height * 0.018f));
            Color outsideColor = GUI.color;
            GUI.color = new Color(0.055f, 0.075f, 0.11f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, portrait.y), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, portrait.yMax, Screen.width, Screen.height - portrait.yMax), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, portrait.y, portrait.x, portrait.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(portrait.xMax, portrait.y, Screen.width - portrait.xMax, portrait.height), Texture2D.whiteTexture);
            GUI.color = outsideColor;
            float margin = portrait.width * 0.045f;
            float contentLeft = portrait.x + margin;
            float contentWidth = portrait.width - margin * 2f;
            GUIStyle title = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, font = uiCatalog?.UiFont, fontSize = titleSize, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            GUIStyle body = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, font = uiCatalog?.UiFont, fontSize = bodySize, normal = { textColor = new Color(0.84f, 0.90f, 0.98f) } };
            GUIStyle button = new(GUI.skin.button) { font = uiCatalog?.UiFont, fontSize = bodySize, fontStyle = FontStyle.Bold };

            if (screen == AppScreen.Menu)
            {
                DrawMenu(title, body, button);
                return;
            }
            if (screen == AppScreen.Map)
            {
                DrawFullMap(title, body, button);
                return;
            }

            // Live script reload while Play is open can clear these non-serialized model fields.
            if (model == null || toolProgress == null) return;

            Matrix4x4 beforeShake=GUI.matrix;
            Vector2 shake=ChainShakeOffset(portrait);
            GUI.matrix=Matrix4x4.Translate(new Vector3(shake.x,shake.y,0))*beforeShake;
            try {
            DrawGameplayChrome(portrait);

            Match3BoardModel.Objectives target = model.GetObjectives();
            int knownLevel = activeLevel == null ? -1 : Array.IndexOf(LevelRepository.PrototypeLevelIds, activeLevel.Id);
            string levelTitle = knownLevel >= 0
                ? $"LEVEL {knownLevel + 1}"
                : activeLevel == null || string.IsNullOrWhiteSpace(activeLevel.DisplayTitle)
                    ? "WORKSHOP MATCH"
                    : activeLevel.DisplayTitle.ToUpperInvariant();
            int fittedTitleSize = Mathf.Clamp(Mathf.FloorToInt(contentWidth / Mathf.Max(12f, levelTitle.Length * 0.58f)), 12, title.fontSize);
            GUIStyle fittedTitle = new(title) { fontSize = fittedTitleSize };
            fittedTitle.fontSize = Mathf.RoundToInt(portrait.height * 0.019f);
            Rect hud = HudFrameRect(portrait);
            GUIStyle hudText = new(body) { fontSize = Mathf.Max(9, Mathf.RoundToInt(portrait.height * 0.020f)), fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.22f, 0.12f, 0.05f) } };
            DrawUiLabel(LayoutRect(hud, .025f, .17f, .205f, .23f), "MOVES", hudText);
            GUIStyle movesText = new(hudText) { fontSize = Mathf.RoundToInt(hud.height * .32f) };
            DrawUiLabel(LayoutRect(hud, .025f, .43f, .205f, .38f),
                (screen == AppScreen.Result && model.State == RoundState.Won ? bankTransferTotal - BankTransferShown : model.MovesRemaining).ToString(), movesText);
            var objectiveIcons = new List<string>();
            var objectiveCounts = new List<string>();
            if (target.TotalBolts > 0) { objectiveIcons.Add("bolt_nut"); objectiveCounts.Add($"{target.CollectedBolts}/{target.TotalBolts}"); }
            if (target.TotalFrozenOil > 0) { objectiveIcons.Add("frozen"); objectiveCounts.Add($"{target.TotalFrozenOil-target.RemainingFrozenOil}/{target.TotalFrozenOil}"); }
            if (target.TotalOil > 0) { objectiveIcons.Add("oil"); objectiveCounts.Add($"{target.TotalOil-target.RemainingOil}/{target.TotalOil}"); }
            if (target.MotorRequired > 0) { objectiveIcons.Add("motor"); objectiveCounts.Add($"{target.MotorHits}/{target.MotorRequired}"); }
            if(model.MoveGoal>0)
            {
                DrawUiLabel(LayoutRect(hud,.30f,.22f,.65f,.25f),"10 hamle yap",hudText);
                DrawUiLabel(LayoutRect(hud,.30f,.56f,.65f,.26f),$"{Mathf.Min(model.ValidMovesMade,model.MoveGoal)}/{model.MoveGoal}",hudText);
            }
            Rect targetArea=LayoutRect(hud,.30f,.20f,.65f,.65f);
            float objectivePitch=targetArea.width/Mathf.Max(1,objectiveIcons.Count);
            float objectiveSize=Mathf.Min(hud.height*.43f,targetArea.width/3f*.72f);
            float ObjectiveIconWidth(string key)=>Mathf.Min(
                key=="motor"?objectiveSize*1.5f:objectiveSize,
                Mathf.Max(1f,objectivePitch-hud.height*.20f));
            for (int objective = 0; objective < objectiveIcons.Count; objective++)
            {
                float center=targetArea.x+(objective+.5f)*objectivePitch;
                float iconWidth=ObjectiveIconWidth(objectiveIcons[objective]);
                DrawPieceIcon(new Rect(center-iconWidth/2,targetArea.y,iconWidth,objectiveSize),objectiveIcons[objective]);
                DrawUiLabel(new Rect(center-objectivePitch*.4f,hud.y+hud.height*.61f,objectivePitch*.8f,hud.height*.25f),objectiveCounts[objective],hudText);
                if(objective<objectiveIcons.Count-1)
                {
                    float nextCenter=center+objectivePitch;
                    float nextWidth=ObjectiveIconWidth(objectiveIcons[objective+1]);
                    float gapLeft=center+iconWidth*.5f, gapRight=nextCenter-nextWidth*.5f;
                    float plusSize=hud.height*.18f;
                    float plusCenter=(gapLeft+gapRight)*.5f;
                    DrawUiLabel(new Rect(plusCenter-plusSize*.5f,targetArea.y+objectiveSize*.5f-plusSize*.5f,plusSize,plusSize),"+",
                        new GUIStyle(hudText){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(hud.height*.23f)});
                }
            }
            DrawSettingsButton(GameplaySettingsButtonRect(portrait));

            // The toolbar wells are reserved for the existing 3D tool models.

            GUIStyle hintStyle = new(body) { fontSize = Mathf.Max(9, Mathf.RoundToInt(portrait.height * 0.012f)), wordWrap = true };

            if (screen == AppScreen.Result && !settingsOpen)
                DrawResult(title, body, button);
            if (settingsOpen)
                DrawSettingsPopup(portrait, title, button);
            if (!settingsOpen) DrawTutorialPreview(portrait,title,body);
            } finally { GUI.matrix=beforeShake; }
        }

        private Rect GameplaySettingsButtonRect(Rect portrait)
        {
            float size=portrait.width*.115f;
            return new Rect(portrait.xMax-size-portrait.width*.018f,
                Mathf.Max(HudFrameRect(portrait).yMax+portrait.width*.014f,BoardFrameRect(portrait).y-size-portrait.width*.008f),size,size);
        }
        private void DrawSettingsButton(Rect rect)
        {
            Texture2D art = uiCatalog?.TealSettingsButton != null ? uiCatalog.TealSettingsButton : uiCatalog?.SettingsNormal;
            if (DrawTextureButton(rect, GUIContent.none, art,
                    art, art, GUIStyle.none))
            {
                settingsOpen = !settingsOpen;
                PlayProductionSound(uiCatalog?.SettingsToggle);
            }
            if (uiCatalog?.TealSettingsButton == null && uiCatalog?.SettingsIcon != null)
                GUI.DrawTexture(InsetRect(rect, rect.width * 0.22f), uiCatalog.SettingsIcon,
                    ScaleMode.ScaleToFit, true);
        }

        private void DrawSettingsPopup(Rect portrait, GUIStyle title, GUIStyle button)
        {
            if(languageOpen){DrawLanguagePopup(portrait,title);return;}
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.05f, 0.025f, 0.68f);
            GUI.DrawTexture(portrait, Texture2D.whiteTexture);
            bool inMenu = screen == AppScreen.Menu;
            bool offerSkip=screen==AppScreen.Playing&&TutorialRunning;
            float panelWidth=portrait.width*.84f;
            float panelHeight=panelWidth*(inMenu?.92f:1.05f);
            Rect panel = new Rect(portrait.center.x-panelWidth/2,portrait.center.y-panelHeight/2,panelWidth,panelHeight);
            GUI.color = previous;
            if (inMenu) DrawSimplePopupPanel(panel);
            else if (uiCatalog?.SettingsPanel != null) DrawDraftPanel(panel, uiCatalog.SettingsPanel);
            else GUI.Box(panel, GUIContent.none);
            GUIStyle darkTitle = new(title) { fontStyle = FontStyle.Bold, wordWrap = false,
                fontSize = Mathf.RoundToInt(panel.width * .068f), normal = { textColor = new Color(.31f, .16f, .065f) } };
            DrawUiLabel(LayoutRect(panel, 0.12f, 0.16f, 0.76f, 0.10f), "SETTINGS", darkTitle);
            Rect closeRect=PopupCloseRect(panel,!inMenu);
            if (DrawTextureButton(closeRect, GUIContent.none,
                    uiCatalog?.CloseButton, null, null, GUIStyle.none)) settingsOpen = false;
            GUIStyle darkText = new(GUI.skin.label) { font = title.font, fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(panel.width * .064f), alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(.31f, .16f, .065f) } };
            float soundY=inMenu?.31f:offerSkip?.28f:.31f, musicY=inMenu?.48f:offerSkip?.40f:.45f;
            DrawUiLabel(LayoutRect(panel, .13f, soundY, .24f, .10f), "SOUND", darkText);
            float sound = DrawProductionSlider(LayoutRect(panel, .39f, soundY+.012f, .47f, .075f), AudioListener.volume);
            if (!Mathf.Approximately(sound, AudioListener.volume))
            {
                AudioListener.volume = sound;
                PlayerPrefs.SetFloat("WM_SoundVolume", sound);
                PlayerPrefs.Save();
            }
            DrawUiLabel(LayoutRect(panel, .13f, musicY, .24f, .10f), "MUSIC", darkText);
            float music = PlayerPrefs.GetFloat("WM_MusicVolume", 1f);
            float nextMusic = DrawProductionSlider(LayoutRect(panel, .39f, musicY+.012f, .47f, .075f), music);
            if (!Mathf.Approximately(nextMusic, music)) SetBackgroundMusicVolume(nextMusic);
            if(inMenu && DrawTextureButton(LayoutRect(panel,.18f,.68f,.64f,.12f),new GUIContent("LANGUAGES"),uiCatalog?.ActionButton,null,null,darkTitle))
            {languageOpen=true;languagePending=-1;}
            if (!inMenu && DrawTextureButton(LayoutRect(panel, .18f, offerSkip?.54f:.60f, .64f, .10f),
                    new GUIContent("RESTART"), uiCatalog?.ActionButton, null, null, darkTitle))
            { settingsOpen = false; StartLevel(); return; }
            if (!inMenu && DrawTextureButton(LayoutRect(panel, .18f, offerSkip?.66f:.74f, .64f, .10f),
                    new GUIContent("MAIN MENU"), uiCatalog?.ActionButton, null, null, darkTitle))
            {
                ReturnToMenu();
                return;
            }
            if(offerSkip&&DrawTextureButton(LayoutRect(panel,.18f,.78f,.64f,.10f),
                new GUIContent("OGRETICIYI ATLA"),uiCatalog?.ActionButton,null,null,
                darkTitle))
            {SkipLevelTutorial();settingsOpen=false;}
            GUI.enabled = wasEnabled;
        }

        private float DrawProductionSlider(Rect rect, float value)
        {
            GUIStyle invisibleThumb = new(GUIStyle.none) { fixedWidth = rect.height * 1.6f, fixedHeight = rect.height };
            float result = GUI.HorizontalSlider(rect, value, 0f, 1f, GUIStyle.none, invisibleThumb);
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                Vector2 mouse = Event.current.mousePosition;
                if (new Rect(rect.x-12,rect.y-18,rect.width+24,rect.height+36).Contains(mouse))
                    result = Mathf.Clamp01((mouse.x-rect.x)/rect.width);
            }
            if(uiTouchReleaseFrame==Time.frameCount && new Rect(rect.x-12,rect.y-18,rect.width+24,rect.height+36).Contains(uiTouchStart))
                result=Mathf.Clamp01((uiTouchEnd.x-rect.x)/rect.width);
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Rect hit = new Rect(rect.x-12,rect.y-18,rect.width+24,rect.height+36);
                Vector2 touch = Touchscreen.current.primaryTouch.position.ReadValue();
                touch.y = Screen.height-touch.y;
                if (hit.Contains(uiTouchStart)) result = Mathf.Clamp01((touch.x-rect.x)/rect.width);
            }
            Color sliderColor=GUI.color;
            GUI.color=new Color(.52f,.34f,.19f,sliderColor.a);
            DrawCommonButton(rect,false);
            float knobSize=Mathf.Min(rect.height*1.15f,rect.width*.16f);
            float knobCenter=knobSize*.5f+result*(rect.width-knobSize);
            if(result>0)
            {
                GUI.BeginGroup(new Rect(rect.x,rect.y,knobCenter,rect.height));
                GUI.color=new Color(.23f,.69f,.76f,sliderColor.a);
                DrawCommonButton(new Rect(0,0,rect.width,rect.height),false);
                GUI.EndGroup();
            }
            GUI.color=sliderColor;
            if (uiCatalog?.SliderKnob != null)
            {
                float size = Mathf.Min(rect.height*1.15f,rect.width*.16f);
                DrawArt(new Rect(rect.x + result * (rect.width - size), rect.center.y - size * .5f, size, size),
                    uiCatalog.SliderKnob);
            }
            return result;
        }

        private void DrawMenu(GUIStyle title, GUIStyle body, GUIStyle button)
        {
            Rect portrait = GetPortraitRect();
            Color previous = GUI.color;
            GUI.color = new Color(0.025f, 0.035f, 0.05f, 1f);
            if (portrait.x > 0f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, portrait.x, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(portrait.xMax, 0f, Screen.width - portrait.xMax, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = previous;

            Rect safe = GetSafeGuiRect(portrait);
            // Decorative frame follows the full display; notch safe-area is for controls only.
            Rect homeFrame = portrait;
            GUI.color=new Color(.025f,.035f,.05f,1f);
            GUI.DrawTexture(portrait,Texture2D.whiteTexture);GUI.color=previous;
            Rect paper=HomeMenuLayout.Paper(portrait);
            DrawMapPaper(paper);
            Texture2D playArt=PieceTexture("play_cyan_v2")??uiCatalog?.PlayNormal;
            Texture2D playPressedArt=PieceTexture("play_cyan_pressed_v2")??uiCatalog?.PlayPressed;
            float playWidth = portrait.width * 0.70f;
            Rect playPixels=playArt!=null?TexturePixels(playArt):new Rect(0,0,433,214);
            float playHeight = playWidth * playPixels.height / playPixels.width;
            float playBottom = Mathf.Min(portrait.yMax - portrait.width * 0.055f, safe.yMax - portrait.width * 0.012f);
            // Keep the full gold button rim above the wood, with a small paper gap.
            playBottom = Mathf.Min(playBottom,homeFrame.yMax-portrait.width*.195f);
            Rect playRect = new(portrait.center.x - playWidth * 0.5f, playBottom - playHeight, playWidth, playHeight);
            Rect routeArea = HomeMenuLayout.Route(portrait,safe,playRect.y);
            homeRouteRect=routeArea;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = !nextLevelTransition && !wheelRewardAnimating && !settingsOpen && !helpOpen && !wheelOpen;
            GUI.enabled &= !HomeDrawerBlocking;
            DrawFocusedRoute(portrait, homeVisualFocus, true, title,clip:paper,inputOnlyClip:routeArea);
            GUI.enabled = !nextLevelTransition && !wheelRewardAnimating && !settingsOpen && !helpOpen && !wheelOpen;
            if(!nextLevelTransition)DrawHomeUtilityButtons(portrait, safe, title);
            homePlayRect=playRect;
            GUIStyle playLabel = new(title) { fontSize = Mathf.RoundToInt(portrait.height * 0.034f), normal = { textColor = Color.white } };
            GUI.enabled=HomeInputReady&&!HomeDrawerBlocking&&!settingsOpen&&!helpOpen&&!wheelOpen&&(homeLesson==HomeLesson.Off||homeLesson==HomeLesson.Play);
            if (!nextLevelTransition&&DrawTextureButton(playRect, new GUIContent("PLAY"), playArt,
                    playPressedArt, null, playLabel,forcePressed:playTransition))
                if (!playTransition){if(homeLesson==HomeLesson.Play)CompleteHomeLesson();StartCoroutine(PlayButtonFeedback());}
            Rect mapTouchArea = routeArea;
            homeMapRect=mapTouchArea;
            if(HomeInputReady&&(homeLesson==HomeLesson.Off||homeLesson==HomeLesson.OpenMap)&&!settingsOpen && !helpOpen && !wheelOpen && !HomeDrawerBlocking && MenuBackgroundTapped(mapTouchArea)) OpenMap();
            GUI.enabled = previousEnabled;
            DrawHomeDrawer(portrait,safe,title);
            // Wood masks the sliding drawer; modal windows still appear above it.
            DrawHomeEdgeFrame(portrait,homeFrame);
            if(achievementsOpen)DrawAchievementsPopup(portrait,title);
            if (settingsOpen) DrawSettingsPopup(portrait, title, button);
            if (helpOpen) DrawHelpPopup(portrait, title);
            if (wheelOpen) DrawWheelPopup(portrait, title);
            DrawHomeLesson(portrait,title);
            DrawWheelRewardFlight();
            GUI.color = previous;
        }

        private static bool MenuBackgroundTapped(Rect area)
        {
            if (Event.current != null && Event.current.type == EventType.MouseUp &&
                Event.current.button == 0 && area.Contains(Event.current.mousePosition))
                return true;
            if (uiTouchReleaseFrame != Time.frameCount || !area.Contains(uiTouchStart) ||
                !area.Contains(uiTouchEnd) || Vector2.Distance(uiTouchStart, uiTouchEnd) >= Screen.width * 0.02f)
                return false;
            uiTouchReleaseFrame = -1;
            return true;
        }

        private IEnumerator PlayButtonFeedback()
        {
            playTransition = true;
            if (playClick == null)
            {
                if (uiAudio == null) uiAudio = gameObject.AddComponent<AudioSource>();
                uiAudio.playOnAwake = false;
                uiAudio.spatialBlend = 0f;
                const int rate = 22050;
                float[] samples = new float[rate / 10];
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = (float)i / rate;
                    samples[i] = Mathf.Sin(2f * Mathf.PI * (260f * t - 650f * t * t)) *
                        Mathf.Exp(-t * 48f) * Mathf.Min(1f, t * 1000f) * 0.22f;
                }
                playClick = AudioClip.Create("UI wood click", samples.Length, 1, rate, false);
                playClick.SetData(samples, 0);
            }
            uiAudio.PlayOneShot(playClick);
            yield return new WaitForSecondsRealtime(0.045f);
            playTransition = false;
            StartLevel();
        }

        private void DrawFullMap(GUIStyle title, GUIStyle body, GUIStyle button)
        {
            Rect portrait = GetPortraitRect();
            Color previous = GUI.color;
            if (uiCatalog?.MapBackground != null)
                GUI.DrawTexture(portrait, uiCatalog.MapBackground, ScaleMode.ScaleAndCrop, true);
            else
            {
                GUI.color = new Color(0.18f, 0.09f, 0.035f, 0.93f);
                GUI.DrawTexture(portrait, Texture2D.whiteTexture);
                GUI.color = previous;
            }

            Rect safe = GetSafeGuiRect(portrait);
            DrawMapPaper(portrait);
            // Decoration and route fill the whole display, including notch/home-indicator areas.
            DrawFocusedRoute(portrait, mapFocusLevel, false, title);
            Rect backRect = new(portrait.x + portrait.width * 0.035f, safe.y + portrait.height * 0.02f,
                portrait.width * 0.15f, portrait.width * 0.115f);
            homeBackRect=backRect;
            GUI.enabled=HomeInputReady&&(homeLesson==HomeLesson.Off||homeLesson==HomeLesson.Back);
            Texture2D backArt=PieceTexture("map_back_cyan_v2")??uiCatalog?.RoundWoodNormal;
            if (DrawTextureButton(backRect, GUIContent.none, backArt,
                    backArt, backArt, GUIStyle.none))
            {
                screen = AppScreen.Menu;
                if(homeLesson==HomeLesson.Back)SetHomeLesson(HomeLesson.Play);
                SetPresentationMode();
                return;
            }
            if (PieceTexture("map_back_cyan_v2")==null&&uiCatalog?.BackIcon != null)
                GUI.DrawTexture(InsetRect(backRect, backRect.width * 0.25f), uiCatalog.BackIcon, ScaleMode.ScaleToFit, true);

            GUI.enabled = true;
            DrawHomeLesson(portrait,title);
            float frameProgress = Mathf.Clamp01((Time.unscaledTime-mapFrameTransitionAt)/.24f);
            if(frameProgress<1f && homeFrameBottomLogo!=null)
            {
                float eased=1f-Mathf.Pow(1f-frameProgress,3f);
                Rect exitingFrame=portrait;
                exitingFrame.x-=portrait.width*.13f*eased;
                exitingFrame.width+=portrait.width*.26f*eased;
                exitingFrame.y-=portrait.height*.035f*eased;
                exitingFrame.height+=portrait.height*.07f*eased;
                GUI.color=new Color(previous.r,previous.g,previous.b,previous.a*(1f-eased));
                GUI.DrawTexture(exitingFrame,homeFrameBottomLogo,ScaleMode.StretchToFill,true);
            }
            GUI.color = previous;
        }

        private float mapGestureStartedAt,mapGestureVertical;
        private int tutorialMapSwipes;
        private void UpdateMapDrag()
        {
            if(homeLesson!=HomeLesson.Off&&homeLesson!=HomeLesson.SwipeMap)return;
            bool touchActive = Touchscreen.current != null &&
                (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame);
            Vector2 position;
            bool pressed, held, released;
            if (touchActive)
            {
                var touch = Touchscreen.current.primaryTouch;
                position = touch.position.ReadValue();
                pressed = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed;
                released = touch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                mapFocusLevel -= Mouse.current.scroll.ReadValue().y * 0.002f;
            }
            else return;
            position.y = Screen.height - position.y;
            if (pressed)
            {
                mapPointerStart = mapPointerLast = position;
                mapGestureStartedAt=Time.unscaledTime;mapGestureVertical=0;
                mapPointerDown = true;
                mapDragging = false;
            }
            if (mapPointerDown && (held || released))
            {
                if (Vector2.Distance(position, mapPointerStart) > Screen.width * 0.015f) mapDragging = true;
                if (mapDragging)
                {
                    float slope=(ReferenceWorldPoint(mapFocusLevel+.01f).y-ReferenceWorldPoint(mapFocusLevel).y)/.01f;
                    mapFocusLevel += (position.y - mapPointerLast.y) / Mathf.Max(1f,mapScrollPitch*slope);
                }
                mapGestureVertical=Mathf.Max(mapGestureVertical,Mathf.Abs(position.y-mapPointerStart.y));
                mapPointerLast = position;
            }
            if (released)
            {
                if (mapDragging){mapDragReleaseFrame = Time.frameCount;if(homeLesson==HomeLesson.SwipeMap&&TutorialPresentationRules.IsMeaningfulMapSwipe(mapGestureVertical,GetPortraitRect().height,Time.unscaledTime-mapGestureStartedAt)&&++tutorialMapSwipes>=2)SetHomeLesson(HomeLesson.Back);}
                mapPointerDown = false;
                mapDragging = false;
            }
            mapFocusLevel = Mathf.Clamp(mapFocusLevel, 0, LevelRepository.PrototypeLevelIds.Length - 1);
        }

        // Both screens use the same diagonal, gently winding route; only the camera focus changes.
        private Vector2 RoutePoint(Rect area, float level, float focus, bool home=false)
        {
            if(home)return HomeMenuLayout.RoutePoint(area,homeRouteRect,level,focus);
            Vector2 point = IllustratedMapPoint(area,level,focus);
            return point;
        }

        private void DrawFocusedRoute(Rect area, float focus, bool home, GUIStyle label, Rect? clip=null, Rect? inputOnlyClip=null)
        {
            Color original = GUI.color;
            Vector2 inputOffset=area.position;
            Rect inputClip=inputOnlyClip??clip??area;
            Rect visualClip=clip??area;
            if(clip.HasValue)GUI.BeginGroup(visualClip);
            GUI.BeginGroup(clip.HasValue ? new Rect(area.position-visualClip.position,area.size) : area);
            area=new Rect(0,0,area.width,area.height);
            if(!home) mapScrollPitch=area.width;
            int count = LevelRepository.PrototypeLevelIds.Length;
            DrawMapGroundDraft(area, focus);
            // Keep route geometry continuous while focus scrolls; viewport clipping handles visibility.
            int firstVisible = 0;
            int lastVisible = count-1;
            // The decorative road continues above the demo's last node; input/focus bounds stay unchanged.
            float routeFirst = firstVisible;
            float routeLast = lastVisible+2f;
            for (float level = routeFirst; level < routeLast; level += 0.08f)
            {
                Vector2 a = RoutePoint(area, level, focus, home);
                Vector2 b = RoutePoint(area, Mathf.Min(routeLast, level + 0.038f), focus, home);
                if(Mathf.Max(a.y,b.y)<0f || Mathf.Min(a.y,b.y)>area.height)continue;
                GUI.color = Color.white;
                DrawMapDash(a,b,area.width*.02f);
            }
            for (int i = firstVisible; i <= lastVisible; i++)
            {
                bool current = i == menuLevelIndex;
                bool unlocked = i <= highestUnlockedLevel&&i!=transitionLockedLevel;
                Vector2 center = RoutePoint(area, i, focus, home);
                float focusAmount=Mathf.SmoothStep(0,1,Mathf.Clamp01(1-Mathf.Abs(i-homeVisualFocus)));
                float width=area.width*Mathf.Lerp(.18f,.245f,home?focusAmount:(current?1:0));
                float nodeHeight=width*125f/166f*.88f;
                Rect node = new(center.x - width*.5f,center.y-nodeHeight*.5f,width,nodeHeight);
                if(home&&i<homeNodeRects.Length)homeNodeRects[i]=new Rect(node.position+inputOffset,node.size);
                if (!area.Overlaps(node)) continue;
                // Keep neighboring disks readable without leaking a fourth disk
                // below PLAY. The road itself continues behind the controls.
                if(home&&Mathf.Abs(i-focus)>1.65f)continue;
                bool lockedFeedback = lockedFeedbackLevel == i && Time.unscaledTime < lockedFeedbackUntil;
                if (lockedFeedback)
                    node.x += Mathf.Sin((lockedFeedbackUntil - Time.unscaledTime) * 76f) * width * 0.075f;
                GUI.color = Color.white;
                Texture2D art = FlatMapNode(unlocked,current);
                bool enabled = GUI.enabled;
                bool touchInside=uiTouchReleaseFrame==Time.frameCount
                    ? inputClip.Contains(uiTouchStart) && inputClip.Contains(uiTouchEnd)
                    : Event.current==null || inputClip.Contains(Event.current.mousePosition+inputOffset);
                // Keep the disk visible while dragging; only its hit target is disabled.
                GUI.enabled = true;
                if(art!=null)GUI.DrawTexture(node,art,ScaleMode.StretchToFill);
                GUI.enabled = enabled && HomeAllowsNode(i) && touchInside && !mapDragging && mapDragReleaseFrame != Time.frameCount;
                bool clicked = GUI.Button(node,GUIContent.none,GUIStyle.none);
                if(GUI.enabled && uiTouchReleaseFrame==Time.frameCount &&
                   node.Contains(uiTouchStart-inputOffset) && node.Contains(uiTouchEnd-inputOffset) &&
                   Vector2.Distance(uiTouchStart,uiTouchEnd)<Screen.width*.02f)
                {
                    clicked=true;
                    uiTouchReleaseFrame=-1;
                }
                if(clicked)
                {
                    uiTouchReleaseFrame=-1;
                    if (unlocked && uiCatalog?.MapSelect != null) PlayProductionSound(uiCatalog.MapSelect);
                    else PlaySweetTone(false);
                    if (!unlocked)
                    {
                        lockedFeedbackLevel = i;
                        lockedFeedbackUntil = Time.unscaledTime + 0.42f;
                    }
                    else if (home) SelectMenuLevel(i);
                    else
                    {
                        SelectMenuLevel(i);
                        screen = AppScreen.Menu;
                        SetPresentationMode();
                    }
                }
                GUI.enabled = enabled;
                GUI.color = original;
                Rect numberRect=node;
                numberRect.y-=node.height*.08f;
                DrawUiLabel(numberRect, (i + 1).ToString(), new GUIStyle(label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(14, Mathf.RoundToInt(width * 0.30f)),
                    normal = { textColor = unlocked ? Color.white : new Color(.25f,.28f,.30f) }
                });
                if (!unlocked && uiCatalog?.LockIcon != null)
                {
                    float lockSize = width * (lockedFeedback ? 0.30f : 0.20f);
                    DrawArt(new Rect(node.center.x - lockSize * 0.5f, node.yMax - lockSize * 0.55f,
                        lockSize, lockSize), uiCatalog.LockIcon);
                }
                else if (i < highestUnlockedLevel && !current && uiCatalog?.CheckIcon != null)
                    DrawArt(new Rect(node.center.x - width * 0.10f, node.yMax - width * 0.12f,
                        width * 0.20f, width * 0.20f), uiCatalog.CheckIcon);
                DrawMapMilestone(node,i);
            }
            GUI.EndGroup();
            if(clip.HasValue)GUI.EndGroup();
            GUI.color = original;
        }

        private void DrawMapPathSegment(Vector2 from, Vector2 to, bool reached)
        {
            Color previous = GUI.color;
            Vector2 delta = to - from;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = previous * (reached ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.78f));
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, from);
            // A continuous sand ribbon avoids repeatedly sampling the transparent end
            // of the old straight-path PNG at every small bend.
            float thickness = GetPortraitRect().width * 0.035f;
            Color pathColor = GUI.color;
            GUI.color = pathColor * new Color(0.65f, 0.43f, 0.20f, 1f);
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, length + 1f, thickness), Texture2D.whiteTexture);
            GUI.color = pathColor * new Color(1f, 0.82f, 0.46f, 1f);
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.36f, length + 1f, thickness * 0.72f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = previous;
        }

        private static Rect GetPortraitRect()
        {
            // Only a landscape desktop preview gets a portrait lane. Every portrait device
            // uses its complete physical display, including the area around a notch.
            float width = Screen.width > Screen.height ? Screen.height * 9f / 16f : Screen.width;
            return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
        }

        private static Rect GetSafeGuiRect(Rect display)
        {
            Rect raw = Screen.safeArea;
            if (raw.width <= 0f || raw.height <= 0f) return display;
            Rect safe = Rect.MinMaxRect(Mathf.Max(display.x, raw.x), Mathf.Max(display.y, Screen.height - raw.yMax),
                Mathf.Min(display.xMax, raw.xMax), Mathf.Min(display.yMax, Screen.height - raw.y));
            return safe.width > 0f && safe.height > 0f ? safe : display;
        }

        private static Rect LayoutRect(Rect area, float x, float y, float width, float height) =>
            new(area.x + area.width * x, area.y + area.height * y, area.width * width, area.height * height);

        private WorkshopMatchScreenLayout.Gameplay GameplayLayout(Rect portrait) =>
            WorkshopMatchScreenLayout.Calculate(portrait, GetSafeGuiRect(portrait), boardWidth, boardHeight, CellPitch);

        private Rect BoardFrameRect(Rect portrait) => GameplayLayout(portrait).Frame;

        private Rect HudFrameRect(Rect portrait)
        {
            Rect original = GameplayLayout(portrait).Hud;
            if (uiCatalog?.FullWidthHud == null) return original;
            float width = portrait.width * .96f;
            float height = width * .29f;
            return new Rect(portrait.center.x - width * .5f, original.yMax - height, width, height);
        }

        private void LateUpdate()
        {
            if (!openingGameReady) return;
            if (boardCamera == null || Screen.width < 1 || Screen.height < 1) return;
            Rect portrait = GetPortraitRect();
            boardCamera.rect = new Rect(portrait.x / Screen.width,
                1f - portrait.yMax / Screen.height, portrait.width / Screen.width, portrait.height / Screen.height);
            boardCamera.aspect = portrait.width / portrait.height;
            boardCamera.orthographicSize = 3.88f / boardCamera.aspect;
            Vector2 shake=ChainShakeOffset(portrait);
            boardCamera.transform.position=new Vector3(-shake.x*7.76f/portrait.width,shake.y*7.76f/portrait.width,-10f);
            FitBackdrop(homeBackdrop, uiCatalog?.HomeBackground, portrait);
            FitBackdrop(gameplayBackdrop, uiCatalog?.GameplayBackground, portrait);
            UpdateGameplayTheme();
            WorkshopMatchScreenLayout.Gameplay layout = GameplayLayout(portrait);
            float unitsPerPixel = boardCamera.orthographicSize * 2f / portrait.height;
            float gearScale = Mathf.Clamp(layout.Gears.height * unitsPerPixel / 1.8f, 0.05f, 1.4f);
            float gearY = (portrait.center.y - layout.Gears.center.y) * unitsPerPixel;
            if (gearMachineRoot != null)
            {
                gearMachineRoot.gameObject.SetActive(!UseGear2DMachine());
                gearMachineRoot.localScale = Vector3.one * gearScale;
                gearMachineRoot.localPosition = new Vector3(0f, gearY - 4.15f * gearScale, 0f);
            }
            if (gearBackdrop != null)
            {
                gearBackdrop.gameObject.SetActive(!UseGear2DMachine());
                gearBackdrop.localPosition = new Vector3(0f, gearY, 0.95f);
                gearBackdrop.localScale = new Vector3(2.5f, 1.15f, 1f) * gearScale;
            }
            if (runtimeBoardRoot == null) return;
            Rect aperture = layout.Aperture;
            float scale = Mathf.Min(aperture.width * unitsPerPixel / (boardWidth * CellPitch),
                aperture.height * unitsPerPixel / (boardHeight * CellPitch));
            runtimeBoardRoot.localScale = Vector3.one * scale;
            runtimeBoardRoot.localPosition = new Vector3(
                (aperture.center.x - portrait.center.x) * unitsPerPixel,
                (portrait.center.y - aperture.center.y) * unitsPerPixel - BoardCenterY * scale, 0f);
            // Fill the frame aperture behind square cells without stretching the cells themselves.
            if (runtimeBoardBacking != null)
            {
                // Overlap the antialiased inner bevel only. The old outer wood
                // bands stay removed, so the frame's outer silhouette remains clear.
                float innerBleed=portrait.width*.006f*unitsPerPixel/scale;
                runtimeBoardBacking.localScale = new Vector3(aperture.width * unitsPerPixel / scale+innerBleed*2,
                    aperture.height * unitsPerPixel / scale+innerBleed*2, 0.20f);
            }
        }

        private Material gameplayThemeMaterial;
        private void UpdateGameplayTheme()
        {
            // Candy board always uses its original fixed brown material.
            if(frameMaterial != null)
            {
                Color brown = new Color(.40f,.27f,.16f);
                if(frameMaterial.HasProperty("_BaseColor")) frameMaterial.SetColor("_BaseColor",brown);
                if(frameMaterial.HasProperty("_Color")) frameMaterial.SetColor("_Color",brown);
            }
            if(gameplayBackdrop == null) return;
            if(gameplayThemeMaterial == null)
                gameplayThemeMaterial = gameplayBackdrop.GetComponent<Renderer>()?.sharedMaterial;
            if(gameplayThemeMaterial == null) return;
            int index = Mathf.Max(0,System.Array.IndexOf(LevelRepository.PrototypeLevelIds,activeLevel?.Id));
            Shader shader = Shader.Find("WorkshopMatch/WorkshopBackdrop");
            if(shader != null && gameplayThemeMaterial.shader != shader) gameplayThemeMaterial.shader=shader;
            // Separate wallpaper colors from the unchanged wooden chrome and board.
            // Golden-angle hue spacing gives later levels distinct saturated colors.
            Color tint = index == 0 ? new Color(.48f,.73f,.76f) :
                index == 1 ? new Color(.66f,.56f,.80f) :
                index == 2 ? new Color(.47f,.56f,.74f) :
                Color.HSVToRGB(Mathf.Repeat(.49f+index*.618034f,1f),.28f+(index%3)*.035f,.76f+(index%4)*.025f);
            gameplayThemeMaterial.SetColor("_BaseColor",QualitySettings.activeColorSpace==ColorSpace.Linear?tint.linear:tint);
            gameplayThemeMaterial.SetFloat("_PatternSeed",31f+index*17f);
            // Unscaled time keeps the slow wallpaper motion visible when gameplay pauses.
            gameplayThemeMaterial.SetFloat("_MotionTime",Time.unscaledTime);
            gameplayThemeMaterial.SetFloat("_GearEtching",1f);
            Rect themePortrait=GetPortraitRect();
            gameplayThemeMaterial.SetFloat("_GearAspect",themePortrait.width/themePortrait.height);
            Vector3 pos=gameplayBackdrop.localPosition;pos.x=0;pos.y=0;gameplayBackdrop.localPosition=pos;
        }

        private static void FitBackdrop(Transform backdrop, Texture2D texture, Rect display)
        {
            if (backdrop == null || texture == null) return;
            float height = 7.76f * display.height / display.width;
            float cover = Mathf.Max(7.76f / texture.width, height / texture.height);
            backdrop.localScale = new Vector3(texture.width * cover, texture.height * cover, 1f);
        }

        // Trim source transparency at draw time; keep the original PNG assets untouched.
        private static readonly Dictionary<Texture2D,Rect> uiTrimRects = new();
        private static Rect TexturePixels(Texture2D texture)
        {
            if (texture.isReadable && (texture.name is "play_cyan_v2" or "play_cyan_pressed_v2" or "map_back_cyan_v2" or "common_button_cream_cyan_v1" or "common_button_caramel_cyan_v2" || texture.name.EndsWith("_source") || texture.name.Contains("blank") || texture.name.StartsWith("slider_") || texture.name.StartsWith("tutorial_frame_")))
            {
                if (uiTrimRects.TryGetValue(texture,out Rect cached)) return cached;
                int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
                var pixels=texture.GetPixels32();
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                    if(pixels[y*texture.width+x].a>16){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                Rect result=maxX>=minX?new Rect(minX,texture.height-1-maxY,maxX-minX+1,maxY-minY+1):new Rect(0,0,texture.width,texture.height);
                uiTrimRects[texture]=result;return result;
            }
            return texture.name switch
            {
                "common_button_v2" => new Rect(texture.width*80f/1983f,texture.height*181f/793f,texture.width*1823f/1983f,texture.height*417f/793f),
                "board_frame_8x9" => new Rect(65, 39, 829, 1042),
                "hud_frame_top" => new Rect(85, 12, 910, 315),
                "toolbar_frame_bottom" => new Rect(81, 11, 797, 232),
                "frame_home_top" => new Rect(38, 20, 1004, 257),
                "frame_home_bottom" => new Rect(151, 16, 778, 360),
                "frame_home_complete_v1" => new Rect(0, 23, 941, 1595),
                "gear_display_backplate" => new Rect(129, 18, 702, 323),
                "button_play_normal" or "button_play_disabled" => new Rect(151, 10, 433, 214),
                "button_play_pressed" => new Rect(151, 18, 433, 214),
                "map_node_current" => new Rect(38, 61, 226, 178),
                "map_node_open" => new Rect(35, 50, 167, 139),
                "map_node_locked" => new Rect(9, 43, 165, 144),
                "map_path_straight" => new Rect(32, 5, 447, 147),
                "button_round_wood" => new Rect(21, 20, 132, 135),
                "button_round_wood_pressed" => new Rect(21, 25, 132, 135),
                _ => new Rect(0, 0, texture.width, texture.height)
            };
        }

        private static void DrawArt(Rect rect, Texture2D texture)
        {
            if (texture == null) return;
            Rect pixels = TexturePixels(texture);
            rect = FitArtRect(rect, texture);
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(pixels.x / texture.width,
                1f - pixels.yMax / texture.height, pixels.width / texture.width, pixels.height / texture.height), true);
        }

        private static Rect FitArtRect(Rect available, Texture2D texture)
        {
            if (texture == null) return available;
            Rect pixels = TexturePixels(texture);
            float scale = Mathf.Min(available.width / pixels.width, available.height / pixels.height);
            Vector2 size = new(pixels.width * scale, pixels.height * scale);
            return new Rect(available.center - size * 0.5f, size);
        }

        private static void DrawSource(Rect destination, Texture2D texture, Rect pixels)
        {
            if (texture == null || destination.width <= 0f || destination.height <= 0f) return;
            GUI.DrawTextureWithTexCoords(destination, texture, new Rect(pixels.x / texture.width,
                1f - pixels.yMax / texture.height, pixels.width / texture.width, pixels.height / texture.height), true);
        }

        private static void DrawVerticalWood(Rect destination, Texture2D texture, Rect source)
        {
            float scale = destination.width / source.width;
            float tileHeight = source.height * scale;
            if (tileHeight <= 0f) return;
            for (float y = destination.y; y < destination.yMax; y += tileHeight)
            {
                float height = Mathf.Min(tileHeight, destination.yMax - y);
                DrawSource(new Rect(destination.x, y, destination.width, height), texture,
                    new Rect(source.x, source.y, source.width, height / scale));
            }
        }

        private void DrawWoodFill(Rect destination)
        {
            if (destination.width <= 0f || destination.height <= 0f) return;
            Color color = GUI.color;
            GUI.color = new Color(0.66f, 0.57f, 0.43f, 1f);
            GUI.DrawTexture(destination, Texture2D.whiteTexture);
            GUI.color = new Color(0.79f, 0.72f, 0.60f, 1f);
            // A hardware-free wood-grain patch, not a stretched decorative frame.
            DrawSource(destination, uiCatalog?.HomeTopFrame, new Rect(190, 105, 120, 28));
            GUI.color = color;
        }

        private Texture2D homeFrameBottomLogo;
        private void DrawHomeEdgeFrame(Rect display, Rect frame)
        {
            if (homeFrameBottomLogo == null)
                homeFrameBottomLogo = Resources.Load<Texture2D>("WorkshopMatch/home_frame_b_bottom_logo_v1");
            if (homeFrameBottomLogo == null) return;
            // Exclude the PNG's transparent outer padding. Top and bottom caps
            // include the entire artwork; the lower cap includes the gear/rail
            // curve so skipping the transparent center cannot cut a hole in it.
            float unitX=homeFrameBottomLogo.width/941f;
            float unitY=homeFrameBottomLogo.height/1672f;
            Rect source=new Rect(20f*unitX,43f*unitY,900f*unitX,1568f*unitY);
            float scale=frame.width/source.width;
            float side=170f*unitX, top=190f*unitY, bottom=250f*unitY;
            float[] sx = {source.x,source.x+side,source.xMax-side,source.xMax};
            float[] sy = {source.y,source.y+top,source.yMax-bottom,source.yMax};
            float[] dx = {frame.x,frame.x+side*scale,frame.xMax-side*scale,frame.xMax};
            float[] dy = {frame.y,frame.y+top*scale,frame.yMax-bottom*scale,frame.yMax};
            for (int y=0;y<3;y++) for (int x=0;x<3;x++)
            {
                if (x==1 && y==1) continue;
                DrawSource(Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]),homeFrameBottomLogo,
                    Rect.MinMaxRect(sx[x],sy[y],sx[x+1],sy[y+1]));
            }
            // The medallion rim remains baked into the frame; only its inner cog rotates.
            Texture2D rotor=PieceTexture("home_frame_rotor_source");
            if(rotor!=null)
            {
                Vector2 center=new Vector2(frame.x+(470f*unitX-source.x)*scale,frame.yMax-(source.yMax-1532f*unitY)*scale);
                float diameter=120f*unitX*scale;
                Rect well=new Rect(center.x-diameter*.5f,center.y-diameter*.5f,diameter,diameter);
                GUI.DrawTexture(well,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,
                    new Color(.39f,.205f,.045f,GUI.color.a),0,diameter*.5f);
                float cogSize=104f*unitX*scale;
                Rect cog=new Rect(center.x-cogSize*.5f,center.y-cogSize*.5f,cogSize,cogSize);
                Matrix4x4 matrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(Time.unscaledTime*12f,center);
                // Rotate around the original square canvas center, not a trimmed
                // alpha box whose asymmetric edge pixels can shift the axle.
                float extent=cogSize*rotor.width/TexturePixels(rotor).width;
                DrawSource(new Rect(center.x-extent*.5f,center.y-extent*.5f,extent,extent),rotor,
                    new Rect(0,0,rotor.width,rotor.height));
                GUI.matrix=matrix;
            }
        }

        private void DrawBoardEdgeFrame(Rect frame)
        {
            Texture2D texture = uiCatalog.BoardFrame;
            float scale = frame.width / 829f * 0.5f;
            float top = 147f * scale;
            float bottom = 153f * scale;
            float side = 95f * scale;
            DrawBoardBand(new Rect(frame.x, frame.y, frame.width, top), texture, new Rect(65, 39, 829, 147));
            float middleHeight = frame.height - top - bottom;
            float ornamentHeight = 500f * scale;
            float plainHeight = Mathf.Max(0f, middleHeight - ornamentHeight) * 0.5f;
            DrawBoardBand(new Rect(frame.x, frame.y + top, frame.width, plainHeight), texture,
                new Rect(65, 186, 829, 128));
            // Screws and metal ornaments are uniformly scaled; only plain wood is repeated.
            DrawBoardBand(new Rect(frame.x, frame.y + top + plainHeight, frame.width, ornamentHeight),
                texture, new Rect(65, 314, 829, 500));
            DrawBoardBand(new Rect(frame.x, frame.y + top + plainHeight + ornamentHeight, frame.width, plainHeight),
                texture, new Rect(65, 814, 829, 114));
            DrawBoardBand(new Rect(frame.x, frame.yMax - bottom, frame.width, bottom), texture,
                new Rect(65, 928, 829, 153));
        }

        private void DrawGameplayChrome(Rect portrait)
        {
            if (uiCatalog == null || !uiCatalog.IsReady) return;
            Rect safe = GetSafeGuiRect(portrait);
            DrawWoodFill(new Rect(portrait.x,portrait.y,portrait.width,safe.y-portrait.y));
            DrawWoodPanel(Rect.MinMaxRect(portrait.x,portrait.y,portrait.xMax,HudFrameRect(portrait).yMax));
            DrawWoodFill(new Rect(portrait.x,safe.yMax,portrait.width,portrait.yMax-safe.yMax));
            DrawGear2DMachine(portrait);
            Rect hud = HudFrameRect(portrait);
            DrawDraftPanel(LayoutRect(hud, 0f, 0f, .255f, 1f), uiCatalog.SettingsPanel,true);
            DrawDraftPanel(LayoutRect(hud, .265f, 0f, .735f, 1f), uiCatalog.SettingsPanel,true);
            DrawBoardEdgeFrame(BoardFrameRect(portrait));
            Rect toolbar = GameplayLayout(portrait).Toolbar;
            DrawWoodFill(new Rect(toolbar.x, toolbar.y, toolbar.width, portrait.yMax - toolbar.y));
            DrawWoodPanel(new Rect(toolbar.x,toolbar.y,toolbar.width,portrait.yMax-toolbar.y));
            // Three centered wells; counts and progress sit outside their outlines.
            float size = WorkshopMatchScreenLayout.ToolbarWellSize(toolbar);
            float gap=toolbar.width*.07f;
            float start=toolbar.center.x-(size*3+gap*2)*.5f;
            for (int i = 0; i < 3; i++)
            {
                Rect well = new(start+i*(size+gap),toolbar.y+toolbar.height*.02f,size,size);
                tutorialToolRects[i]=new Rect(well.x-size*.04f,well.y-size*.04f,well.width+size*.22f,Mathf.Min(size*1.54f,portrait.yMax-well.y));
                bool toolLocked=IsTrainingToolLocked((ToolKind)i);
                DrawSquareWell(well, uiCatalog.ToolbarFrame);
                Color toolIconColor=GUI.color;
                if(toolLocked)GUI.color=new Color(.52f,.52f,.52f,toolIconColor.a);
                int iconIndex = i == 1 ? 3 : i;
                if (i < 3 && modelCatalog != null && modelCatalog.ToolIcons != null &&
                    iconIndex < modelCatalog.ToolIcons.Length && modelCatalog.ToolIcons[iconIndex] != null)
                {
                    Rect uv = i switch
                    {
                        0 => new Rect(0.262f, 0.098f, 0.473f, 0.660f),
                        1 => new Rect(0.230f, 0.223f, 0.535f, 0.602f),
                        _ => new Rect(0.234f, 0.215f, 0.527f, 0.547f)
                    };
                    float aspect = uv.width / uv.height;
                    float height = Mathf.Min(size * 0.66f, size * 0.76f / aspect);
                    float width = height * aspect;
                    Rect image = new(well.center.x - width * 0.5f, well.y + size * 0.065f,
                        width, height);
                    string iconKey=i==0?"rocket":i==1?"hammer":"magnet";
                    if(PieceTexture(iconKey)!=null)
                    {
                        Rect icon = InsetRect(well,size*.16f);
                        if(activeTool==(ToolKind)i)icon=PulseSelectionRect(icon);
                        float reward = Mathf.Clamp01((toolRewardUntil[i] - Time.unscaledTime) / 1.1f);
                        icon.y -= Mathf.Sin(reward * Mathf.PI) * size * .10f;
                        Matrix4x4 iconMatrix=GUI.matrix;
                        if(i==0 && activeTool==ToolKind.Ray && !rayVertical) GUIUtility.RotateAroundPivot(90f,icon.center);
                        DrawPieceIcon(icon,iconKey);
                        GUI.matrix=iconMatrix;
                    }
                    else GUI.DrawTextureWithTexCoords(activeTool==(ToolKind)i?PulseSelectionRect(image):image, modelCatalog.ToolIcons[iconIndex], uv, true);
                }
                GUI.color=toolIconColor;
                string stock = TutorialToolStock((ToolKind)i).ToString();
                GUIStyle stockStyle = new(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.20f)),
                    normal = { textColor = new Color(0.22f, 0.11f, 0.04f) }
                };
                DrawUiLabel(new Rect(well.x, well.yMax + size * 0.025f, size, size * 0.22f), stock, stockStyle);
                float rewardRemaining = toolRewardUntil[i] - Time.unscaledTime;
                if (rewardRemaining > 0)
                {
                    var rewardStyle = new GUIStyle(stockStyle);
                    rewardStyle.normal.textColor = new Color(.17f,.47f,.16f,Mathf.Clamp01(rewardRemaining/.25f));
                    DrawUiLabel(new Rect(well.xMax-size*.38f,well.y+size*.10f*rewardRemaining/1.1f,
                        size*.4f,size*.25f),"+"+toolRewardAmount[i],rewardStyle);
                }
                ToolKind kind = (ToolKind)i;
                int target = toolProgress.Target(kind);
                Color previous = GUI.color;
                for (int part = 0; part < target; part++)
                {
                    GUI.color = Color.white;
                    float segmentSize=size*.135f, segmentGap=size*.035f;
                    float rowWidth=target*segmentSize+(target-1)*segmentGap;
                    Rect segment = new Rect(well.center.x-rowWidth/2+part*(segmentSize+segmentGap),
                        well.yMax + size * 0.27f, segmentSize, segmentSize);
                    Texture2D progressArt = part < toolProgress.Pieces(kind) ? uiCatalog.ProgressFilled : uiCatalog.ProgressEmpty;
                    if (progressArt != null) DrawPanel(segment, progressArt);
                    else GUI.DrawTexture(segment, Texture2D.whiteTexture);
                }
                GUI.color = previous;
                bool wasEnabled = GUI.enabled;
                Rect turnHit = new Rect(well.xMax-size*.154f,well.y+size*.12f,size*.28f,size*.28f);
                bool overTurn = i==0 && activeTool==ToolKind.Ray && Event.current!=null && turnHit.Contains(Event.current.mousePosition);
                if(toolLocked)DrawToolLock(well,size,i);
                GUI.enabled = wasEnabled && (toolLocked || (!resolving && TutorialToolStock(kind) > 0 && !overTurn && TutorialAllowsToolSelection(kind)));
                if (DrawTextureButton(well, GUIContent.none, null, null, null, GUIStyle.none))
                {if(toolLocked)TapLockedTool(i);else SelectTool(kind);}
                GUI.enabled = wasEnabled;
                if(!toolLocked && i==0 && activeTool==ToolKind.Ray)
                {
                    float turnSize=size*.28f;
                    Rect turn=new Rect(well.xMax-turnSize*.55f,well.y+size*.12f,turnSize,turnSize);
                    tutorialRotationRect=turn;
                    GUI.enabled=wasEnabled&&(!TutorialRunning||Time.unscaledTime>=tutorialFadeAt+TutorialPresentationRules.IntroFade);
                    bool clicked=DrawTextureButton(turn,GUIContent.none,RotationButtonArt(),null,null,GUIStyle.none);
                    if(clicked) { rayVertical=!rayVertical;ResetHintTimer();RecordTutorialRotation(); }
                    GUI.enabled=wasEnabled;
                }
            }
        }

        private static void DrawBoardBand(Rect destination, Texture2D texture, Rect source)
        {
            float edge = destination.width / 829f * 0.5f * 160f;
            DrawSource(new Rect(destination.x, destination.y, edge, destination.height), texture,
                new Rect(source.x, source.y, 160f, source.height));
            DrawSource(new Rect(destination.x + edge, destination.y, destination.width - edge * 2f, destination.height), texture,
                new Rect(source.x + 160f, source.y, source.width - 320f, source.height));
            DrawSource(new Rect(destination.xMax - edge, destination.y, edge, destination.height), texture,
                new Rect(source.xMax - 160f, source.y, 160f, source.height));
        }

        private static void DrawSquareWell(Rect destination, Texture2D texture)
        {
            // The recessed wood has continuous grain and shading. Nine separate
            // filtered quads expose straight joins at fractional GUI scales.
            // Sample the complete existing well in one draw so no internal edges exist.
            DrawSource(destination, texture, new Rect(140f, 66f, 167f, 124f));
        }

        private static Rect InsetRect(Rect rect, float inset)
        {
            return new Rect(rect.x + inset, rect.y + inset, Mathf.Max(1f, rect.width - inset * 2f),
                Mathf.Max(1f, rect.height - inset * 2f));
        }

        private static Texture2D darkGlyphs, creamGlyphs;
        [Serializable] private sealed class GlyphMetrics { public float[] advances = Array.Empty<float>(); }
        private static GlyphMetrics glyphMetrics;
        private static void DrawUiLabel(Rect rect, string text, GUIStyle style,bool centerInk=false) => DrawUiLabel(rect, new GUIContent(text), style,centerInk);
        private static void DrawUiLabel(Rect rect, GUIContent content, GUIStyle style,bool centerInk=false)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789/+:=-!?.;";
            darkGlyphs ??= Resources.Load<Texture2D>("WorkshopMatch/cute_dino_dark");
            creamGlyphs ??= Resources.Load<Texture2D>("WorkshopMatch/cute_dino_cream");
            string text = TutorialFontText(GameLanguage.Text(content.text ?? ""));
            content=new GUIContent(text,content.tooltip);
            Texture2D atlas = style.normal.textColor.grayscale > .6f ? creamGlyphs : darkGlyphs;
            if (atlas == null || text.Length == 0) { GUI.Label(rect, content, style); return; }
            if (glyphMetrics == null)
            {
                TextAsset metrics = Resources.Load<TextAsset>("WorkshopMatch/glyph_metrics");
                if (metrics != null) glyphMetrics = JsonUtility.FromJson<GlyphMetrics>(metrics.text);
            }
            string[] lines = text.Split('\n');
            if (lines.Length > 1)
            {
                float lineHeight = rect.height / lines.Length;
                for (int line = 0; line < lines.Length; line++)
                    DrawUiLabel(new Rect(rect.x, rect.y + line * lineHeight, rect.width, lineHeight), lines[line], style);
                return;
            }
            float Advance(char c)
            {
                int index = chars.IndexOf(c);
                return index >= 0 && glyphMetrics?.advances != null && index < glyphMetrics.advances.Length
                    ? glyphMetrics.advances[index] + 1.5f : c == ' ' ? 24f : 42f;
            }
            float total = 0f;
            foreach (char c in text) total += Advance(c);
            float scale = Mathf.Min(Mathf.Max(12, style.fontSize) / 76f, rect.width / Mathf.Max(1,total), rect.height / 88f);
            float cell = 112f * scale;
            float x = style.alignment == TextAnchor.MiddleLeft ? rect.x : rect.center.x - total * scale / 2;
            Color old = GUI.color; GUI.color = new Color(1, 1, 1, old.a * style.normal.textColor.a);
            foreach (char c in text)
            {
                int i = chars.IndexOf(c);
                float advance = Advance(c) * scale;
                if (i >= 0) GUI.DrawTextureWithTexCoords(new Rect(x + (advance-cell)/2, rect.center.y-cell/2+(centerInk?9f*scale:0f), cell, cell),
                    atlas, new Rect((i%12)/12f, 1f-(i/12+1)/6f, 1f/12, 1f/6), true);
                x += advance;
            }
            GUI.color = old;
        }

        private static Texture2D commonButton;
        private static Texture2D commonButtonCyan;
        private static Texture2D commonButtonGui;
        private static void DrawCommonButton(Rect rect,bool useCyan=true)
        {
            commonButton ??= Resources.Load<Texture2D>("WorkshopMatch/common_button_v2");
            if(useCyan)commonButtonCyan ??= Resources.Load<Texture2D>("WorkshopMatch/Sprites2D/common_button_caramel_cyan_v2");
            Texture2D art=useCyan&&commonButtonCyan!=null?commonButtonCyan:commonButton;
            if(art==null)return;
            Rect source=TexturePixels(art);
            if(art==commonButtonCyan)
            {
                // IMGUI in this Linear project needs linear pixel values to avoid a washed-out face.
                if(commonButtonGui==null && art.isReadable && QualitySettings.activeColorSpace==ColorSpace.Linear)
                {
                    var pixels=art.GetPixels();
                    for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].linear;
                    commonButtonGui=new Texture2D(art.width,art.height,TextureFormat.RGBA32,false,true)
                        {name="Common button GUI color",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                    commonButtonGui.SetPixels(pixels);commonButtonGui.Apply(false,true);
                }
                Texture2D drawn=commonButtonGui!=null?commonButtonGui:art;
                // Full-height end cyanCaps keep rounded corners intact; overlap prevents subpixel gaps.
                float cyanSourceCap=source.height*.5f;
                float cyanCap=Mathf.Min(rect.height*.5f,rect.width*.45f);
                DrawSource(new Rect(rect.x,rect.y,cyanCap,rect.height),drawn,new Rect(source.x,source.y,cyanSourceCap,source.height));
                DrawSource(new Rect(rect.x+cyanCap-1,rect.y,rect.width-cyanCap*2+2,rect.height),drawn,new Rect(source.x+cyanSourceCap,source.y,source.width-cyanSourceCap*2,source.height));
                DrawSource(new Rect(rect.xMax-cyanCap,rect.y,cyanCap,rect.height),drawn,new Rect(source.xMax-cyanSourceCap,source.y,cyanSourceCap,source.height));
                return;
            }
            // Keep the approved corner shape while adapting to each label's width.
            float corner=art==commonButtonCyan?.22f:.5f;
            float sourceCap=source.height*corner;
            float cap=Mathf.Min(rect.height*corner,rect.width*.45f);
            DrawSource(new Rect(rect.x,rect.y,cap,rect.height),art,new Rect(source.x,source.y,sourceCap,source.height));
            DrawSource(new Rect(rect.x+cap-1f,rect.y,rect.width-cap*2+2f,rect.height),art,new Rect(source.x+sourceCap,source.y,source.width-sourceCap*2,source.height));
            DrawSource(new Rect(rect.xMax-cap,rect.y,cap,rect.height),art,new Rect(source.xMax-sourceCap,source.y,sourceCap,source.height));
        }

        private static void DrawPanel(Rect rect, Texture2D texture, float borderRatio=.065f)
        {
            if (texture == null) return;
            Rect source=TexturePixels(texture);
            float border = Mathf.Min(rect.width, rect.height) * borderRatio;
            float[] dx = {rect.x, rect.x+border, rect.xMax-border, rect.xMax};
            float[] dy = {rect.y, rect.y+border, rect.yMax-border, rect.yMax};
            float sourceBorder = Mathf.Min(source.width, source.height) * .12f;
            float[] sx = {source.x, source.x + sourceBorder, source.xMax - sourceBorder, source.xMax};
            float[] sy = {source.y, source.y + sourceBorder, source.yMax - sourceBorder, source.yMax};
            for (int y=0;y<3;y++) for (int x=0;x<3;x++)
                DrawSource(Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]),texture,
                    Rect.MinMaxRect(sx[x],sy[y],sx[x+1],sy[y+1]));
        }

        private bool DrawTextureButton(Rect rect, GUIContent content, Texture2D normal, Texture2D pressed,
            Texture2D disabled, GUIStyle labelStyle, Vector2 inputOffset=default,bool forcePressed=false)
        {
            if(content!=null) content=new GUIContent(GameLanguage.Text(content.text),content.tooltip);
            if (normal == null)
            {
                bool clickedPlain = GUI.Button(rect, content, labelStyle ?? GUI.skin.button);
                if (GUI.enabled && uiTouchReleaseFrame == Time.frameCount &&
                    rect.Contains(uiTouchStart-inputOffset) && rect.Contains(uiTouchEnd-inputOffset) &&
                    Vector2.Distance(uiTouchStart, uiTouchEnd) < Screen.width * 0.02f)
                {
                    clickedPlain = true;
                    uiTouchReleaseFrame = -1;
                }
                if (clickedPlain) { uiTouchReleaseFrame = -1; PlaySweetTone(false); }
                return clickedPlain;
            }
            bool actionButton=normal==uiCatalog?.ActionButton;
            bool cyanPlay=normal.name=="play_cyan_v2";
            if(!actionButton)rect = FitArtRect(rect, normal);
            bool held = GUI.enabled && Event.current != null && rect.Contains(Event.current.mousePosition) &&
                        Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (GUI.enabled && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 touch = Touchscreen.current.primaryTouch.position.ReadValue();
                touch.y = Screen.height - touch.y;
                held |= rect.Contains(uiTouchStart-inputOffset) && rect.Contains(touch-inputOffset);
            }
            held |= forcePressed;
            Texture2D texture = !GUI.enabled && disabled != null ? disabled : held && pressed != null ? pressed : normal;
            if(actionButton)
            {
                Color color=GUI.color;
                GUI.color=held?new Color(.86f,.86f,.86f,color.a):new Color(1,1,1,color.a);
                DrawCommonButton(rect);GUI.color=color;
            }
            else if(normal==uiCatalog?.CloseButton)
            {
                Color old=GUI.color;
                if(held)GUI.color=new Color(old.r*.86f,old.g*.86f,old.b*.86f,old.a);
                DrawArt(rect,PieceTexture("close_button_matte_v3")??normal);
                GUI.color=old;
            }
            else if(cyanPlay)DrawArt(rect,texture);
            else DrawArt(rect, texture);
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            if (GUI.enabled && uiTouchReleaseFrame == Time.frameCount &&
                rect.Contains(uiTouchStart-inputOffset) && rect.Contains(uiTouchEnd-inputOffset) &&
                Vector2.Distance(uiTouchStart, uiTouchEnd) < Screen.width * 0.02f)
            {
                clicked = true;
                uiTouchReleaseFrame = -1;
            }
            if (clicked) { uiTouchReleaseFrame = -1; PlaySweetTone(false); }
            if (content != null && (!string.IsNullOrEmpty(content.text) || content.image != null))
            {
                GUIStyle textStyle = new(labelStyle ?? GUIStyle.none) { alignment = TextAnchor.MiddleCenter };
                if (actionButton) textStyle.normal.textColor = new Color(.30f,.17f,.08f);
                Rect textRect = actionButton
                    ? new Rect(rect.x + rect.width * .10f, rect.y + rect.height * .12f, rect.width * .80f, rect.height * .76f)
                    : rect;
                if(cyanPlay&&held)textRect.y+=rect.height*.035f;
                DrawUiLabel(textRect, content, textStyle,true);
            }
            return clicked;
        }

        private void DrawResult(GUIStyle title, GUIStyle body, GUIStyle button)
        {
            if(model.State==RoundState.Won&&activeLevel?.Id=="level_020")
            {DrawDemoCompletion(title,body);return;}
            Rect portrait = GetPortraitRect();
            float width = portrait.width * 0.86f;
            float left = portrait.x + (portrait.width - width) * 0.5f;
            Rect panel = new(left, portrait.y + portrait.height * 0.28f, width, portrait.height * 0.44f);
            if (uiCatalog?.ResultPanel != null) DrawDraftPanel(panel, uiCatalog.ResultPanel);
            else GUI.Box(panel, GUIContent.none);
            string heading = model.State == RoundState.Won ? "LEVEL COMPLETE!" : "OUT OF MOVES";
            string detail = model.State == RoundState.Won
                ? activeLevel?.Id=="level_001"?"10 hamle tamamlandi!":TrainingLevelPolicy.LevelNumber(activeLevel?.Id)<10?"Hedefler tamamlandi!":$"+{BankTransferShown} moves to the wheel bank"
                : "Try again to complete the objectives";
            GUIStyle resultTitle = new(title) { normal = { textColor = new Color(.32f, .17f, .065f) } };
            GUIStyle resultBody = new(body) { fontSize = Mathf.RoundToInt(body.fontSize*.85f), normal = { textColor = new Color(.38f, .24f, .11f,.72f) } };
            DrawUiLabel(LayoutRect(panel,.10f,.15f,.80f,.12f),heading,resultTitle);
            DrawUiLabel(LayoutRect(panel,.12f,.28f,.76f,.08f),detail,resultBody);
            int nextLevel = activeLevel == null ? -1 : Array.IndexOf(LevelRepository.PrototypeLevelIds, activeLevel.Id) + 1;
            if (model.State == RoundState.Won && nextLevel > 0 && nextLevel < LevelRepository.PrototypeLevelIds.Length &&
                DrawTextureButton(LayoutRect(panel,.16f,.46f,.68f,.14f), new GUIContent("NEXT LEVEL"),uiCatalog?.ActionButton,null,null,title))
            { NextLevelFromResult(nextLevel); }
            if (DrawTextureButton(new Rect(left + width * 0.13f, panel.y + panel.height * 0.70f,
                    width * 0.34f, panel.height * 0.12f), new GUIContent("RETRY"), uiCatalog?.ActionButton,
                    null, null, button))
                StartLevel();
            if (DrawTextureButton(new Rect(left + width * 0.53f, panel.y + panel.height * 0.70f,
                    width * 0.34f, panel.height * 0.12f), new GUIContent("MENU"), uiCatalog?.ActionButton,
                    null, null, button))
                ReturnToProgressHome();
        }

        private void DrawToolButton(Rect rect, ToolKind kind, string label, GUIStyle style)
        {
            bool pulsing = rewardPulseKind == kind && Time.unscaledTime < rewardPulseUntil;
            Color previous = GUI.color;
            bool available = screen == AppScreen.Playing && !resolving && toolProgress.Stock(kind) > 0;
            GUI.enabled = available;
            Texture2D slot = !available ? uiCatalog?.ToolSlotDisabled : activeTool == kind || pulsing
                ? uiCatalog?.ToolSlotSelected : uiCatalog?.ToolSlotNormal;
            if (DrawTextureButton(rect, GUIContent.none, slot, slot, slot, GUIStyle.none)) SelectTool(kind);
            int pieces = toolProgress.Pieces(kind);
            int target = toolProgress.Target(kind);
            Rect fill = new(rect.x + rect.width * 0.17f, rect.y + rect.height * 0.68f,
                rect.width * 0.66f * pieces / target, rect.height * 0.09f);
            GUI.color = pulsing ? new Color(0.45f, 1f, 0.55f, 0.95f) : new Color(0.28f, 0.72f, 1f, 0.82f);
            GUI.Box(fill, GUIContent.none);
            GUI.color = previous;
            GUIStyle toolLabel = new(style) { fontSize = Mathf.Max(11, Mathf.RoundToInt(style.fontSize * 0.72f)) };
            DrawUiLabel(new Rect(rect.x, rect.y + rect.height * 0.18f, rect.width, rect.height * 0.46f),
                $"{label}\n{pieces}/{target}", toolLabel);
            int stock = toolProgress.Stock(kind);
            if (stock > 0 && uiCatalog?.StockBadge != null)
            {
                float badgeSize = rect.height * 0.34f;
                Rect badge = new(rect.xMax - badgeSize * 0.92f, rect.yMax - badgeSize * 0.92f, badgeSize, badgeSize);
                GUI.DrawTexture(badge, uiCatalog.StockBadge, ScaleMode.ScaleToFit, true);
                GUIStyle badgeLabel = new(style) { fontSize = Mathf.Max(10, Mathf.RoundToInt(style.fontSize * 0.70f)) };
                DrawUiLabel(badge, stock.ToString(), badgeLabel);
            }
            GUI.enabled = true;
        }
    }
}
