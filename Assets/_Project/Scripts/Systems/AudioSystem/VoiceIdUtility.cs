using System.Globalization;
using System.Text;

namespace _Project.Scripts.Systems.AudioSystem
{
    public static class VoiceIdUtility
    {
        public static string ForStory(string cutsceneId, int lineIndex)
        {
            return $"VO_CS_{NormalizeIdSegment(cutsceneId)}_{lineIndex.ToString("000", CultureInfo.InvariantCulture)}";
        }

        public static string ForTutorial(string tutorialId, int lineIndex)
        {
            return $"VO_TUT_{NormalizeIdSegment(tutorialId)}_{lineIndex.ToString("000", CultureInfo.InvariantCulture)}";
        }

        public static string ForGameplay(string dialogueId)
        {
            return $"VO_GP_{NormalizeIdSegment(dialogueId)}";
        }

        public static string NormalizeIdSegment(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var result = new StringBuilder(value.Length);
            bool lastWasUnderscore = false;

            foreach (char rawCharacter in value.ToUpperInvariant())
            {
                bool separator = char.IsWhiteSpace(rawCharacter) || rawCharacter == '-' || rawCharacter == '_';
                if (separator)
                {
                    if (result.Length > 0 && !lastWasUnderscore)
                    {
                        result.Append('_');
                        lastWasUnderscore = true;
                    }

                    continue;
                }

                bool supported = (rawCharacter >= 'A' && rawCharacter <= 'Z')
                    || (rawCharacter >= '0' && rawCharacter <= '9');
                if (!supported)
                {
                    continue;
                }

                result.Append(rawCharacter);
                lastWasUnderscore = false;
            }

            if (result.Length > 0 && result[result.Length - 1] == '_')
            {
                result.Length--;
            }

            return result.ToString();
        }
    }
}
