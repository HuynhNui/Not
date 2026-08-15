using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrueGate.PlayModeTests
{
    public sealed class EnemyContactAndProjectilePlayModeTests
    {
        private const string ProductionEnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy.prefab";

        [UnityTest]
        public IEnumerator EnemyOverlapPolling_DamagesPlayerAndDespawns()
        {
            GameObject playerObject = CreatePlayer("Player", Vector3.zero);
            GameObject enemyObject = CreateEnemy("Enemy", Vector3.zero);
            Component player = playerObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            Component enemy = enemyObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));

            try
            {
                Invoke(player, "SetMaxHp", 10f, false);
                Invoke(player, "RestoreFullHealth");
                Invoke(enemy, "Init", null, null, null, null);
                Invoke(enemy, "SetMovementEnabled", false);

                Physics2D.SyncTransforms();
                yield return null;

                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(9f).Within(0.001f));
                Assert.That((bool)GetProperty(enemy, "IsActive"), Is.False);
            }
            finally
            {
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(enemyObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyContact_DoesNotUseTransparentSpritePadding()
        {
            GameObject playerObject = CreatePlayer("Player", Vector3.zero, withLargeSprite: true);
            GameObject enemyObject = CreateEnemy("Enemy", new Vector3(0.4f, 0f, 0f), colliderSize: new Vector2(0.1f, 0.1f));
            Component player = playerObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            Component enemy = enemyObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));

            try
            {
                Invoke(player, "SetMaxHp", 10f, false);
                Invoke(player, "RestoreFullHealth");
                Invoke(enemy, "Init", null, null, null, null);
                Invoke(enemy, "SetMovementEnabled", false);

                Physics2D.SyncTransforms();
                yield return null;

                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(10f).Within(0.001f));
                Assert.That((bool)GetProperty(enemy, "IsActive"), Is.True);
            }
            finally
            {
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(enemyObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyRepeatedContact_RespectsCooldown()
        {
            GameObject playerObject = CreatePlayer("Player", Vector3.zero);
            GameObject enemyObject = CreateEnemy("Enemy", Vector3.zero);
            Component player = playerObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            Component enemy = enemyObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));

            try
            {
                SetField(enemy, "destroyOnPlayerHit", false);
                SetField(enemy, "repeatedContactDamageCooldown", 0.5f);
                Invoke(player, "SetMaxHp", 10f, false);
                Invoke(player, "RestoreFullHealth");
                Invoke(enemy, "Init", null, null, null, null);
                Invoke(enemy, "SetMovementEnabled", false);

                Physics2D.SyncTransforms();
                yield return null;
                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(9f).Within(0.001f));

                yield return null;
                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(9f).Within(0.001f));

                yield return new WaitForSeconds(0.55f);
                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(8f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(enemyObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyChase_TargetsPlayerLogicalCenter()
        {
            GameObject controllerObject = new GameObject("SquadController");
            GameObject playerObject = CreatePlayer("Player", Vector3.zero, withLargeSprite: true);
            GameObject enemyObject = CreateEnemy("Enemy", new Vector3(0f, 1f, 0f), colliderSize: new Vector2(0.1f, 0.1f));
            Component controller = controllerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerController"));
            Component player = playerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.MainPlayerUnit"));
            Component enemy = enemyObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));

            try
            {
                Invoke(controller, "SetMainPlayerUnit", player);
                Invoke(player, "SetMaxHp", 10f, false);
                Invoke(player, "RestoreFullHealth");
                Invoke(enemy, "Init", playerObject.transform, player, null, controller);

                Vector3 targetPosition = (Vector3)Invoke(enemy, "GetCurrentTargetPosition");

                Assert.That(targetPosition.x, Is.EqualTo(playerObject.transform.position.x).Within(0.001f));
                Assert.That(targetPosition.y, Is.EqualTo(playerObject.transform.position.y).Within(0.001f));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(controllerObject);
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(enemyObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyPooling_DoesNotReplaceOrDuplicateManualCollider()
        {
            GameObject poolObject = new GameObject("Pool");
            Component pool = poolObject.AddComponent(RuntimeType(
                "_Project.Scripts.Systems.PoolSystem.PoolSystem"));
            GameObject prefabObject = CreateEnemy("EnemyPrefab", Vector3.zero);
            prefabObject.SetActive(false);
            Component prefabEnemy = prefabObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));
            BoxCollider2D prefabCollider = prefabObject.GetComponent<BoxCollider2D>();
            prefabCollider.offset = new Vector2(0.2f, 0.3f);
            prefabCollider.size = new Vector2(0.4f, 0.5f);

            try
            {
                Component enemy = (Component)InvokeGeneric(
                    pool,
                    "Spawn",
                    RuntimeType("_Project.Scripts.Gameplay.Enemies.EnemyController"),
                    prefabEnemy,
                    Vector3.zero,
                    Quaternion.identity);
                Invoke(enemy, "Init", null, null, null, null);
                Invoke(enemy, "Despawn");

                Component reusedEnemy = (Component)InvokeGeneric(
                    pool,
                    "Spawn",
                    RuntimeType("_Project.Scripts.Gameplay.Enemies.EnemyController"),
                    prefabEnemy,
                    Vector3.one,
                    Quaternion.identity);
                Invoke(reusedEnemy, "Init", null, null, null, null);

                BoxCollider2D[] colliders = reusedEnemy.GetComponents<BoxCollider2D>();
                Assert.That(colliders, Has.Length.EqualTo(1));
                Assert.That(colliders[0].offset.x, Is.EqualTo(0.2f).Within(0.001f));
                Assert.That(colliders[0].offset.y, Is.EqualTo(0.3f).Within(0.001f));
                Assert.That(colliders[0].size.x, Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(colliders[0].size.y, Is.EqualTo(0.5f).Within(0.001f));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(poolObject);
                UnityEngine.Object.Destroy(prefabObject);
            }
        }

        [UnityTest]
        public IEnumerator ProductionEnemyPrefab_DoesNotDamagePlayerBeforeRealColliderContact()
        {
#if UNITY_EDITOR
            GameObject enemyObject = UnityEngine.Object.Instantiate(
                LoadProductionEnemyPrefab(),
                Vector3.zero,
                Quaternion.identity);
            GameObject playerObject = CreatePlayer("Player", Vector3.zero);
            Component player = playerObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            Component enemy = enemyObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));
            CircleCollider2D playerCollider = playerObject.GetComponent<CircleCollider2D>();
            BoxCollider2D enemyCollider = enemyObject.GetComponent<BoxCollider2D>();

            try
            {
                Invoke(player, "SetMaxHp", 10f, false);
                Invoke(player, "RestoreFullHealth");

                Physics2D.SyncTransforms();
                Bounds enemyBounds = enemyCollider.bounds;
                playerObject.transform.position = new Vector3(
                    enemyBounds.center.x,
                    enemyBounds.min.y - playerCollider.radius - 0.04f,
                    0f);
                Physics2D.SyncTransforms();

                Invoke(enemy, "Init", null, null, null, null);
                Invoke(enemy, "SetMovementEnabled", false);
                yield return null;

                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(10f).Within(0.001f));
                Assert.That((bool)GetProperty(enemy, "IsActive"), Is.True);

                playerObject.transform.position = enemyBounds.center;
                Physics2D.SyncTransforms();
                yield return null;

                Assert.That((float)GetProperty(player, "CurrentHp"), Is.EqualTo(9f).Within(0.001f));
                Assert.That((bool)GetProperty(enemy, "IsActive"), Is.False);
            }
            finally
            {
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(enemyObject);
            }
