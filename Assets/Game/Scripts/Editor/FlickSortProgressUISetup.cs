#if UNITY_EDITOR
using System.IO;
using FlickSort.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    internal static class FlickSortProgressUISetup
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/GameplayUI.prefab";
        private const string MarkerPath = "Assets/Game/.progress-ui-setup-v2";
        private const string ScenePath = "Assets/Scenes/FlickSort.unity";
        private const string ProgressSoundPath =
            "Assets/Game/Sprite/KenneyUIPack/Sounds/tap-b.ogg";
        private const int StarCount = 10;

        static FlickSortProgressUISetup()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Flick Sort/Setup Progress Star UI %#g")]
        public static void Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var gameplayUI = root.GetComponent<GameplayUI>();
                var progressBorder = root.transform.Find("ProgressionBoarder") as RectTransform;
                var levelStar = progressBorder?.Find("Image")?.GetComponent<Image>();
                if (gameplayUI == null || progressBorder == null || levelStar == null || levelStar.sprite == null)
                    throw new MissingReferenceException("Gameplay progress UI or level star sprite is missing.");

                var oldLayer = root.transform.Find("StarFxLayer");
                if (oldLayer != null)
                    Object.DestroyImmediate(oldLayer.gameObject);

                var layer = CreateRect("StarFxLayer", root.transform);
                Stretch(layer);
                layer.SetAsLastSibling();

                var spawnPoint = CreateRect("StarSpawnPoint", layer);
                spawnPoint.anchorMin = spawnPoint.anchorMax = new Vector2(0.5f, 0.5f);
                spawnPoint.anchoredPosition = new Vector2(0f, -80f);
                spawnPoint.sizeDelta = Vector2.zero;

                var stars = new Image[StarCount];
                for (var i = 0; i < StarCount; i++)
                {
                    var starRect = CreateRect($"FlyingStar_{i + 1:00}", layer);
                    starRect.anchorMin = starRect.anchorMax = new Vector2(0.5f, 0.5f);
                    starRect.sizeDelta = new Vector2(86f, 86f);
                    var image = starRect.gameObject.AddComponent<Image>();
                    image.sprite = levelStar.sprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.gameObject.SetActive(false);
                    stars[i] = image;
                }

                var serialized = new SerializedObject(gameplayUI);
                serialized.FindProperty("_starSpawnPoint").objectReferenceValue = spawnPoint;
                serialized.FindProperty("_starTarget").objectReferenceValue = progressBorder;
                var starArray = serialized.FindProperty("_flyingStars");
                starArray.arraySize = stars.Length;
                for (var i = 0; i < stars.Length; i++)
                    starArray.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
                serialized.FindProperty("_starFlyDuration").floatValue = 0.5f;
                serialized.FindProperty("_starStagger").floatValue = 0.06f;
                serialized.FindProperty("_starSpawnSpread").vector2Value = new Vector2(150f, 80f);
                serialized.FindProperty("_starAppearDuration").floatValue = 0.12f;
                serialized.FindProperty("_targetPunchScale").floatValue = 0.12f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            WireProgressSound();
            File.WriteAllText(MarkerPath, "Progress star UI and sound setup version 2.\n");
            AssetDatabase.ImportAsset(MarkerPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Flick Sort progress star UI setup complete.");
        }

        private static void BuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var view = prefab != null ? prefab.GetComponent<GameplayUI>() : null;
            var serialized = view != null ? new SerializedObject(view) : null;
            var stars = serialized?.FindProperty("_flyingStars");
            if (!File.Exists(MarkerPath) ||
                prefab == null ||
                prefab.transform.Find("StarFxLayer") == null ||
                stars == null ||
                stars.arraySize != StarCount)
                Build();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = LayerMask.NameToLayer("UI");
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void WireProgressSound()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProgressSoundPath);
            if (clip == null)
                throw new MissingReferenceException($"Progress sound is missing: {ProgressSoundPath}");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var soundManager = root.GetComponentInChildren<FlickSortSoundManager>(true);
                    if (soundManager == null)
                        continue;

                    var serialized = new SerializedObject(soundManager);
                    serialized.FindProperty("progressStarSound").objectReferenceValue = clip;
                    serialized.FindProperty("progressStarSoundMinInterval").floatValue = 0.04f;
                    serialized.FindProperty("progressStarSoundVolume").floatValue = 0.45f;
                    serialized.FindProperty("progressStarSoundPitchRange").vector2Value =
                        new Vector2(1f, 1.35f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(soundManager);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    return;
                }

                throw new MissingReferenceException($"FlickSortSoundManager was not found in {ScenePath}.");
            }
            finally
            {
                if (openedForSetup)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
