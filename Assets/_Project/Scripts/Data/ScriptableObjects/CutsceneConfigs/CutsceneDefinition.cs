using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.CutsceneConfigs
{
    [CreateAssetMenu(
        fileName = "CutsceneDefinition",
        menuName = "Chibi Pixel Gate/Data/Cutscene Definition")]
    public sealed class CutsceneDefinition : ScriptableObject
    {
        [SerializeField] private string cutsceneId;
        [SerializeField] private CutsceneTriggerType triggerType;
        [SerializeField] private int triggerValue;
        [SerializeField] private bool playOnlyOnce = true;
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        public string Id => cutsceneId;
        public CutsceneTriggerType TriggerType => triggerType;
        public int TriggerValue => Mathf.Max(0, triggerValue);
        public bool PlayOnlyOnce => playOnlyOnce;
        public IReadOnlyList<DialogueLine> Lines => lines;
        public bool HasLines => lines != null && lines.Count > 0;

        public void ConfigureRuntime(
            string runtimeId,
            CutsceneTriggerType runtimeTriggerType,
            int runtimeTriggerValue,
            bool runtimePlayOnlyOnce,
            IEnumerable<DialogueLine> runtimeLines)
        {
            cutsceneId = runtimeId;
            triggerType = runtimeTriggerType;
            triggerValue = Mathf.Max(0, runtimeTriggerValue);
            playOnlyOnce = runtimePlayOnlyOnce;
            lines = runtimeLines != null
                ? new List<DialogueLine>(runtimeLines)
                : new List<DialogueLine>();
        }

        private void OnValidate()
        {
            cutsceneId = string.IsNullOrWhiteSpace(cutsceneId)
                ? string.Empty
                : cutsceneId.Trim();
            triggerValue = Mathf.Max(0, triggerValue);
            lines ??= new List<DialogueLine>();
        }
    }

    public enum CutsceneTriggerType
    {
        BeforeFirstRun,
        AfterCompletedRun
    }

    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private string speakerId;
        [SerializeField] private string speakerName;
        [SerializeField, TextArea(2, 6)] private string text;
        [SerializeField] private Sprite avatar;
        [SerializeField] private AudioClip blipAudio;

        public DialogueLine()
        {
        }

        public DialogueLine(
            string speakerId,
            string speakerName,
            string text,
            Sprite avatar = null,
            AudioClip blipAudio = null)
        {
            this.speakerId = speakerId;
            this.speakerName = speakerName;
            this.text = text;
            this.avatar = avatar;
            this.blipAudio = blipAudio;
        }

        public string SpeakerId => speakerId;
        public string SpeakerName => string.IsNullOrWhiteSpace(speakerName)
            ? speakerId
            : speakerName;
        public string Text => text ?? string.Empty;
        public Sprite Avatar => avatar;
        public AudioClip BlipAudio => blipAudio;
    }
}