#else
            Assert.Ignore("Production prefab contact test requires UnityEditor AssetDatabase.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator ProductionEnemyPrefab_PoolingPreservesColliderGeometry()
        {
#if UNITY_EDITOR
            GameObject poolObject = new GameObject("Pool");
            Component pool = poolObject.AddComponent(RuntimeType(
                "_Project.Scripts.Systems.PoolSystem.PoolSystem"));
            GameObject prefabObject = UnityEngine.Object.Instantiate(LoadProductionEnemyPrefab());
            prefabObject.SetActive(false);
            Component prefabEnemy = prefabObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));
            BoxCollider2D prefabCollider = prefabObject.GetComponent<BoxCollider2D>();
            Vector2 expectedOffset = prefabCollider.offset;
            Vector2 expectedSize = prefabCollider.size;

            try
            {
                Component enemy = (Component)InvokeGeneric(
                    pool,
                    "Spawn",
                    RuntimeType("_Project.Scripts.Gameplay.Enemies.EnemyController"),
                    prefabEnemy,
                    Vector3.zero,
                    Quaternion.identity);
                Invoke(enemy, "Init", null, null, null, null);
                Invoke(enemy, "Despawn");

                Component reusedEnemy = (Component)InvokeGeneric(
                    pool,
                    "Spawn",
                    RuntimeType("_Project.Scripts.Gameplay.Enemies.EnemyController"),
                    prefabEnemy,
                    Vector3.one,
                    Quaternion.identity);
                Invoke(reusedEnemy, "Init", null, null, null, null);

                BoxCollider2D[] colliders = reusedEnemy.GetComponents<BoxCollider2D>();
                Assert.That(colliders, Has.Length.EqualTo(1));
                Assert.That(colliders[0].isTrigger, Is.True);
                Assert.That(colliders[0].offset.x, Is.EqualTo(expectedOffset.x).Within(0.001f));
                Assert.That(colliders[0].offset.y, Is.EqualTo(expectedOffset.y).Within(0.001f));
                Assert.That(colliders[0].size.x, Is.EqualTo(expectedSize.x).Within(0.001f));
                Assert.That(colliders[0].size.y, Is.EqualTo(expectedSize.y).Within(0.001f));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(poolObject);
                UnityEngine.Object.Destroy(prefabObject);
            }
