namespace _Project.Scripts.Interfaces
{
    /// <summary>
    /// Minimal damage contract used by projectiles without coupling to a concrete target type.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damageAmount);
    }
}
