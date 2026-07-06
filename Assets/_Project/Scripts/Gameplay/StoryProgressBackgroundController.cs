using _Project.Cutscenes;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay
{
    public sealed class StoryProgressBackgroundController : MonoBehaviour
    {
        [SerializeField] private ScrollingGameplayBackground scrollingBackground;
        [SerializeField] private Sprite bg1;
        [SerializeField] private Sprite bg2;
        [SerializeField] private Sprite bg3;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SaveService.Instance.DataChanged -= RefreshBackground;
            SaveService.Instance.DataChanged += RefreshBackground;
            RefreshBackground();
        }

        private void OnDisable()
        {
            if (!SaveService.HasInstance)
            {
                return;
            }

            SaveService.Instance.DataChanged -= RefreshBackground;
        }

        public void RefreshBackground()
        {
            if (scrollingBackground == null)
            {
                return;
            }

            Sprite sprite = ResolveSpriteForSave(SaveService.Instance.Data);
            scrollingBackground.SetBackgroundSprite(sprite);
        }

        private Sprite ResolveSpriteForSave(SaveData saveData)
        {
            if (saveData == null)
            {
                return bg1;
            }

            if (HasReachedBg3(saveData))
            {
                return bg3 != null ? bg3 : bg1;
            }

            if (HasReachedBg2(saveData))
            {
                return bg2 != null ? bg2 : bg1;
            }

            return bg1;
        }

        private static bool HasReachedBg2(SaveData saveData)
        {
            return saveData.HasSeenCutscene(StoryCutsceneIds.GateMemoryLeak)
                || saveData.HasSeenCutscene(StoryCutsceneIds.HumanCommand);
        }

        private static bool HasReachedBg3(SaveData saveData)
        {
            return saveData.HasSeenCutscene(StoryCutsceneIds.SystemFatigue)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoicePreChoice)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoiceContinueProtocol)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoiceShutDownCore)
                || saveData.HasSeenCutscene(StoryCutsceneIds.FinalChoice);
        }

        private void ResolveReferences()
        {
            if (scrollingBackground == null)
            {
                scrollingBackground = GetComponent<ScrollingGameplayBackground>();
            }
        }
    }
}
