using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay
{
    public sealed class ScrollingGameplayBackground : MonoBehaviour
    {
        private const float MinimumPortraitAspect = 9f / 16f;
        private const float WidthPadding = 1.02f;
        private const float TileOverlap = 0.01f;

        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private float scrollSpeed = 1.5f;
        [SerializeField, Min(1)] private int tileCount = 3;
        [SerializeField] private int sortingOrder = -100;
        [SerializeField] private bool fitToCameraWidth = true;
        [SerializeField] private Camera targetCamera;

        private readonly List<SpriteRenderer> _tiles = new List<SpriteRenderer>();
        private float _tileHeight;
        private float _lastCameraHalfWidth;
        private float _lastCameraHalfHeight;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            tileCount = Mathf.Max(1, tileCount);
            scrollSpeed = Mathf.Max(0f, scrollSpeed);
        }

        private void LateUpdate()
        {
            if (backgroundSprite == null)
            {
                return;
            }

            if (_tiles.Count == 0 || HasCameraSizeChanged())
            {
                Initialize();
            }

            ScrollTiles(Time.deltaTime);
            RecycleTilesBelowCamera();
        }

        public void Initialize()
        {
            ResolveCamera();
            CacheTiles();
            ConfigureTiles();
            LayoutTiles();
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void CacheTiles()
        {
            _tiles.Clear();

            foreach (Transform child in transform)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    _tiles.Add(renderer);
                }
            }

            if (_tiles.Count < tileCount)
            {
                Debug.LogWarning(
                    $"{nameof(ScrollingGameplayBackground)} expects {tileCount} child SpriteRenderers, "
                    + $"but found {_tiles.Count}. Add tile children to the prefab.",
                    this);
            }
        }

        private void ConfigureTiles()
        {
            if (backgroundSprite == null)
            {
                return;
            }

            CenterRootOnCamera();

            float scale = CalculateSpriteScale();
            _tileHeight = backgroundSprite.bounds.size.y * scale;
            int managedTileCount = GetManagedTileCount();

            for (int i = 0; i < _tiles.Count; i++)
            {
                SpriteRenderer renderer = _tiles[i];
                bool isManagedTile = i < managedTileCount;
                renderer.enabled = isManagedTile;

                if (!isManagedTile)
                {
                    continue;
                }

                renderer.sprite = backgroundSprite;
                renderer.sortingOrder = sortingOrder;
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
                renderer.transform.localRotation = Quaternion.identity;
            }
        }

        private void LayoutTiles()
        {
            if (_tileHeight <= 0f)
            {
                return;
            }

            int managedTileCount = GetManagedTileCount();
            float tileSpacing = GetTileSpacing();
            float firstTileY = -tileSpacing;

            for (int i = 0; i < managedTileCount; i++)
            {
                Transform tileTransform = _tiles[i].transform;
                tileTransform.localPosition = new Vector3(0f, firstTileY + (tileSpacing * i), 0f);
            }
        }

        private float CalculateSpriteScale()
        {
            if (!fitToCameraWidth || targetCamera == null || !targetCamera.orthographic)
            {
                return 1f;
            }

            float spriteWidth = backgroundSprite.bounds.size.x;
            if (spriteWidth <= 0f)
            {
                return 1f;
            }

            float cameraAspect = Mathf.Max(targetCamera.aspect, MinimumPortraitAspect);
            float cameraWidth = targetCamera.orthographicSize * 2f * cameraAspect * WidthPadding;
            return cameraWidth / spriteWidth;
        }

        private void ScrollTiles(float deltaTime)
        {
            float distance = scrollSpeed * deltaTime;
            int managedTileCount = GetManagedTileCount();

            for (int i = 0; i < managedTileCount; i++)
            {
                Transform tileTransform = _tiles[i].transform;
                tileTransform.position += Vector3.down * distance;
            }
        }

        private void RecycleTilesBelowCamera()
        {
            if (targetCamera == null || !targetCamera.orthographic || _tileHeight <= 0f)
            {
                return;
            }

            int managedTileCount = GetManagedTileCount();
            float cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
            float tileHalfHeight = _tileHeight * 0.5f;
            float topTileY = GetTopTileY();
            float tileSpacing = GetTileSpacing();

            for (int i = 0; i < managedTileCount; i++)
            {
                Transform tileTransform = _tiles[i].transform;
                bool isBelowCamera = tileTransform.position.y + tileHalfHeight < cameraBottom;

                if (!isBelowCamera)
                {
                    continue;
                }

                topTileY += tileSpacing;
                tileTransform.position = new Vector3(GetCameraCenterX(), topTileY, transform.position.z);
            }
        }

        private float GetTopTileY()
        {
            float top = float.NegativeInfinity;
            int managedTileCount = GetManagedTileCount();

            for (int i = 0; i < managedTileCount; i++)
            {
                top = Mathf.Max(top, _tiles[i].transform.position.y);
            }

            return top;
        }

        private bool HasCameraSizeChanged()
        {
            if (targetCamera == null || !targetCamera.orthographic)
            {
                return false;
            }

            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * Mathf.Max(targetCamera.aspect, MinimumPortraitAspect) * WidthPadding;

            bool changed = !Mathf.Approximately(_lastCameraHalfWidth, halfWidth)
                || !Mathf.Approximately(_lastCameraHalfHeight, halfHeight);

            _lastCameraHalfWidth = halfWidth;
            _lastCameraHalfHeight = halfHeight;

            return changed;
        }

        private float GetCameraCenterX()
        {
            return targetCamera != null ? targetCamera.transform.position.x : transform.position.x;
        }

        private float GetCameraCenterY()
        {
            return targetCamera != null ? targetCamera.transform.position.y : transform.position.y;
        }

        private void CenterRootOnCamera()
        {
            transform.position = new Vector3(GetCameraCenterX(), GetCameraCenterY(), transform.position.z);
        }

        private int GetManagedTileCount()
        {
            return Mathf.Min(Mathf.Max(1, tileCount), _tiles.Count);
        }

        private float GetTileSpacing()
        {
            return Mathf.Max(0.01f, _tileHeight - TileOverlap);
        }
    }
}
