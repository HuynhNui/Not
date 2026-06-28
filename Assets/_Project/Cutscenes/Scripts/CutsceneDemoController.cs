using UnityEngine;
using UnityEngine.UI;

namespace _Project.Cutscenes
{
    public sealed class CutsceneDemoController : MonoBehaviour
    {
        [SerializeField] private StoryCutsceneDirector director;
        [SerializeField] private Button buttonCs1;
        [SerializeField] private Button buttonCs2;
        [SerializeField] private Button buttonCs3;
        [SerializeField] private Button buttonCs4;
        [SerializeField] private Button buttonCs5;
        [SerializeField] private Button buttonCs6;
        [SerializeField] private Button buttonCs7;

        public void Init(
            StoryCutsceneDirector storyDirector,
            Button cs1,
            Button cs2,
            Button cs3,
            Button cs4,
            Button cs5,
            Button cs6,
            Button cs7)
        {
            director = storyDirector;
            buttonCs1 = cs1;
            buttonCs2 = cs2;
            buttonCs3 = cs3;
            buttonCs4 = cs4;
            buttonCs5 = cs5;
            buttonCs6 = cs6;
            buttonCs7 = cs7;
            WireButtons();
        }

        private void Awake()
        {
            if (director == null)
            {
                director = FindAnyObjectByType<StoryCutsceneDirector>();
            }

            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void OnClick_CS1()
        {
            director?.PlayBootSequence();
        }

        public void OnClick_CS2()
        {
            director?.PlayFirstDeathRecovery();
        }

        public void OnClick_CS3()
        {
            director?.PlayEnemyDoesNotCharge();
        }

        public void OnClick_CS4()
        {
            director?.PlayGateMemoryLeak();
        }

        public void OnClick_CS5()
        {
            director?.PlayHumanCommand();
        }

        public void OnClick_CS6()
        {
            director?.PlaySystemFatigue();
        }

        public void OnClick_CS7()
        {
            director?.PlayFinalChoice();
        }

        private void WireButtons()
        {
            UnwireButtons();
            buttonCs1?.onClick.AddListener(OnClick_CS1);
            buttonCs2?.onClick.AddListener(OnClick_CS2);
            buttonCs3?.onClick.AddListener(OnClick_CS3);
            buttonCs4?.onClick.AddListener(OnClick_CS4);
            buttonCs5?.onClick.AddListener(OnClick_CS5);
            buttonCs6?.onClick.AddListener(OnClick_CS6);
            buttonCs7?.onClick.AddListener(OnClick_CS7);
        }

        private void UnwireButtons()
        {
            buttonCs1?.onClick.RemoveListener(OnClick_CS1);
            buttonCs2?.onClick.RemoveListener(OnClick_CS2);
            buttonCs3?.onClick.RemoveListener(OnClick_CS3);
            buttonCs4?.onClick.RemoveListener(OnClick_CS4);
            buttonCs5?.onClick.RemoveListener(OnClick_CS5);
            buttonCs6?.onClick.RemoveListener(OnClick_CS6);
            buttonCs7?.onClick.RemoveListener(OnClick_CS7);
        }
    }
}
