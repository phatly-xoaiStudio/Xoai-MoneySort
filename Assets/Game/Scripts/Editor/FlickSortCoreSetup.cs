using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    public static class FlickSortCoreSetup
    {
        private const string ConfigPath = "Assets/Game/Data/FlickSortGameConfig.asset";
        private const string ScenePath = "Assets/Scenes/FlickSort.unity";
        private const string VersionMarkerPath = "Assets/Game/Data/CoreSetupVersion4.txt";

        static FlickSortCoreSetup()
        {
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("Flick Sort/Build Core Gameplay")]
        public static void BuildCoreGameplay()
        {
            EnsureFolder("Assets/Game/Data");
            var config = CreateOrUpdateConfig();
            CreateGameplayScene(config);
            UpdateBuildSettings();
            File.WriteAllText(VersionMarkerPath, "Flick Sort core setup version 2\n");
            AssetDatabase.ImportAsset(VersionMarkerPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Flick Sort core gameplay setup complete: Assets/Scenes/FlickSort.unity");
        }

        private static void RunOnce()
        {
            if (Application.isBatchMode || File.Exists(VersionMarkerPath))
                return;
            BuildCoreGameplay();
        }

        private static FlickSortGameConfig CreateOrUpdateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<FlickSortGameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FlickSortGameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.mergeChipCount = 10;
            config.maxChipLevel = 10;
            config.stackCapacity = 10;
            config.defaultDealChipCount = 10;
            config.randomChipsPerStack = new Vector2Int(1, 3);
            config.chipSpacing = 0.11f;
            config.stackSpacing = 1.45f;
            config.moveDuration = 0.28f;
            config.dealDuration = 0.34f;
            config.mergeDuration = 0.22f;
            config.chipMoveDelay = 0.045f;
            config.jumpPower = 0.75f;
            config.hammerFlyDuration = 0.5f;
            config.hammerFlyDistance = 1.25f;
            config.hammerFlyStagger = 0.025f;

            config.levels = new List<LevelSettings>();
            for (var level = 1; level <= 10; level++)
            {
                var settings = LevelSettings.Default(level);
                settings.openedStackCount = 20;
                settings.colorCount = level < 4 ? 3 : level < 8 ? 4 : 5;
                settings.initialChipCount = 22 + level * 2;
                settings.dealChipCount = 7 + level;
                settings.requiredChipScore = (4 + level * 2) * config.mergeChipCount;
                settings.chipsPerStackRange = level < 5 ? new Vector2Int(1, 2) : new Vector2Int(1, 3);
                settings.randomSeed = 1709 + level * 101;
                config.levels.Add(settings);
            }
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void CreateGameplayScene(FlickSortGameConfig config)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "FlickSort";
            var bootstrapObject = new GameObject("FlickSortBootstrap");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            var bootstrap = bootstrapObject.AddComponent<FlickSortBootstrap>();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.FindProperty("chipPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Gameplay/Chip.prefab");
            serialized.FindProperty("chipTrayPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Gameplay/ChipTray.prefab");
            serialized.FindProperty("uiFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/UI/KenneyUIPack/Font/Kenney Future.ttf");
            serialized.FindProperty("moveSound").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Sound/kenney_casino-audio/Audio/chip-lay-1.ogg");
            serialized.FindProperty("mergeSound").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Sound/kenney_casino-audio/Audio/chips-collide-1.ogg");
            serialized.FindProperty("dealSound").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Sound/kenney_casino-audio/Audio/chips-handle-1.ogg");

            var materials = serialized.FindProperty("chipMaterials");
            materials.arraySize = 5;
            var names = new[] { "Red", "Blue", "Yellow", "Green", "Purple" };
            for (var i = 0; i < names.Length; i++)
            {
                materials.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>($"Assets/Game/3D/Materials/Chip_{names[i]}.mat");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void UpdateBuildSettings()
        {
            var result = new List<EditorBuildSettingsScene>
            {
                new(ScenePath, true)
            };
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath)
                    continue;
                result.Add(existing);
            }
            EditorBuildSettings.scenes = result.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
