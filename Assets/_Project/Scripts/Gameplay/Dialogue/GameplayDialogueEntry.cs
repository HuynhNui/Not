using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Dialogue
{
    [Serializable]
    public sealed class GameplayDialogueEntry : IEquatable<GameplayDialogueEntry>
    {
        [SerializeField] private string dialogueId;
        [SerializeField] private PsychologyPhase psychologyPhase;
        [SerializeField] private string tag;
        [SerializeField, TextArea] private string text;

        public GameplayDialogueEntry(
            string dialogueId,
            PsychologyPhase psychologyPhase,
            string tag,
            string text)
        {
            this.dialogueId = dialogueId;
            this.psychologyPhase = psychologyPhase;
            this.tag = tag;
            this.text = text;
        }

        public string DialogueId => dialogueId;
        public PsychologyPhase PsychologyPhase => psychologyPhase;
        public string Tag => tag;
        public string Text => text;
        public bool IsOpening => string.Equals(tag, GameplayDialogueTags.Opening, StringComparison.OrdinalIgnoreCase);

        public bool Equals(GameplayDialogueEntry other)
        {
            return other != null
                && string.Equals(dialogueId, other.dialogueId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GameplayDialogueEntry);
        }

        public override int GetHashCode()
        {
            return dialogueId != null ? dialogueId.GetHashCode() : 0;
        }
    }

    public static class GameplayDialogueTags
    {
        public const string Opening = "OPENING";
    }
}
