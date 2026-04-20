namespace _Project.Scripts.Interfaces
{
    /// <summary>
    /// Defines spawn and despawn hooks for pooled runtime objects.
    /// </summary>
    public interface IPoolable
    {
        void Spawn();
        void Despawn();
    }
}
