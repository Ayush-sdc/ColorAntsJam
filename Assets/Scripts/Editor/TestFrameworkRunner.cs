#if UNITY_EDITOR
using System;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using EditorTestStatus = UnityEditor.TestTools.TestRunner.Api.TestStatus;

public static class TestFrameworkRunner
{
    private class TestCallbackReceiver : ICallbacks
    {
        public int PassedCount = 0;
        public int FailedCount = 0;
        public int TotalCount = 0;
        public bool IsFinished = false;

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"[TestRunnerApi] Run Started: {testsToRun.Name} ({testsToRun.TestCaseCount} test cases)");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            IsFinished = true;
            Debug.Log($"[TestRunnerApi] ==========================================");
            Debug.Log($"[TestRunnerApi] Run Finished: Result = {result.TestStatus}");
            Debug.Log($"[TestRunnerApi] Total Tests: {result.PassCount + result.FailCount + result.SkipCount} (Passed: {result.PassCount}, Failed: {result.FailCount}, Inconclusive: {result.InconclusiveCount})");
            Debug.Log($"[TestRunnerApi] ==========================================");
        }

        public void TestStarted(ITestAdaptor test)
        {
            if (!test.IsSuite)
            {
                Debug.Log($"[TestRunnerApi]   -> Running: {test.FullName}");
            }
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
            {
                TotalCount++;
                if (result.TestStatus == EditorTestStatus.Passed)
                {
                    PassedCount++;
                    Debug.Log($"[TestRunnerApi]   [PASS] {result.Test.Name} ({result.Duration:F3}s)");
                }
                else
                {
                    FailedCount++;
                    Debug.LogError($"[TestRunnerApi]   [FAIL] {result.Test.Name} - {result.Message}\n{result.StackTrace}");
                }
            }
        }
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run EditMode Tests")]
    public static void RunEditModeTests()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        var filter = new Filter()
        {
            testMode = TestMode.EditMode
        };
        var receiver = new TestCallbackReceiver();
        testRunnerApi.RegisterCallbacks(receiver);
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run PlayMode Tests")]
    public static void RunPlayModeTests()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        var filter = new Filter()
        {
            testMode = TestMode.PlayMode
        };
        var receiver = new TestCallbackReceiver();
        testRunnerApi.RegisterCallbacks(receiver);
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }
}
#endif
