using UnityEditor;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    public sealed class WorkshopMusicImportSettings : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if(assetPath!="Assets/WorkshopMatch/Resources/WorkshopMatch/Audio/wizard_music.wav")return;
            var importer=(AudioImporter)assetImporter;
            var sample=importer.defaultSampleSettings;
            sample.loadType=AudioClipLoadType.CompressedInMemory;
            sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.55f;
            importer.defaultSampleSettings=sample;
            importer.loadInBackground=true;importer.forceToMono=false;
        }
    }
}
