using System;
using System.Collections.Generic;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;
using RuntimeEnemySpawnerSystem =
    _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeRunStatsTracker =
    _Project.Scripts.Systems.RunStatsSystem.RunStatsTracker;

namespace _Project.Scripts.Gameplay.Gates
{
    public sealed class GateRuntimeEffectController : MonoBehaviour
    {
        private const int MaxProjectileCount = 50;

        private readonly GateTimedModifierSet _timedModifiers = new GateTimedModifierSet();
        private readonly List<GateTemporaryStatModifier> _temporaryStatModifiers =
            new List<GateTemporaryStatModifier>();
        private MainPlayerUnit _mainPlayerUnit;
        private PlayerController _playerController;
        private RuntimeEnemySpawnerSystem _enemySpawnerSystem;
        private RuntimeRunStatsTracker _runStatsTracker;
        private GateScalingProfile _gateScalingProfile;

        public GateTimedModifierSet TimedModifiers => _timedModifiers;
        public float IncomingDamageMultiplier => GetCappedCombinedModifier(
            BalanceEffectType.IncomingDamageMultiplier);
        public float EnemySpeedMultiplier => GetCappedCombinedModifier(
            BalanceEffectType.EnemySpeedMultiplier);
        public float EnemyPressureMultiplier => GetCappedCombinedModifier(
            BalanceEffectType.EnemyPressureMultiplier);

        public void SetGateScalingProfile(GateScalingProfile profile)
        {
            _gateScalingProfile = profile;
            _gateScalingProfile?.ValidateValues();
            ApplyCombinedModifiers();
        }

        public void Configure(
            MainPlayerUnit mainPlayerUnit,
            PlayerController playerController,
            RuntimeEnemySpawnerSystem enemySpawnerSystem)
        {
            _mainPlayerUnit = mainPlayerUnit;
            _playerController = playerController;
            _enemySpawnerSystem = enemySpawnerSystem;
            _runStatsTracker ??= FindAnyObjectByType<RuntimeRunStatsTracker>();
            ApplyCombinedModifiers();
        }

        public void BeginRun()
        {
            _timedModifiers.Clear();
            _temporaryStatModifiers.Clear();
            ApplyCombinedModifiers();
        }

        private void Update()
        {
            if (_timedModifiers.Tick(Time.deltaTime))
            {
                ApplyCombinedModifiers();
            }

            TickTemporaryStatModifiers(Time.deltaTime);
        }

        public void Apply(GateConfig config)
        {
            if (config == null || _mainPlayerUnit == null)
            {
                return;
            }

            if (!config.HasRuntimeEffects)
            {
                GateEffectApplier.Apply(config, _mainPlayerUnit, _playerController);
                return;
            }

            IReadOnlyList<GateRuntimeEffect> effects = config.RuntimeEffects;
            for (int index = 0; index < effects.Count; index++)
            {
                ApplyEffect(effects[index]);
            }

            ApplyCombinedModifiers();
        }

        private void ApplyEffect(GateRuntimeEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            if (effect.IsDrawback
                && effect.DurationSeconds > 0f
                && TryApplyTemporaryStatModifier(effect))
            {
                return;
            }

            switch (effect.EffectType)
            {
                case BalanceEffectType.DamageMultiplier:
                    _mainPlayerUnit.SetDamage(ClampDamage(_mainPlayerUnit.Damage * effect.Magnitude));
                    SyncFollowers(
                        syncDamage: true,
                        syncFireRate: false,
                        syncMaxHp: false,
                        healMaxHpByDelta: false,
                        syncProjectileCount: false);
                    break;
                case BalanceEffectType.FireRateFlat:
                    _mainPlayerUnit.SetFireRate(ClampFireRate(_mainPlayerUnit.FireRate + effect.Magnitude));
                    SyncFollowers(false, true, false, false, false);
                    break;
                case BalanceEffectType.FireRateMultiplier:
                    _mainPlayerUnit.SetFireRate(ClampFireRate(_mainPlayerUnit.FireRate * effect.Magnitude));
                    SyncFollowers(false, true, false, false, false);
                    break;
                case BalanceEffectType.MaxHpMultiplier:
                    _mainPlayerUnit.SetMaxHp(
                        ClampMaxHp(_mainPlayerUnit.MaxHp * effect.Magnitude),
                        healByDelta: true);
                    SyncFollowers(false, false, true, true, false);
                    break;
                case BalanceEffectType.HealMissingHpRatio:
                    if (_playerController != null)
                    {
                        _playerController.HealSquadMissingHealth(effect.Magnitude);
                    }
                    else
                    {
                        _mainPlayerUnit.HealMissingHealth(effect.Magnitude);
                    }

                    break;
                case BalanceEffectType.BarrierHits:
                    int barrierHits = Mathf.Max(1, Mathf.RoundToInt(effect.Magnitude));
                    if (_playerController != null)
                    {
                        _playerController.AddSquadBarrier(barrierHits, effect.DurationSeconds);
                    }
                    else
                    {
                        _mainPlayerUnit.AddBarrierHits(barrierHits, effect.DurationSeconds);
                    }

                    break;
                case BalanceEffectType.ProjectileFlat:
                    ApplyProjectileFlat(effect.Magnitude);
                    break;
                case BalanceEffectType.SquadFlat:
                    ApplySquadFlat(effect.Magnitude);
                    break;
                case BalanceEffectType.IncomingDamageMultiplier:
                case BalanceEffectType.EnemySpeedMultiplier:
                case BalanceEffectType.EnemyPressureMultiplier:
                case BalanceEffectType.CoinRewardMultiplier:
                    _timedModifiers.Add(
                        effect.EffectType,
                        effect.Magnitude,
                        effect.DurationSeconds);
                    break;
            }
        }

