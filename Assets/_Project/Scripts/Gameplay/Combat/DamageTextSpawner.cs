using _Project.Scripts.Gameplay.Enemies;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Gameplay.Combat
{
    public sealed class DamageTextSpawner : MonoBehaviour
    {
        private const string DamageTextPrefsKey = "Settings.DamageText";
        private const string UpheavalFontAssetPath = "Assets/Front/Upheaval_TMP.asset";

        private static DamageTextSpawner _instance;

        [SerializeField] private TMP_FontAsset damageFont;
        [SerializeField] private int poolSize = 48;
        [SerializeField] private Color damageColor = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private Vector2 randomOffset = new Vector2(0.12f, 0.08f);
        [SerializeField] private float verticalOffset = 0.25f;
        [SerializeField] private int sortingOrder = 60;

        private readonly Queue<DamageTextView> _available = new Queue<DamageTextView>();
        private readonly List<DamageTextView> _active = new List<DamageTextView>();

        public static void ShowDamage(float damage, Collider2D hitCollider, EnemyController enemy)
        {
            if (damage <= 0f || hitCollider == null || enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                return;
            }

            if (PlayerPrefs.GetInt(DamageTextPrefsKey, 1) == 0)
            {
                return;
            }

            Instance.Show(damage, hitCollider);
        }

        private static DamageTextSpawner Instance
        {
            get
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
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolveFontIfNeeded();
            WarmPool();
        }

        private void Show(float damage, Collider2D hitCollider)
        {
            ResolveFontIfNeeded();
            WarmPool();

            DamageTextView view = GetView();
            Vector3 position = hitCollider.bounds.center + Vector3.up * verticalOffset;
            position += new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                Random.Range(0f, randomOffset.y),
                0f);

            view.Play(Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(), position, damageFont, damageColor, sortingOrder, Release);
        }

        private DamageTextView GetView()
        {
            if (_available.Count > 0)
            {
                DamageTextView pooled = _available.Dequeue();
                _active.Add(pooled);
                return pooled;
            }

            if (_active.Count >= Mathf.Max(1, poolSize))
            {
                DamageTextView oldest = _active[0];
                _active.RemoveAt(0);
                _active.Add(oldest);
                return oldest;
            }

            DamageTextView created = CreateView();
            _active.Add(created);
            return created;
        }

        private void Release(DamageTextView view)
        {
            if (view == null)
            {
                return;
            }

            _active.Remove(view);
            _available.Enqueue(view);
        }

        private void WarmPool()
        {
            int targetCount = Mathf.Max(1, poolSize);
            while (_available.Count + _active.Count < targetCount)
            {
                DamageTextView view = CreateView();
                view.gameObject.SetActive(false);
                _available.Enqueue(view);
            }
        }

        private DamageTextView CreateView()
        {
            GameObject viewObject = new GameObject("DamageText");
            viewObject.transform.SetParent(transform, false);
            return viewObject.AddComponent<DamageTextView>();
        }

        private void ResolveFontIfNeeded()
        {
            if (damageFont != null)
            {
                return;
            }

            TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (int index = 0; index < loadedFonts.Length; index++)
            {
                TMP_FontAsset candidate = loadedFonts[index];
                if (candidate != null && candidate.name.Contains("Upheaval"))
                {
                    damageFont = candidate;
                    return;
                }
            }

#if UNITY_EDITOR
            damageFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UpheavalFontAssetPath);
#endif
        }
    }
}
