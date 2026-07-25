using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Voice
{
    internal sealed class VoiceAudioImporter : AssetPostprocessor
    {
        internal const string VoiceRoot = "Assets/_Project/Audio/Voice/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(VoiceRoot, System.StringComparison.Ordinal))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = true;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
        }
    }
}
