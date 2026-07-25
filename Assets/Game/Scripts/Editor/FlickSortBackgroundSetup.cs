#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    internal static class FlickSortBackgroundSetup
    {
        private const string ScenePath = "Assets/Scenes/FlickSort.unity";
        private const string SpritePath = "Assets/Game/Sprite/Background/felt_green.jpg";
        private const string BackgroundName = "PokerBackground";

        static FlickSortBackgroundSetup()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Flick Sort/Setup Poker Background")]
        public static void Build()
        {
            PrepareSprite();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
                throw new MissingReferenceException(
                    $"Poker background sprite was not found at {SpritePath}.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var camera = FindMainCamera(scene);
                if (camera == null)
                    throw new MissingReferenceException(
                        $"Main Camera was not found in {ScenePath}.");

                var background = FindBackground(scene);
                if (background == null)
                {
                    background = new GameObject(BackgroundName);
                    SceneManager.MoveGameObjectToScene(background, scene);
                }

                var renderer = background.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = background.AddComponent<SpriteRenderer>();

                renderer.sprite = sprite;
                renderer.sortingOrder = -100;
                renderer.color = new Color(1.08f, 1.12f, 1.04f, 1f);

                var transform = background.transform;
                transform.SetPositionAndRotation(
                    new Vector3(
                        camera.transform.position.x,
                        camera.transform.position.y,
                        10f),
                    Quaternion.identity);
                transform.localScale = CalculateCoverScale(camera, sprite);

                EditorUtility.SetDirty(renderer);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = background;
                Debug.Log("Flick Sort poker felt background created behind the 3D tray.");
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

            PrepareSprite();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedForCheck = !scene.IsValid() || !scene.isLoaded;
            if (openedForCheck)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var background = FindBackground(scene);
                var renderer = background != null
                    ? background.GetComponent<SpriteRenderer>()
                    : null;
                var expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
                if (renderer == null || renderer.sprite != expectedSprite)
                    Build();
            }
            finally
            {
                if (openedForCheck && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void PrepareSprite()
        {
            if (AssetImporter.GetAtPath(SpritePath) is not TextureImporter importer)
                return;

            var changed =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp;
            if (!changed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Vector3 CalculateCoverScale(Camera camera, Sprite sprite)
        {
            var visibleHeight = camera.orthographicSize * 2f;
            var visibleWidth = visibleHeight * camera.aspect;
            var scale = Mathf.Max(
                visibleWidth / sprite.bounds.size.x,
                visibleHeight / sprite.bounds.size.y);
            return Vector3.one * scale * 1.02f;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.CompareTag("MainCamera"))
                        return camera;
                }
            }

            return null;
        }

        private static GameObject FindBackground(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == BackgroundName)
                    return root;
            }

            return null;
        }
    }
}
#endif
