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

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.quality = 1f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }
    }
}
