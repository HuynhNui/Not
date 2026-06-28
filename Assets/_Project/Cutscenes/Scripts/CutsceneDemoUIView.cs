using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Cutscenes
{
    public sealed class CutsceneDemoUIView : MonoBehaviour
    {
        private const string ActorUnit07 = "UNIT-07";
        private const string ActorSystem = "SYSTEM";
        private const string ActorAlienAdult = "ALIEN_ADULT";
        private const string ActorAlienChild = "ALIEN_CHILD";
        private const string ActorHumanCommand = "HUMAN_COMMAND";

        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject cutsceneRoot;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text emotionText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject finalChoicePanel;
        [SerializeField] private Button continueProtocolButton;
        [SerializeField] private Button shutDownCoreButton;
        [SerializeField] private GameObject actorUnit07;
        [SerializeField] private GameObject actorSystem;
        [SerializeField] private GameObject actorAlienAdult;
        [SerializeField] private GameObject actorAlienChild;
        [SerializeField] private GameObject actorHumanCommand;

        private readonly Dictionary<string, GameObject> _actors = new Dictionary<string, GameObject>();

        public Button NextButton => nextButton;
        public Button CloseButton => closeButton;
        public Button ContinueProtocolButton => continueProtocolButton;
        public Button ShutDownCoreButton => shutDownCoreButton;

        public void Init(
            GameObject menu,
            GameObject cutscene,
            GameObject dialogue,
            TMP_Text speaker,
            TMP_Text emotion,
            TMP_Text body,
            Button next,
            Button close,
            GameObject choices,
            Button continueButton,
            Button shutDownButton,
            GameObject unit07,
            GameObject system,
            GameObject alienAdult,
            GameObject alienChild,
            GameObject humanCommand)
        {
            menuRoot = menu;
            cutsceneRoot = cutscene;
            dialoguePanel = dialogue;
            speakerNameText = speaker;
            emotionText = emotion;
            dialogueText = body;
            nextButton = next;
            closeButton = close;
            finalChoicePanel = choices;
            continueProtocolButton = continueButton;
            shutDownCoreButton = shutDownButton;
            actorUnit07 = unit07;
            actorSystem = system;
            actorAlienAdult = alienAdult;
            actorAlienChild = alienChild;
            actorHumanCommand = humanCommand;

            RebuildActorMap();
            ShowMenu();
        }

        private void Awake()
        {
            RebuildActorMap();
            ShowMenu();
        }

        public void ShowMenu()
        {
            SetActive(menuRoot, true);
            SetActive(cutsceneRoot, false);
            HideAllActors();
            SetActive(dialoguePanel, false);
            SetActive(finalChoicePanel, false);
            SetButtonVisible(nextButton, false);
            SetButtonVisible(closeButton, false);
        }

        public void ShowCutscene()
        {
            SetActive(menuRoot, false);
            SetActive(cutsceneRoot, true);
            SetActive(dialoguePanel, true);
            SetActive(finalChoicePanel, false);
            SetButtonVisible(nextButton, true);
            SetButtonVisible(closeButton, true);
        }

        public void ShowFinalChoice()
        {
            ShowCutscene();
            SetActive(finalChoicePanel, true);
            SetButtonVisible(nextButton, false);
        }

        public void SetDialogueLine(StoryDialogueLine line)
        {
            if (line == null)
            {
                return;
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = line.Speaker;
            }

            if (emotionText != null)
            {
                emotionText.text = line.Emotion;
            }

            if (dialogueText != null)
            {
                dialogueText.text = line.Text;
            }

            HideAllActors();
            SetActorVisible(line.Speaker, true);
        }

        public void HideAllActors()
        {
            foreach (GameObject actor in _actors.Values)
            {
                SetActive(actor, false);
            }
        }

        public void SetActorVisible(string actorId, bool visible)
        {
            string key = NormalizeActorId(actorId);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_actors.TryGetValue(key, out GameObject actor))
            {
                SetActive(actor, visible);
            }
        }

        public void ReturnToMenu()
        {
            ShowMenu();
        }

        private void RebuildActorMap()
        {
            _actors.Clear();
            AddActor(ActorUnit07, actorUnit07);
            AddActor(ActorSystem, actorSystem);
            AddActor(ActorAlienAdult, actorAlienAdult);
            AddActor(ActorAlienChild, actorAlienChild);
            AddActor(ActorHumanCommand, actorHumanCommand);
        }

        private void AddActor(string actorId, GameObject actor)
        {
            if (actor == null)
            {
                return;
            }

            _actors[actorId] = actor;
        }

        private static string NormalizeActorId(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return string.Empty;
            }

            string normalized = actorId.Trim().ToUpperInvariant();
            if (normalized.Contains("UNIT"))
            {
                return ActorUnit07;
            }

            if (normalized.Contains("HUMAN"))
            {
                return ActorHumanCommand;
            }

            if (normalized.Contains("ALIEN_CHILD") || normalized.Contains("CHILD"))
            {
                return ActorAlienChild;
            }

            if (normalized.Contains("ALIEN"))
            {
                return ActorAlienAdult;
            }

            if (normalized.Contains("SYSTEM"))
            {
                return ActorSystem;
            }

            return normalized;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}
