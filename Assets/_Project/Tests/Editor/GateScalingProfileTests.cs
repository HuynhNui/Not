using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Systems.Balance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class GateScalingProfileTests
    {
        [Test]
        public void Resolve_UsesPhaseSpecificGateValues()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            BalanceGateEntry baseEntry = FindDefaultEntry("stable_damage");

            ResolvedGateEntry early = profile.Resolve(baseEntry, 0f);
            ResolvedGateEntry endgame = profile.Resolve(baseEntry, 480f);

            Assert.AreEqual("early", early.PhaseId);
            Assert.AreEqual(1.10f, early.Magnitude, 0.0001f);
            Assert.AreEqual("endgame", endgame.PhaseId);
            Assert.AreEqual(1.20f, endgame.Magnitude, 0.0001f);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void MajorRollResult_ForcesAfterOneEligibleMiss()
        {
            MajorRollResult firstMiss = MajorRollResult.Evaluate(
                isEligible: true,
                chance: 0f,
                randomValue: 1f,
                consecutiveMisses: 0,
                guaranteedAfterMisses: 1,
                hasApplicableMajor: true);
            MajorRollResult forced = MajorRollResult.Evaluate(
                isEligible: true,
                chance: 0f,
                randomValue: 1f,
                consecutiveMisses: firstMiss.ConsecutiveMissesAfter,
                guaranteedAfterMisses: 1,
                hasApplicableMajor: true);

            Assert.False(firstMiss.MajorSpawned);
            Assert.True(forced.MajorSpawned);
            Assert.True(forced.WasForced);
            Assert.AreEqual(0, forced.ConsecutiveMissesAfter);
        }

        [Test]
        public void Resolve_UsesDamageForwardPhaseTimingAndWeights()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            BalanceGateEntry baseEntry = FindDefaultEntry("stable_fire_rate");

            Assert.AreEqual("growth", profile.Resolve(baseEntry, 90f).PhaseId);
            Assert.AreEqual("pressure", profile.Resolve(baseEntry, 180f).PhaseId);
            Assert.AreEqual("late", profile.Resolve(baseEntry, 300f).PhaseId);
            ResolvedGateEntry endgame = profile.Resolve(baseEntry, 420f);

            Assert.AreEqual("endgame", endgame.PhaseId);
            Assert.AreEqual(1.5f, endgame.OfferWeightMultiplier, 0.0001f);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void Resolve_UsesTemporaryRiskyDrawbacks()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            BalanceGateEntry bulletStorm = FindDefaultEntry("risky_bullet_storm");
            BalanceGateEntry reinforcement = FindDefaultEntry("risky_reinforcement");

            ResolvedGateEntry pressureBulletStorm = profile.Resolve(bulletStorm, 180f);
            ResolvedGateEntry earlyReinforcement = profile.Resolve(reinforcement, 0f);

            Assert.AreEqual(1f, pressureBulletStorm.Magnitude, 0.0001f);
            Assert.AreEqual(0.94f, pressureBulletStorm.DrawbackMagnitude, 0.0001f);
            Assert.AreEqual(20f, pressureBulletStorm.DrawbackDurationSeconds, 0.0001f);
            Assert.AreEqual(1f, earlyReinforcement.Magnitude, 0.0001f);
            Assert.AreEqual(1.15f, earlyReinforcement.DrawbackMagnitude, 0.0001f);
            Assert.AreEqual(25f, earlyReinforcement.DrawbackDurationSeconds, 0.0001f);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void RunStatCaps_UseEliteSquadValues()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            GateRunStatCaps caps = profile.RunStatCaps;

            Assert.AreEqual(6.00f, caps.Damage, 0.0001f);
            Assert.AreEqual(7.00f, caps.FireRate, 0.0001f);
            Assert.AreEqual(36f, caps.MaxHp, 0.0001f);
            Assert.AreEqual(4, caps.ProjectileCount);
            Assert.AreEqual(5, caps.SquadCount);
            Assert.AreEqual(1.75f, caps.MaxIncomingDamageMultiplier, 0.0001f);
            Assert.AreEqual(1.50f, caps.MaxEnemyPressureMultiplier, 0.0001f);
            Assert.AreEqual(0.50f, caps.MinEnemySpeedMultiplier, 0.0001f);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void GateEffectPreview_ClampsToEliteSquadCaps()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            BalanceGateEntry projectileEntry = FindDefaultEntry("major_projectile");
            BalanceGateEntry recruitEntry = FindDefaultEntry("major_recruit");
            GateConfig projectileGate = ScriptableObject.CreateInstance<GateConfig>();
            GateConfig recruitGate = ScriptableObject.CreateInstance<GateConfig>();
            projectileGate.ConfigureRuntime(profile.Resolve(projectileEntry, 480f), 480f);
            recruitGate.ConfigureRuntime(profile.Resolve(recruitEntry, 480f), 480f);

            var projectileBefore = new GateStatSnapshot(6.4f, 6.9f, 36f, 3, 4);
            GateEffectPreviewResult projectilePreview = GateEffectPreview.Preview(
                projectileGate,
                projectileBefore,
                profile.RunStatCaps,
                technicalProjectileCap: 50,
                technicalSquadCap: 50);

            Assert.True(projectilePreview.HasStatChange);
            Assert.AreEqual(4, projectilePreview.After.ProjectileCount);

            GateEffectPreviewResult cappedProjectilePreview = GateEffectPreview.Preview(
                projectileGate,
                new GateStatSnapshot(6.4f, 6.9f, 36f, 4, 4),
                profile.RunStatCaps,
                technicalProjectileCap: 50,
                technicalSquadCap: 50);
            Assert.False(cappedProjectilePreview.HasStatChange);
            Assert.True(cappedProjectilePreview.WasCapped);
            Assert.AreEqual(4, cappedProjectilePreview.After.ProjectileCount);

            GateEffectPreviewResult recruitPreview = GateEffectPreview.Preview(
                recruitGate,
                new GateStatSnapshot(6.4f, 6.9f, 36f, 4, 4),
                profile.RunStatCaps,
                technicalProjectileCap: 50,
                technicalSquadCap: 50);
            Assert.True(recruitPreview.HasStatChange);
            Assert.AreEqual(5, recruitPreview.After.SquadCount);

            Object.DestroyImmediate(projectileGate);
            Object.DestroyImmediate(recruitGate);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void GateEffectPreview_ClampsDamageAndFireToDamageForwardCaps()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            GateConfig damageGate = ScriptableObject.CreateInstance<GateConfig>();
            damageGate.ConfigureRuntime(profile.Resolve(FindDefaultEntry("stable_damage"), 480f), 480f);
            GateConfig fireGate = ScriptableObject.CreateInstance<GateConfig>();
            fireGate.ConfigureRuntime(profile.Resolve(FindDefaultEntry("stable_fire_rate"), 90f), 90f);

            GateEffectPreviewResult damagePreview = GateEffectPreview.Preview(
                damageGate,
                new GateStatSnapshot(5.95f, 6.90f, 36f, 4, 5),
                profile.RunStatCaps,
                technicalProjectileCap: 50,
                technicalSquadCap: 50);
            GateEffectPreviewResult firePreview = GateEffectPreview.Preview(
                fireGate,
                new GateStatSnapshot(6.40f, 6.90f, 36f, 4, 5),
                profile.RunStatCaps,
                technicalProjectileCap: 50,
                technicalSquadCap: 50);

            Assert.AreEqual(6.00f, damagePreview.After.Damage, 0.0001f);
            Assert.True(damagePreview.WasCapped);
            Assert.AreEqual(7.00f, firePreview.After.FireRate, 0.0001f);
            Assert.True(firePreview.WasCapped);

            Object.DestroyImmediate(damageGate);
            Object.DestroyImmediate(fireGate);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void BenchmarkProfile_IsEditorOnlyAndMatchesStrongestStart()
        {
            BalanceBenchmarkProfile profile = ScriptableObject.CreateInstance<BalanceBenchmarkProfile>();
            PlayerRunStartStats stats = profile.ToRunStartStats();

            Assert.True(profile.IsActive);
            Assert.AreEqual(3.00f, stats.Damage, 0.0001f);
            Assert.AreEqual(6.40f, stats.FireRate, 0.0001f);
            Assert.AreEqual(20f, stats.MaxHp, 0.0001f);
            Assert.AreEqual(3, stats.ProjectileCount);
            Assert.AreEqual(4, stats.SquadSize);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void EliteSquad_MathMatchesLinearSquadAndVisualBudget()
        {
            CombatScalingConfig combat = ScriptableObject.CreateInstance<CombatScalingConfig>();
            SetSquadPowerModel(combat, SquadPowerModel.EqualStrengthUnits);

            float startDps = BalanceV1Math.EffectiveDps(
                damage: 3.00f,
                rawFireRate: 6.40f,
                projectileCount: 3,
                squadCount: 4,
                config: combat);
            float newCapDps = BalanceV1Math.EffectiveDps(
                damage: 6.00f,
                rawFireRate: 7.00f,
                projectileCount: 4,
                squadCount: 5,
                config: combat);
            float startEmissions = BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                rawFireRate: 6.40f,
                projectileCount: 3,
                squadCount: 4,
                config: combat);
            float newCapEmissions = BalanceV1Math.EstimatedBaseProjectileEmissionsPerSecond(
                rawFireRate: 7.00f,
                projectileCount: 4,
                squadCount: 5,
                config: combat);

            Assert.AreEqual(4f, BalanceV1Math.SquadFactor(4, combat), 0.0001f);
            Assert.AreEqual(1f, BalanceV1Math.FollowerDamageScale(4, combat), 0.0001f);
            Assert.AreEqual(1f, BalanceV1Math.FollowerHpScale(combat), 0.0001f);
            Assert.AreEqual(229.935f, startDps, 0.01f);
            Assert.AreEqual(830.769f, newCapDps, 0.01f);
            Assert.AreEqual(76.645f, startEmissions, 0.1f);
            Assert.AreEqual(138.462f, newCapEmissions, 0.1f);
            Assert.LessOrEqual(newCapEmissions, 140f);

            Object.DestroyImmediate(combat);
        }

        private static void SetSquadPowerModel(
            CombatScalingConfig combat,
            SquadPowerModel model)
        {
            var serializedObject = new SerializedObject(combat);
            serializedObject.FindProperty("squadPowerModel").enumValueIndex = (int)model;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            combat.ValidateValues();
        }

        private static BalanceGateEntry FindDefaultEntry(string gateId)
        {
            foreach (BalanceGateEntry entry in GatePoolConfig.CreateDefaultEntries())
            {
                if (entry.GateId == gateId)
                {
                    return entry;
                }
            }

            Assert.Fail($"Default gate entry not found: {gateId}");
            return null;
        }
    }
}
