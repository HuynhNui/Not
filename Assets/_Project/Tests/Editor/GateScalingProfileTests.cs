using _Project.Scripts.Data.Balance;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Systems.Balance;
using NUnit.Framework;
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
        public void MajorRollResult_ForcesAfterTwoEligibleMisses()
        {
            MajorRollResult firstMiss = MajorRollResult.Evaluate(
                isEligible: true,
                chance: 0f,
                randomValue: 1f,
                consecutiveMisses: 0,
                guaranteedAfterMisses: 2,
                hasApplicableMajor: true);
            MajorRollResult secondMiss = MajorRollResult.Evaluate(
                isEligible: true,
                chance: 0f,
                randomValue: 1f,
                consecutiveMisses: firstMiss.ConsecutiveMissesAfter,
                guaranteedAfterMisses: 2,
                hasApplicableMajor: true);
            MajorRollResult forced = MajorRollResult.Evaluate(
                isEligible: true,
                chance: 0f,
                randomValue: 1f,
                consecutiveMisses: secondMiss.ConsecutiveMissesAfter,
                guaranteedAfterMisses: 2,
                hasApplicableMajor: true);

            Assert.False(firstMiss.MajorSpawned);
            Assert.False(secondMiss.MajorSpawned);
            Assert.True(forced.MajorSpawned);
            Assert.True(forced.WasForced);
            Assert.AreEqual(0, forced.ConsecutiveMissesAfter);
        }

        [Test]
        public void GateEffectPreview_FiltersStatGateAtCap()
        {
            GateScalingProfile profile = ScriptableObject.CreateInstance<GateScalingProfile>();
            BalanceGateEntry baseEntry = FindDefaultEntry("major_projectile");
            ResolvedGateEntry resolved = profile.Resolve(baseEntry, 480f);
            GateConfig gate = ScriptableObject.CreateInstance<GateConfig>();
            gate.ConfigureRuntime(resolved, 480f);

            var before = new GateStatSnapshot(
                damage: 3.5f,
                fireRate: 8.5f,
                maxHp: 36f,
                projectileCount: 9,
                squadCount: 16);
            GateEffectPreviewResult preview = GateEffectPreview.Preview(
                gate,
                before,
                profile.RunStatCaps,
                technicalProjectileCap: 50,
                technicalSquadCap: 50);

            Assert.False(preview.HasStatChange);
            Assert.True(preview.WasCapped);
            Assert.AreEqual(9, preview.After.ProjectileCount);

            Object.DestroyImmediate(gate);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void BenchmarkProfile_IsEditorOnlyAndMatchesStrongestStart()
        {
            BalanceBenchmarkProfile profile = ScriptableObject.CreateInstance<BalanceBenchmarkProfile>();
            PlayerRunStartStats stats = profile.ToRunStartStats();

            Assert.True(profile.IsActive);
            Assert.AreEqual(1.90f, stats.Damage, 0.0001f);
            Assert.AreEqual(6.40f, stats.FireRate, 0.0001f);
            Assert.AreEqual(20f, stats.MaxHp, 0.0001f);
            Assert.AreEqual(6, stats.ProjectileCount);
            Assert.AreEqual(12, stats.SquadSize);

            Object.DestroyImmediate(profile);
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
