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
        [SerializeField] private Image progressBackgroundImage;
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
            float safeProgress = Mathf.Clamp(progressValue, 0f, safeTarget);
            float normalizedProgress = safeTarget > 0f
                ? Mathf.Clamp01(safeProgress / safeTarget)
                : 1f;

            SetSprite(backgroundImage, GetBackgroundSprite(state));
            SetSprite(statusIconImage, GetStatusIconSprite(state));
            SetText(phaseText, state == MissionRowState.Active ? $"{missionNumber:00} / {mission.Phase}" : string.Empty);
            SetText(titleText, GetTitle(mission, state));
            SetText(progressText, state == MissionRowState.Active
                ? $"{FormatValue(safeProgress)} / {FormatValue(safeTarget)}"
                : string.Empty);
            SetText(rewardText, string.Empty);
            SetText(stateText, string.Empty);

            SetGraphicVisible(statusIconImage, state == MissionRowState.Completed || state == MissionRowState.Locked);
            SetTextVisible(phaseText, state == MissionRowState.Active);
            SetTextVisible(progressText, state == MissionRowState.Active);
            SetTextVisible(rewardText, false);
            SetTextVisible(stateText, false);
            SetGraphicVisible(progressBackgroundImage, state == MissionRowState.Active);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = state == MissionRowState.Active ? normalizedProgress : 0f;
                progressFillImage.gameObject.SetActive(state == MissionRowState.Active);
            }
        }

        public void ConfigureLockedPhaseCard()
        {
            gameObject.SetActive(true);

            SetSprite(backgroundImage, lockedSprite);
            SetSprite(statusIconImage, lockIconSprite);
            SetGraphicVisible(statusIconImage, true);
            SetText(titleText, "LOCKED PHASE");
            SetText(phaseText, string.Empty);
            SetText(progressText, string.Empty);
            SetText(rewardText, string.Empty);
            SetText(stateText, string.Empty);
            SetTextVisible(phaseText, false);
            SetTextVisible(progressText, false);
            SetTextVisible(rewardText, false);
            SetTextVisible(stateText, false);
            SetGraphicVisible(progressBackgroundImage, false);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = 0f;
                progressFillImage.gameObject.SetActive(false);
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

        private static string GetTitle(MissionDefinition mission, MissionRowState state)
        {
            return state switch
            {
                MissionRowState.Locked => "ENCRYPTED OBJECTIVE",
                _ => mission.Title
            };
        }

        private static void SetGraphicVisible(Graphic graphic, bool visible)
        {
            if (graphic != null)
            {
                graphic.gameObject.SetActive(visible);
            }
        }

        private static void SetTextVisible(TextMeshProUGUI text, bool visible)
        {
            if (text != null)
            {
                text.gameObject.SetActive(visible);
            }
        }

        private static string FormatValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString("N0")
                : value.ToString("0.0");
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null)
            {
                image.sprite = sprite;
                image.enabled = sprite != null;
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
