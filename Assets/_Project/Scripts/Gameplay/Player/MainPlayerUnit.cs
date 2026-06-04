using _Project.Scripts.Data.ScriptableObjects.PlayerConfigs;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Player
{
    /// <summary>
    /// Main squad unit with health and death handling.
    /// Combat behavior remains inherited from PlayerUnit.
    /// </summary>
    public sealed class MainPlayerUnit : PlayerUnit
    {
        [SerializeField] private PlayerUnitConfig mainUnitConfig;

        protected override void ApplyUnitConfig()
        {
            if (mainUnitConfig == null)
            {
                base.ApplyUnitConfig();
                return;
            }

            SetMaxHp(mainUnitConfig.MaxHealth);
            SetDamage(mainUnitConfig.Damage);
            SetFireRate(mainUnitConfig.FireRate);
        }
    }
}
