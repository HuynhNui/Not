#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace _Project.Editor
{
    [InitializeOnLoad]
    public static class MobileReadinessTestRunner
    {
        private const string ModeKey = "TrueGate.MobileReadiness.TestMode";
        private const string OutputDirectory = "Temp/MobileReadiness";
        private static readonly TestRunnerApi Api;

        static MobileReadinessTestRunner()
        {
            Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            Api.RegisterCallbacks(new ResultCallbacks());
        }

        public static void RunEditMode()
        {
            Run(TestMode.EditMode);
        }

        public static void RunPlayMode()
        {
            Run(TestMode.PlayMode);
        }

        private static void Run(TestMode mode)
        {
            Directory.CreateDirectory(OutputDirectory);
            EditorPrefs.SetString(ModeKey, mode.ToString());
            Api.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode
            }));
        }

        private sealed class ResultCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string mode = EditorPrefs.GetString(ModeKey, "Unknown");
                var failures = new List<string>();
                CollectFailures(result, failures);

                var report = new StringBuilder();
                report.AppendLine($"Mode={mode}");
                report.AppendLine($"Pass={result.PassCount}");
                report.AppendLine($"Fail={result.FailCount}");
                report.AppendLine($"Skip={result.SkipCount}");
                report.AppendLine($"Inconclusive={result.InconclusiveCount}");
                report.AppendLine($"Duration={result.Duration:0.###}");
                report.AppendLine("Failures:");
                foreach (string failure in failures)
                {
                    report.AppendLine(failure);
                }

                File.WriteAllText(
                    Path.Combine(OutputDirectory, $"{mode}-results.txt"),
                    report.ToString(),
                    new UTF8Encoding(false));
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }

            private static void CollectFailures(ITestResultAdaptor result, ICollection<string> failures)
            {
                if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                {
                    string message = string.IsNullOrWhiteSpace(result.Message)
                        ? "No failure message."
                        : result.Message.Replace('\r', ' ').Replace('\n', ' ');
                    failures.Add($"{result.FullName} | {message}");
                }

                foreach (ITestResultAdaptor child in result.Children)
                {
                    CollectFailures(child, failures);
                }
            }
        }
    }
}
#endif
