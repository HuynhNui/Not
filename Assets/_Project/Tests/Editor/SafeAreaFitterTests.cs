using _Project.Scripts.Systems.UISystem;
using NUnit.Framework;
using UnityEngine;

namespace _Project.Tests.Editor
{
    public sealed class SafeAreaFitterTests
    {
        [Test]
        public void FullScreenSafeArea_ReturnsFullAnchors()
        {
            AssertAnchors(
                new Rect(0f, 0f, 1080f, 1920f),
                new Vector2Int(1080, 1920),
                true,
                true,
                true,
                true,
                Vector2.zero,
                Vector2.one);
        }

        [Test]
        public void TopInset_UpdatesTopAnchor()
        {
            AssertAnchors(
                new Rect(0f, 0f, 1080f, 1800f),
                new Vector2Int(1080, 1920),
                true,
                true,
                true,
                true,
                Vector2.zero,
                new Vector2(1f, 0.9375f));
        }

        [Test]
        public void BottomInset_UpdatesBottomAnchor()
        {
            AssertAnchors(
                new Rect(0f, 120f, 1080f, 1800f),
                new Vector2Int(1080, 1920),
                true,
                true,
                true,
                true,
                new Vector2(0f, 0.0625f),
                Vector2.one);
        }

        [Test]
        public void HorizontalInsets_UpdateLeftAndRightAnchors()
        {
            AssertAnchors(
                new Rect(80f, 0f, 920f, 1920f),
                new Vector2Int(1080, 1920),
                true,
                true,
                true,
                true,
                new Vector2(80f / 1080f, 0f),
                new Vector2(1000f / 1080f, 1f));
        }

        [Test]
        public void DisabledEdges_StayAtFullScreenAnchors()
        {
            AssertAnchors(
                new Rect(80f, 120f, 920f, 1680f),
                new Vector2Int(1080, 1920),
                false,
                false,
                false,
                false,
                Vector2.zero,
                Vector2.one);
        }

        [Test]
        public void MixedEdges_OnlyApplyEnabledInsets()
        {
            AssertAnchors(
                new Rect(80f, 120f, 920f, 1680f),
                new Vector2Int(1080, 1920),
                true,
                false,
                true,
                false,
                new Vector2(80f / 1080f, 0f),
                new Vector2(1f, 1800f / 1920f));
        }

        [Test]
        public void ZeroResolution_ReturnsFalse()
        {
            bool success = SafeAreaFitter.TryCalculateAnchors(
                Rect.zero,
                Vector2Int.zero,
                true,
                true,
                true,
                true,
                out Vector2 anchorMin,
                out Vector2 anchorMax);

            Assert.That(success, Is.False);
            Assert.That(anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(anchorMax, Is.EqualTo(Vector2.one));
        }

        private static void AssertAnchors(
            Rect safeArea,
            Vector2Int screenSize,
            bool applyTop,
            bool applyBottom,
            bool applyLeft,
            bool applyRight,
            Vector2 expectedMin,
            Vector2 expectedMax)
        {
            bool success = SafeAreaFitter.TryCalculateAnchors(
                safeArea,
                screenSize,
                applyTop,
                applyBottom,
                applyLeft,
                applyRight,
                out Vector2 anchorMin,
                out Vector2 anchorMax);

            Assert.That(success, Is.True);
            Assert.That(anchorMin.x, Is.EqualTo(expectedMin.x).Within(0.0001f));
            Assert.That(anchorMin.y, Is.EqualTo(expectedMin.y).Within(0.0001f));
            Assert.That(anchorMax.x, Is.EqualTo(expectedMax.x).Within(0.0001f));
            Assert.That(anchorMax.y, Is.EqualTo(expectedMax.y).Within(0.0001f));
        }
    }
}
