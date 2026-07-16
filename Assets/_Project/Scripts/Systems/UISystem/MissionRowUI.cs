using _Project.Scripts.Systems.MissionSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.UISystem
{
    public sealed class MissionRowUI : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image statusIconImage;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite checkIconSprite;
        [SerializeField] private Sprite lockIconSprite;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI stateText;

        public void Configure(
            MissionDefinition mission,
            int missionNumber,
            MissionRowState state,
            float progressValue,
            float targetValue)
        {
            if (mission == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            float safeTarget = Mathf.Max(0f, targetValue);
            float safeProgress = state == MissionRowState.Completed
                ? safeTarget
                : Mathf.Clamp(progressValue, 0f, safeTarget);
            float normalizedProgress = safeTarget > 0f
                ? Mathf.Clamp01(safeProgress / safeTarget)
                : 1f;

            SetSprite(backgroundImage, GetBackgroundSprite(state));
            SetSprite(statusIconImage, GetStatusIconSprite(state));
            SetText(phaseText, $"{missionNumber:00} / {mission.Phase}");
            SetText(titleText, mission.Title);
            SetText(progressText, state == MissionRowState.Locked
                ? "LOCKED"
                : $"{FormatValue(safeProgress)} / {FormatValue(safeTarget)}");
            SetText(rewardText, mission.RewardCoins > 0 ? $"+{mission.RewardCoins:N0}" : string.Empty);
            SetText(stateText, GetStateLabel(state));

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = state == MissionRowState.Locked ? 0f : normalizedProgress;
            }
        }

        private Sprite GetBackgroundSprite(MissionRowState state)
        {
            return state switch
            {
                MissionRowState.Active => activeSprite,
                MissionRowState.Completed => completedSprite,
                MissionRowState.Locked => lockedSprite,
                _ => null
            };
        }

        private Sprite GetStatusIconSprite(MissionRowState state)
        {
            return state switch
            {
                MissionRowState.Completed => checkIconSprite,
                MissionRowState.Locked => lockIconSprite,
                _ => null
            };
        }

        private static string GetStateLabel(MissionRowState state)
        {
            return state switch
            {
                MissionRowState.Active => "ACTIVE",
                MissionRowState.Completed => "DONE",
                MissionRowState.Locked => "LOCKED",
                _ => string.Empty
            };
        }

        private static string FormatValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString("N0")
                : value.ToString("0.0");
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
            }
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }

    public enum MissionRowState
    {
        Locked,
        Active,
        Completed
    }
}
