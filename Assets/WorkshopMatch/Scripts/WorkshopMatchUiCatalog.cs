using UnityEngine;

namespace WorkshopMatch
{
    [CreateAssetMenu(menuName = "Workshop Match/UI Catalog", fileName = "WorkshopMatchUiCatalog")]
    public sealed class WorkshopMatchUiCatalog : ScriptableObject
    {
        [Header("Backgrounds")]
        public Texture2D HomeBackground;
        public Texture2D MapBackground;
        public Texture2D GameplayBackground;

        [Header("Home")]
        public Texture2D HomeCompleteFrame;
        public Texture2D HomeTopFrame;
        public Texture2D HomeBottomFrame;
        public Texture2D PlayNormal;
        public Texture2D PlayPressed;
        public Texture2D PlayDisabled;

        [Header("Map")]
        public Texture2D MapNodeOpen;
        public Texture2D MapNodeCurrent;
        public Texture2D MapNodeLocked;
        public Texture2D MapPathStraight;
        public Texture2D MapPathCurve;
        public Texture2D StarFilled;
        public Texture2D StarEmpty;
        public Texture2D LockIcon;
        public Texture2D CheckIcon;
        public Texture2D BackIcon;
        public Texture2D RoundWoodNormal;
        public Texture2D RoundWoodPressed;

        [Header("Gameplay")]
        public Texture2D HudFrame;
        public Texture2D GearBackplate;
        public Texture2D BoardFrame;
        public Texture2D ToolbarFrame;
        public Texture2D ToolSlotNormal;
        public Texture2D ToolSlotSelected;
        public Texture2D ToolSlotDisabled;
        public Texture2D SettingsNormal;
        public Texture2D SettingsPressed;
        public Texture2D SettingsIcon;
        public Texture2D StockBadge;
        public Texture2D CellNormal;
        public Texture2D CellBlocked;

        [Header("Production UI")]
        public Font UiFont;
        public Texture2D FullWidthHud;
        public Texture2D ProgressEmpty;
        public Texture2D ProgressFilled;
        public Texture2D TealSettingsButton;
        public Texture2D SettingsPanel;
        public Texture2D SliderRail;
        public Texture2D SliderKnob;
        public Texture2D ActionButton;
        public Texture2D CloseButton;
        public Texture2D ResultPanel;
        public Texture2D ChainLabel;
        public Texture2D ChainFlame;

        [Header("Production Board Sprites")]
        public Texture2D NutRust2;
        public Texture2D NutRust1;
        public Texture2D NutClean;
        public Texture2D ScrewRust2;
        public Texture2D ScrewRust1;
        public Texture2D ScrewClean;

        [Header("Production VFX")]
        public Texture2D CandyPop;
        public Texture2D OilSplash;
        public Texture2D FrozenOilShards;
        public Texture2D HammerImpact;
        public Texture2D MagnetGlint;
        public Texture2D RocketSparks;
        public Texture2D MotorBurst;

        [Header("Production Audio")]
        public AudioClip UiClick;
        public AudioClip MapSelect;
        public AudioClip SettingsToggle;
        public AudioClip CandyPopSound;
        public AudioClip ChainStep;
        public AudioClip BoltPing;
        public AudioClip OilSquish;
        public AudioClip FrozenOilCrack;
        public AudioClip HammerHit;
        public AudioClip MagnetClose;
        public AudioClip MagnetPull;
        public AudioClip RocketIgnite;
        public AudioClip RocketFlight;
        public AudioClip RocketBurst;
        public AudioClip MotorBreak;
        public AudioClip LevelWin;
        public AudioClip LevelLose;
        public AudioClip GearSpinLoopDraft;
        public AudioClip MotorRunLoopDraft;

        public bool IsReady => HomeBackground != null && MapBackground != null && GameplayBackground != null &&
                               HomeTopFrame != null && HomeBottomFrame != null && PlayNormal != null &&
                               MapNodeOpen != null && MapNodeCurrent != null && MapNodeLocked != null &&
                               HudFrame != null && BoardFrame != null && ToolbarFrame != null &&
                               ToolSlotNormal != null && SettingsNormal != null;
    }
}
