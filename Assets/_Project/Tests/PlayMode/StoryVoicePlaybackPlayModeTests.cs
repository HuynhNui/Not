using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TrueGate.PlayModeTests
{
    public sealed class StoryVoicePlaybackPlayModeTests
    {
        private const string PreChoiceId = "CS_07_FinalChoice_PreChoice";
        private const string ContinueId = "CS_07_FinalChoice_ContinueProtocol";

        private GameObject _root;
        private GameObject _cutsceneRoot;
        private Component _audioSystem;
        private Component _director;
        private Component _controller;
        private ScriptableObject _catalog;
        private Button _nextButton;
        private Button _closeButton;
        private Button _continueButton;
        private Button _shutdownButton;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.SetInt("Settings.SfxEnabled", 1);

            _root = new GameObject("StoryVoiceTestRoot");
            _audioSystem = _root.AddComponent(RuntimeType("_Project.Scripts.Systems.AudioSystem.AudioSystem"));
            Component view = CreateView();
            _director = _root.AddComponent(RuntimeType("_Project.Cutscenes.StoryCutsceneDirector"));
            Invoke(_director, "Init", null, view);

            _catalog = ScriptableObject.CreateInstance(
                RuntimeType("_Project.Scripts.Systems.AudioSystem.VoiceLineCatalog"));
            _controller = _root.AddComponent(RuntimeType("_Project.Cutscenes.StoryVoicePlaybackController"));
            Invoke(_controller, "Init", _director, _catalog, _audioSystem);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("Settings.SfxEnabled");
            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }

            if (_cutsceneRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_cutsceneRoot);
            }

            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
            }
        }

        [UnityTest]
        public IEnumerator ShowLine_PlaysMappedClip()
        {
            AudioClip clip = CreateClip("line-0");
            SetCatalog(new[] { CreateEntry("TEST", 0, clip) });

            PlayTransient("TEST", Line("First"));
            yield return null;

            Assert.That(DialogueSource.clip, Is.SameAs(clip));
            Assert.That(GetProperty<bool>(_audioSystem, "IsDialoguePlaying"), Is.True);
        }

        [UnityTest]
        public IEnumerator Next_StopsOldClipAndPlaysNewClip()
        {
            AudioClip first = CreateClip("line-0");
            AudioClip second = CreateClip("line-1");
            SetCatalog(new[]
            {
                CreateEntry("TEST", 0, first),
                CreateEntry("TEST", 1, second)
            });

            PlayTransient("TEST", Line("First"), Line("Second"));
            yield return null;
            Assert.That(DialogueSource.clip, Is.SameAs(first));

            _nextButton.onClick.Invoke();
            yield return null;

            Assert.That(DialogueSource.clip, Is.SameAs(second));
            Assert.That(GetProperty<bool>(_audioSystem, "IsDialoguePlaying"), Is.True);
        }

        [UnityTest]
        public IEnumerator Close_StopsVoice()
        {
            AudioClip clip = CreateClip("close-line");
            SetCatalog(new[] { CreateEntry("TEST", 0, clip) });
            PlayTransient("TEST", Line("Close"));
            yield return null;

            _closeButton.onClick.Invoke();
            yield return null;

            Assert.That(DialogueSource.clip, Is.Null);
            Assert.That(GetProperty<bool>(_audioSystem, "IsDialoguePlaying"), Is.False);
        }

        [UnityTest]
        public IEnumerator Finish_StopsVoice()
        {
            AudioClip clip = CreateClip("finish-line");
            SetCatalog(new[] { CreateEntry("TEST", 0, clip) });
            PlayTransient("TEST", Line("Finish"));
            yield return null;

            _nextButton.onClick.Invoke();
            yield return null;

            Assert.That(DialogueSource.clip, Is.Null);
            Assert.That(GetProperty<bool>(_audioSystem, "IsDialoguePlaying"), Is.False);
        }

        [UnityTest]
        public IEnumerator SkipLine_DoesNotPlayClip()
        {
            SetCatalog(
                Array.Empty<object>(),
                new[] { CreateSkip("TEST", 0) });

            PlayTransient("TEST", Line("..."));
            yield return null;

            Assert.That(DialogueSource.clip, Is.Null);
        }

        [UnityTest]
        public IEnumerator MissingClip_DoesNotCrashOrPlay()
        {
            SetCatalog(Array.Empty<object>());
            LogAssert.Expect(
                LogType.Warning,
                "Missing STORY voice for 'TEST' line 0. Dialogue continues without audio.");

            PlayTransient("TEST", Line("Missing"));
            yield return null;

            Assert.That(DialogueSource.clip, Is.Null);
        }

        [UnityTest]
        public IEnumerator FinalChoiceTransition_DoesNotLeakPreviousClip()
        {
            AudioClip preChoice = CreateClip("pre-choice");
            AudioClip branch = CreateClip("branch");
            var skips = new List<object>();
            for (int index = 1; index < 20; index++)
            {
                skips.Add(CreateSkip(PreChoiceId, index));
            }

            SetCatalog(
                new[]
                {
                    CreateEntry(PreChoiceId, 0, preChoice),
                    CreateEntry(ContinueId, 0, branch)
                },
                skips);

            Invoke(_director, "Play", PreChoiceId);
            yield return null;
            Assert.That(DialogueSource.clip, Is.SameAs(preChoice));

            for (int index = 0; index < 20; index++)
            {
                _nextButton.onClick.Invoke();
            }

            _continueButton.onClick.Invoke();
            yield return null;

            Assert.That(DialogueSource.clip, Is.SameAs(branch));
            Assert.That(DialogueSource.clip, Is.Not.SameAs(preChoice));
        }

        [UnityTest]
        public IEnumerator DialogueSetting_MutesStoryVoice()
        {
            AudioClip clip = CreateClip("muted-line");
            SetCatalog(new[] { CreateEntry("TEST", 0, clip) });

            Invoke(_audioSystem, "SetSfxEnabled", false);
            PlayTransient("TEST", Line("Muted"));
            yield return null;

            Assert.That(DialogueSource.mute, Is.True);
            Assert.That(DialogueSource.clip, Is.SameAs(clip));
        }

        [UnityTest]
        public IEnumerator AdvancingLines_DoesNotCreateAudioSources()
        {
            AudioClip first = CreateClip("source-count-0");
            AudioClip second = CreateClip("source-count-1");
            SetCatalog(new[]
            {
                CreateEntry("TEST", 0, first),
                CreateEntry("TEST", 1, second)
            });
            int sourceCount = _root.GetComponentsInChildren<AudioSource>(true).Length;

            PlayTransient("TEST", Line("First"), Line("Second"));
            _nextButton.onClick.Invoke();
            yield return null;

            Assert.That(_root.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(sourceCount));
        }

        private AudioSource DialogueSource => _root.transform.Find("DialogueSource").GetComponent<AudioSource>();

        private void SetCatalog(IEnumerable<object> entries, IEnumerable<object> skipped = null)
        {
            Array entryArray = ToRuntimeArray(
                entries,
                "_Project.Scripts.Systems.AudioSystem.VoiceLineCatalogEntry");
            Array skipArray = ToRuntimeArray(
                skipped ?? Array.Empty<object>(),
                "_Project.Scripts.Systems.AudioSystem.VoiceLineCatalogSkip");

            Invoke(_catalog, "SetEntries", entryArray, skipArray);
            object[] arguments = { null };
            bool valid = (bool)GetMethod(_catalog.GetType(), "TryRebuildLookup", 1)
                .Invoke(_catalog, arguments);
            Assert.That(valid, Is.True, arguments[0] as string);
        }

        private void PlayTransient(string sourceId, params object[] lines)
        {
            Type lineType = RuntimeType("_Project.Cutscenes.StoryDialogueLine");
            Array lineArray = Array.CreateInstance(lineType, lines.Length);
            for (int index = 0; index < lines.Length; index++)
            {
                lineArray.SetValue(lines[index], index);
            }

            Type definitionType = RuntimeType("_Project.Cutscenes.StoryCutsceneDefinition");
            object definition = Activator.CreateInstance(definitionType, sourceId, lineArray);
            Type presentationType = RuntimeType("_Project.Cutscenes.StoryCutscenePresentationMode");
            object fullScreen = Enum.Parse(presentationType, "FullScreen");
            Invoke(_director, "PlayTransient", definition, fullScreen);
        }

        private Component CreateView()
        {
            _cutsceneRoot = new GameObject("CutsceneRoot");
            GameObject dialogue = new GameObject("Dialogue");
            dialogue.transform.SetParent(_cutsceneRoot.transform);
            _nextButton = CreateButton("Next", dialogue.transform);
            _closeButton = CreateButton("Close", dialogue.transform);

            GameObject choices = new GameObject("Choices");
            choices.transform.SetParent(dialogue.transform);
            _continueButton = CreateButton("Continue", choices.transform);
            _shutdownButton = CreateButton("Shutdown", choices.transform);

            Component view = _cutsceneRoot.AddComponent(
                RuntimeType("_Project.Cutscenes.CutsceneDemoUIView"));
            Invoke(
                view,
                "Init",
                null,
                _cutsceneRoot,
                dialogue,
                null,
                null,
                null,
                _nextButton,
                _closeButton,
                choices,
                _continueButton,
                _shutdownButton,
                null,
                null,
                null,
                null,
                null);
            return view;
        }

        private static object Line(string text)
        {
            return Activator.CreateInstance(
                RuntimeType("_Project.Cutscenes.StoryDialogueLine"),
                "SYSTEM",
                "cold",
                text);
        }

        private static object CreateEntry(string sourceId, int lineIndex, AudioClip clip)
        {
            return Activator.CreateInstance(
                RuntimeType("_Project.Scripts.Systems.AudioSystem.VoiceLineCatalogEntry"),
                $"VO_{sourceId}_{lineIndex:000}",
                "STORY",
                sourceId,
                lineIndex,
                "SYSTEM",
                "cold",
                clip);
        }

        private static object CreateSkip(string sourceId, int lineIndex)
        {
            return Activator.CreateInstance(
                RuntimeType("_Project.Scripts.Systems.AudioSystem.VoiceLineCatalogSkip"),
                sourceId,
                lineIndex);
        }

        private static Array ToRuntimeArray(IEnumerable<object> values, string typeName)
        {
            object[] items = values.ToArray();
            Array array = Array.CreateInstance(RuntimeType(typeName), items.Length);
            for (int index = 0; index < items.Length; index++)
            {
                array.SetValue(items[index], index);
            }

            return array;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent);
            return buttonObject.AddComponent<Button>();
        }

        private static AudioClip CreateClip(string name)
        {
            return AudioClip.Create(name, 24000, 1, 24000, false);
        }

        private static Type RuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, $"Could not find runtime type {fullName}.");
            return type;
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            return GetMethod(target.GetType(), methodName, arguments.Length).Invoke(target, arguments);
        }

        private static MethodInfo GetMethod(Type type, string methodName, int parameterCount)
        {
            MethodInfo method = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SingleOrDefault(candidate =>
                    candidate.Name == methodName
                    && candidate.GetParameters().Length == parameterCount);
            Assert.That(method, Is.Not.Null, $"Missing {type.FullName}.{methodName}/{parameterCount}.");
            return method;
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return (T)property.GetValue(target);
        }
    }
}
