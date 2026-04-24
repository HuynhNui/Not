using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Runtime modifier hook contract for projectiles.
    /// </summary>
    public interface IBulletModifier
    {
        void OnInit(Bullet bullet);
        void OnUpdate(Bullet bullet);
        void OnHit(Bullet bullet, Collider target);
    }
}
