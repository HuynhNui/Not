using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TrueGate.PlayModeTests
{
    public sealed class BalanceRuntimePlayModeTests
    {
        [UnityTest]
        public IEnumerator PermanentUpgradeApplication_IsIdempotent()
        {
            string saveDirectory = CreateTempDirectory("meta");
            object saveService = CreateTestSaveService(saveDirectory);
            GameObject squadObject = new GameObject("MetaApplyPlayModeTest");

            try
            {
                Component bulletSpawner = squadObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Combat.BulletSpawner"));
                Component main = squadObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.MainPlayerUnit"));
                Component controller = squadObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.PlayerController"));
                Invoke(controller, "SetMainPlayerUnit", main);

                Type upgradeService = RuntimeType(
                    "_Project.Scripts.Systems.ProgressionSystem.PlayerMetaUpgradeService");
                MethodInfo apply = upgradeService.GetMethod(
                    "ApplyToPlayer",
                    BindingFlags.Public | BindingFlags.Static);

                apply.Invoke(null, new object[] { main, controller });
                RuntimeStats first = CaptureStats(main, controller, bulletSpawner);
                apply.Invoke(null, new object[] { main, controller });
                RuntimeStats second = CaptureStats(main, controller, bulletSpawner);

                Assert.That(second.damage, Is.EqualTo(first.damage).Within(0.0001f));
                Assert.That(second.fireRate, Is.EqualTo(first.fireRate).Within(0.0001f));
                Assert.That(second.maxHp, Is.EqualTo(first.maxHp).Within(0.0001f));
                Assert.That(second.projectiles, Is.EqualTo(first.projectiles));
                Assert.That(second.squadCount, Is.EqualTo(first.squadCount));
                yield return null;
            }
            finally
            {
                ResetTestSaveService();
                UnityEngine.Object.Destroy(squadObject);
                DeleteDirectory(saveDirectory);
            }
        }

        [UnityTest]
        public IEnumerator RecruitAndPromotion_PreserveExpectedHealthRatios()
        {
            GameObject squadObject = new GameObject("SquadHealthPlayModeTest");

            try
            {
                squadObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Combat.BulletSpawner"));
                Component main = squadObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.MainPlayerUnit"));
                Component controller = squadObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.PlayerController"));
                Invoke(controller, "SetMainPlayerUnit", main);
                Invoke(main, "SetMaxHp", 20f, false);
                Invoke(main, "RestoreFullHealth");
                Invoke(controller, "SetSquadCount", 2, 0.5f);

                var followers = (IList)GetProperty(controller, "Followers");
                Component follower = (Component)followers[0];
                Assert.That((float)GetProperty(follower, "MaxHp"), Is.EqualTo(5f).Within(0.0001f));
                Assert.That((float)GetProperty(follower, "CurrentHp"), Is.EqualTo(2.5f).Within(0.0001f));

                Invoke(follower, "SetCurrentHp", 3f);
                Invoke(main, "ReviveWithStateFrom", follower);
                Assert.That((float)GetProperty(main, "MaxHp"), Is.EqualTo(20f).Within(0.0001f));
                Assert.That((float)GetProperty(main, "CurrentHp"), Is.EqualTo(3f).Within(0.0001f));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(squadObject);
            }
        }

        [UnityTest]
        public IEnumerator ChomboomExplosion_HitsNearbyUnitsOnce()
        {
            GameObject nearAObject = new GameObject("NearA");
            GameObject nearBObject = new GameObject("NearB");
            GameObject farObject = new GameObject("Far");
            GameObject explosionObject = new GameObject("Explosion");

            try
            {
                Component nearA = nearAObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.PlayerUnit"));
                Component nearB = nearBObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.PlayerUnit"));
                Component far = farObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Player.PlayerUnit"));
                Component explosion = explosionObject.AddComponent(
                    RuntimeType("_Project.Scripts.Gameplay.Enemies.ChomboomBoomFx"));

                InitializeHealth(nearA);
                InitializeHealth(nearB);
                InitializeHealth(far);
                nearAObject.transform.position = Vector3.zero;
                nearBObject.transform.position = Vector3.right;
                farObject.transform.position = Vector3.right * 3f;
                explosionObject.transform.position = Vector3.zero;

                Invoke(explosion, "Init", null, 3f, 1.75f);
                Invoke(explosion, "Spawn");
                yield return null;

                Assert.That((float)GetProperty(nearA, "CurrentHp"), Is.EqualTo(7f).Within(0.0001f));
                Assert.That((float)GetProperty(nearB, "CurrentHp"), Is.EqualTo(7f).Within(0.0001f));
                Assert.That((float)GetProperty(far, "CurrentHp"), Is.EqualTo(10f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.Destroy(nearAObject);
                UnityEngine.Object.Destroy(nearBObject);
                UnityEngine.Object.Destroy(farObject);
                UnityEngine.Object.Destroy(explosionObject);
            }
        }

        [UnityTest]
        public IEnumerator WalletCoins_CommitOnlyWhenRunEnds()
        {
            string saveDirectory = CreateTempDirectory("wallet");
            object saveService = CreateTestSaveService(saveDirectory);
            GameObject trackerObject = new GameObject("RunStatsPlayModeTest");

            try
            {
                Component tracker = trackerObject.AddComponent(
                    RuntimeType("_Project.Scripts.Systems.RunStatsSystem.RunStatsTracker"));
                object saveData = GetProperty(saveService, "Data");
                int walletBefore = (int)GetField(saveData, "walletCoins");

                Invoke(tracker, "BeginRun");
                SetField(tracker, "_coinRewardPoints", 2.6f);
                SetField(tracker, "_survivalTime", 10f);
                Assert.That((int)GetField(saveData, "walletCoins"), Is.EqualTo(walletBefore));

                Invoke(tracker, "EndRun");
                Assert.That((int)GetField(saveData, "walletCoins"), Is.EqualTo(walletBefore + 3));
                yield return null;
            }
            finally
            {
                ResetTestSaveService();
                UnityEngine.Object.Destroy(trackerObject);
                DeleteDirectory(saveDirectory);
            }
        }

        [UnityTest]
        public IEnumerator PressureThreatAndGateCadence_RespectRuntimeConstraints()
        {
            Type pressureType = RuntimeType("_Project.Scripts.Data.Balance.RunPressureConfig");
            object pressure = pressureType.GetMethod(
                "EvaluateDefault",
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { 300f });
            int activeCap = (int)GetField(pressure, "ActiveCap");
            int minimumVisible = (int)GetField(pressure, "MinimumVisible");
            Assert.That(minimumVisible, Is.LessThanOrEqualTo(activeCap));

            Type roleDefaults = RuntimeType(
                "_Project.Scripts.Data.Balance.EnemyRoleBalanceDefaults");
            bool specialFits = (bool)roleDefaults.GetMethod(
                "CanFitThreat",
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { 1.5f, 1.5f, 2f });
            bool basicFits = (bool)roleDefaults.GetMethod(
                "CanFitThreat",
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { 1.5f, 0f, 2f });
            Assert.That(specialFits, Is.False);
            Assert.That(basicFits, Is.True);

            Type gateSystem = RuntimeType("_Project.Scripts.Systems.GateSystem.GateSystem");
            MethodInfo eligibility = gateSystem.GetMethod(
                "IsMajorEligibilitySet",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That((bool)eligibility.Invoke(null, new object[] { 3, 15f, 60f }), Is.False);
            Assert.That((bool)eligibility.Invoke(null, new object[] { 4, 15f, 60f }), Is.True);
            yield return null;
        }

        private static RuntimeStats CaptureStats(
            Component main,
            Component controller,
            Component bulletSpawner)
        {
            return new RuntimeStats
            {
                damage = (float)GetProperty(main, "Damage"),
                fireRate = (float)GetProperty(main, "FireRate"),
                maxHp = (float)GetProperty(main, "MaxHp"),
                projectiles = (int)GetProperty(bulletSpawner, "ProjectileCount"),
                squadCount = (int)GetProperty(controller, "CurrentSquadCount")
            };
        }

        private static void InitializeHealth(Component unit)
        {
            Invoke(unit, "SetMaxHp", 10f, false);
            Invoke(unit, "RestoreFullHealth");
        }

        private static Type RuntimeType(string fullName)
        {
            return Type.GetType($"{fullName}, Assembly-CSharp", throwOnError: true);
        }

        private static object CreateTestSaveService(string directory)
        {
            Type saveServiceType = RuntimeType(
                "_Project.Scripts.Systems.SaveSystem.SaveService");
            object service = saveServiceType.GetMethod(
                "CreateForTests",
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { directory });
            saveServiceType.GetMethod(
                "SetInstanceForTests",
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { service });
            return service;
        }

        private static void ResetTestSaveService()
        {
            Type saveServiceType = RuntimeType(
                "_Project.Scripts.Systems.SaveSystem.SaveService");
            saveServiceType.GetMethod(
                "SetInstanceForTests",
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { null });
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, arguments.Length);
            return method.Invoke(target, arguments);
        }

        private static MethodInfo FindMethod(Type type, string methodName, int argumentCount)
        {
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name == methodName
                    && method.GetParameters().Length == argumentCount)
                {
                    return method;
                }
            }

            throw new MissingMethodException(type.FullName, methodName);
        }

        private static object GetProperty(object target, string propertyName)
        {
            return target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(target);
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);
        }

        private static string CreateTempDirectory(string suffix)
        {
            return Path.Combine(
                Application.temporaryCachePath,
                $"true-gate-playmode-{suffix}-{Guid.NewGuid():N}");
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private sealed class RuntimeStats
        {
            public float damage;
            public float fireRate;
            public float maxHp;
            public int projectiles;
            public int squadCount;
        }
    }
}
