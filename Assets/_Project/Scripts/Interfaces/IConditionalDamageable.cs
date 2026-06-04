using UnityEngine;

namespace _Project.Scripts.Interfaces
{
    /// <summary>
    /// Lets a damageable target reject hits without changing the projectile collision flow.
    /// </summary>
    public interface IConditionalDamageable
    {
        bool CanReceiveDamageFrom(GameObject damageSource);
    }
}
