using UnityEditor;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    public sealed class OpeningAssetImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/WorkshopMatch/Resources/WorkshopMatch/Opening/";
        private const string Sound = "Assets/WorkshopMatch/Resources/WorkshopMatch/Audio/opening_clockwork.mp3";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
            Apply((TextureImporter)assetImporter);
        }

        private static void Apply(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.format = TextureImporterFormat.RGBA32;
            defaults.maxTextureSize = 4096;
            defaults.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(defaults);
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            foreach (string platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
            {
                var settings = importer.GetPlatformTextureSettings(platform);
                settings.overridden = false;
                importer.SetPlatformTextureSettings(settings);
            }
        }

        private void OnPreprocessAudio()
        {
            if (assetPath != Sound) return;
            ApplyAudio((AudioImporter)assetImporter);
        }

        private static void ApplyAudio(AudioImporter importer)
        {
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.forceToMono = false; // Preserve the selected source's authored channels.
        }

        // Also repair textures imported while this new postprocessor was compiling.
        [InitializeOnLoadMethod]
        private static void ScheduleImportCheck() => EditorApplication.delayCall += CheckExistingImports;

        private static void CheckExistingImports()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += CheckExistingImports; return; }
            foreach (string name in new[] { "background", "foreground", "gear", "logo_hd", "cluster_hd" })
            {
                var importer = AssetImporter.GetAtPath(Root + "opening_" + name + ".png") as TextureImporter;
                if (importer == null || (importer.npotScale == TextureImporterNPOTScale.None &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed &&
                    !importer.mipmapEnabled && importer.alphaIsTransparency && importer.filterMode == FilterMode.Bilinear)) continue;
                Apply(importer);
                importer.SaveAndReimport();
            }
            var audio = AssetImporter.GetAtPath(Sound) as AudioImporter;
            if (audio != null && (audio.defaultSampleSettings.compressionFormat != AudioCompressionFormat.PCM ||
                audio.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad || !audio.loadInBackground ||
                audio.forceToMono || !audio.defaultSampleSettings.preloadAudioData))
            {
                ApplyAudio(audio);
                audio.SaveAndReimport();
            }
        }
    }
}
