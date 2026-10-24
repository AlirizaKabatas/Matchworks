using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    public static class WorkshopMatchUiCatalogBuilder
    {
        private const string SourceFolder = "Assets/Art/UI/WorkshopMatchGenerated";
        private const string ProductionFolder = "Assets/Art/UI/WorkshopMatchProduction";
        private const string CatalogPath = "Assets/Resources/WorkshopMatch/WorkshopMatchUiCatalog.asset";

        [MenuItem("Window/Workshop Match/Rebuild UI Catalog")]
        public static void Rebuild()
        {
            ConfigureImporters();
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath) ?? "Assets/Resources/WorkshopMatch");
            WorkshopMatchUiCatalog catalog = AssetDatabase.LoadAssetAtPath<WorkshopMatchUiCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorkshopMatchUiCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.HomeBackground = Load("bg_home_path_v2");
            catalog.HomeCompleteFrame = Load("frame_home_complete_v1");
            catalog.MapBackground = Load("bg_map_valley");
            catalog.GameplayBackground = Load("bg_game_workshop");
            catalog.HomeTopFrame = Load("frame_home_top");
            catalog.HomeBottomFrame = Load("frame_home_bottom");
            catalog.PlayNormal = Load("button_play_normal");
            catalog.PlayPressed = Load("button_play_pressed");
            catalog.PlayDisabled = Load("button_play_disabled");
            catalog.MapNodeOpen = Load("map_node_open");
            catalog.MapNodeCurrent = Load("map_node_current");
            catalog.MapNodeLocked = Load("map_node_locked");
            catalog.MapPathStraight = Load("map_path_straight");
            catalog.MapPathCurve = Load("map_path_curve");
            catalog.StarFilled = Load("icon_star_filled");
            catalog.StarEmpty = Load("icon_star_empty");
            catalog.LockIcon = Load("icon_lock");
            catalog.CheckIcon = Load("icon_check");
            catalog.BackIcon = Load("icon_back");
            catalog.RoundWoodNormal = Load("button_round_wood");
            catalog.RoundWoodPressed = Load("button_round_wood_pressed");
            catalog.HudFrame = Load("hud_frame_top");
            catalog.GearBackplate = Load("gear_display_backplate");
            catalog.BoardFrame = Load("board_frame_8x9");
            catalog.ToolbarFrame = Load("toolbar_frame_bottom");
            catalog.ToolSlotNormal = Load("tool_slot_normal");
            catalog.ToolSlotSelected = Load("tool_slot_selected");
            catalog.ToolSlotDisabled = Load("tool_slot_disabled");
            catalog.SettingsNormal = Load("button_settings_round");
            catalog.SettingsPressed = Load("button_settings_round_pressed");
            catalog.SettingsIcon = Load("icon_settings");
            catalog.StockBadge = Load("badge_stock_red");
            catalog.CellNormal = Load("cell_tile_normal");
            catalog.CellBlocked = Load("cell_tile_blocked");

            catalog.UiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/cute_dino_2/Cute Dino.ttf");
            catalog.FullWidthHud = LoadProduction("Group02_HUD/hud_full_width_source");
            catalog.ProgressEmpty = LoadProduction("Group03_Progress/segment_empty_source");
            catalog.ProgressFilled = LoadProduction("Group03_Progress/segment_filled_source");
            catalog.TealSettingsButton = LoadProduction("Group04_Settings/settings_button_teal");
            catalog.SettingsPanel = LoadProduction("Group04_Settings/settings_panel_blank");
            catalog.SliderRail = LoadProduction("Group04_Settings/slider_rail");
            catalog.SliderKnob = LoadProduction("Group04_Settings/slider_knob");
            catalog.ActionButton = LoadProduction("Group04_Settings/action_button_blank");
            catalog.CloseButton = LoadProduction("Group04_Settings/close_button_teal");
            catalog.ResultPanel = LoadProduction("Group05_MapResult/result_panel_blank");
            catalog.NutRust2 = LoadProduction("Group06_Rust/nut_rust2_source");
            catalog.NutRust1 = LoadProduction("Group06_Rust/nut_rust1_source");
            catalog.NutClean = LoadProduction("Group06_Rust/nut_clean_existing");
            catalog.ScrewRust2 = LoadProduction("Group06_Rust/screw_rust2_source");
            catalog.ScrewRust1 = LoadProduction("Group06_Rust/screw_rust1_source");
            catalog.ScrewClean = LoadProduction("Group06_Rust/screw_clean_existing");
            catalog.CandyPop = LoadProduction("Group07_VFX/candy_pop_source");
            catalog.OilSplash = LoadProduction("Group07_VFX/oil_splash_source");
            catalog.FrozenOilShards = LoadProduction("Group07_VFX/frozen_oil_shards_source");
            catalog.HammerImpact = LoadProduction("Group07_VFX/hammer_impact_source");
            catalog.MagnetGlint = LoadProduction("Group07_VFX/magnet_glint_source");
            catalog.RocketSparks = LoadProduction("Group07_VFX/rocket_sparks_source");
            catalog.MotorBurst = LoadProduction("Group07_VFX/motor_burst_source");
            catalog.ChainLabel = LoadProduction("Group08_Chain/chain_label_blank");
            catalog.ChainFlame = LoadProduction("Group08_Chain/flame_tongue");
            catalog.UiClick = LoadAudio("ui_click");
            catalog.MapSelect = LoadAudio("map_select");
            catalog.SettingsToggle = LoadAudio("settings_toggle");
            catalog.CandyPopSound = LoadAudio("candy_pop");
            catalog.ChainStep = LoadAudio("chain_step");
            catalog.BoltPing = LoadAudio("bolt_ping");
            catalog.OilSquish = LoadAudio("oil_squish");
            catalog.FrozenOilCrack = LoadAudio("frozen_oil_crack");
            catalog.HammerHit = LoadAudio("hammer_hit");
            catalog.MagnetClose = LoadAudio("magnet_close");
            catalog.MagnetPull = LoadAudio("magnet_pull");
            catalog.RocketIgnite = LoadAudio("rocket_ignite");
            catalog.RocketFlight = LoadAudio("rocket_flight");
            catalog.RocketBurst = LoadAudio("rocket_burst");
            catalog.MotorBreak = LoadAudio("motor_break");
            catalog.LevelWin = LoadAudio("level_win");
            catalog.LevelLose = LoadAudio("level_lose");
            catalog.GearSpinLoopDraft = LoadAudio("gear_spin_loop_draft");
            catalog.MotorRunLoopDraft = LoadAudio("motor_run_loop_draft");

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!catalog.IsReady)
                throw new InvalidOperationException("Workshop Match UI catalog has missing required textures.");
            Debug.Log("T12_UI_CATALOG_READY base_textures=34 production_groups=9 required=1 alpha_import=1");
        }

        private static Texture2D Load(string fileName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SourceFolder}/{fileName}.png");
            if (texture == null) throw new FileNotFoundException($"Missing UI texture: {fileName}.png");
            return texture;
        }

        private static Texture2D LoadProduction(string fileName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProductionFolder}/{fileName}.png");
            if (texture == null) throw new FileNotFoundException($"Missing production texture: {fileName}.png");
            return texture;
        }

        private static AudioClip LoadAudio(string fileName)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{ProductionFolder}/Group09_Audio/{fileName}.wav");
            if (clip == null) throw new FileNotFoundException($"Missing production audio: {fileName}.wav");
            return clip;
        }

        private static void ConfigureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceFolder, ProductionFolder, "Assets/Resources/WorkshopMatch" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               !importer.alphaIsTransparency || importer.mipmapEnabled ||
                               !importer.isReadable || importer.npotScale != TextureImporterNPOTScale.None;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                if (changed) importer.SaveAndReimport();
            }
        }

        [InitializeOnLoadMethod]
        private static void RepairUiImportsAfterReload()
        {
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) ConfigureImporters(); };
        }
    }
}
