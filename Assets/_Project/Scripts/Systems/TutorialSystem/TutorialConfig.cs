using UnityEngine;

namespace _Project.Scripts.Systems.TutorialSystem
{
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "True Gate/Tutorial Config")]
    public sealed class TutorialConfig : ScriptableObject
    {
        [Header("Gameplay")]
        [SerializeField] private float introDelaySeconds = 1f;
        [SerializeField] private float movementRequiredWorldDistance = 0.75f;
        [SerializeField] private float autoFireTimeoutSeconds = 10f;
        [SerializeField] private int autoFireRequiredKills = 3;
        [SerializeField] private float enemyWarningSeconds = 5f;
        [SerializeField] private float gateTimeoutSeconds = 15f;
        [SerializeField] private int gateRespawnCount = 1;

        [Header("Upgrade")]
        [SerializeField] private float currencyHighlightSeconds = 1.25f;
        [SerializeField] private float postPurchaseDelaySeconds = 1f;

        public float IntroDelaySeconds => Mathf.Max(0f, introDelaySeconds);
        public float MovementRequiredWorldDistance => Mathf.Max(0.05f, movementRequiredWorldDistance);
        public float AutoFireTimeoutSeconds => Mathf.Max(1f, autoFireTimeoutSeconds);
        public int AutoFireRequiredKills => Mathf.Max(1, autoFireRequiredKills);
        public float EnemyWarningSeconds => Mathf.Max(1f, enemyWarningSeconds);
        public float GateTimeoutSeconds => Mathf.Max(1f, gateTimeoutSeconds);
        public int GateRespawnCount => Mathf.Max(0, gateRespawnCount);
        public float CurrencyHighlightSeconds => Mathf.Max(0f, currencyHighlightSeconds);
        public float PostPurchaseDelaySeconds => Mathf.Max(0f, postPurchaseDelaySeconds);

        public const string Speaker = "SYSTEM";
        public const string MovementIntro = "Movement calibration required.";
        public const string Movement = "Drag left or right to reposition UNIT-07.";
        public const string AutoFire = "Weapon system is automatic.\nFocus on survival.";
        public const string Enemy = "Hostile signatures detected.\nAvoid direct contact.";
        public const string Gate = "Enhancement gate detected.\nEnter the highlighted gate.";
        public const string GameplayComplete = "Calibration complete.\nDeployment authorized.";
        public const string GameOver = "Combat shell destroyed.\nCore recovered.";
        public const string UpgradeIntro = "Resources can reinforce the next shell.";
        public const string OpenUpgrade = "Open the upgrade bay.";
        public const string BuyUpgrade = "Increase damage output.";
        public const string UpgradeComplete = "Combat output increased.\nRedeployment recommended.";
    }
}
