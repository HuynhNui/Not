using System.Collections;
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TrueGate.PlayModeTests
{
    public sealed class GameplayDialoguePlayModeTests
    {
        [UnityTest]
        public IEnumerator SpeechBubblePresenter_Show_DoesNotBlockRaycasts()
        {
            GameObject canvasObject = new GameObject("DialogueCanvas", typeof(Canvas));
            GameObject bubbleObject = new GameObject("GameplaySpeechBubble", typeof(RectTransform), typeof(CanvasGroup));
            GameObject backgroundObject = new GameObject("BubbleBackground", typeof(RectTransform), typeof(Image));
            GameObject textObject = new GameObject("DialogueText", typeof(RectTransform));
            GameObject playerObject = new GameObject("PlayerController");
            GameObject mainObject = new GameObject("MainPlayerUnit");

            try
            {
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                bubbleObject.transform.SetParent(canvasObject.transform, false);
                backgroundObject.transform.SetParent(bubbleObject.transform, false);
                textObject.transform.SetParent(bubbleObject.transform, false);

                Component playerController = playerObject.AddComponent(RuntimeType("_Project.Scripts.Gameplay.Player.PlayerController"));
                Component mainPlayerUnit = mainObject.AddComponent(RuntimeType("_Project.Scripts.Gameplay.Player.MainPlayerUnit"));
                Component text = textObject.AddComponent(RuntimeType("TMPro.TextMeshProUGUI", "Unity.TextMeshPro"));
                Invoke(playerController, "SetMainPlayerUnit", mainPlayerUnit);

                Component presenter = bubbleObject.AddComponent(RuntimeType("_Project.Scripts.Gameplay.Dialogue.SpeechBubblePresenter"));
                Invoke(presenter, "Show", "UNIT-07 online. Mission parameters confirmed.", playerController);
                yield return null;

                CanvasGroup canvasGroup = bubbleObject.GetComponent<CanvasGroup>();
                Image image = backgroundObject.GetComponent<Image>();

                Assert.That(canvasGroup.blocksRaycasts, Is.False);
                Assert.That(canvasGroup.interactable, Is.False);
                Assert.That(image.raycastTarget, Is.False);
                Assert.That(GetProperty<bool>(text, "raycastTarget"), Is.False);
                Assert.That(GetProperty<int>(text, "maxVisibleLines"), Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.Destroy(canvasObject);
                UnityEngine.Object.Destroy(playerObject);
                UnityEngine.Object.Destroy(mainObject);
            }
        }

        private static Type RuntimeType(string fullName, string assemblyName = "Assembly-CSharp")
        {
            Type type = Type.GetType($"{fullName}, {assemblyName}");
            Assert.That(type, Is.Not.Null, $"Could not find runtime type {fullName}.");
            return type;
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Could not find method {methodName} on {target.GetType().FullName}.");
            method.Invoke(target, args);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Could not find property {propertyName} on {target.GetType().FullName}.");
            return (T)property.GetValue(target);
        }
    }
}
