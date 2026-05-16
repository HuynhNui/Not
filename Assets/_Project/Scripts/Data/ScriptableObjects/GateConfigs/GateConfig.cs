using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.GateConfigs
{
    /// <summary>
    /// Describes a gate modifier so new gate types can be added without changing gameplay code structure.
    /// </summary>
    [CreateAssetMenu(fileName = "GateConfig", menuName = "Chibi Pixel Gate/Data/Gate Config")]
    public sealed class GateConfig : ScriptableObject
    {
        [SerializeField] private GateStatTarget statTarget = GateStatTarget.Damage;
        [SerializeField] private GateOperationType operationType = GateOperationType.Add;
        [SerializeField] private float amount = 1f;
        [SerializeField] private string displayLabel = "+1";
        [SerializeField] private bool autoGenerateLabel = true;

        public GateStatTarget StatTarget => statTarget;
        public GateOperationType OperationType => operationType;
        public float Amount => amount;
        public string DisplayLabel => displayLabel;

        public string GetDisplayText()
        {
            if (!autoGenerateLabel && !string.IsNullOrWhiteSpace(displayLabel))
            {
                return displayLabel;
            }

            string prefix = operationType switch
            {
                GateOperationType.Add => "+",
                GateOperationType.Subtract => "-",
                GateOperationType.Multiply => "x",
                GateOperationType.Divide => "/",
                _ => string.Empty
            };

            string statShort = statTarget switch
            {
                GateStatTarget.Damage => "DMG",
                GateStatTarget.FireRate => "AS",
                GateStatTarget.MaxHp => "HP",
                GateStatTarget.ProjectileCount => "SHOT",
                _ => statTarget.ToString()
            };

            string valueText = Mathf.Approximately(amount % 1f, 0f)
                ? Mathf.RoundToInt(amount).ToString()
                : amount.ToString("0.#");

            return $"{prefix}{valueText} {statShort}";
        }
    }

    public enum GateStatTarget
    {
        Damage,
        FireRate,
        MaxHp,
        ProjectileCount
    }

    public enum GateOperationType
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }
}
