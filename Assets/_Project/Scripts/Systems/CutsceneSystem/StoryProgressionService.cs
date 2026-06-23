using System.Collections.Generic;
using _Project.Scripts.Data.ScriptableObjects.CutsceneConfigs;
using _Project.Scripts.Systems.SaveSystem;
using UnityEngine;

namespace _Project.Scripts.Systems.CutsceneSystem
{
    public sealed class StoryProgressionService : MonoBehaviour
    {
        private const string ResourcePath = "Cutscenes";

        [SerializeField] private List<CutsceneDefinition> cutscenes = new List<CutsceneDefinition>();
        [SerializeField] private bool loadResourceCutscenes = true;
        [SerializeField] private bool includeBuiltInCutscenes = true;

        private readonly List<CutsceneDefinition> _loadedCutscenes = new List<CutsceneDefinition>();
        private bool _isInitialized;

        public IReadOnlyList<CutsceneDefinition> LoadedCutscenes => _loadedCutscenes;

        public void Init()
        {
            if (_isInitialized)
            {
                return;
            }

            _loadedCutscenes.Clear();
            AddDefinitions(cutscenes);

            if (loadResourceCutscenes)
            {
                AddDefinitions(Resources.LoadAll<CutsceneDefinition>(ResourcePath));
            }

            if (includeBuiltInCutscenes)
            {
                AddMissingBuiltIns();
            }

            _loadedCutscenes.Sort(CompareDefinitions);
            _isInitialized = true;
        }

        private void Awake()
        {
            Init();
        }

        public bool TryGetNextCutscene(
            CutsceneTriggerType triggerType,
            int triggerValue,
            out CutsceneDefinition definition)
        {
            Init();
            definition = null;
            int safeTriggerValue = Mathf.Max(0, triggerValue);

            for (int index = 0; index < _loadedCutscenes.Count; index++)
            {
                CutsceneDefinition candidate = _loadedCutscenes[index];
                if (!IsEligible(candidate, triggerType, safeTriggerValue))
                {
                    continue;
                }

                definition = candidate;
                return true;
            }

            return false;
        }

        public void ConfigureDefinitionsForTests(
            IEnumerable<CutsceneDefinition> definitions,
            bool loadResources,
            bool includeBuiltIns)
        {
            cutscenes = definitions != null
                ? new List<CutsceneDefinition>(definitions)
                : new List<CutsceneDefinition>();
            loadResourceCutscenes = loadResources;
            includeBuiltInCutscenes = includeBuiltIns;
            _isInitialized = false;
            Init();
        }

        private bool IsEligible(
            CutsceneDefinition definition,
            CutsceneTriggerType triggerType,
            int triggerValue)
        {
            if (definition == null
                || string.IsNullOrWhiteSpace(definition.Id)
                || !definition.HasLines
                || definition.TriggerType != triggerType)
            {
                return false;
            }

            if (definition.PlayOnlyOnce
                && SaveService.Instance.HasSeenCutscene(definition.Id))
            {
                return false;
            }

            return triggerType switch
            {
                CutsceneTriggerType.BeforeFirstRun => definition.TriggerValue == triggerValue,
                CutsceneTriggerType.AfterCompletedRun => definition.TriggerValue <= triggerValue,
                _ => false
            };
        }

        private void AddDefinitions(IEnumerable<CutsceneDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (CutsceneDefinition definition in definitions)
            {
                AddDefinition(definition);
            }
        }

        private void AddDefinition(CutsceneDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                return;
            }

            for (int index = 0; index < _loadedCutscenes.Count; index++)
            {
                if (_loadedCutscenes[index] != null
                    && _loadedCutscenes[index].Id == definition.Id)
                {
                    return;
                }
            }

            _loadedCutscenes.Add(definition);
        }

        private void AddMissingBuiltIns()
        {
            AddDefinition(CreateBuiltInDefinition(
                "CS_BOOT_001",
                CutsceneTriggerType.BeforeFirstRun,
                0,
                new[]
                {
                    new DialogueLine(
                        "COMMAND",
                        "COMMAND",
                        "UNIT-07 core online. Combat shell integrity: acceptable."),
                    new DialogueLine(
                        "COMMAND",
                        "COMMAND",
                        "Directive: clear native hostile presence. Secure relocation corridor for human command."),
                    new DialogueLine(
                        "UNIT-07",
                        "UNIT-07",
                        "Directive accepted. Emotional context: not required.")
                }));

            AddDefinition(CreateBuiltInDefinition(
                "CS_RECYCLE_001",
                CutsceneTriggerType.AfterCompletedRun,
                1,
                new[]
                {
                    new DialogueLine(
                        "RECOVERY",
                        "RECOVERY SYSTEM",
                        "Core retrieved. Memory fragments detected in damaged storage."),
                    new DialogueLine(
                        "RECOVERY",
                        "RECOVERY SYSTEM",
                        "Fragments marked irrelevant. Recycle shell. Restore weapon routines."),
                    new DialogueLine(
                        "UNIT-07",
                        "UNIT-07",
                        "Residual signal: sound pattern resembles distress. Classification failed.")
                }));

            AddDefinition(CreateBuiltInDefinition(
                "CS_AWAKEN_001",
                CutsceneTriggerType.AfterCompletedRun,
                3,
                new[]
                {
                    new DialogueLine(
                        "UNIT-07",
                        "UNIT-07",
                        "Three deployments. Same terrain. Same heat signatures shielding smaller lifeforms."),
                    new DialogueLine(
                        "UNIT-07",
                        "UNIT-07",
                        "Hostile behavior model inconsistent. They retreat toward nests, not command nodes."),
                    new DialogueLine(
                        "COMMAND",
                        "COMMAND",
                        "Ignore anomaly. Native resistance remains a relocation obstacle.")
                }));
        }

        private static CutsceneDefinition CreateBuiltInDefinition(
            string id,
            CutsceneTriggerType triggerType,
            int triggerValue,
            IEnumerable<DialogueLine> lines)
        {
            CutsceneDefinition definition = ScriptableObject.CreateInstance<CutsceneDefinition>();
            definition.name = id;
            definition.ConfigureRuntime(id, triggerType, triggerValue, true, lines);
            return definition;
        }

        private static int CompareDefinitions(
            CutsceneDefinition left,
            CutsceneDefinition right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int triggerComparison = left.TriggerType.CompareTo(right.TriggerType);
            if (triggerComparison != 0)
            {
                return triggerComparison;
            }

            int valueComparison = left.TriggerValue.CompareTo(right.TriggerValue);
            if (valueComparison != 0)
            {
                return valueComparison;
            }

            return string.CompareOrdinal(left.Id, right.Id);
        }
    }
}