        private void ApplyProjectileFlat(float amount)
        {
            if (_mainPlayerUnit.BulletSpawner == null)
            {
                return;
            }

            int nextProjectileCount = Mathf.Clamp(
                _mainPlayerUnit.BulletSpawner.ProjectileCount + Mathf.RoundToInt(amount),
                1,
                ActiveProjectileCap);
            _mainPlayerUnit.BulletSpawner.SetProjectileCount(nextProjectileCount);
            SyncFollowers(false, false, false, false, true);
        }

        private void ApplySquadFlat(float amount)
        {
            if (_playerController == null)
            {
                return;
            }

            int next = Mathf.Clamp(
                _playerController.CurrentSquadCount + Mathf.RoundToInt(amount),
                1,
                Mathf.Min(_playerController.MaxSquadCount, ActiveSquadCap));
            _playerController.SetSquadCount(
                next,
                next > _playerController.CurrentSquadCount
                    ? _playerController.RecruitSpawnHpRatio
                    : 1f);
        }

        private bool TryApplyTemporaryStatModifier(GateRuntimeEffect effect)
        {
            switch (effect.EffectType)
            {
                case BalanceEffectType.DamageMultiplier:
                    _mainPlayerUnit.SetDamage(ClampDamage(_mainPlayerUnit.Damage * effect.Magnitude));
                    SyncFollowers(true, false, false, false, false);
                    _temporaryStatModifiers.Add(new GateTemporaryStatModifier(
                        effect.EffectType,
                        effect.Magnitude,
                        effect.DurationSeconds));
                    return true;
                case BalanceEffectType.FireRateMultiplier:
                    _mainPlayerUnit.SetFireRate(ClampFireRate(_mainPlayerUnit.FireRate * effect.Magnitude));
                    SyncFollowers(false, true, false, false, false);
                    _temporaryStatModifiers.Add(new GateTemporaryStatModifier(
                        effect.EffectType,
                        effect.Magnitude,
                        effect.DurationSeconds));
                    return true;
                default:
                    return false;
            }
        }

        private void TickTemporaryStatModifiers(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            for (int index = _temporaryStatModifiers.Count - 1; index >= 0; index--)
            {
                GateTemporaryStatModifier modifier = _temporaryStatModifiers[index];
                modifier.RemainingSeconds -= safeDeltaTime;
                if (modifier.RemainingSeconds > 0f)
                {
                    continue;
                }

                RevertTemporaryStatModifier(modifier);
                _temporaryStatModifiers.RemoveAt(index);
            }
        }

        private void RevertTemporaryStatModifier(GateTemporaryStatModifier modifier)
        {
            if (_mainPlayerUnit == null || modifier.Magnitude <= 0f)
            {
                return;
            }

            switch (modifier.EffectType)
            {
                case BalanceEffectType.DamageMultiplier:
                    _mainPlayerUnit.SetDamage(ClampDamage(_mainPlayerUnit.Damage / modifier.Magnitude));
                    SyncFollowers(true, false, false, false, false);
                    break;
                case BalanceEffectType.FireRateMultiplier:
                    _mainPlayerUnit.SetFireRate(ClampFireRate(_mainPlayerUnit.FireRate / modifier.Magnitude));
                    SyncFollowers(false, true, false, false, false);
                    break;
            }
        }

        private void SyncFollowers(
            bool syncDamage,
            bool syncFireRate,
            bool syncMaxHp,
            bool healMaxHpByDelta,
            bool syncProjectileCount)
        {
            _playerController?.SyncFollowersFromMain(
                syncDamage,
                syncFireRate,
                syncMaxHp,
                healMaxHpByDelta,
                syncProjectileCount);
        }

