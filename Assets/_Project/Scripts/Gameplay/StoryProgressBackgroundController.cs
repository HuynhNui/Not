using _Project.Scripts.Gameplay.Dialogue;
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
            switch (StoryPsychologyPhaseResolver.Resolve(saveData))
            {
                case PsychologyPhase.Awakening:
                    return bg3 != null ? bg3 : bg1;
                case PsychologyPhase.Doubt:
                    return bg2 != null ? bg2 : bg1;
                default:
                    return bg1;
            }
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
