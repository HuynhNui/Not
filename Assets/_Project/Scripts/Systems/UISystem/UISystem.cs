using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        [SerializeField] private Button playAgainButton;

        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(RestartCurrentScene);
                playAgainButton.onClick.AddListener(RestartCurrentScene);
            }

            ShowHud();
        }

        private void Awake()
        {
            Init();
        }

        public void ShowHud()
        {
            Time.timeScale = 1f;

            if (hudRoot != null)
            {
                hudRoot.SetActive(true);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public void ShowGameOver()
        {
            Time.timeScale = 0f;

            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }
    }
}
