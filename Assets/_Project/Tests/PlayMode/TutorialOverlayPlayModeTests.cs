using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace _Project.Tests.PlayMode
{
    public sealed class TutorialOverlayPlayModeTests
    {
        [UnityTest]
        public IEnumerator Overlay_ShowHide_DoesNotThrow()
        {
            Component overlay = CreateOverlay(out GameObject root, out _, out _);

            Assert.DoesNotThrow(() => Invoke(overlay, "ShowOverlay", true, true));
            Assert.DoesNotThrow(() => Invoke(overlay, "HideOverlay"));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HighlightMissingTarget_DoesNotThrow()
        {
            Component overlay = CreateOverlay(out GameObject root, out _, out _);

            Assert.DoesNotThrow(() => Invoke(overlay, "HighlightRect", null, Vector2.zero));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkipButton_InvokesEvent()
        {
            Component overlay = CreateOverlay(out GameObject root, out Button skipButton, out _);
            bool invoked = false;
            AddEventHandler(overlay, "SkipClicked", () => invoked = true);

            skipButton.onClick.Invoke();

            Assert.That(invoked, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NextButton_InvokesEvent()
        {
            Component overlay = CreateOverlay(out GameObject root, out _, out Button nextButton);
            bool invoked = false;
            AddEventHandler(overlay, "NextClicked", () => invoked = true);

            nextButton.onClick.Invoke();

            Assert.That(invoked, Is.True);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowOverlay_MovesOverlayAboveSiblingPanels()
        {
            var parent = new GameObject("TutorialOverlayParent", typeof(RectTransform));
            var sibling = new GameObject("GameplayHudSibling", typeof(RectTransform));
            sibling.transform.SetParent(parent.transform, false);
            Component overlay = CreateOverlay(out GameObject root, out _, out _);
            root.transform.SetParent(parent.transform, false);
            sibling.transform.SetAsLastSibling();

            Invoke(overlay, "ShowOverlay", false, false);

            Assert.That(root.transform.GetSiblingIndex(), Is.EqualTo(parent.transform.childCount - 1));
            Object.Destroy(parent);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowSwipeIcon_EnablesSpriteAndPreservesAspect()
        {
            Component overlay = CreateOverlay(out GameObject root, out _, out _);
            RectTransform swipe = (RectTransform)GetField(overlay, "swipeLeftRightIcon");
            Image swipeImage = swipe.GetComponent<Image>();
            Sprite sprite = CreateTestSprite();
            swipeImage.sprite = null;
            SetField(overlay, "swipeLeftRightSprite", sprite);

            Invoke(overlay, "ShowSwipeIcon");

            Assert.That(swipe.gameObject.activeSelf, Is.True);
            Assert.That(swipeImage.sprite, Is.EqualTo(sprite));
            Assert.That(swipeImage.enabled, Is.True);
            Assert.That(swipeImage.preserveAspect, Is.True);
            Assert.That(swipeImage.raycastTarget, Is.False);

            Object.Destroy(root);
            Object.Destroy(sprite.texture);
            yield return null;
        }

        private static Component CreateOverlay(
            out GameObject root,
            out Button skipButton,
            out Button nextButton)
        {
            root = new GameObject("TutorialOverlayTestRoot", typeof(RectTransform), typeof(CanvasGroup));
            root.SetActive(false);
            Component overlay = root.AddComponent(RuntimeType(
                "_Project.Scripts.Systems.TutorialSystem.TutorialOverlayUI"));

            Image dim = CreateImage(root.transform, "DimBackground");
            RectTransform highlight = CreateImage(root.transform, "FocusHighlightFrame").rectTransform;
            RectTransform swipe = CreateImage(root.transform, "SwipeLeftRightIcon").rectTransform;
            RectTransform dialog = CreateImage(root.transform, "TutorialDialogPanel").rectTransform;
            RectTransform callout = CreateImage(root.transform, "UpgradeCallout").rectTransform;
            skipButton = CreateButton(root.transform, "SkipButton");
            nextButton = CreateButton(dialog, "NextButton");

            SetField(overlay, "canvasGroup", root.GetComponent<CanvasGroup>());
            SetField(overlay, "dimBackground", dim);
            SetField(overlay, "focusHighlightFrame", highlight);
            SetField(overlay, "focusHighlightImage", highlight.GetComponent<Image>());
            SetField(overlay, "swipeLeftRightIcon", swipe);
            SetField(overlay, "tutorialDialogPanel", dialog);
            SetField(overlay, "nextButton", nextButton);
            SetField(overlay, "upgradeCallout", callout);
            SetField(overlay, "skipButton", skipButton);

            root.SetActive(true);
            return overlay;
        }

        private static Image CreateImage(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(Transform parent, string name)
        {
            Image image = CreateImage(parent, name);
            image.raycastTarget = true;
            return image.gameObject.AddComponent<Button>();
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(target, value);
        }

        private static object GetField(Object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            return field.GetValue(target);
        }

        private static void Invoke(Component target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
            method.Invoke(target, args);
        }

        private static void AddEventHandler(Component target, string eventName, System.Action action)
        {
            EventInfo eventInfo = target.GetType().GetEvent(
                eventName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(eventInfo, Is.Not.Null, $"Missing event {eventName}");
            eventInfo.AddEventHandler(target, action);
        }

        private static System.Type RuntimeType(string typeName)
        {
            System.Type type = System.Type.GetType($"{typeName}, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, $"Missing runtime type {typeName}");
            return type;
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(8, 8);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f));
        }
    }
}
