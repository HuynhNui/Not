namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Keeps a bullet alive for a configurable number of successful hits.
    /// </summary>
    public sealed class PierceModifier : IBulletModifier
    {
        private int _remainingPierces;

        public PierceModifier(int pierceCount)
        {
            _remainingPierces = pierceCount;
        }

        public void OnInit(Bullet bullet)
        {
        }

        public void OnUpdate(Bullet bullet)
        {
        }

        public void OnHit(Bullet bullet, UnityEngine.Collider2D target)
        {
            if (_remainingPierces <= 0)
            {
                return;
            }

            _remainingPierces--;
            bullet.PreserveAfterHit();
        }
    }
}
