using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.UISystem
{
    public sealed class GameOverPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Run Stats")]
        [SerializeField] private TextMeshProUGUI timeValueText;
        [SerializeField] private TextMeshProUGUI scoreValueText;
        [SerializeField] private TextMeshProUGUI coinsValueText;
        [SerializeField] private TextMeshProUGUI killsValueText;
        [SerializeField] private TextMeshProUGUI rewardCoinsValueText;

        [Header("Best Record")]
        [SerializeField] private TextMeshProUGUI bestScoreValueText;
        [SerializeField] private TextMeshProUGUI bestTimeValueText;
        [SerializeField] private TextMeshProUGUI bestKillsValueText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button homeButton;

        public Button RetryButton => retryButton;
        public Button UpgradeButton => upgradeButton;
        public Button HomeButton => homeButton;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }
        }

        public void Show(GameResultData resultData)
        {
            SetText(titleText, "RUN FAILED");
            SetText(subtitleText, "THE LOOP WILL CONTINUE, COMMANDER!");
            SetText(timeValueText, FormatTime(resultData.SurvivalTime));
            SetText(scoreValueText, resultData.Score.ToString("N0"));
            SetText(coinsValueText, resultData.CoinsEarned.ToString("N0"));
            SetText(killsValueText, resultData.Kills.ToString("N0"));
            SetText(rewardCoinsValueText, $"+{resultData.RewardCoins:N0}");
            SetText(bestScoreValueText, resultData.BestScore.ToString("N0"));
            SetText(bestTimeValueText, FormatTime(resultData.BestSurvivalTime));
            SetText(bestKillsValueText, resultData.BestKills.ToString("N0"));
            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        private void SetRootActive(bool active)
        {
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            if (root.activeSelf != active)
            {
                root.SetActive(active);
            }
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static string FormatTime(float seconds)
        {
            int safeSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{safeSeconds / 60:00}:{safeSeconds % 60:00}";
        }
    }
}
