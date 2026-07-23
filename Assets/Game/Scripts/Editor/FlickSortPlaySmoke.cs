using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    public static class FlickSortPlaySmoke
    {
        private const string ScenePath = "Assets/Scenes/FlickSort.unity";
        private const string MarkerPath = "Assets/Game/Data/CorePlaySmokeVersion2.txt";
        private const string SessionKey = "FlickSort.PlaySmoke.Running.v2";
        private static double _enteredPlayTime;

        static FlickSortPlaySmoke()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnUpdate;
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            if (Application.isBatchMode || File.Exists(MarkerPath) || SessionState.GetBool(SessionKey, false))
                return;

            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(SessionKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
                _enteredPlayTime = EditorApplication.timeSinceStartup;

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (scene.isLoaded && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);

                File.WriteAllText(MarkerPath, "Flick Sort Play Mode smoke completed.\n");
                AssetDatabase.ImportAsset(MarkerPath);
                SessionState.SetBool(SessionKey, false);
                Debug.Log("Flick Sort Play Mode smoke completed after 6 seconds.");
            }
        }

        private static void OnUpdate()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(SessionKey, false) || _enteredPlayTime <= 0d)
                return;
            if (EditorApplication.timeSinceStartup - _enteredPlayTime >= 6d)
                EditorApplication.isPlaying = false;
        }
    }
}
