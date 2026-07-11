using System.Collections;
using _Project.Scripts.Systems.ProgressionSystem;
using _Project.Scripts.Systems.SaveSystem;
using _Project.Scripts.Systems.UISystem;
using UnityEngine;
using RuntimeUISystem = _Project.Scripts.Systems.UISystem.UISystem;

namespace _Project.Scripts.Systems.TutorialSystem
{
    public sealed class TutorialUpgradeDirector : MonoBehaviour
    {
        private TutorialManager _manager;
        private TutorialOverlayUI _overlay;
        private RuntimeUISystem _uiSystem;
        private Coroutine _routine;
        private PlayerMetaUpgradeType _preferredUpgradeType;
        private int _preferredUpgradeLevelBefore;
        private bool _purchaseDetected;

        public bool IsRunning { get; private set; }

        public void Init(TutorialManager manager, TutorialOverlayUI overlay, RuntimeUISystem uiSystem)
        {
            _manager = manager;
            _overlay = overlay;
            _uiSystem = uiSystem;
        }

        public void StartTutorial()
        {
            StopTutorial();
            IsRunning = true;
            _purchaseDetected = false;
            Subscribe();
            _routine = StartCoroutine(RunTutorial());
        }

        public void SkipTutorial()
        {
            Cleanup();
            _manager?.CompleteUpgradeTutorial();
        }

        public void StopTutorial()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            Cleanup();
        }

        private IEnumerator RunTutorial()
        {
            _preferredUpgradeType = ResolvePreferredUpgradeType();
            EnsureAffordableFirstUpgrade(_preferredUpgradeType);
            _preferredUpgradeLevelBefore = PlayerMetaUpgradeService.GetLevel(_preferredUpgradeType);

            ShowUpgradeOverlay(TutorialConfig.GameOver + "\n" + TutorialConfig.UpgradeIntro);
            yield return new WaitForSecondsRealtime(1f);

            RectTransform gameOverUpgradeButton = _uiSystem != null ? _uiSystem.GameOverUpgradeButtonTarget : null;
            ShowUpgradeOverlay(TutorialConfig.OpenUpgrade);
            _overlay?.HighlightRect(gameOverUpgradeButton, new Vector2(20f, 12f));
            _overlay?.SetOnlyAllowTarget(gameOverUpgradeButton);

            while (IsRunning && _uiSystem != null && _uiSystem.CurrentScreen != UIScreen.Upgrade)
            {
                yield return null;
            }

            _overlay?.ClearAllowedTarget();
            _overlay?.HideHighlight();

            RectTransform currencyTarget = _uiSystem != null ? _uiSystem.UpgradeCurrencyTarget : null;
            if (currencyTarget != null)
            {
                ShowUpgradeOverlay(TutorialConfig.UpgradeIntro);
                _overlay?.HighlightRect(currencyTarget, new Vector2(16f, 10f));
                yield return new WaitForSecondsRealtime(_manager != null && _manager.Config != null
                    ? _manager.Config.CurrencyHighlightSeconds
                    : 1.25f);
            }

            RectTransform rowTarget = null;
            _uiSystem?.TryGetUpgradeRowTarget(_preferredUpgradeType, out rowTarget);
            ShowUpgradeOverlay(TutorialConfig.BuyUpgrade);
            _overlay?.ShowUpgradeCallout(rowTarget);

            RectTransform buttonTarget = null;
            _uiSystem?.TryGetUpgradeButtonTarget(_preferredUpgradeType, out buttonTarget);
            if (buttonTarget != null)
            {
                _overlay?.HighlightRect(buttonTarget, new Vector2(20f, 12f));
                _overlay?.SetOnlyAllowTarget(buttonTarget);
            }

            while (IsRunning && !HasPurchaseCompleted())
            {
                yield return null;
            }

            _overlay?.ClearAllowedTarget();
            _overlay?.HideUpgradeCallout();
            _overlay?.HideHighlight();
            ShowUpgradeOverlay(TutorialConfig.UpgradeComplete);
            yield return new WaitForSecondsRealtime(_manager != null && _manager.Config != null
                ? _manager.Config.PostPurchaseDelaySeconds
                : 1f);

            Cleanup();
            _manager?.CompleteUpgradeTutorial();
        }

        private void ShowUpgradeOverlay(string body)
        {
            _overlay?.ShowOverlay(dimBackgroundVisible: true, blockInput: true);
            _overlay?.ShowSkipButton(true);
            _overlay?.ShowDialogue(TutorialConfig.Speaker, body);
        }

        private PlayerMetaUpgradeType ResolvePreferredUpgradeType()
        {
            if (IsPurchasable(PlayerMetaUpgradeType.Damage))
            {
                return PlayerMetaUpgradeType.Damage;
            }

            for (int index = 0; index < PlayerMetaUpgradeService.Definitions.Length; index++)
            {
                PlayerMetaUpgradeType type = PlayerMetaUpgradeService.Definitions[index].Type;
                if (IsPurchasable(type))
                {
                    return type;
                }
            }

            return PlayerMetaUpgradeType.Damage;
        }

        private static bool IsPurchasable(PlayerMetaUpgradeType type)
        {
            return PlayerMetaUpgradeService.IsSupportedUpgrade(type)
                && !PlayerMetaUpgradeService.IsMaxLevel(type);
        }

        private static void EnsureAffordableFirstUpgrade(PlayerMetaUpgradeType type)
        {
            int cost = PlayerMetaUpgradeService.GetCost(type);
            int wallet = SaveService.Instance.Data.walletCoins;

            if (cost <= 0 || wallet >= cost || SaveService.Instance.HasGrantedTutorialFirstRunBonus())
            {
                return;
            }

            SaveService.Instance.GrantTutorialFirstRunBonusIfNeeded(cost - wallet);
        }

        private bool HasPurchaseCompleted()
        {
            if (_purchaseDetected)
            {
                return true;
            }

            return PlayerMetaUpgradeService.GetLevel(_preferredUpgradeType) > _preferredUpgradeLevelBefore;
        }

        private void Subscribe()
        {
            if (_uiSystem != null)
            {
                _uiSystem.UpgradePurchased -= HandleUpgradePurchased;
                _uiSystem.UpgradePurchased += HandleUpgradePurchased;
            }
        }

        private void Cleanup()
        {
            IsRunning = false;
            _overlay?.HideOverlay();

            if (_uiSystem != null)
            {
                _uiSystem.UpgradePurchased -= HandleUpgradePurchased;
            }
        }

        private void HandleUpgradePurchased(PlayerMetaUpgradeType type)
        {
            _purchaseDetected = true;
        }
    }
}
