using TMPro;
using UnityEngine;

namespace _Project.Scripts.Systems.UISystem
{
    public sealed class MissionToastUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;

        public void Show(string title, string body)
        {
            SetText(titleText, title);
            SetText(bodyText, body);
            SetRootActive(true);
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }

        private void SetRootActive(bool active)
        {
            GameObject target = root != null ? root : gameObject;
            if (target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
