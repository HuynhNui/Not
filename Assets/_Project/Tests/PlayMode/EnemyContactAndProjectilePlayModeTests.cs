using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TrueGate.PlayModeTests
{
    public sealed class EnemyContactAndProjectilePlayModeTests
    {
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

        private static GameObject CreatePlayer(string name, Vector3 position)
        {
            GameObject playerObject = new GameObject(name);
            playerObject.transform.position = position;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.25f;

            playerObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            return playerObject;
        }

        private static GameObject CreateEnemy(string name, Vector3 position)
        {
            GameObject enemyObject = new GameObject(name);
            enemyObject.transform.position = position;

            Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;

            BoxCollider2D collider = enemyObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.5f, 0.5f);

            enemyObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyController"));
            return enemyObject;
        }

        private static GameObject CreateProjectile(string name, Vector3 position)
        {
            GameObject projectileObject = new GameObject(name);
            projectileObject.transform.position = position;
            projectileObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.EnemyProjectile"));
            return projectileObject;
        }

        private static Type RuntimeType(string fullName)
        {
            return Type.GetType($"{fullName}, Assembly-CSharp", throwOnError: true);
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
    }
}
