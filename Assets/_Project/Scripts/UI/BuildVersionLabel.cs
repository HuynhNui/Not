using TMPro;
using UnityEngine;

namespace _Project.Scripts.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class BuildVersionLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string channel = "Beta";

        private void Awake()
        {
            ResolveLabel();
            Refresh();
        }

        private void OnEnable()
        {
            ResolveLabel();
            Refresh();
        }

        private void OnValidate()
        {
            ResolveLabel();
            Refresh();
        }

        private void ResolveLabel()
        {
            if (label == null)
            {
                label = GetComponent<TMP_Text>();
            }
        }

        private void Refresh()
        {
            if (label == null)
            {
                return;
            }

            string prefix = string.IsNullOrWhiteSpace(channel) ? string.Empty : $"{channel.Trim()} ";
            label.text = $"{prefix}{Application.version}";
        }
    }
}
