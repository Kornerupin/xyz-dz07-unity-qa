#if UNITY_EDITOR
using System.IO;
using System.Text;
using EchoesOfAscent.Tests.PlayMode;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EchoesOfAscent.Editor
{
    public static class BddPlayModeTestRunner
    {
        const string ResultsPath = "Logs/playmode-bdd-results.txt";

        [MenuItem("Ascent Tools/Run PlayMode BDD Tests")]
        public static void RunFromMenu()
        {
            RunTests();
        }

        public static void RunTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ResultsCallbacks());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = new[]
                {
                    typeof(GddPositiveBddIntegrationTests).FullName
                }
            }));
        }

        sealed class ResultsCallbacks : ICallbacks
        {
            readonly StringBuilder report = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun)
            {
                report.Clear();
                report.AppendLine("PlayMode BDD test run started.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                report.AppendLine($"Passed: {result.PassCount}");
                report.AppendLine($"Failed: {result.FailCount}");
                report.AppendLine($"Skipped: {result.SkipCount}");
                report.AppendLine($"Total: {result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount}");
                report.AppendLine();

                AppendFailures(result);

                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultsPath, report.ToString());
                Debug.Log($"PlayMode BDD tests finished. Report: {ResultsPath}");
            }

            void AppendFailures(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                {
                    foreach (ITestResultAdaptor child in result.Children)
                        AppendFailures(child);
                    return;
                }

                if (result.TestStatus == TestStatus.Passed)
                    return;

                report.AppendLine($"{result.FullName}");
                report.AppendLine($"Status: {result.TestStatus}");
                report.AppendLine(result.Message);
                report.AppendLine(result.StackTrace);
                report.AppendLine();
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
#endif
