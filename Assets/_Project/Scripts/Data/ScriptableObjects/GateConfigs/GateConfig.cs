using System;
using System.Collections.Generic;
using _Project.Scripts.Data.Balance;
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
        [SerializeField] private string gateId;
        [SerializeField] private BalanceGateCategory category = BalanceGateCategory.Stable;
        [SerializeField] private List<GateRuntimeEffect> runtimeEffects = new List<GateRuntimeEffect>();

        public GateStatTarget StatTarget => statTarget;
        public GateOperationType OperationType => operationType;
        public float Amount => amount;
        public string DisplayLabel => displayLabel;
        public string GateId => gateId;
        public BalanceGateCategory Category => category;
        public IReadOnlyList<GateRuntimeEffect> RuntimeEffects => runtimeEffects;
        public bool HasRuntimeEffects => runtimeEffects != null && runtimeEffects.Count > 0;
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
            gateId = string.Empty;
            category = BalanceGateCategory.Stable;
            runtimeEffects ??= new List<GateRuntimeEffect>();
            runtimeEffects.Clear();
        }

        public void ConfigureRuntime(BalanceGateEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            gateId = entry.GateId;
            category = entry.Category;
            displayLabel = entry.DisplayLabel;
            autoGenerateLabel = false;
            operationType = category == BalanceGateCategory.Risky
                ? GateOperationType.Subtract
                : GateOperationType.Add;
            runtimeEffects ??= new List<GateRuntimeEffect>();
            runtimeEffects.Clear();
            AddRuntimeEffect(entry.EffectType, entry.Magnitude, entry.DurationSeconds, false);
            AddRuntimeEffect(
                entry.SecondaryEffectType,
                entry.SecondaryMagnitude,
                entry.SecondaryDurationSeconds,
                false);
            AddRuntimeEffect(
                entry.DrawbackType,
                entry.DrawbackMagnitude,
                entry.DrawbackDurationSeconds,
                true);
        }

        private void AddRuntimeEffect(
            BalanceEffectType effectType,
            float magnitude,
            float durationSeconds,
            bool isDrawback)
        {
            if (effectType == BalanceEffectType.None)
            {
                return;
            }

            runtimeEffects.Add(new GateRuntimeEffect(
                effectType,
                magnitude,
                durationSeconds,
                isDrawback));
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
                GateStatTarget.PlayerCount => "PLAYER",
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
        ProjectileCount,
        PlayerCount
    }

    public enum GateOperationType
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    [Serializable]
    public sealed class GateRuntimeEffect
    {
        [SerializeField] private BalanceEffectType effectType;
        [SerializeField] private float magnitude;
        [SerializeField] private float durationSeconds;
        [SerializeField] private bool isDrawback;

        public GateRuntimeEffect(
            BalanceEffectType effectType,
            float magnitude,
            float durationSeconds,
            bool isDrawback)
        {
            this.effectType = effectType;
            this.magnitude = Mathf.Max(0f, magnitude);
            this.durationSeconds = Mathf.Max(0f, durationSeconds);
            this.isDrawback = isDrawback;
        }

        public BalanceEffectType EffectType => effectType;
        public float Magnitude => magnitude;
        public float DurationSeconds => durationSeconds;
        public bool IsDrawback => isDrawback;
    }
}
