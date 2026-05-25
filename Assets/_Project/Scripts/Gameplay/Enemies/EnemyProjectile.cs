using _Project.Scripts.Gameplay.Player;
using _Project.Scripts.Interfaces;
using _Project.Scripts.Systems.PoolSystem;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    /// <summary>
    /// Enemy-owned projectile that only damages the player squad's main unit.
    /// </summary>
    public sealed class EnemyProjectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField] private float animationFps = 12f;

        private PoolSystem _poolSystem;
        private float _remainingLifetime;
        private float _animationTimer;
        private int _frameIndex;
        private bool _isActive;

        public void Init(float projectileDamage, float projectileSpeed)
        {
            damage = Mathf.Max(0f, projectileDamage);
            speed = Mathf.Max(0f, projectileSpeed);
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            transform.position += Vector3.down * (speed * Time.deltaTime);
            _remainingLifetime -= Time.deltaTime;
            UpdateAnimation();

            if (_remainingLifetime <= 0f)
            {
                Despawn();
            }
        }

        public void Spawn()
        {
            _remainingLifetime = Mathf.Max(0.01f, lifetime);
            _animationTimer = 0f;
            _frameIndex = 0;
            _isActive = true;
            ApplyFrame();
        }

        public void Despawn()
        {
            _isActive = false;

            if (_poolSystem != null)
            {
                _poolSystem.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        public void SetPoolSystem(PoolSystem poolSystem)
        {
            _poolSystem = poolSystem;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamagePlayer(collision.collider);
        }

        private void TryDamagePlayer(Collider2D other)
        {
            if (!_isActive || other == null)
            {
                return;
            }

            MainPlayerUnit player = other.GetComponent<MainPlayerUnit>();

            if (player == null)
            {
                player = other.GetComponentInParent<MainPlayerUnit>();
            }

            if (player == null || player.IsDead)
            {
                return;
            }

            player.TakeDamage(damage);
            Despawn();
        }

        private void UpdateAnimation()
        {
            if (animationFrames == null || animationFrames.Length <= 1 || animationFps <= 0f)
            {
                return;
            }

            _animationTimer += Time.deltaTime;
            float frameDuration = 1f / animationFps;

            while (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _frameIndex = (_frameIndex + 1) % animationFrames.Length;
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null || animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            _frameIndex = Mathf.Clamp(_frameIndex, 0, animationFrames.Length - 1);
            spriteRenderer.sprite = animationFrames[_frameIndex];
        }
    }
}
