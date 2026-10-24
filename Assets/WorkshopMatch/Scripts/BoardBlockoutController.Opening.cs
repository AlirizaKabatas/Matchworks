using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private readonly OpeningScreenTiming openingTiming = new();
        private ResourceRequest openingBackgroundRequest, openingLogoRequest, openingClusterRequest, openingGearRequest, openingWoodRequest, openingAudioRequest;
        private Texture2D openingBackground, openingLogo, openingCluster, openingGear, openingWood;
        private AudioSource openingAudio;
        private OpeningScreenView openingView;
        private bool openingArtResolved, openingGameReady, openingFirstLaunch;
        private float openingRotationStartedAt;
        private ThreadPriority savedOpeningLoadingPriority;
        private bool openingLoadingPriorityOverridden;

        private bool OpeningArtLoaded => openingArtResolved;

        private IEnumerator PreloadOpeningPieceTextures()
        {
            var requests = new Dictionary<string, ResourceRequest>();
            foreach (string key in new[] { "candy_0", "candy_1", "candy_2", "candy_3", "candy_4", "hammer", "magnet", "rocket", "frozen", "motor" })
            {
                string source = key == "candy_0" ? "candy_0_hex" : key == "candy_2" ? "candy_2_full" : key;
                requests[key] = Resources.LoadAsync<Texture2D>("WorkshopMatch/Sprites2D/" + source);
            }
            foreach (var item in requests)
            {
                while (!item.Value.isDone) yield return null;
                var texture = item.Value.asset as Texture2D;
                pieceTextures[item.Key] = texture;
                // Reading alpha bounds can be expensive on the first encounter. Do one
                // sprite per frame instead of all five during the first visible row.
                if (texture != null) PieceRect(texture);
                yield return null;
            }
        }

        private void BeginOpening()
        {
            BeginOpeningTrace(false);
            // Give asset integration a smaller per-frame budget while the gears
            // are visible. The gate already waits if loading takes longer.
            savedOpeningLoadingPriority=Application.backgroundLoadingPriority;
            openingLoadingPriorityOverridden=true;
            Application.backgroundLoadingPriority=ThreadPriority.Low;
            openingTiming.Begin();
            Canvas.willRenderCanvases += OnOpeningCanvasRendering;
            openingRotationStartedAt = Time.unscaledTime;
            const string root = "WorkshopMatch/Opening/";
            openingBackgroundRequest = Resources.LoadAsync<Texture2D>(root + "opening_background");
            openingLogoRequest = Resources.LoadAsync<Texture2D>(root + "opening_logo_hd");
            openingClusterRequest = Resources.LoadAsync<Texture2D>(root + "opening_cluster_hd");
            openingGearRequest = Resources.LoadAsync<Texture2D>(root + "opening_gear");
            openingWoodRequest = Resources.LoadAsync<Texture2D>("WorkshopMatch/home_frame_b_bottom_logo_v1");
            openingAudioRequest = Resources.LoadAsync<AudioClip>("WorkshopMatch/Audio/opening_clockwork");
            openingAudio = gameObject.AddComponent<AudioSource>();
            openingAudio.playOnAwake = false;
            openingAudio.loop = true;
            openingAudio.spatialBlend = 0;
            openingAudio.volume = .52f;
            // Match the SOUND preference even before the main game has initialized.
            AudioListener.volume = PlayerPrefs.GetFloat("WM_SoundVolume",
                PlayerPrefs.GetInt("WM_SoundEnabled", 1) == 1 ? 1f : 0f);
        }

        private void ResolveOpeningArt()
        {
            if (openingArtResolved || !openingBackgroundRequest.isDone ||
                !openingLogoRequest.isDone || !openingClusterRequest.isDone || !openingGearRequest.isDone ||
                !openingWoodRequest.isDone) return;
            openingBackground = openingBackgroundRequest.asset as Texture2D;
            openingLogo = openingLogoRequest.asset as Texture2D;
            openingCluster = openingClusterRequest.asset as Texture2D;
            openingGear = openingGearRequest.asset as Texture2D;
            openingWood = openingWoodRequest.asset as Texture2D;
            openingBackgroundRequest = openingLogoRequest = openingClusterRequest = openingGearRequest = openingWoodRequest = null;
            openingArtResolved = true;
            if (openingBackground == null || openingLogo == null || openingCluster == null || openingGear == null)
                Debug.LogError("MATCHWORKS_OPENING_ART_MISSING: check Resources/WorkshopMatch/Opening.");
            else openingView = new OpeningScreenView(transform, openingBackground, openingLogo, openingCluster, openingGear, openingWood);
        }

        private bool UpdateOpening()
        {
            if (!openingTiming.Visible) return false;
            SampleOpeningTrace();
            ResolveOpeningArt();
            if (openingView != null)
            {
                Rect portrait = GetPortraitRect();
                openingView.Layout(portrait, GetSafeGuiRect(portrait));
                openingView.Rotate(Time.unscaledTime - openingRotationStartedAt);
            }
            if (openingAudioRequest != null && openingAudioRequest.isDone)
            {
                openingAudio.clip = openingAudioRequest.asset as AudioClip;
                openingAudioRequest = null;
            }
            if (openingTiming.HasPresented && openingAudio != null && openingAudio.clip != null && !openingAudio.isPlaying)
                openingAudio.Play();
            bool ready = openingGameReady && openingAudioRequest == null && backgroundMusicRequest == null;
            if (!openingTiming.CanClose(Time.realtimeSinceStartupAsDouble, ready)) return true;
            // All startup work and minimum exposure are complete. No fade-out delay.
            EnterPreparedGame();
            EndOpeningTrace();
            RestoreOpeningLoadingPriority();
            openingTiming.Close();
            openingView?.SetVisible(false);
            StopOpeningAudio();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // Final players never expose replay controls; release the opening-only memory.
            ReleaseOpeningAssets();
#endif
            backgroundMusic?.UnPause();
            uiTouchReleaseFrame = -1;
            lastInteractionAt = Time.unscaledTime;
            return true; // Never pass the closing frame's pointer/key through to gameplay.
        }

        private bool DrawOpening(Rect portrait)
        {
            if (!openingTiming.Visible) return false;
            if (openingView == null)
            {
                Color color = GUI.color;
                GUI.color = new Color(.02f, .70f, .77f);
                GUI.DrawTexture(portrait, Texture2D.whiteTexture);
                GUI.color = color;
            }
            return true;
        }

        private void OnOpeningCanvasRendering()
        {
            if (openingTiming.Visible && openingView != null && openingView.Root.activeInHierarchy && !openingTiming.HasPresented)
            {
                openingTiming.Presented(Time.realtimeSinceStartupAsDouble);
                openingRotationStartedAt = Time.unscaledTime;
            }
        }

        private void DestroyOpeningView()
        {
            RestoreOpeningLoadingPriority();
            Canvas.willRenderCanvases -= OnOpeningCanvasRendering;
            if (openingView != null && openingView.Root != null) Destroy(openingView.Root);
            openingView = null;
        }

        private void RestoreOpeningLoadingPriority()
        {
            if(!openingLoadingPriorityOverridden)return;
            Application.backgroundLoadingPriority=savedOpeningLoadingPriority;
            openingLoadingPriorityOverridden=false;
        }

        private void StopOpeningAudio()
        {
            if (openingAudio != null) openingAudio.Stop();
        }

        private void ReleaseOpeningAssets()
        {
            if (openingBackground != null) Resources.UnloadAsset(openingBackground);
            DestroyOpeningView();
            if (openingLogo != null) Resources.UnloadAsset(openingLogo);
            if (openingCluster != null) Resources.UnloadAsset(openingCluster);
            if (openingGear != null) Resources.UnloadAsset(openingGear);
            openingBackground = openingLogo = openingCluster = openingGear = null;
            // Shared main-menu wood stays loaded for DrawHomeEdgeFrame.
            openingWood = null;
            if (openingAudio != null && openingAudio.clip != null)
            {
                AudioClip clip = openingAudio.clip;
                openingAudio.clip = null;
                Resources.UnloadAsset(clip);
            }
        }

    }
}
