using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TrueGate.PlayModeTests
{
    public sealed class HitboxRegressionPlayModeTests
    {
        [Test]
        public void UnitContactPoint_UsesColliderInsteadOfRendererBounds()
        {
            GameObject unitObject = CreateUnitWithLargeSpriteAndSmallCollider("Unit");
            Component unit = unitObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));

            try
            {
                Vector3 queryPoint = new Vector3(0.4f, 0f, 0f);
                Vector3 contact = (Vector3)RuntimeType(
                    "_Project.Scripts.Gameplay.Player.PlayerController")
                    .GetMethod(
                        "GetUnitContactPoint",
                        BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { unit, queryPoint });

                Assert.That(contact.x, Is.EqualTo(0.1f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unitObject);
            }
        }

        [UnityTest]
        public IEnumerator PlayerController_DoesNotReplaceManualHurtboxWithSpriteBounds()
        {
            GameObject squadObject = new GameObject("Squad");
            GameObject mainObject = CreateUnitWithLargeSpriteAndSmallCollider("Main");
            Component controller = squadObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerController"));
            Component main = mainObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.MainPlayerUnit"));
            CircleCollider2D manualHurtbox = mainObject.GetComponent<CircleCollider2D>();

            try
            {
                Invoke(controller, "SetMainPlayerUnit", main);
                yield return null;

                Assert.That(manualHurtbox != null, Is.True);
                Assert.That(manualHurtbox.enabled, Is.True);
                Assert.That(manualHurtbox.radius, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(mainObject.GetComponent<BoxCollider2D>(), Is.Null);
            }
            finally
            {
                UnityEngine.Object.Destroy(squadObject);
                UnityEngine.Object.Destroy(mainObject);
            }
        }

        [UnityTest]
        public IEnumerator ChomboomExplosion_DoesNotHitTransparentSpritePadding()
        {
            GameObject unitObject = CreateUnitWithLargeSpriteAndSmallCollider("Player");
            GameObject explosionObject = new GameObject("Explosion");
            Component unit = unitObject.GetComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            Component explosion = explosionObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Enemies.ChomboomBoomFx"));

            try
            {
                Invoke(unit, "SetMaxHp", 10f, false);
                Invoke(unit, "RestoreFullHealth");

                unitObject.transform.position = Vector3.zero;
                explosionObject.transform.position = new Vector3(0.55f, 0f, 0f);

                Invoke(explosion, "Init", null, 3f, 0.08f);
                Invoke(explosion, "Spawn");
                yield return null;

                Assert.That((float)GetProperty(unit, "CurrentHp"), Is.EqualTo(10f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.Destroy(unitObject);
                UnityEngine.Object.Destroy(explosionObject);
            }
        }

        private static GameObject CreateUnitWithLargeSpriteAndSmallCollider(string name)
        {
            GameObject unitObject = new GameObject(name);

            SpriteRenderer renderer = unitObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateOneUnitSprite();

            CircleCollider2D collider = unitObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;

            unitObject.AddComponent(RuntimeType(
                "_Project.Scripts.Gameplay.Player.PlayerUnit"));
            return unitObject;
        }

        private static Type RuntimeType(string fullName)
        {
            return Type.GetType($"{fullName}, Assembly-CSharp", throwOnError: true);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, arguments.Length);
            return method.Invoke(target, arguments);
        }

        private static MethodInfo FindMethod(Type type, string methodName, int argumentCount)
        {
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name == methodName
                    && method.GetParameters().Length == argumentCount)
                {
                    return method;
                }
            }

            throw new MissingMethodException(type.FullName, methodName);
        }

        private static object GetProperty(object target, string propertyName)
        {
            return target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(target);
        }

        private static Sprite CreateOneUnitSprite()
        {
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 16f, 16f),
                new Vector2(0.5f, 0.5f),
                16f);
        }
    }
}
