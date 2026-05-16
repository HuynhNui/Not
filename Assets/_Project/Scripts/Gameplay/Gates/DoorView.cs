using _Project.Scripts.Data.ScriptableObjects.GateConfigs;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Gates
{
    /// <summary>
    /// 2D door presentation: sprite frame + world-space TMP label in a fixed rect.
    /// Gameplay pivot stays on the door root; text size does not affect lane position.
    /// </summary>
    public sealed class DoorView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer frameRenderer;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Color positiveColor = new Color(0.2f, 0.85f, 0.45f, 0.95f);
        [SerializeField] private Color negativeColor = new Color(0.95f, 0.3f, 0.25f, 0.95f);
        [SerializeField] private Color neutralColor = new Color(0.35f, 0.65f, 1f, 0.95f);

        public void Init()
        {
            if (frameRenderer == null)
            {
                frameRenderer = GetComponent<SpriteRenderer>();
            }

            if (labelText == null)
            {
                labelText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void Awake()
        {
            Init();
        }

        public void Bind(GateConfig config)
        {
            Init();

            if (config == null)
            {
                return;
            }

            if (labelText != null)
            {
                labelText.text = config.GetDisplayText();
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.enableWordWrapping = false;
                labelText.overflowMode = TextOverflowModes.Overflow;
            }

            if (frameRenderer != null)
            {
                frameRenderer.color = GetFrameColor(config.OperationType);
            }
        }

        private Color GetFrameColor(GateOperationType operationType)
        {
            return operationType switch
            {
                GateOperationType.Add => positiveColor,
                GateOperationType.Multiply => positiveColor,
                GateOperationType.Subtract => negativeColor,
                GateOperationType.Divide => negativeColor,
                _ => neutralColor
            };
        }
    }
}
