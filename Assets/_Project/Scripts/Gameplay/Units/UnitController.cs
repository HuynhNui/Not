//UnitController.cs
using _Project.Scripts.Data.ScriptableObjects.UnitData;
using _Project.Scripts.Gameplay.Combat;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Units
{
    /// <summary>
    /// Defines the shared composition root for units that can move, attack, and take damage.
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        [SerializeField] protected UnitData unitData;
        [SerializeField] protected UnitMovement unitMovement;
        [SerializeField] protected WeaponController weaponController;

        public virtual void Init()
        {
        }

        protected virtual void Update()
        {
        }

        public virtual void Fire()
        {
        }

        public virtual void TakeDamage(float damageAmount)
        {
        }
    }
}
