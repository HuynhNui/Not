using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Keeps UGUI event systems compatible with projects using the Input System package only.
    /// </summary>
    public static class EventSystemInputSystemBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void ReplaceLegacyInputModules()
        {
            StandaloneInputModule[] legacyModules = Object.FindObjectsByType<StandaloneInputModule>(FindObjectsInactive.Exclude);

            for (int index = 0; index < legacyModules.Length; index++)
            {
                StandaloneInputModule legacyModule = legacyModules[index];

                if (legacyModule == null)
                {
                    continue;
                }

                GameObject eventSystemObject = legacyModule.gameObject;

                if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
                {
                    eventSystemObject.AddComponent<InputSystemUIInputModule>();
                }

                legacyModule.enabled = false;
                Object.Destroy(legacyModule);
            }

            if (EventSystem.current != null)
            {
                return;
            }

            EventSystem existingEventSystem = Object.FindAnyObjectByType<EventSystem>();

            if (existingEventSystem != null)
            {
                EventSystem.current = existingEventSystem;
            }
        }
    }
}
