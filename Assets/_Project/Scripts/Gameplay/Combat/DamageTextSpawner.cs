using System.Collections.Generic;
using System.Globalization;
using _Project.Scripts.Gameplay.Enemies;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Combat
{
    /// <summary>
    /// Runtime pool for lightweight world-space damage number popups.
    /// </summary>
    public sealed class DamageTextSpawner : MonoBehaviour
    {
        private const string DamageTextPrefsKey = "Settings.DamageText";

        private static DamageTextSpawner _instance;

        [SerializeField] private int poolSize = 48;
        [SerializeField] private Color normalDamageColor = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private TMP_FontAsset damageFont;
        [SerializeField] private float outlineWidth = 0.25f;
        [SerializeField] private float randomOffsetX = 0.12f;
        [SerializeField] private float randomOffsetY = 0.08f;
        [SerializeField] private int sortingOrderOffset = 60;

        private readonly List<DamageTextView> _pool = new List<DamageTextView>();
        private int _nextReuseIndex;

        public static void ShowDamage(float damage, Collider2D hitCollider, EnemyController enemy)
        {
            if (damage <= 0f || enemy == null || !IsDamageTextEnabled())
            {
                return;
            }

            DamageTextSpawner spawner = GetOrCreateInstance();
            if (spawner == null)
            {
                return;
            }

            spawner.ShowInternal(damage, hitCollider, enemy);
        }

        private static DamageTextSpawner GetOrCreateInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindAnyObjectByType<DamageTextSpawner>();
            if (_instance != null)
            {
                return _instance;
            }

            GameObject spawnerObject = new GameObject("DamageTextSpawner");
            _instance = spawnerObject.AddComponent<DamageTextSpawner>();
            return _instance;
        }

        private static bool IsDamageTextEnabled()
        {
            return !PlayerPrefs.HasKey(DamageTextPrefsKey)
                || PlayerPrefs.GetInt(DamageTextPrefsKey, 1) != 0;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void ShowInternal(float damage, Collider2D hitCollider, EnemyController enemy)
        {
            DamageTextView view = GetView();
            if (view == null)
            {
                return;
            }

            Vector3 position = ResolveSpawnPosition(hitCollider, enemy);
            position += new Vector3(
                Random.Range(-randomOffsetX, randomOffsetX),
                Random.Range(0f, randomOffsetY),
                0f);

            SpriteRenderer referenceRenderer = enemy.GetComponentInChildren<SpriteRenderer>();
            int sortingLayerId = referenceRenderer != null ? referenceRenderer.sortingLayerID : 0;
            int sortingOrder = referenceRenderer != null
                ? referenceRenderer.sortingOrder + sortingOrderOffset
                : sortingOrderOffset;

            view.Show(
                FormatDamage(damage),
                position,
                normalDamageColor,
                sortingLayerId,
                sortingOrder);
        }

        private DamageTextView GetView()
        {
            for (int index = 0; index < _pool.Count; index++)
            {
                DamageTextView view = _pool[index];
                if (view != null && !view.IsPlaying)
                {
                    view.ConfigureStyle(damageFont, outlineWidth);
                    return view;
                }
            }

            int maxPoolSize = Mathf.Max(1, poolSize);
            if (_pool.Count < maxPoolSize)
            {
                DamageTextView createdView = CreateView();
                _pool.Add(createdView);
                return createdView;
            }

            _nextReuseIndex = (_nextReuseIndex + 1) % _pool.Count;
            DamageTextView reusedView = _pool[_nextReuseIndex];
            reusedView.Stop();
            return reusedView;
        }

        private DamageTextView CreateView()
        {
            GameObject viewObject = new GameObject("DamageText");
            viewObject.transform.SetParent(transform, false);
            DamageTextView view = viewObject.AddComponent<DamageTextView>();
            view.ConfigureStyle(damageFont, outlineWidth);
            viewObject.SetActive(false);
            return view;
        }

        private static Vector3 ResolveSpawnPosition(Collider2D hitCollider, EnemyController enemy)
        {
            if (hitCollider != null)
            {
                Vector3 boundsCenter = hitCollider.bounds.center;
                return new Vector3(boundsCenter.x, boundsCenter.y, enemy.transform.position.z);
            }

            Collider2D enemyCollider = enemy.GetComponentInChildren<Collider2D>();
            if (enemyCollider != null)
            {
                Vector3 boundsCenter = enemyCollider.bounds.center;
                return new Vector3(boundsCenter.x, boundsCenter.y, enemy.transform.position.z);
            }

            return enemy.transform.position;
        }

        private static string FormatDamage(float damage)
        {
            float rounded = Mathf.Round(damage);
            if (Mathf.Abs(damage - rounded) <= 0.01f)
            {
                return Mathf.Max(0, Mathf.RoundToInt(damage)).ToString(CultureInfo.InvariantCulture);
            }

            return damage.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