        private void ApplyCombinedModifiers()
        {
            _playerController?.SetGateIncomingDamageMultiplier(
                IncomingDamageMultiplier);
            _enemySpawnerSystem?.SetGateSpeedMultiplier(
                EnemySpeedMultiplier);
            _enemySpawnerSystem?.SetGatePressureMultiplier(
                EnemyPressureMultiplier);
            _runStatsTracker?.SetCoinRewardMultiplier(
                _timedModifiers.GetCombinedMultiplier(
                    BalanceEffectType.CoinRewardMultiplier));
        }

        private GateRunStatCaps ActiveCaps => _gateScalingProfile != null
            ? _gateScalingProfile.RunStatCaps
            : null;

        private int ActiveProjectileCap => ActiveCaps != null
            ? Mathf.Min(MaxProjectileCount, ActiveCaps.ProjectileCount)
            : MaxProjectileCount;

        private int ActiveSquadCap => ActiveCaps != null
            ? ActiveCaps.SquadCount
            : int.MaxValue;

        private float ClampDamage(float value)
        {
            return ActiveCaps != null
                ? Mathf.Min(value, ActiveCaps.Damage)
                : value;
        }

        private float ClampFireRate(float value)
        {
            return ActiveCaps != null
                ? Mathf.Min(value, ActiveCaps.FireRate)
                : value;
        }

        private float ClampMaxHp(float value)
        {
            return ActiveCaps != null
                ? Mathf.Min(value, ActiveCaps.MaxHp)
                : value;
        }

        private float GetCappedCombinedModifier(BalanceEffectType effectType)
        {
            float value = _timedModifiers.GetCombinedMultiplier(effectType);
            GateRunStatCaps caps = ActiveCaps;
            if (caps == null)
            {
                return value;
            }

            return effectType switch
            {
                BalanceEffectType.IncomingDamageMultiplier => Mathf.Min(
                    value,
                    caps.MaxIncomingDamageMultiplier),
                BalanceEffectType.EnemyPressureMultiplier => Mathf.Min(
                    value,
                    caps.MaxEnemyPressureMultiplier),
                BalanceEffectType.EnemySpeedMultiplier => Mathf.Max(
                    value,
                    caps.MinEnemySpeedMultiplier),
                _ => value
            };
        }
    }

    public sealed class GateTimedModifierSet
    {
        private readonly List<GateTimedModifier> _modifiers = new List<GateTimedModifier>();

        public int Count => _modifiers.Count;

        public void Add(BalanceEffectType effectType, float magnitude, float durationSeconds)
        {
            if (effectType == BalanceEffectType.None)
            {
                return;
            }

            _modifiers.Add(new GateTimedModifier(
                effectType,
                Mathf.Max(0f, magnitude),
                durationSeconds > 0f ? durationSeconds : -1f));
        }

        public bool Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            bool changed = false;

            for (int index = _modifiers.Count - 1; index >= 0; index--)
            {
                GateTimedModifier modifier = _modifiers[index];
                if (modifier.RemainingSeconds < 0f)
                {
                    continue;
                }

                modifier.RemainingSeconds -= safeDeltaTime;
                if (modifier.RemainingSeconds <= 0f)
                {
                    _modifiers.RemoveAt(index);
                    changed = true;
                }
            }

            return changed;
        }

        public float GetCombinedMultiplier(BalanceEffectType effectType)
        {
            float multiplier = 1f;

            for (int index = 0; index < _modifiers.Count; index++)
            {
                GateTimedModifier modifier = _modifiers[index];
                if (modifier.EffectType == effectType)
                {
                    multiplier *= modifier.Magnitude;
                }
            }

            return Mathf.Max(0f, multiplier);
        }

        public void Clear()
        {
            _modifiers.Clear();
        }
    }

    internal sealed class GateTimedModifier
    {
        public readonly BalanceEffectType EffectType;
        public readonly float Magnitude;
        public float RemainingSeconds;

        public GateTimedModifier(
            BalanceEffectType effectType,
            float magnitude,
            float remainingSeconds)
        {
            EffectType = effectType;
            Magnitude = magnitude;
            RemainingSeconds = remainingSeconds;
        }
    }

    internal sealed class GateTemporaryStatModifier
    {
        public readonly BalanceEffectType EffectType;
        public readonly float Magnitude;
        public float RemainingSeconds;

        public GateTemporaryStatModifier(
            BalanceEffectType effectType,
            float magnitude,
            float remainingSeconds)
        {
            EffectType = effectType;
            Magnitude = Mathf.Max(0.01f, magnitude);
            RemainingSeconds = Mathf.Max(0f, remainingSeconds);
        }
    }
}
