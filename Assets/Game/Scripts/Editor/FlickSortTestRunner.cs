using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    public static class FlickSortTestRunner
    {
        private const string SessionKey = "FlickSort.CoreTests.Run.v4";
        private static TestRunnerApi _api;
        private static TestCallbacks _callbacks;

        static FlickSortTestRunner()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("Flick Sort/Run Core EditMode Tests")]
        public static void RunTests()
        {
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new TestCallbacks();
            _api.RegisterCallbacks(_callbacks);
            _api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "FlickSort.EditModeTests" }
            }));
        }

        private static void RunOnce()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);
            RunTests();
        }

        private sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log($"Flick Sort EditMode tests finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
                if (result.FailCount > 0)
                    Debug.LogError(result.Message);
            }
        }
    }
}
