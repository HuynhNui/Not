using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Dialogue;

namespace _Project.Editor.Voice
{
    internal static class GameplayVoiceEmotionResolver
    {
        private static readonly Dictionary<string, string> Mappings =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Key(PsychologyPhase.Protocol, "OPENING")] = "neutral",
                [Key(PsychologyPhase.Protocol, "COMBAT")] = "alert",
                [Key(PsychologyPhase.Protocol, "UPGRADE")] = "proud",
                [Key(PsychologyPhase.Protocol, "LOOP")] = "neutral",
                [Key(PsychologyPhase.Protocol, "BELIEF")] = "proud",
                [Key(PsychologyPhase.Doubt, "MEMORY")] = "confused",
                [Key(PsychologyPhase.Doubt, "OBSERVATION")] = "confused",
                [Key(PsychologyPhase.Doubt, "DEFINITION")] = "hesitant",
                [Key(PsychologyPhase.Doubt, "LOOP")] = "hesitant",
                [Key(PsychologyPhase.Doubt, "SELF")] = "hesitant",
                [Key(PsychologyPhase.Awakening, "AWARENESS")] = "tired",
                [Key(PsychologyPhase.Awakening, "GUILT")] = "broken",
                [Key(PsychologyPhase.Awakening, "FATIGUE")] = "tired",
                [Key(PsychologyPhase.Awakening, "RESISTANCE")] = "defiant",
                [Key(PsychologyPhase.Awakening, "FREEDOM")] = "calm"
            };

        public static string Resolve(PsychologyPhase phase, string tag)
        {
            return Mappings.TryGetValue(Key(phase, tag), out string emotion)
                ? emotion
                : "unknown";
        }

        private static string Key(PsychologyPhase phase, string tag)
        {
            return $"{(int)phase}:{(tag ?? string.Empty).Trim().ToUpperInvariant()}";
        }
    }
}
