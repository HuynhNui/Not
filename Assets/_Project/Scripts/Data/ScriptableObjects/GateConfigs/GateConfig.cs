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
        public bool IsBuff => operationType == GateOperationType.Add || operationType == GateOperationType.Multiply;

        public void ConfigureRuntime(
            GateStatTarget runtimeStatTarget,
            GateOperationType runtimeOperationType,
            float runtimeAmount,
            string runtimeDisplayLabel = null)
        {
            statTarget = runtimeStatTarget;
            operationType = runtimeOperationType;
            amount = runtimeAmount;

            if (!string.IsNullOrWhiteSpace(runtimeDisplayLabel))
            {
                displayLabel = runtimeDisplayLabel;
                autoGenerateLabel = false;
                return;
            }

            displayLabel = string.Empty;
            autoGenerateLabel = true;
        }

        public string GetDisplayText()
        {
            if (!autoGenerateLabel && !string.IsNullOrWhiteSpace(displayLabel))
            {
                return displayLabel;
            }

            return $"{GetOperationPrefix()}{GetValueText()} {GetStatShortText()}";
        }

        public string GetCompactDisplayText()
        {
            if (!autoGenerateLabel && !string.IsNullOrWhiteSpace(displayLabel))
            {
                return displayLabel;
            }

            return $"{GetOperationPrefix()}{GetValueText()} {GetStatShortText()}";
        }

        private string GetOperationPrefix()
        {
            return operationType switch
            {
                GateOperationType.Add => "+",
                GateOperationType.Subtract => "-",
                GateOperationType.Multiply => "x",
                GateOperationType.Divide => "/",
                _ => string.Empty
            };
        }

        private string GetStatShortText()
        {
            return statTarget switch
            {
                GateStatTarget.Damage => "DMG",
                GateStatTarget.FireRate => "FIRE",
                GateStatTarget.MaxHp => "HP",
                GateStatTarget.ProjectileCount => "BULLET",
                _ => statTarget.ToString()
            };
        }

        private string GetValueText()
        {
            return Mathf.Approximately(amount % 1f, 0f)
                ? Mathf.RoundToInt(amount).ToString()
                : amount.ToString("0.#");
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
