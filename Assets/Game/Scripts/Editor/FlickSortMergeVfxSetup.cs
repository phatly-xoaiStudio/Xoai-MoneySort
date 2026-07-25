#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    internal static class FlickSortMergeVfxSetup
    {
        private const string PrefabPath = "Assets/Game/Prefabs/VFX/MergeBurst.prefab";
        private const string MaterialPath = "Assets/Game/VFX/FlickSort/Materials/MergeBurst.mat";
        private const string ScenePath = "Assets/Scenes/FlickSort.unity";
        private const string MarkerPath = "Assets/Game/.merge-vfx-setup-v3";

        static FlickSortMergeVfxSetup()
        {
            EditorApplication.delayCall += BuildIfNeeded;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += BuildIfNeeded;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Flick Sort/Setup Merge VFX %#m")]
        public static void Build()
        {
            EnsureFolder(Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(MaterialPath)?.Replace('\\', '/'));

            var material = CreateMaterial();
            var prefab = CreatePrefab(material);
            WireScene(prefab.GetComponent<ParticleSystem>());
            File.WriteAllText(MarkerPath, "Merge firework VFX prefab setup version 3.\n");
            AssetDatabase.ImportAsset(MarkerPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Flick Sort merge VFX prefab created and assigned.");
        }

        private static void BuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!File.Exists(MarkerPath) ||
                prefab == null ||
                prefab.GetComponent<ParticleSystem>() == null ||
                !SceneHasPrefabReference())
                Build();
        }

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                throw new System.InvalidOperationException("URP Particles/Unlit shader was not found.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MergeBurst" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePrefab(Material material)
        {
            var root = new GameObject("MergeBurst");
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.1f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.05f),
                Color.white);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(1.45f, 2.1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 30)
            });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alpha = new Gradient();
            alpha.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = alpha;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.12f, 1f),
                    new Keyframe(1f, 0f)));

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static bool SceneHasPrefabReference()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                return false;

            var prefabParticles = prefab.GetComponent<ParticleSystem>();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            foreach (var root in scene.GetRootGameObjects())
            {
                var feedback = root.GetComponentInChildren<FlickSortFeedbackManager>(true);
                if (feedback == null)
                    continue;

                var serialized = new SerializedObject(feedback);
                return serialized.FindProperty("mergeBurstPrefab").objectReferenceValue == prefabParticles;
            }

            return false;
        }

        private static void WireScene(ParticleSystem prefab)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var feedback = root.GetComponentInChildren<FlickSortFeedbackManager>(true);
                    if (feedback == null)
                        continue;

                    var serialized = new SerializedObject(feedback);
                    serialized.FindProperty("mergeBurstPrefab").objectReferenceValue = prefab;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(feedback);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Selection.activeGameObject = feedback.gameObject;
                    EditorGUIUtility.PingObject(prefab);
                    return;
                }

                Debug.LogError($"FlickSortFeedbackManager was not found in {ScenePath}.");
            }
            finally
            {
                if (openedForSetup)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