#else
            Assert.Ignore("Production prefab pooling test requires UnityEditor AssetDatabase.");
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator Chomboom_ArmsOnlyInsideRealTriggerRadius()
        {
            GameObject playerObject = CreatePlayer("Player", new Vector3(0.6f, 0f, 0f), withLargeSprite: true);
            GameObject chomboomObject = CreateEnemy("Chomboom", Vector3.zero, colliderSize: new Vector2(0.1f, 0.1f));
            Component enemy = chomboomObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));
            Component chomboom = chomboomObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.ChomboomController"));

            try
            {
                SetField(chomboom, "triggerRadius", 0.45f);
                SetField(chomboom, "armingDuration", 10f);
                Invoke(enemy, "Init", playerObject.transform, null, null, null);
                Invoke(enemy, "SetExternalMoveSpeedMultiplier", 0f);
                Invoke(enemy, "Spawn");

                yield return null;

                Assert.That((bool)GetField(enemy, "_movementEnabled"), Is.True);

                chomboomObject.transform.position = new Vector3(0.25f, 0f, 0f);
                yield return null;

                Assert.That((bool)GetField(enemy, "_movementEnabled"), Is.False);
            }
            finally
            {
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(chomboomObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyProjectile_MovesAlongInitializedDirection()
        {
            GameObject projectileObject = CreateProjectile("Projectile", Vector3.zero);
            Component projectile = projectileObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyProjectile"));

            try
            {
                Vector2 direction = new Vector2(1f, -1f).normalized;
                Invoke(projectile, "Init", 1f, 10f, direction);
                Invoke(projectile, "Spawn");

                yield return null;

                Assert.That(projectileObject.transform.position.x, Is.GreaterThan(0f));
                Assert.That(projectileObject.transform.position.y, Is.LessThan(0f));
            }
            finally
            {
                UnityEngine.Object.Destroy(projectileObject);
            }
        }

        [UnityTest]
        public IEnumerator EnemyProjectile_DefaultInitStillMovesDown()
        {
            GameObject projectileObject = CreateProjectile("Projectile", Vector3.zero);
            Component projectile = projectileObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyProjectile"));

            try
            {
                Invoke(projectile, "Init", 1f, 10f);
                Invoke(projectile, "Spawn");

                yield return null;

                Assert.That(projectileObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(projectileObject.transform.position.y, Is.LessThan(0f));
            }
            finally
            {
                UnityEngine.Object.Destroy(projectileObject);
            }
        }

        [UnityTest]
        public IEnumerator BulletSpawner_SingleProjectile_SpawnsAtFirePointX()
        {
            GameObject spawnerObject = new GameObject("Spawner");
            Component spawner = spawnerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Combat.BulletSpawner"));
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(spawnerObject.transform, false);
            firePoint.transform.position = new Vector3(2f, 1f, 0f);
            GameObject bulletPrefabObject = CreatePlayerBulletPrefab("BulletPrefab");

            try
            {
                SetField(spawner, "bulletPrefab", bulletPrefabObject.GetComponent(RuntimeType(
                    "_Project.Scripts.Gameplay.Combat.Bullet")));
                SetField(spawner, "fireRate", 100f);
                SetField(spawner, "projectileCount", 1);
                Invoke(spawner, "SetFirePoint", firePoint.transform);

                HashSet<UnityEngine.Object> before = GetBulletSet();
                Invoke(spawner, "Shoot");
                yield return null;
                List<Component> spawned = GetNewBullets(before);

                Assert.That(spawned, Has.Count.EqualTo(1));
                Assert.That(spawned[0].transform.position.x, Is.EqualTo(2f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.Destroy(spawnerObject);
                UnityEngine.Object.Destroy(firePoint);
                UnityEngine.Object.Destroy(bulletPrefabObject);
            }
        }

        [UnityTest]
        public IEnumerator BulletSpawner_MultipleProjectiles_AreCenteredAroundFirePoint()
        {
            GameObject spawnerObject = new GameObject("Spawner");
            Component spawner = spawnerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Combat.BulletSpawner"));
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(spawnerObject.transform, false);
            firePoint.transform.position = Vector3.zero;
            GameObject bulletPrefabObject = CreatePlayerBulletPrefab("BulletPrefab");

            try
            {
                SetField(spawner, "bulletPrefab", bulletPrefabObject.GetComponent(RuntimeType(
                    "_Project.Scripts.Gameplay.Combat.Bullet")));
                SetField(spawner, "fireRate", 100f);
                SetField(spawner, "projectileCount", 3);
                SetField(spawner, "burstSpread", 0.4f);
                Invoke(spawner, "SetFirePoint", firePoint.transform);

                HashSet<UnityEngine.Object> before = GetBulletSet();
                Invoke(spawner, "Shoot");
                yield return null;
                List<Component> spawned = GetNewBullets(before);
                spawned.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));

                Assert.That(spawned, Has.Count.EqualTo(3));
                Assert.That(spawned[0].transform.position.x, Is.EqualTo(-0.4f).Within(0.001f));
                Assert.That(spawned[1].transform.position.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(spawned[2].transform.position.x, Is.EqualTo(0.4f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.Destroy(spawnerObject);
                UnityEngine.Object.Destroy(firePoint);
                UnityEngine.Object.Destroy(bulletPrefabObject);
            }
        }

        [UnityTest]
        public IEnumerator BulletSpawner_MissingFirePoint_LogsErrorAndDoesNotShoot()
        {
            GameObject spawnerObject = new GameObject("Spawner");
            Component spawner = spawnerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Combat.BulletSpawner"));
            GameObject bulletPrefabObject = CreatePlayerBulletPrefab("BulletPrefab");

            try
            {
                SetField(spawner, "bulletPrefab", bulletPrefabObject.GetComponent(RuntimeType(
                    "_Project.Scripts.Gameplay.Combat.Bullet")));
                SetField(spawner, "fireRate", 100f);

                HashSet<UnityEngine.Object> before = GetBulletSet();
                LogAssert.Expect(LogType.Error, new Regex("BulletSpawner requires a FirePoint reference"));
                Invoke(spawner, "Shoot");
                yield return null;

                Assert.That(GetNewBullets(before), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.Destroy(spawnerObject);
                UnityEngine.Object.Destroy(bulletPrefabObject);
            }
        }

        private static GameObject CreatePlayer(string name, Vector3 position, bool withLargeSprite = false)
        {
            GameObject playerObject = new GameObject(name);
            playerObject.transform.position = position;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.25f;

            if (withLargeSprite)
            {
                SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateOneUnitSprite();
            }

            playerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            return playerObject;
        }

        private static GameObject CreateEnemy(string name, Vector3 position, Vector2? colliderSize = null)
        {
            GameObject enemyObject = new GameObject(name);
            enemyObject.transform.position = position;

            Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;

            BoxCollider2D collider = enemyObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = colliderSize ?? new Vector2(0.5f, 0.5f);

            enemyObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));
            return enemyObject;
        }

        private static GameObject CreatePlayerBulletPrefab(string name)
        {
            GameObject bulletObject = new GameObject(name);
            bulletObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Combat.Bullet"));
            return bulletObject;
        }

        private static GameObject CreateProjectile(string name, Vector3 position)
        {
            GameObject projectileObject = new GameObject(name);
            projectileObject.transform.position = position;
            projectileObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyProjectile"));
            return projectileObject;
        }

#if UNITY_EDITOR
        private static GameObject LoadProductionEnemyPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProductionEnemyPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing production prefab at {ProductionEnemyPrefabPath}");
            return prefab;
        }
#endif

        private static Type RuntimeType(string fullName)
        {
            return Type.GetType($"{fullName}, Assembly-CSharp", throwOnError: true);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, arguments.Length);
            return method.Invoke(target, arguments);
        }

        private static object InvokeGeneric(
            object target,
            string methodName,
            Type genericType,
            params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, arguments.Length);
            return method.MakeGenericMethod(genericType).Invoke(target, arguments);
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

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
            return field.GetValue(target);
        }

        private static HashSet<UnityEngine.Object> GetBulletSet()
        {
            return new HashSet<UnityEngine.Object>(FindBulletObjects());
        }

        private static List<Component> GetNewBullets(HashSet<UnityEngine.Object> before)
        {
            var bullets = new List<Component>();
            UnityEngine.Object[] after = FindBulletObjects();
            for (int index = 0; index < after.Length; index++)
            {
                if (!before.Contains(after[index]) && after[index] is Component component)
                {
                    bullets.Add(component);
                }
            }

            return bullets;
        }

        private static UnityEngine.Object[] FindBulletObjects()
        {
            return UnityEngine.Object.FindObjectsByType(
                RuntimeType("_Project.Scripts.Gameplay.Combat.Bullet"),
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        private static Sprite CreateOneUnitSprite()
        {
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 16f, 16f),
                new Vector2(0.5f, 0.5f),
                16f);
        }
    }
}
