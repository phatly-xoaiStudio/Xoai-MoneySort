#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    internal static class FlickSortAudioSetup
    {
        private const string ScenePath = "Assets/Scenes/FlickSort.unity";
        private const string BgmPath = "Assets/Game/Sound/Pixabay/LasVegasCasinoMusic_MFCC_385955.mp3";
        private const string LevelUpPath = "Assets/Game/Sound/Pixabay/WinnerGameSound_PuyoPuyoMegaFan1234_404167.mp3";
        private const string LosePath = "Assets/Game/Sound/Pixabay/GameOver39_TuomasData_199830.mp3";

        static FlickSortAudioSetup()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Flick Sort/Setup Casino Audio")]
        public static void Build()
        {
            var bgm = LoadClip(BgmPath);
            var levelUp = LoadClip(LevelUpPath);
            var lose = LoadClip(LosePath);

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var manager = FindSoundManager(scene);
                if (manager == null)
                    throw new MissingReferenceException(
                        $"{nameof(FlickSortSoundManager)} was not found in {ScenePath}.");

                var sources = manager.GetComponents<AudioSource>();
                var sfxSource = sources.Length > 0
                    ? sources[0]
                    : manager.gameObject.AddComponent<AudioSource>();
                var musicSource = sources.Length > 1
                    ? sources[1]
                    : manager.gameObject.AddComponent<AudioSource>();

                ConfigureSfxSource(sfxSource);
                ConfigureMusicSource(musicSource, bgm);

                var serialized = new SerializedObject(manager);
                serialized.FindProperty("musicSource").objectReferenceValue = musicSource;
                serialized.FindProperty("backgroundMusic").objectReferenceValue = bgm;
                serialized.FindProperty("levelUpSound").objectReferenceValue = levelUp;
                serialized.FindProperty("loseSound").objectReferenceValue = lose;
                serialized.FindProperty("musicVolume").floatValue = 0.28f;
                serialized.FindProperty("levelUpSoundVolume").floatValue = 0.75f;
                serialized.FindProperty("loseSoundVolume").floatValue = 0.8f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(sfxSource);
                EditorUtility.SetDirty(musicSource);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Flick Sort BGM, level-up and lose sounds assigned.");
            }
            finally
            {
                if (openedForSetup)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var expectedBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
            var expectedLevelUp = AssetDatabase.LoadAssetAtPath<AudioClip>(LevelUpPath);
            var expectedLose = AssetDatabase.LoadAssetAtPath<AudioClip>(LosePath);
            if (expectedBgm == null || expectedLevelUp == null || expectedLose == null)
                return;

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForCheck = !scene.IsValid() || !scene.isLoaded;
            if (openedForCheck)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var manager = FindSoundManager(scene);
                if (manager == null)
                {
                    Build();
                    return;
                }

                var serialized = new SerializedObject(manager);
                if (serialized.FindProperty("musicSource").objectReferenceValue == null ||
                    serialized.FindProperty("backgroundMusic").objectReferenceValue != expectedBgm ||
                    serialized.FindProperty("levelUpSound").objectReferenceValue != expectedLevelUp ||
                    serialized.FindProperty("loseSound").objectReferenceValue != expectedLose)
                    Build();
            }
            finally
            {
                if (openedForCheck && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static AudioClip LoadClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return clip != null
                ? clip
                : throw new MissingReferenceException($"Audio clip was not found at {path}.");
        }

        private static FlickSortSoundManager FindSoundManager(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var manager = root.GetComponentInChildren<FlickSortSoundManager>(true);
                if (manager != null)
                    return manager;
            }

            return null;
        }

        private static void ConfigureSfxSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private static void ConfigureMusicSource(AudioSource source, AudioClip bgm)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0.28f;
            source.clip = bgm;
        }
    }
}
#endif
