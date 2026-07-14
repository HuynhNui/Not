using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Player;
using UnityEngine;

namespace _Project.Scripts.Systems.Balance
{
    public readonly struct GateStatSnapshot
    {
        public readonly float Damage;
        public readonly float FireRate;
        public readonly float MaxHp;
        public readonly int ProjectileCount;
        public readonly int SquadCount;

        public GateStatSnapshot(
            float damage,
            float fireRate,
            float maxHp,
            int projectileCount,
            int squadCount)
        {
            Damage = Mathf.Max(0f, damage);
            FireRate = Mathf.Max(0f, fireRate);
            MaxHp = Mathf.Max(0f, maxHp);
            ProjectileCount = Mathf.Max(0, projectileCount);
            SquadCount = Mathf.Max(0, squadCount);
        }

        public static GateStatSnapshot FromRuntime(
            MainPlayerUnit mainPlayerUnit,
            PlayerController playerController)
        {
            return new GateStatSnapshot(
                mainPlayerUnit != null ? mainPlayerUnit.Damage : 0f,
                mainPlayerUnit != null ? mainPlayerUnit.FireRate : 0f,
                mainPlayerUnit != null ? mainPlayerUnit.MaxHp : 0f,
                mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null
                    ? mainPlayerUnit.BulletSpawner.ProjectileCount
                    : 0,
                playerController != null ? playerController.CurrentSquadCount : 0);
        }
    }

    public readonly struct GateEffectPreviewResult
    {
        public readonly GateStatSnapshot Before;
        public readonly GateStatSnapshot After;
        public readonly bool WasCapped;
        public readonly bool HasStatChange;

        public GateEffectPreviewResult(
            GateStatSnapshot before,
            GateStatSnapshot after,
            bool wasCapped,
            bool hasStatChange)
        {
            Before = before;
            After = after;
            WasCapped = wasCapped;
            HasStatChange = hasStatChange;
        }
    }

    public static class GateEffectPreview
    {
        public static GateEffectPreviewResult Preview(
            GateConfig config,
            GateStatSnapshot before,
            GateRunStatCaps caps,
            int technicalProjectileCap,
            int technicalSquadCap)
        {
            if (config == null || !config.HasRuntimeEffects)
            {
                return new GateEffectPreviewResult(before, before, false, false);
            }

            caps ??= new GateRunStatCaps();
            float damage = before.Damage;
            float fireRate = before.FireRate;
            float maxHp = before.MaxHp;
            int projectileCount = before.ProjectileCount;
            int squadCount = before.SquadCount;
            bool capped = false;

            foreach (GateRuntimeEffect effect in config.RuntimeEffects)
            {
                ApplyEffect(
                    effect,
                    caps,
                    technicalProjectileCap,
                    technicalSquadCap,
                    ref damage,
                    ref fireRate,
                    ref maxHp,
                    ref projectileCount,
                    ref squadCount,
                    ref capped);
            }

            var after = new GateStatSnapshot(damage, fireRate, maxHp, projectileCount, squadCount);
            return new GateEffectPreviewResult(
                before,
                after,
                capped,
                !Approximately(before, after));
        }

        private static void ApplyEffect(
            GateRuntimeEffect effect,
            GateRunStatCaps caps,
            int technicalProjectileCap,
            int technicalSquadCap,
            ref float damage,
            ref float fireRate,
            ref float maxHp,
            ref int projectileCount,
            ref int squadCount,
            ref bool capped)
        {
            if (effect == null || effect.IsDrawback)
            {
                return;
            }

            switch (effect.EffectType)
            {
                case BalanceEffectType.DamageMultiplier:
                    damage = ClampFloat(damage * effect.Magnitude, caps.Damage, ref capped);
                    break;
                case BalanceEffectType.FireRateFlat:
                    fireRate = ClampFloat(fireRate + effect.Magnitude, caps.FireRate, ref capped);
                    break;
                case BalanceEffectType.FireRateMultiplier:
                    fireRate = ClampFloat(fireRate * effect.Magnitude, caps.FireRate, ref capped);
                    break;
                case BalanceEffectType.MaxHpMultiplier:
                    maxHp = ClampFloat(maxHp * effect.Magnitude, caps.MaxHp, ref capped);
                    break;
                case BalanceEffectType.ProjectileFlat:
                    projectileCount = ClampInt(
                        projectileCount + Mathf.RoundToInt(effect.Magnitude),
                        Mathf.Min(caps.ProjectileCount, Mathf.Max(1, technicalProjectileCap)),
                        ref capped);
                    break;
                case BalanceEffectType.SquadFlat:
                    squadCount = ClampInt(
                        squadCount + Mathf.RoundToInt(effect.Magnitude),
                        Mathf.Min(caps.SquadCount, Mathf.Max(1, technicalSquadCap)),
                        ref capped);
                    break;
            }
        }

        private static float ClampFloat(float value, float cap, ref bool capped)
        {
            if (value > cap)
            {
                capped = true;
                return cap;
            }

            return value;
        }

        private static int ClampInt(int value, int cap, ref bool capped)
        {
            if (value > cap)
            {
                capped = true;
                return cap;
            }

            return Mathf.Max(1, value);
        }

        private static bool Approximately(GateStatSnapshot left, GateStatSnapshot right)
        {
            return Mathf.Approximately(left.Damage, right.Damage)
                && Mathf.Approximately(left.FireRate, right.FireRate)
                && Mathf.Approximately(left.MaxHp, right.MaxHp)
                && left.ProjectileCount == right.ProjectileCount
                && left.SquadCount == right.SquadCount;
        }
    }
}
