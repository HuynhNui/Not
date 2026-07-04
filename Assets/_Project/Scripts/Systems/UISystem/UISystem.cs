using System;
using System.Collections.Generic;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.RunStatsSystem;
using _Project.Scripts.Systems.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Controls prefab/scene-built UI panels. This class does not create UI objects.
    /// </summary>
    public sealed class UISystem : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [FormerlySerializedAs("hudRoot")]
        [SerializeField] private GameObject gameplayHudPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Main Menu")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button mainMenuUpgradeButton;
        [SerializeField] private Button mainMenuSettingsButton;
        [SerializeField] private TextMeshProUGUI bestRunText;
        [SerializeField] private TextMeshProUGUI walletText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI bestTimeText;
        [SerializeField] private TextMeshProUGUI bestEnemiesKilledText;
        [SerializeField] private TextMeshProUGUI bestCoinsText;
        [SerializeField] private TextMeshProUGUI loopValueText;

        [Header("Gameplay HUD")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI timeSurvivalText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI enemyDefeatedCountText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Upgrade")]
        [SerializeField] private TextMeshProUGUI upgradeCurrencyText;
        [SerializeField] private TextMeshProUGUI upgradePowerText;
        [SerializeField] private TextMeshProUGUI upgradeSquadText;
        [SerializeField] private List<UpgradeRowBinding> upgradeRows = new List<UpgradeRowBinding>();
        [SerializeField] private Button upgradeBackButton;

        [Header("Settings")]
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle vibrationToggle;
        [FormerlySerializedAs("performanceModeToggle")]
        [SerializeField] private Toggle damageTextToggle;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button resetDataButton;
        [SerializeField] private GameObject resetConfirmPopup;
        [SerializeField] private Button resetConfirmCancelButton;
        [SerializeField] private Button resetConfirmButton;
        [SerializeField] private Sprite settingsToggleOnSprite;
        [SerializeField] private Sprite settingsToggleOffSprite;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button pauseHomeButton;

        [Header("Game Over")]
        [SerializeField] private GameOverPanelUI gameOverPanelUI;
        [SerializeField] private TextMeshProUGUI finalTimeText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI finalKillText;
        [SerializeField] private TextMeshProUGUI moneyEarnedText;
        [SerializeField] private TextMeshProUGUI coinRewardText;
        [SerializeField] private TextMeshProUGUI gameOverBestScoreText;
        [FormerlySerializedAs("bestTimeText")]
        [SerializeField] private TextMeshProUGUI gameOverBestTimeText;
        [FormerlySerializedAs("bestKillText")]
        [SerializeField] private TextMeshProUGUI gameOverBestKillText;
        [FormerlySerializedAs("playAgainButton")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverUpgradeButton;
        [SerializeField] private Button gameOverHomeButton;

        private const string MusicVolumePrefsKey = "Settings.MusicVolume";
        private const string SfxVolumePrefsKey = "Settings.SfxVolume";
        private const string MusicEnabledPrefsKey = "Settings.MusicEnabled";
        private const string SfxEnabledPrefsKey = "Settings.SfxEnabled";
        private const string VibrationPrefsKey = "Settings.Vibration";
        private const string DamageTextPrefsKey = "Settings.DamageText";

        private RunStatsTracker _runStatsTracker;
        private UIScreen _currentScreen = UIScreen.None;
        private UIScreen _settingsReturnScreen = UIScreen.MainMenu;
        private readonly HashSet<string> _missingReferenceWarnings = new HashSet<string>();
        private bool _isInitialized;

        public event Action PlayRequested;
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action HomeRequested;

        public void Init(RunStatsTracker runStatsTracker = null)
        {
            if (runStatsTracker != null)
            {
                _runStatsTracker = runStatsTracker;
            }

            ResolveGameOverReferences();
            ValidateRequiredReferences();
            EnsureSettingsPrefsInitialized();
            WireButtons();
            SaveService.Instance.DataChanged -= HandleSaveDataChanged;
            SaveService.Instance.DataChanged += HandleSaveDataChanged;
            RefreshSettingsControls();
            RefreshMenuStats();
            RefreshUpgradePanel();

            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            ShowMainMenu();
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            if (_currentScreen == UIScreen.Gameplay)
            {
                RefreshHud();
            }
        }

        private void OnDestroy()
        {
            if (SaveService.HasInstance)
            {
                SaveService.Instance.DataChanged -= HandleSaveDataChanged;
            }
        }

        public void BindRunStatsTracker(RunStatsTracker runStatsTracker)
        {
            _runStatsTracker = runStatsTracker;
            RefreshMenuStats();
            RefreshUpgradePanel();
            RefreshHud();
        }

        public void ShowMainMenu()
        {
            Time.timeScale = 1f;
            SetPrimaryPanel(UIScreen.MainMenu);
            RefreshMenuStats();
        }

        public void ShowHud()
        {
            ShowGameplayHud();
        }

        public void ShowGameplayHud()
        {
            Time.timeScale = 1f;
            SetPrimaryPanel(UIScreen.Gameplay);
            RefreshHud();
        }

        public void ShowPause()
        {
            Time.timeScale = 0f;
            SetPrimaryPanel(UIScreen.Pause);
        }

        public void ShowSettingsFromMainMenu()
        {
            _settingsReturnScreen = UIScreen.MainMenu;
            SetPrimaryPanel(UIScreen.Settings);
            HideResetConfirmPopup();
            RefreshSettingsControls();
        }

        public void ShowSettingsFromPause()
        {
            _settingsReturnScreen = UIScreen.Pause;
            SetPrimaryPanel(UIScreen.Settings);
            HideResetConfirmPopup();
            RefreshSettingsControls();
        }

        public void ShowUpgrade()
        {
            Time.timeScale = 1f;
            SetPrimaryPanel(UIScreen.Upgrade);
            RefreshUpgradePanel();
        }

        public void ShowGameOver()
        {
            RunStatsSnapshot snapshot = _runStatsTracker != null ? _runStatsTracker.CreateSnapshot() : default;
            ShowGameOver(snapshot);
        }

        public void ShowGameOver(RunStatsSnapshot snapshot)
        {
            Time.timeScale = 0f;
            SetPrimaryPanel(UIScreen.GameOver);
            ResolveGameOverReferences();

            GameResultData resultData = CreateGameResultData(snapshot);
            if (gameOverPanelUI != null)
            {
                gameOverPanelUI.Show(resultData);
            }
            else
            {
                ApplyGameOverText(resultData);
            }

            RefreshMenuStats();
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }

        private void WireButtons()
        {
            ResolveGameOverReferences();
            WireButton(playButton, nameof(playButton), () => PlayRequested?.Invoke());
            WireButton(mainMenuUpgradeButton, nameof(mainMenuUpgradeButton), ShowUpgrade);
            WireButton(mainMenuSettingsButton, nameof(mainMenuSettingsButton), ShowSettingsFromMainMenu);
            WireButton(pauseButton, nameof(pauseButton), () => PauseRequested?.Invoke());
            WireButton(upgradeBackButton, nameof(upgradeBackButton), ShowMainMenu);
            WireButton(settingsBackButton, nameof(settingsBackButton), HandleSettingsBack);
            WireButton(resetDataButton, nameof(resetDataButton), ShowResetConfirmPopup);
            WireButton(resetConfirmCancelButton, nameof(resetConfirmCancelButton), HideResetConfirmPopup);
            WireButton(resetConfirmButton, nameof(resetConfirmButton), ConfirmResetPlayerProgression);
            WireButton(resumeButton, nameof(resumeButton), () => ResumeRequested?.Invoke());
            WireButton(pauseRestartButton, nameof(pauseRestartButton), () => RestartRequested?.Invoke());
            WireButton(pauseSettingsButton, nameof(pauseSettingsButton), ShowSettingsFromPause);
            WireButton(pauseHomeButton, nameof(pauseHomeButton), () => HomeRequested?.Invoke());
            WireButton(retryButton, nameof(retryButton), () => RestartRequested?.Invoke());
            WireButton(gameOverUpgradeButton, nameof(gameOverUpgradeButton), ShowUpgrade);
            WireButton(gameOverHomeButton, nameof(gameOverHomeButton), () => HomeRequested?.Invoke());

            WireSettingsControls();
            WireUpgradeRows();
        }

        private void ResolveGameOverReferences()
        {
            if (gameOverPanelUI == null && gameOverPanel != null)
            {
                gameOverPanelUI = gameOverPanel.GetComponent<GameOverPanelUI>();
            }

            if (gameOverPanelUI == null)
            {
                return;
            }

            retryButton ??= gameOverPanelUI.RetryButton;
            gameOverUpgradeButton ??= gameOverPanelUI.UpgradeButton;
            gameOverHomeButton ??= gameOverPanelUI.HomeButton;
        }

        private void WireSettingsControls()
        {
            WireSettingToggle(musicToggle, MusicEnabledPrefsKey, true, nameof(musicToggle));
            WireSettingToggle(sfxToggle, SfxEnabledPrefsKey, true, nameof(sfxToggle));
            WireSettingToggle(vibrationToggle, VibrationPrefsKey, true, nameof(vibrationToggle));
            WireSettingToggle(damageTextToggle, DamageTextPrefsKey, true, nameof(damageTextToggle));
        }

        private void WireUpgradeRows()
        {
            if (upgradeRows == null)
            {
                return;
            }

            for (int index = 0; index < upgradeRows.Count; index++)
            {
                UpgradeRowBinding row = upgradeRows[index];
                if (row == null || row.UpgradeButton == null)
                {
                    continue;
                }

                PlayerMetaUpgradeType upgradeType = row.UpgradeType;
                row.UpgradeButton.onClick.RemoveAllListeners();
                row.UpgradeButton.onClick.AddListener(() => TryPurchaseUpgrade(upgradeType));
            }
        }

        private void WireButton(Button button, string fieldName, Action action)
        {
            if (button == null)
            {
                WarnMissing(fieldName);
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke());
        }

        private void WireSettingToggle(Toggle toggle, string prefsKey, bool defaultValue, string fieldName)
        {
            if (toggle == null)
            {
                WarnMissing(fieldName);
                return;
            }

            bool initialValue = GetBoolSetting(prefsKey, defaultValue);
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = initialValue;
            SetToggleSprite(toggle, initialValue);
            toggle.onValueChanged.AddListener(value =>
            {
                SetBoolSetting(prefsKey, value);
                SetToggleSprite(toggle, value);
                PlayerPrefs.Save();
            });
        }

        private void HandleSettingsBack()
        {
            HideResetConfirmPopup();

            if (_settingsReturnScreen == UIScreen.Pause)
            {
                ShowPause();
                return;
            }

            ShowMainMenu();
        }

        private void TryPurchaseUpgrade(PlayerMetaUpgradeType upgradeType)
        {
            if (!PlayerMetaUpgradeService.TryPurchase(upgradeType))
            {
                return;
            }

            RefreshUpgradePanel();
            RefreshMenuStats();
        }

        private void ShowResetConfirmPopup()
        {
            SetActive(resetConfirmPopup, true);
        }

        private void HideResetConfirmPopup()
        {
            SetActive(resetConfirmPopup, false);
        }

        private void ConfirmResetPlayerProgression()
        {
            SaveService.Instance.ResetPlayerProgression();
            RefreshMenuStats();
            RefreshUpgradePanel();
            RefreshSettingsControls();
            HideResetConfirmPopup();
        }

        private void RefreshSettingsControls()
        {
            RefreshSettingToggle(musicToggle, MusicEnabledPrefsKey, true);
            RefreshSettingToggle(sfxToggle, SfxEnabledPrefsKey, true);
            RefreshSettingToggle(vibrationToggle, VibrationPrefsKey, true);
            RefreshSettingToggle(damageTextToggle, DamageTextPrefsKey, true);
        }

        private void RefreshSettingToggle(Toggle toggle, string prefsKey, bool defaultValue)
        {
            if (toggle == null)
            {
                return;
            }

            bool value = GetBoolSetting(prefsKey, defaultValue);
            toggle.SetIsOnWithoutNotify(value);
            SetToggleSprite(toggle, value);
        }

        private static bool GetBoolSetting(string prefsKey, bool defaultValue)
        {
            return PlayerPrefs.GetInt(prefsKey, defaultValue ? 1 : 0) != 0;
        }

        private static void SetBoolSetting(string prefsKey, bool value)
        {
            PlayerPrefs.SetInt(prefsKey, value ? 1 : 0);
        }

        private static bool GetEnabledDefaultFromLegacyVolume(string legacyPrefsKey)
        {
            return !PlayerPrefs.HasKey(legacyPrefsKey)
                || PlayerPrefs.GetFloat(legacyPrefsKey, 1f) > 0.0001f;
        }

        private static void EnsureSettingsPrefsInitialized()
        {
            bool changed = false;

            if (!PlayerPrefs.HasKey(MusicEnabledPrefsKey))
            {
                SetBoolSetting(MusicEnabledPrefsKey, GetEnabledDefaultFromLegacyVolume(MusicVolumePrefsKey));
                changed = true;
            }

            if (!PlayerPrefs.HasKey(SfxEnabledPrefsKey))
            {
                SetBoolSetting(SfxEnabledPrefsKey, GetEnabledDefaultFromLegacyVolume(SfxVolumePrefsKey));
                changed = true;
            }

            if (!PlayerPrefs.HasKey(VibrationPrefsKey))
            {
                SetBoolSetting(VibrationPrefsKey, true);
                changed = true;
            }

            if (!PlayerPrefs.HasKey(DamageTextPrefsKey))
            {
                SetBoolSetting(DamageTextPrefsKey, true);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        private void SetToggleSprite(Toggle toggle, bool value)
        {
            if (toggle == null)
            {
                return;
            }

            Image image = toggle.targetGraphic as Image;
            if (image == null)
            {
                image = toggle.GetComponent<Image>();
            }

            if (image == null)
            {
                return;
            }

            Sprite sprite = value ? settingsToggleOnSprite : settingsToggleOffSprite;
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        private void RefreshMenuStats()
        {
            SaveData saveData = SaveService.Instance.Data;
            SetText(loopValueText, saveData.totalRunsCompleted.ToString());

            if (_runStatsTracker == null)
            {
                int walletCoins = saveData.walletCoins;
                float bestSurvivalTime = saveData.bestSurvivalTime;
                int bestKillCount = saveData.bestKillCount;
                int bestCoinsEarned = saveData.bestCoinsEarned;
                int bestScore = saveData.bestScore;

                SetText(bestRunText, $"BEST {FormatTime(bestSurvivalTime)} | KILLS {bestKillCount}");
                SetText(walletText, $"{walletCoins}");
                SetText(bestScoreText, bestScore.ToString());
                SetText(bestTimeText, FormatTime(bestSurvivalTime));
                SetText(bestEnemiesKilledText, bestKillCount.ToString());
                SetText(bestCoinsText, bestCoinsEarned.ToString());
                return;
            }

            SetText(bestRunText, $"BEST {FormatTime(_runStatsTracker.BestSurvivalTime)} | KILLS {_runStatsTracker.BestKillCount}");
            SetText(walletText, $"{_runStatsTracker.WalletCoins}");
            SetText(bestScoreText, _runStatsTracker.BestScore.ToString());
            SetText(bestTimeText, FormatTime(_runStatsTracker.BestSurvivalTime));
            SetText(bestEnemiesKilledText, _runStatsTracker.BestKillCount.ToString());
            SetText(bestCoinsText, _runStatsTracker.BestCoinsEarned.ToString());
        }

        private void RefreshHud()
        {
            if (_runStatsTracker == null)
            {
                SetText(timeSurvivalText, "00:00");
                SetText(moneyText, "0");
                SetText(enemyDefeatedCountText, "0");
                SetText(scoreText, "0");
                return;
            }

            SetText(timeSurvivalText, FormatTime(_runStatsTracker.SurvivalTime));
            SetText(moneyText, _runStatsTracker.CoinsEarned.ToString());
            SetText(enemyDefeatedCountText, _runStatsTracker.EnemyKills.ToString());
            SetText(scoreText, _runStatsTracker.Score.ToString());
        }

        private void RefreshUpgradePanel()
        {
            int walletCoins = _runStatsTracker != null
                ? _runStatsTracker.WalletCoins
                : SaveService.Instance.Data.walletCoins;

            SetText(upgradeCurrencyText, walletCoins.ToString("N0"));
            SetText(upgradePowerText, PlayerMetaUpgradeService.GetPowerScore().ToString("N0"));

            int currentSquadSize = Mathf.RoundToInt(
                PlayerMetaUpgradeService.GetCurrentValue(PlayerMetaUpgradeType.SquadSize));
            int maxSquadSize = Mathf.RoundToInt(
                PlayerMetaUpgradeService.CalculateMaxValue(PlayerMetaUpgradeType.SquadSize));
            SetText(upgradeSquadText, $"{currentSquadSize} / {maxSquadSize}");

            if (upgradeRows == null)
            {
                return;
            }

            for (int index = 0; index < upgradeRows.Count; index++)
            {
                UpgradeRowBinding row = upgradeRows[index];
                if (row == null)
                {
                    continue;
                }

                int level = PlayerMetaUpgradeService.GetLevel(row.UpgradeType);
                int cost = PlayerMetaUpgradeService.GetCost(row.UpgradeType);
                bool isMaxLevel = PlayerMetaUpgradeService.IsMaxLevel(row.UpgradeType);
                float currentValue = PlayerMetaUpgradeService.GetCurrentValue(row.UpgradeType);
                float nextValue = PlayerMetaUpgradeService.GetNextValue(row.UpgradeType);

                SetText(row.LevelText, $"LV. {level}/{PlayerMetaUpgradeService.MaxUpgradeLevel}");
                SetText(
                    row.CurrentValueText,
                    PlayerMetaUpgradeService.FormatValue(row.UpgradeType, currentValue));
                SetText(
                    row.NextValueText,
                    isMaxLevel
                        ? "MAX"
                        : PlayerMetaUpgradeService.FormatValue(row.UpgradeType, nextValue));
                SetText(row.CostText, isMaxLevel ? "MAX" : cost.ToString("N0"));
                SetText(row.UpgradeButtonText, isMaxLevel ? "MAX" : "UPGRADE");

                if (row.UpgradeButton != null)
                {
                    row.UpgradeButton.interactable = !isMaxLevel && walletCoins >= cost;
                }
            }
        }

        private void SetPrimaryPanel(UIScreen screen)
        {
            _currentScreen = screen;

            SetActive(mainMenuPanel, screen == UIScreen.MainMenu);
            SetActive(upgradePanel, screen == UIScreen.Upgrade);
            SetActive(settingsPanel, screen == UIScreen.Settings);
            SetActive(pausePanel, screen == UIScreen.Pause);
            SetActive(gameOverPanel, screen == UIScreen.GameOver);
            SetActive(gameplayHudPanel, screen == UIScreen.Gameplay
                || screen == UIScreen.Pause
                || screen == UIScreen.GameOver
                || (_settingsReturnScreen == UIScreen.Pause && screen == UIScreen.Settings));
        }

        private void ValidateRequiredReferences()
        {
            WarnIfMissing(mainMenuPanel, nameof(mainMenuPanel), "GameCanvas/UIRoot/SafeAreaRoot/MainMenuPanel");
            WarnIfMissing(gameplayHudPanel, nameof(gameplayHudPanel), "GameCanvas/UIRoot/SafeAreaRoot/GameplayHUDPanel");
            WarnIfMissing(upgradePanel, nameof(upgradePanel), "GameCanvas/UIRoot/SafeAreaRoot/UpgradePanel");
            WarnIfMissing(settingsPanel, nameof(settingsPanel), "GameCanvas/UIRoot/SafeAreaRoot/SettingsPanel");
            WarnIfMissing(pausePanel, nameof(pausePanel), "GameCanvas/UIRoot/SafeAreaRoot/PausePanel");
            WarnIfMissing(gameOverPanel, nameof(gameOverPanel), "GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel");
            WarnIfMissing(gameOverPanelUI, nameof(gameOverPanelUI), "GameCanvas/UIRoot/SafeAreaRoot/GameOverPanel");
            WarnIfMissing(musicToggle, nameof(musicToggle), "SettingsPanel/MainPanel/RowsContainer/MusicRow/ToggleButton");
            WarnIfMissing(sfxToggle, nameof(sfxToggle), "SettingsPanel/MainPanel/RowsContainer/SfxRow/ToggleButton");
            WarnIfMissing(vibrationToggle, nameof(vibrationToggle), "SettingsPanel/MainPanel/RowsContainer/VibrationRow/ToggleButton");
            WarnIfMissing(damageTextToggle, nameof(damageTextToggle), "SettingsPanel/MainPanel/RowsContainer/DamageTextRow/ToggleButton");
            WarnIfMissing(settingsBackButton, nameof(settingsBackButton), "SettingsPanel/MainPanel/Header/BackButton");
            WarnIfMissing(resetDataButton, nameof(resetDataButton), "SettingsPanel/MainPanel/ResetButton");
            WarnIfMissing(resetConfirmPopup, nameof(resetConfirmPopup), "SettingsPanel/ResetConfirmPopup");
            WarnIfMissing(resetConfirmCancelButton, nameof(resetConfirmCancelButton), "SettingsPanel/ResetConfirmPopup/ConfirmPanel/ButtonRow/CancelButton");
            WarnIfMissing(resetConfirmButton, nameof(resetConfirmButton), "SettingsPanel/ResetConfirmPopup/ConfirmPanel/ButtonRow/ConfirmButton");
            WarnIfMissing(playButton, nameof(playButton), "MainMenuPanel/StartRunButton");
            WarnIfMissing(mainMenuUpgradeButton, nameof(mainMenuUpgradeButton), "MainMenuPanel/BottomNavigationBar/UPDATEButton");
            WarnIfMissing(mainMenuSettingsButton, nameof(mainMenuSettingsButton), "MainMenuPanel/BottomNavigationBar/SETTINGButton");
            WarnIfMissing(walletText, nameof(walletText), "MainMenuPanel/TopHUD/ResourceBox/CoinValueText");
            WarnIfMissing(bestScoreText, nameof(bestScoreText), "MainMenuPanel/StatsBar/BESTSCORECell/BestScoreValueText");
            WarnIfMissing(bestTimeText, nameof(bestTimeText), "MainMenuPanel/StatsBar/BESTTIMECell/BestTimeValueText");
            WarnIfMissing(bestEnemiesKilledText, nameof(bestEnemiesKilledText), "MainMenuPanel/StatsBar/ENEMIESKILLEDCell/BestEnemiesKilledValueText");
            WarnIfMissing(bestCoinsText, nameof(bestCoinsText), "MainMenuPanel/StatsBar/BESTCOINSCell/BestCoinsValueText");
            WarnIfMissing(loopValueText, nameof(loopValueText), "MainMenuPanel/PlayerProfile/LoopValueText");
            WarnIfMissing(pauseButton, nameof(pauseButton), "GameplayHUDPanel/HudContentRoot/HudTopBar/PauseButton");
            WarnIfMissing(timeSurvivalText, nameof(timeSurvivalText), "GameplayHUDPanel/HudContentRoot/HudTopBar/MetricsPanel/TimeMetric/ValueText");
            WarnIfMissing(moneyText, nameof(moneyText), "GameplayHUDPanel/HudContentRoot/HudTopBar/MetricsPanel/CoinsMetric/ValueText");
            WarnIfMissing(enemyDefeatedCountText, nameof(enemyDefeatedCountText), "GameplayHUDPanel/HudContentRoot/HudTopBar/MetricsPanel/KillsMetric/ValueText");
            WarnIfMissing(scoreText, nameof(scoreText), "GameplayHUDPanel/HudContentRoot/HudTopBar/MetricsPanel/ScoreMetric/ValueText");
            WarnIfMissing(finalTimeText, nameof(finalTimeText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/StatsSection/StatsGrid/TimeStat/FinalTimeText");
            WarnIfMissing(finalScoreText, nameof(finalScoreText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/StatsSection/StatsGrid/ScoreStat/FinalScoreText");
            WarnIfMissing(moneyEarnedText, nameof(moneyEarnedText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/StatsSection/StatsGrid/CoinsStat/MoneyEarnedText");
            WarnIfMissing(coinRewardText, nameof(coinRewardText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/RewardSection/CoinRewardPanel/CoinRewardValueText");
            WarnIfMissing(finalKillText, nameof(finalKillText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/StatsSection/StatsGrid/KillsStat/FinalKillText");
            WarnIfMissing(gameOverBestScoreText, nameof(gameOverBestScoreText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/FooterSection/BestRecordRow/BestScoreGroup/GameOverBestScoreText");
            WarnIfMissing(gameOverBestTimeText, nameof(gameOverBestTimeText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/FooterSection/BestRecordRow/BestTimeGroup/GameOverBestTimeText");
            WarnIfMissing(gameOverBestKillText, nameof(gameOverBestKillText), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/FooterSection/BestRecordRow/BestKillsGroup/GameOverBestKillText");
            WarnIfMissing(retryButton, nameof(retryButton), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/ButtonsSection/ButtonsStack/RetryButton");
            WarnIfMissing(gameOverUpgradeButton, nameof(gameOverUpgradeButton), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/ButtonsSection/ButtonsStack/GameOverUpgradeButton");
            WarnIfMissing(gameOverHomeButton, nameof(gameOverHomeButton), "GameOverPanel/GameOverContentFrame/PanelCard/ContentRoot/ButtonsSection/ButtonsStack/GameOverHomeButton");
        }

        private void WarnIfMissing(UnityEngine.Object reference, string fieldName, string expectedObject)
        {
            if (reference != null)
            {
                return;
            }

            WarnMissing(fieldName, expectedObject);
        }

        private void WarnMissing(string fieldName, string expectedObject = null)
        {
            if (!_missingReferenceWarnings.Add(fieldName))
            {
                return;
            }

            string expectedMessage = string.IsNullOrEmpty(expectedObject)
                ? "Assign it in the Inspector."
                : $"Create or assign '{expectedObject}' in the scene/prefab Inspector.";

            Debug.LogWarning($"{nameof(UISystem)} missing reference '{fieldName}'. {expectedMessage}", this);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static string FormatTime(float seconds)
        {
            int safeSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{safeSeconds / 60:00}:{safeSeconds % 60:00}";
        }

        private static GameResultData CreateGameResultData(RunStatsSnapshot snapshot)
        {
            return new GameResultData(
                snapshot.SurvivalTime,
                snapshot.Score,
                snapshot.CoinsEarned,
                snapshot.EnemyKills,
                snapshot.CoinsEarned,
                Mathf.Max(snapshot.BestScore, snapshot.Score),
                Mathf.Max(snapshot.BestSurvivalTime, snapshot.SurvivalTime),
                Mathf.Max(snapshot.BestKillCount, snapshot.EnemyKills));
        }

        private void ApplyGameOverText(GameResultData resultData)
        {
            SetText(finalTimeText, FormatTime(resultData.SurvivalTime));
            SetText(finalScoreText, resultData.Score.ToString("N0"));
            SetText(finalKillText, resultData.Kills.ToString("N0"));
            SetText(moneyEarnedText, resultData.CoinsEarned.ToString("N0"));
            SetText(coinRewardText, $"+{resultData.RewardCoins:N0}");
            SetText(gameOverBestScoreText, resultData.BestScore.ToString("N0"));
            SetText(gameOverBestTimeText, FormatTime(resultData.BestSurvivalTime));
            SetText(gameOverBestKillText, resultData.BestKills.ToString("N0"));
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private void HandleSaveDataChanged()
        {
            RefreshMenuStats();
            RefreshUpgradePanel();
        }

        private enum UIScreen
        {
            None,
            MainMenu,
            Gameplay,
            Upgrade,
            Settings,
            Pause,
            GameOver
        }
    }

    [Serializable]
    public sealed class UpgradeRowBinding
    {
        [SerializeField] private PlayerMetaUpgradeType upgradeType;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI currentValueText;
        [SerializeField] private TextMeshProUGUI nextValueText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI upgradeButtonText;
        [SerializeField] private Button upgradeButton;

        public PlayerMetaUpgradeType UpgradeType => upgradeType;
        public TextMeshProUGUI LevelText => levelText;
        public TextMeshProUGUI CurrentValueText => currentValueText;
        public TextMeshProUGUI NextValueText => nextValueText;
        public TextMeshProUGUI CostText => costText;
        public TextMeshProUGUI UpgradeButtonText => upgradeButtonText;
        public Button UpgradeButton => upgradeButton;
    }
}
