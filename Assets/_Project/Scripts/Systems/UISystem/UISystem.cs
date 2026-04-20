using UnityEngine;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Owns HUD and game-over UI references for score and run-state presentation.
    /// </summary>
    public sealed class UISystem : MonoBehaviour
    {
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject scoreLabel;

        public void Init()
        {
        }

        private void Update()
        {
        }
    }
}
