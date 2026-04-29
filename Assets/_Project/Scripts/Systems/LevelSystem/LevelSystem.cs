using UnityEngine;

namespace _Project.Scripts.Systems.LevelSystem
{
    /// <summary>
    /// Holds level-scoped references such as lanes, bounds, and runtime progression anchors.
    /// </summary>
    public sealed class LevelSystem : MonoBehaviour
    {
        [SerializeField] private Transform levelRoot;
        [SerializeField] private float runDistance = 100f;

        public float RunDistance => runDistance;

        public void Init()
        {
        }

        private void Update()
        {
        }
    }
}
