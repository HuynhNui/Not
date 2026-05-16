using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using _Project.Scripts.Gameplay.Gates;
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
        [SerializeField] private List<FollowerUnit> followers = new List<FollowerUnit>();
        [SerializeField] private bool autoFire = true;
        private bool _controlsEnabled = true;

        public MainPlayerUnit MainPlayerUnit => mainPlayerUnit;
        public IReadOnlyList<FollowerUnit> Followers => followers;

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
    }
}
