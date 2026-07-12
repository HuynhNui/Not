using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class EnemyPrefabAuthoringTests
    {
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy.prefab";
        private const float DamageTextVerticalOffset = 0.25f;

        [Test]
        public void EnemyPrefab_HasSingleEnabledTriggerCombatCollider()
        {
            GameObject prefab = LoadEnemyPrefab();

            Collider2D[] enabledColliders = prefab
                .GetComponentsInChildren<Collider2D>(true)
                .Where(collider => collider != null && collider.enabled)
                .ToArray();

            Assert.That(enabledColliders, Has.Length.EqualTo(1));
            Assert.That(enabledColliders[0], Is.TypeOf<BoxCollider2D>());
            Assert.That(enabledColliders[0].isTrigger, Is.True);
            Assert.That(enabledColliders[0].transform, Is.EqualTo(prefab.transform));
        }

        [Test]
        public void EnemyPrefab_ColliderCenterIsAlignedWithRenderer()
        {
            using PrefabInstance instance = InstantiateEnemyPrefab();
            Bounds rendererBounds = GetRendererBounds(instance.Root);
            Bounds colliderBounds = GetCollider(instance.Root).bounds;

            Assert.That(rendererBounds.Contains(colliderBounds.center), Is.True);
            Assert.That(
                Mathf.Abs(colliderBounds.center.x - rendererBounds.center.x),
                Is.LessThanOrEqualTo(rendererBounds.size.x * 0.2f));
            Assert.That(
                Mathf.Abs(colliderBounds.center.y - rendererBounds.center.y),
                Is.LessThanOrEqualTo(rendererBounds.size.y * 0.2f));
        }

        [Test]
        public void EnemyPrefab_ColliderSizeIsReasonableForRenderer()
        {
            using PrefabInstance instance = InstantiateEnemyPrefab();
            Bounds rendererBounds = GetRendererBounds(instance.Root);
            Bounds colliderBounds = GetCollider(instance.Root).bounds;

            float rendererArea = rendererBounds.size.x * rendererBounds.size.y;
            float colliderArea = colliderBounds.size.x * colliderBounds.size.y;
            float areaRatio = colliderArea / rendererArea;
            Bounds overlap = GetOverlap(rendererBounds, colliderBounds);
            float overlapArea = overlap.size.x * overlap.size.y;

            Assert.That(colliderBounds.size.x, Is.LessThanOrEqualTo(rendererBounds.size.x));
            Assert.That(colliderBounds.size.y, Is.LessThanOrEqualTo(rendererBounds.size.y));
            Assert.That(areaRatio, Is.GreaterThan(0.35f));
            Assert.That(areaRatio, Is.LessThan(0.85f));
            Assert.That(overlapArea / colliderArea, Is.GreaterThan(0.9f));
        }

        [Test]
        public void DamageTextAnchor_FromProductionEnemy_RemainsNearVisual()
        {
            using PrefabInstance instance = InstantiateEnemyPrefab();
            Bounds rendererBounds = GetRendererBounds(instance.Root);
            Bounds colliderBounds = GetCollider(instance.Root).bounds;
            Vector3 damageTextPosition = colliderBounds.center + Vector3.up * DamageTextVerticalOffset;

            Assert.That(damageTextPosition.x, Is.InRange(rendererBounds.min.x, rendererBounds.max.x));
            Assert.That(damageTextPosition.y, Is.LessThanOrEqualTo(rendererBounds.max.y + rendererBounds.size.y * 0.35f));
            Assert.That(
                Vector2.Distance(damageTextPosition, rendererBounds.center),
                Is.LessThanOrEqualTo(rendererBounds.size.y * 0.55f));
        }

        private static GameObject LoadEnemyPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing production prefab at {EnemyPrefabPath}");
            return prefab;
        }

        private static PrefabInstance InstantiateEnemyPrefab()
        {
            GameObject instance = Object.Instantiate(LoadEnemyPrefab(), Vector3.zero, Quaternion.identity);
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = false;
            }

            Physics2D.SyncTransforms();
            return new PrefabInstance(instance);
        }

        private static BoxCollider2D GetCollider(GameObject root)
        {
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
            Assert.That(collider, Is.Not.Null);
            return collider;
        }

        private static Bounds GetRendererBounds(GameObject root)
        {
            SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            Assert.That(renderer, Is.Not.Null);
            return renderer.bounds;
        }

        private static Bounds GetOverlap(Bounds first, Bounds second)
        {
            Vector3 min = Vector3.Max(first.min, second.min);
            Vector3 max = Vector3.Min(first.max, second.max);
            Vector3 size = new Vector3(
                Mathf.Max(0f, max.x - min.x),
                Mathf.Max(0f, max.y - min.y),
                Mathf.Max(0f, max.z - min.z));
            return new Bounds((min + max) * 0.5f, size);
        }

        private readonly struct PrefabInstance : System.IDisposable
        {
            public PrefabInstance(GameObject root)
            {
                Root = root;
            }

            public GameObject Root { get; }

            public void Dispose()
            {
                if (Root != null)
                {
                    Object.DestroyImmediate(Root);
                }
            }
        }
    }
}
