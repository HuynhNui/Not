using System;
using _Project.Scripts.Data.ScriptableObjects.CutsceneConfigs;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.CutsceneSystem
{
    public sealed class CutsceneManager : MonoBehaviour
    {
        [SerializeField] private StoryProgressionService storyProgressionService;
        [SerializeField] private DialogueManager dialogueManager;

        public bool IsPlaying => dialogueManager != null && dialogueManager.IsPlaying;
        public StoryProgressionService StoryProgression => storyProgressionService;

        public void Init()
        {
            if (storyProgressionService == null)
            {
                storyProgressionService = FindAnyObjectByType<StoryProgressionService>();
            }

            if (storyProgressionService == null)
            {
                storyProgressionService = gameObject.AddComponent<StoryProgressionService>();
            }

            storyProgressionService.Init();

            if (dialogueManager == null)
            {
                dialogueManager = FindAnyObjectByType<DialogueManager>();
            }

            if (dialogueManager == null)
            {
                dialogueManager = gameObject.AddComponent<DialogueManager>();
            }

            dialogueManager.Init();
        }

        private void Awake()
        {
            Init();
        }

        public void Play(CutsceneDefinition definition, Action onComplete)
        {
            if (definition == null || !definition.HasLines)
            {
                onComplete?.Invoke();
                return;
            }

            Init();

            bool canSkip = SaveService.Instance.HasSeenCutscene(definition.Id);
            dialogueManager.Play(definition, canSkip, () =>
            {
                SaveService.Instance.RecordCutsceneSeen(definition.Id);
                onComplete?.Invoke();
            });
        }
    }
}
