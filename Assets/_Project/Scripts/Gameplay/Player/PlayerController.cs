using System.Collections.Generic;
using System;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Combat;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Coordinates the player squad.
    /// Input and movement stay on dedicated components, while firing is triggered here for the whole squad.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private MainPlayerUnit mainPlayerUnit;
        [SerializeField] private FollowerUnit followerPrefab;
        [SerializeField] private List<FollowerUnit> followers = new List<FollowerUnit>();
        [SerializeField] private int maxSquadCount = 50;
        [FormerlySerializedAs("followerSpacing")]
        [FormerlySerializedAs("trailSpacing")]
        [SerializeField] private float formationSpacing = 1.15f;
        [SerializeField] private float ringRadiusStep = 0.75f;
        [SerializeField, Range(30f, 180f)] private float rearArcDegrees = 140f;
        [SerializeField] private float hurtboxRadius = 0.1f;
        [SerializeField] private bool autoFire = true;
        private bool _controlsEnabled = true;

        public event Action<PlayerController> SquadDefeated;

        public MainPlayerUnit MainPlayerUnit => mainPlayerUnit;
        public PlayerMovement PlayerMovement => playerMovement;
        public IReadOnlyList<FollowerUnit> Followers => followers;
        public int CurrentSquadCount => GetAliveMainCount() + GetActiveFollowerCount();
        public int MaxSquadCount => Mathf.Max(1, maxSquadCount);

        private void Awake()
        {
            ConfigureSquadUnitPhysics(mainPlayerUnit != null ? mainPlayerUnit.gameObject : null);

            if (playerMovement != null)
            {
                playerMovement.Init();
            }

            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Initialize();
                SubscribeToUnit(mainPlayerUnit);
            }

            for (int index = 0; index < followers.Count; index++)
            {
                if (followers[index] == null)
                {
                    continue;
                }

                followers[index].Initialize();
                ConfigureSquadUnitPhysics(followers[index].gameObject);
                SubscribeToUnit(followers[index]);
            }

            RefreshFollowerFormation();
        }

        private void Update()
        {
            if (!_controlsEnabled || !autoFire)
            {
                return;
            }

            ShootSquad();
        }

        public void SetMainPlayerUnit(MainPlayerUnit unit)
        {
            UnsubscribeFromUnit(mainPlayerUnit);
            mainPlayerUnit = unit;

            if (mainPlayerUnit != null)
            {
                ConfigureSquadUnitPhysics(mainPlayerUnit.gameObject);
                mainPlayerUnit.Initialize();
                SubscribeToUnit(mainPlayerUnit);
            }

            RefreshFollowerFormation(snapFollowers: true);
        }

        public void AddFollower(FollowerUnit follower)
        {
            if (follower == null || followers.Contains(follower))
            {
                return;
            }

            followers.Add(follower);
            follower.Initialize();
            ConfigureSquadUnitPhysics(follower.gameObject);
            ConfigureFollower(follower, followers.Count - 1, restoreFullHealth: true);
            SubscribeToUnit(follower);
            RefreshFollowerFormation();
        }

        public void RemoveFollower(FollowerUnit follower)
        {
            if (follower == null)
            {
                return;
            }

            UnsubscribeFromUnit(follower);
            followers.Remove(follower);
            RefreshFollowerFormation();
        }

        public void SetSquadCount(int targetCount)
        {
            if (mainPlayerUnit == null)
            {
                return;
            }

            RemoveNullFollowers();

            int clampedTarget = Mathf.Clamp(targetCount, 1, MaxSquadCount);
            while (CurrentSquadCount < clampedTarget)
            {
                FollowerUnit follower = CreateFollower(followers.Count);
                if (follower == null)
                {
                    break;
                }

                AddFollower(follower);
            }

            while (CurrentSquadCount > clampedTarget && followers.Count > 0)
            {
                FollowerUnit follower = followers[followers.Count - 1];
                followers.RemoveAt(followers.Count - 1);

                if (follower != null)
                {
                    UnsubscribeFromUnit(follower);
                    Destroy(follower.gameObject);
                }
            }

            RefreshFollowerFormation();
        }

        public void ShootSquad()
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (mainPlayerUnit != null && !mainPlayerUnit.IsDead)
            {
                mainPlayerUnit.Shoot();
            }

            for (int index = 0; index < followers.Count; index++)
            {
                FollowerUnit follower = followers[index];

                if (follower == null || follower.IsDead)
                {
                    continue;
                }

                follower.Shoot();
            }
        }

        public void SetControlsEnabled(bool isEnabled)
        {
            _controlsEnabled = isEnabled;

            if (playerMovement != null)
            {
                playerMovement.SetInputEnabled(isEnabled);
            }
        }

        public void ResetRunPosition()
        {
            if (playerMovement != null)
            {
                playerMovement.SnapToRunStartViewport(mainPlayerUnit != null ? mainPlayerUnit.transform : transform);
            }

            RefreshFollowerFormation(snapFollowers: true);
        }

        public void ApplyGateEffect(GateConfig config)
        {
            if (config == null || mainPlayerUnit == null)
            {
                return;
            }

            GateEffectApplier.Apply(config, mainPlayerUnit, this);
        }

        public void SyncFollowersFromMain(
            bool syncDamage,
            bool syncFireRate,
            bool syncMaxHp,
            bool healMaxHpByDelta,
            bool syncProjectileCount)
        {
            if (mainPlayerUnit == null)
            {
                return;
            }

            int projectileCount = mainPlayerUnit.BulletSpawner != null
                ? mainPlayerUnit.BulletSpawner.ProjectileCount
                : 1;

            for (int index = 0; index < followers.Count; index++)
            {
                FollowerUnit follower = followers[index];
                if (follower == null || follower.IsDead)
                {
                    continue;
                }

                if (syncDamage)
                {
                    follower.SetDamage(mainPlayerUnit.Damage);
                    follower.BulletSpawner?.SetVisualTierDamage(mainPlayerUnit.Damage);
                }

                if (syncFireRate)
                {
                    follower.SetFireRate(mainPlayerUnit.FireRate);
                }

                if (syncMaxHp)
                {
                    follower.SetMaxHp(mainPlayerUnit.MaxHp, healByDelta: healMaxHpByDelta);
                }

                if (syncProjectileCount && follower.BulletSpawner != null)
                {
                    follower.BulletSpawner.SetProjectileCount(projectileCount);
                }
            }
        }

        private FollowerUnit CreateFollower(int followerIndex)
        {
            Vector3 spawnPosition = GetFollowerSpawnPosition(followerIndex);
            if (followerPrefab != null)
            {
                return Instantiate(followerPrefab, spawnPosition, Quaternion.identity, transform);
            }

            return CreateRuntimeFollower(spawnPosition);
        }

        private FollowerUnit CreateRuntimeFollower(Vector3 spawnPosition)
        {
            GameObject followerObject = new GameObject($"Follower_{followers.Count + 1}");
            followerObject.transform.SetParent(transform, true);
            followerObject.transform.position = spawnPosition;
            followerObject.layer = mainPlayerUnit != null ? mainPlayerUnit.gameObject.layer : followerObject.layer;

            CopyMainVisual(followerObject);
            ConfigureSquadUnitPhysics(followerObject);

            Transform firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(followerObject.transform, false);
            firePoint.localPosition = Vector3.zero;
            firePoint.localRotation = Quaternion.identity;
            firePoint.localScale = Vector3.one;

            BulletSpawner followerSpawner = followerObject.AddComponent<BulletSpawner>();
            if (mainPlayerUnit != null && mainPlayerUnit.BulletSpawner != null)
            {
                followerSpawner.ConfigureFromTemplate(mainPlayerUnit.BulletSpawner);
            }

            followerSpawner.SetFirePoint(firePoint);
            FollowerUnit follower = followerObject.AddComponent<FollowerUnit>();
            return follower;
        }

        private void CopyMainVisual(GameObject followerObject)
        {
            if (mainPlayerUnit == null)
            {
                return;
            }

            Transform mainTransform = mainPlayerUnit.transform;
            followerObject.transform.localScale = mainTransform.localScale;

            SpriteRenderer mainRenderer = mainPlayerUnit.GetComponent<SpriteRenderer>();
            if (mainRenderer == null)
            {
                return;
            }

            SpriteRenderer followerRenderer = followerObject.AddComponent<SpriteRenderer>();
            followerRenderer.sprite = mainRenderer.sprite;
            followerRenderer.color = mainRenderer.color;
            followerRenderer.flipX = mainRenderer.flipX;
            followerRenderer.flipY = mainRenderer.flipY;
            followerRenderer.drawMode = mainRenderer.drawMode;
            followerRenderer.size = mainRenderer.size;
            followerRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            followerRenderer.sortingOrder = mainRenderer.sortingOrder - 1;

            if (mainRenderer.sharedMaterial != null)
            {
                followerRenderer.sharedMaterial = mainRenderer.sharedMaterial;
            }
        }

        private void ConfigureSquadUnitPhysics(GameObject unitObject)
        {
            if (unitObject == null)
            {
                return;
            }

            BoxCollider2D[] boxColliders = unitObject.GetComponents<BoxCollider2D>();
            for (int index = 0; index < boxColliders.Length; index++)
            {
                if (boxColliders[index] != null)
                {
                    Destroy(boxColliders[index]);
                }
            }

            CircleCollider2D hurtbox = unitObject.GetComponent<CircleCollider2D>();
            if (hurtbox == null)
            {
                hurtbox = unitObject.AddComponent<CircleCollider2D>();
            }

            hurtbox.enabled = true;
            hurtbox.isTrigger = true;
            hurtbox.offset = Vector2.zero;
            hurtbox.radius = Mathf.Max(0.01f, hurtboxRadius);

            Rigidbody2D body = unitObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = unitObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.angularDamping = 0.05f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            body.interpolation = RigidbodyInterpolation2D.None;
        }

        private void ConfigureFollower(FollowerUnit follower, int followerIndex, bool restoreFullHealth = false)
        {
            if (follower == null || mainPlayerUnit == null)
            {
                return;
            }

            follower.transform.SetParent(transform, true);
            follower.SetFollowTarget(mainPlayerUnit);
            follower.SetFollowOffset(GetRearArcOffset(followerIndex));
            follower.ConfigureRuntimeFrom(mainPlayerUnit, restoreFullHealth);

            if (mainPlayerUnit.BulletSpawner != null && follower.BulletSpawner != null)
            {
                follower.BulletSpawner.SetVisualTierDamage(mainPlayerUnit.Damage);
                follower.BulletSpawner.SetProjectileCount(mainPlayerUnit.BulletSpawner.ProjectileCount);
            }
        }

        private void RefreshFollowerFormation(bool snapFollowers = false)
        {
            RemoveNullFollowers();

            for (int index = 0; index < followers.Count; index++)
            {
                FollowerUnit follower = followers[index];
                ConfigureFollower(follower, index);

                if (!snapFollowers || follower == null)
                {
                    continue;
                }

                if (mainPlayerUnit != null)
                {
                    follower.transform.position = mainPlayerUnit.transform.position + GetRearArcOffset(index);
                }
            }
        }

        private Vector3 GetFollowerSpawnPosition(int followerIndex)
        {
            if (mainPlayerUnit == null)
            {
                return transform.position;
            }

            return mainPlayerUnit.transform.position + GetRearArcOffset(followerIndex);
        }

        private Vector3 GetRearArcOffset(int followerIndex)
        {
            int remainingIndex = Mathf.Max(0, followerIndex);
            int ringIndex = 0;
            int slotsInRing = GetRingSlotCount(ringIndex);

            while (remainingIndex >= slotsInRing)
            {
                remainingIndex -= slotsInRing;
                ringIndex++;
                slotsInRing = GetRingSlotCount(ringIndex);
            }

            float radius = GetRingRadius(ringIndex);
            float angleDegrees = GetSymmetricRingAngle(remainingIndex, slotsInRing);
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Sin(angleRadians) * radius,
                -Mathf.Cos(angleRadians) * radius,
                0f);
        }

        private int GetRingSlotCount(int ringIndex)
        {
            float radius = GetRingRadius(ringIndex);
            float arcRadians = Mathf.Clamp(rearArcDegrees, 30f, 180f) * Mathf.Deg2Rad;
            float spacing = Mathf.Max(0.01f, formationSpacing);
            return Mathf.Max(1, Mathf.FloorToInt(radius * arcRadians / spacing) + 1);
        }

        private float GetRingRadius(int ringIndex)
        {
            return Mathf.Max(0.01f, formationSpacing) + Mathf.Max(0f, ringRadiusStep) * Mathf.Max(0, ringIndex);
        }

        private float GetSymmetricRingAngle(int slotIndex, int slotsInRing)
        {
            if (slotIndex <= 0 || slotsInRing <= 1)
            {
                return 0f;
            }

            float halfArc = Mathf.Clamp(rearArcDegrees, 30f, 180f) * 0.5f;
            int pairs = Mathf.Max(1, slotsInRing / 2);
            int pairIndex = (slotIndex + 1) / 2;
            float side = slotIndex % 2 == 1 ? -1f : 1f;
            float angleStep = halfArc / pairs;
            return side * pairIndex * angleStep;
        }

        private int GetActiveFollowerCount()
        {
            int count = 0;
            for (int index = 0; index < followers.Count; index++)
            {
                if (followers[index] != null && !followers[index].IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        private int GetAliveMainCount()
        {
            return mainPlayerUnit != null && !mainPlayerUnit.IsDead ? 1 : 0;
        }

        private void RemoveNullFollowers()
        {
            for (int index = followers.Count - 1; index >= 0; index--)
            {
                if (followers[index] == null)
                {
                    followers.RemoveAt(index);
                }
            }
        }

        private void SubscribeToUnit(PlayerUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.Died -= HandleUnitDied;
            unit.Died += HandleUnitDied;
        }

        private void UnsubscribeFromUnit(PlayerUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.Died -= HandleUnitDied;
        }

        private void HandleUnitDied(PlayerUnit deadUnit)
        {
            if (deadUnit == null)
            {
                return;
            }

            if (deadUnit == mainPlayerUnit)
            {
                HandleMainUnitDied();
                return;
            }

            FollowerUnit deadFollower = deadUnit as FollowerUnit;
            if (deadFollower != null)
            {
                RemoveDeadFollower(deadFollower);
            }

            if (CurrentSquadCount <= 0)
            {
                SquadDefeated?.Invoke(this);
            }
        }

        private void HandleMainUnitDied()
        {
            FollowerUnit promotedFollower = GetHighestHealthFollower();

            if (promotedFollower == null)
            {
                SquadDefeated?.Invoke(this);
                return;
            }

            mainPlayerUnit.ReviveWithStateFrom(promotedFollower);
            UnsubscribeFromUnit(promotedFollower);
            followers.Remove(promotedFollower);
            Destroy(promotedFollower.gameObject);
            RefreshFollowerFormation(snapFollowers: true);
        }

        private FollowerUnit GetHighestHealthFollower()
        {
            FollowerUnit bestFollower = null;
            float bestHealth = float.NegativeInfinity;

            for (int index = 0; index < followers.Count; index++)
            {
                FollowerUnit follower = followers[index];

                if (follower == null || follower.IsDead)
                {
                    continue;
                }

                if (follower.CurrentHp <= bestHealth)
                {
                    continue;
                }

                bestHealth = follower.CurrentHp;
                bestFollower = follower;
            }

            return bestFollower;
        }

        private void RemoveDeadFollower(FollowerUnit deadFollower)
        {
            UnsubscribeFromUnit(deadFollower);
            followers.Remove(deadFollower);

            if (deadFollower != null)
            {
                Destroy(deadFollower.gameObject);
            }

            RefreshFollowerFormation();
        }
    }
}
