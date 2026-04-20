using UnityEngine;

namespace _Project.Scripts.Data.ScriptableObjects.GateConfigs
{
    /// <summary>
    /// Describes a gate modifier so new gate types can be added without changing gameplay code structure.
    /// </summary>
    [CreateAssetMenu(fileName = "GateConfig", menuName = "Chibi Pixel Gate/Data/Gate Config")]
    public sealed class GateConfig : ScriptableObject
    {
        [SerializeField] private GateOperationType operationType = GateOperationType.Add;
        [SerializeField] private int amount = 1;
        [SerializeField] private string displayLabel = "+1";

        public GateOperationType OperationType => operationType;
        public int Amount => amount;
        public string DisplayLabel => displayLabel;
    }

    public enum GateOperationType
    {
        Add,
        Subtract,
        Multiply
    }
}
