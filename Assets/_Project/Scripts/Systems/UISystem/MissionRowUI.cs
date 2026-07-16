using System;
using _Project.Scripts.Systems.MissionSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private Image rewardCoinImage;
        [SerializeField] private Button claimButton;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite claimableSprite;
        [FormerlySerializedAs("completedSprite")]
        [SerializeField] private Sprite claimedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite phaseLockedSprite;
        [SerializeField] private Sprite checkIconSprite;
        [SerializeField] private Sprite lockIconSprite;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI claimButtonText;

        public void Configure(
            MissionDefinition mission,
            int missionNumber,
            MissionRowState state,
            float progressValue,
            float targetValue,
            Func<string, bool> onClaimRequested = null)
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
            ApplyLayout(state);
            SetText(phaseText, state == MissionRowState.Active ? $"{missionNumber:00} / {mission.Phase}" : string.Empty);
            SetText(titleText, GetTitle(mission, state));
            SetText(progressText, state == MissionRowState.Active
                ? $"{FormatValue(safeProgress)}/{FormatValue(safeTarget)}"
                : string.Empty);
            SetText(rewardText, ShouldShowReward(state) ? $"+{mission.RewardCoins}" : string.Empty);
            SetText(stateText, string.Empty);
            SetText(claimButtonText, "CLAIM");

            SetGraphicVisible(statusIconImage, state == MissionRowState.CompletedUnclaimed
                || state == MissionRowState.CompletedClaimed
                || state == MissionRowState.Locked);
            SetTextVisible(phaseText, state == MissionRowState.Active);
            SetTextVisible(progressText, state == MissionRowState.Active);
            SetTextVisible(rewardText, ShouldShowReward(state));
            SetTextVisible(stateText, false);
            SetGraphicVisible(rewardCoinImage, ShouldShowReward(state));
            SetGraphicVisible(progressBackgroundImage, state == MissionRowState.Active);
            ConfigureClaimButton(mission, state, onClaimRequested);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = state == MissionRowState.Active ? normalizedProgress : 0f;
                progressFillImage.gameObject.SetActive(state == MissionRowState.Active);
            }
        }

        public void ConfigureLockedPhaseCard()
        {
            gameObject.SetActive(true);

            SetSprite(backgroundImage, phaseLockedSprite != null ? phaseLockedSprite : lockedSprite);
            SetSprite(statusIconImage, lockIconSprite);
            ApplyLayout(MissionRowState.Locked);
            SetGraphicVisible(statusIconImage, true);
            SetText(titleText, "LOCKED PHASE");
            SetText(phaseText, string.Empty);
            SetText(progressText, string.Empty);
            SetText(rewardText, string.Empty);
            SetText(stateText, string.Empty);
            SetText(claimButtonText, "CLAIM");
            SetTextVisible(phaseText, false);
            SetTextVisible(progressText, false);
            SetTextVisible(rewardText, false);
            SetTextVisible(stateText, false);
            SetGraphicVisible(rewardCoinImage, false);
            ConfigureClaimButton(null, MissionRowState.Locked, null);
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
                MissionRowState.CompletedUnclaimed => claimableSprite != null ? claimableSprite : claimedSprite,
                MissionRowState.CompletedClaimed => claimedSprite != null ? claimedSprite : claimableSprite,
                MissionRowState.Locked => lockedSprite,
                _ => null
            };
        }

        private Sprite GetStatusIconSprite(MissionRowState state)
        {
            return state switch
            {
                MissionRowState.CompletedUnclaimed => checkIconSprite,
                MissionRowState.CompletedClaimed => checkIconSprite,
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

        private void ConfigureClaimButton(
            MissionDefinition mission,
            MissionRowState state,
            Func<string, bool> onClaimRequested)
        {
            if (claimButton == null)
            {
                return;
            }

            claimButton.onClick.RemoveAllListeners();
            bool canClaim = mission != null
                && state == MissionRowState.CompletedUnclaimed
                && onClaimRequested != null;
            claimButton.gameObject.SetActive(canClaim);
            claimButton.interactable = canClaim;
            if (!canClaim)
            {
                return;
            }

            string missionId = mission.Id;
            claimButton.onClick.AddListener(() =>
            {
                claimButton.interactable = false;
                if (!onClaimRequested(missionId))
                {
                    claimButton.interactable = true;
                }
            });
        }

        private static bool ShouldShowReward(MissionRowState state)
        {
            return state == MissionRowState.CompletedUnclaimed
                || state == MissionRowState.Active;
        }

        private void ApplyLayout(MissionRowState state)
        {
            bool tallRow = state == MissionRowState.Active || state == MissionRowState.CompletedUnclaimed;
            SetRowHeight(tallRow ? 210f : 164f);

            RectTransform statusRect = statusIconImage != null ? statusIconImage.rectTransform : null;
            RectTransform titleRect = titleText != null ? titleText.rectTransform : null;
            RectTransform phaseRect = phaseText != null ? phaseText.rectTransform : null;
            RectTransform progressRect = progressBackgroundImage != null ? progressBackgroundImage.rectTransform : null;
            RectTransform progressTextRect = progressText != null ? progressText.rectTransform : null;
            RectTransform coinRect = rewardCoinImage != null ? rewardCoinImage.rectTransform : null;
            RectTransform rewardRect = rewardText != null ? rewardText.rectTransform : null;
            RectTransform claimRect = claimButton != null ? claimButton.transform as RectTransform : null;

            switch (state)
            {
                case MissionRowState.Active:
                    SetRect(statusRect, new Vector2(70f, 0f), new Vector2(64f, 64f));
                    SetStretchTop(phaseRect, 44f, 250f, 32f, 36f);
                    SetStretchTop(titleRect, 44f, 250f, 76f, 44f);
                    SetStretchBottom(progressRect, 44f, 44f, 34f, 34f);
                    SetStretchBottom(progressTextRect, 44f, 44f, 33f, 36f);
                    SetRect(coinRect, new Vector2(-206f, 44f), new Vector2(44f, 44f), rightAnchored: true);
                    SetRect(rewardRect, new Vector2(-58f, 44f), new Vector2(172f, 48f), rightAnchored: true);
                    SetRect(claimRect, new Vector2(-124f, -42f), new Vector2(214f, 72f), rightAnchored: true);
                    SetFontSize(phaseText, 30f);
                    SetFontSize(titleText, 36f);
                    SetFontSize(progressText, 27f);
                    SetFontSize(rewardText, 34f);
                    SetFontSize(claimButtonText, 31f);
                    break;
                case MissionRowState.CompletedUnclaimed:
                    SetRect(statusRect, new Vector2(78f, -2f), new Vector2(80f, 80f));
                    SetStretchTop(phaseRect, 160f, 292f, 44f, 34f);
                    SetStretchTop(titleRect, 160f, 292f, 82f, 58f);
                    SetStretchBottom(progressRect, 160f, 292f, 32f, 28f);
                    SetStretchBottom(progressTextRect, 160f, 292f, 28f, 34f);
                    SetRect(coinRect, new Vector2(-210f, 48f), new Vector2(46f, 46f), rightAnchored: true);
                    SetRect(rewardRect, new Vector2(-58f, 48f), new Vector2(172f, 50f), rightAnchored: true);
                    SetRect(claimRect, new Vector2(-124f, -42f), new Vector2(214f, 72f), rightAnchored: true);
                    SetFontSize(phaseText, 28f);
                    SetFontSize(titleText, 34f);
                    SetFontSize(progressText, 24f);
                    SetFontSize(rewardText, 34f);
                    SetFontSize(claimButtonText, 31f);
                    break;
                case MissionRowState.CompletedClaimed:
                case MissionRowState.Locked:
                default:
                    SetRect(statusRect, new Vector2(72f, 0f), new Vector2(72f, 72f));
                    SetStretchTop(phaseRect, 146f, 46f, 40f, 32f);
                    SetStretchTop(titleRect, 146f, 46f, 61f, 54f);
                    SetStretchBottom(progressRect, 146f, 46f, 26f, 26f);
                    SetStretchBottom(progressTextRect, 146f, 46f, 24f, 30f);
                    SetRect(coinRect, new Vector2(-210f, 0f), new Vector2(42f, 42f), rightAnchored: true);
                    SetRect(rewardRect, new Vector2(-58f, 0f), new Vector2(172f, 48f), rightAnchored: true);
                    SetRect(claimRect, new Vector2(-124f, -34f), new Vector2(214f, 68f), rightAnchored: true);
                    SetFontSize(phaseText, 26f);
                    SetFontSize(titleText, 34f);
                    SetFontSize(progressText, 23f);
                    SetFontSize(rewardText, 32f);
                    SetFontSize(claimButtonText, 30f);
                    break;
            }
        }

        private void SetRowHeight(float height)
        {
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }

            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout == null)
            {
                return;
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchoredPosition,
            Vector2 size,
            bool rightAnchored = false)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 anchor = rightAnchored ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = rightAnchored ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void SetStretchTop(
            RectTransform rect,
            float left,
            float right,
            float top,
            float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetStretchBottom(
            RectTransform rect,
            float left,
            float right,
            float bottom,
            float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetFontSize(TextMeshProUGUI text, float size)
        {
            if (text != null)
            {
                text.fontSize = size;
            }
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
        CompletedUnclaimed,
        CompletedClaimed
    }
}
