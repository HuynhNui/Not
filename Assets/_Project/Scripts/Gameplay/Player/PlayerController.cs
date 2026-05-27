using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Gates;
using _Project.Scripts.Gameplay.Combat;
using UnityEngine;

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
        [SerializeField] private float followerSpacing = 0.34f;
        [SerializeField] private int followerColumns = 5;
        [SerializeField] private bool autoFire = true;
        private bool _controlsEnabled = true;

        public MainPlayerUnit MainPlayerUnit => mainPlayerUnit;
        public PlayerMovement PlayerMovement => playerMovement;
        public IReadOnlyList<FollowerUnit> Followers => followers;
        public int CurrentSquadCount => mainPlayerUnit == null ? 0 : 1 + GetActiveFollowerCount();
        public int MaxSquadCount => Mathf.Max(1, maxSquadCount);

        private void Awake()
        {
            if (playerMovement != null)
            {
                playerMovement.Init();
            }

            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Initialize();
            }

            for (int index = 0; index < followers.Count; index++)
            {
                if (followers[index] == null)
                {
                    continue;
                }

                followers[index].Initialize();
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
            mainPlayerUnit = unit;

            if (mainPlayerUnit != null)
            {
                mainPlayerUnit.Initialize();
            }
        }

        public void AddFollower(FollowerUnit follower)
        {
            if (follower == null || followers.Contains(follower))
            {
                return;
            }

            followers.Add(follower);
            ConfigureFollower(follower, followers.Count - 1);
            follower.Initialize();
        }

        public void RemoveFollower(FollowerUnit follower)
        {
            if (follower == null)
            {
                return;
            }

            followers.Remove(follower);
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

                if (follower == null)
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

        public void ApplyGateEffect(GateConfig config)
        {
            if (config == null || mainPlayerUnit == null)
            {
                return;
            }

            GateEffectApplier.Apply(config, mainPlayerUnit, this);
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

            CopyMainVisual(followerObject);

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

        private void ConfigureFollower(FollowerUnit follower, int followerIndex)
        {
            if (follower == null || mainPlayerUnit == null)
            {
                return;
            }

            follower.transform.SetParent(transform, true);
            follower.SetFollowTarget(mainPlayerUnit);
            follower.SetFollowOffset(GetFormationOffset(followerIndex));
            follower.SetDamage(mainPlayerUnit.Damage);
            follower.SetFireRate(mainPlayerUnit.FireRate);

            if (mainPlayerUnit.BulletSpawner != null && follower.BulletSpawner != null)
            {
                follower.BulletSpawner.SetProjectileCount(mainPlayerUnit.BulletSpawner.ProjectileCount);
            }
        }

        private void RefreshFollowerFormation()
        {
            RemoveNullFollowers();

            for (int index = 0; index < followers.Count; index++)
            {
                ConfigureFollower(followers[index], index);
            }
        }

        private Vector3 GetFollowerSpawnPosition(int followerIndex)
        {
            if (mainPlayerUnit == null)
            {
                return transform.position;
            }

            return mainPlayerUnit.transform.position + GetFormationOffset(followerIndex);
        }

        private Vector3 GetFormationOffset(int followerIndex)
        {
            int columns = Mathf.Max(1, followerColumns);
            int row = followerIndex / columns + 1;
            int column = followerIndex % columns;
            int activeFollowerCount = Mathf.Max(1, GetActiveFollowerCount());
            int rowStart = (row - 1) * columns;
            int rowCount = Mathf.Clamp(activeFollowerCount - rowStart, 1, columns);
            float centeredColumn = column - (rowCount - 1) * 0.5f;
            return new Vector3(centeredColumn * Mathf.Max(0.01f, followerSpacing), -row * Mathf.Max(0.01f, followerSpacing), 0f);
        }

        private int GetActiveFollowerCount()
        {
            int count = 0;
            for (int index = 0; index < followers.Count; index++)
            {
                if (followers[index] != null)
                {
                    count++;
                }
            }

            return count;
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
    }
}
