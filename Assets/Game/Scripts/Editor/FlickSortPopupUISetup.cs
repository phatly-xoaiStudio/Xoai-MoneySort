#if UNITY_EDITOR
using FlickSort.Data;
using FlickSort.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.Editor
{
    [InitializeOnLoad]
    internal static class FlickSortPopupUISetup
    {
        private const string LevelUpPrefabPath = "Assets/Game/Prefabs/UI/LevelUpUI.prefab";
        private const string LosePrefabPath = "Assets/Game/Prefabs/UI/LoseUI.prefab";
        private const string GameplayPrefabPath = "Assets/Game/Prefabs/UI/GameplayUI.prefab";
        private const string DefinitionPath = "Assets/Game/Data/UIDefinitionSO.asset";
        private const string ChipPrefabPath = "Assets/Game/Prefabs/Gameplay/Chip.prefab";
        private const string SunburstSpritePath = "Assets/Game/Sprite/VFX/sunburst_yellowtransparent.png";
        private const string RewardRenderTexturePath = "Assets/Game/VFX/FlickSort/RewardChipPreview.renderTexture";
        private const string FontGuid = "3577af8a805888344b4b32e2be5e8e9b";

        static FlickSortPopupUISetup()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            BuildIfNeeded();
        }

        [MenuItem("Flick Sort/Setup Popup UI")]
        public static void Build()
        {
            var levelUp = BuildLevelUpPrefab();
            var lose = BuildLosePrefab();
            EnsureAnimation(LevelUpPrefabPath);
            EnsureAnimation(LosePrefabPath);
            EnsureAnimation(GameplayPrefabPath);
            EnsureButtonAnimation(GameplayPrefabPath, "Button");
            EnsureButtonAnimation(LosePrefabPath, "RetryButton");
            levelUp = AssetDatabase.LoadAssetAtPath<GameObject>(LevelUpPrefabPath).GetComponent<LevelUpUI>();
            lose = AssetDatabase.LoadAssetAtPath<GameObject>(LosePrefabPath).GetComponent<LoseUI>();
            Register(UIEnum.LEVEL_UP_UI, levelUp);
            Register(UIEnum.LOSE_UI, lose);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildIfNeeded()
        {
            var levelUpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelUpPrefabPath);
            var losePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LosePrefabPath);
            if (levelUpPrefab == null || levelUpPrefab.GetComponent<LevelUpUI>() == null ||
                losePrefab == null || losePrefab.GetComponent<LoseUI>() == null ||
                !IsRegistered(UIEnum.LEVEL_UP_UI) ||
                !IsRegistered(UIEnum.LOSE_UI))
            {
                Build();
                return;
            }

            EnsureAnimation(LevelUpPrefabPath);
            EnsureAnimation(LosePrefabPath);
            EnsureAnimation(GameplayPrefabPath);
            EnsureButtonAnimation(GameplayPrefabPath, "Button");
            EnsureButtonAnimation(LosePrefabPath, "RetryButton");
            AssetDatabase.SaveAssets();
        }

        private static void EnsureButtonAnimation(string prefabPath, string buttonName)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var changed = false;
            Button targetButton = null;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                {
                    targetButton = button;
                    break;
                }
            }

            if (targetButton != null)
            {
                var animation = targetButton.GetComponent<ButtonScaleAnimation>();
                if (animation == null)
                {
                    animation = targetButton.gameObject.AddComponent<ButtonScaleAnimation>();
                    changed = true;
                }

                var serialized = new SerializedObject(animation);
                var targetProperty = serialized.FindProperty("target");
                if (targetProperty.objectReferenceValue != targetButton.transform)
                {
                    targetProperty.objectReferenceValue = targetButton.transform;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void EnsureAnimation(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var changed = false;

            var canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = root.AddComponent<CanvasGroup>();
                changed = true;
            }

            var animation = root.GetComponent<ScaleFadeUIAnimation>();
            if (animation == null)
            {
                animation = root.AddComponent<ScaleFadeUIAnimation>();
                changed = true;
            }

            var view = root.GetComponent<UIBase>();
            var viewSerialized = new SerializedObject(view);
            var animationProperty = viewSerialized.FindProperty("_animation");
            if (animationProperty.objectReferenceValue != animation)
            {
                animationProperty.objectReferenceValue = animation;
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            var animationSerialized = new SerializedObject(animation);
            var targetProperty = animationSerialized.FindProperty("target");
            var canvasGroupProperty = animationSerialized.FindProperty("canvasGroup");
            if (targetProperty.objectReferenceValue != root.transform ||
                canvasGroupProperty.objectReferenceValue != canvasGroup)
            {
                targetProperty.objectReferenceValue = root.transform;
                canvasGroupProperty.objectReferenceValue = canvasGroup;
                animationSerialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static LevelUpUI BuildLevelUpPrefab()
        {
            PrepareSunburstSprite();
            var chipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChipPrefabPath);
            var sunburstSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SunburstSpritePath);
            var previewTexture = GetOrCreateRewardRenderTexture();
            if (chipPrefab == null)
                throw new MissingReferenceException($"Chip prefab was not found at {ChipPrefabPath}.");
            if (sunburstSprite == null)
                throw new MissingReferenceException(
                    $"Sunburst sprite was not found or is not imported as Sprite at {SunburstSpritePath}.");

            var root = CreateOverlay("LevelUpUI");
            var card = CreateImage("Card", root.transform, new Color(1f, 0.62f, 0.08f, 1f));
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 760f));

            var title = CreateText("Title", card.transform, "LEVEL 2!", 72f);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f),
                Vector2.zero, new Vector2(620f, 130f));

            var confirmIndicator = CreateText(
                "ConfirmIndicator",
                card.transform,
                "Tap anywhere to continue",
                48f);
            confirmIndicator.color = Color.white;
            SetRect(
                confirmIndicator.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 85f),
                new Vector2(620f, 100f));

            var rewardRoot = new GameObject("UnlockReward", typeof(RectTransform));
            rewardRoot.layer = LayerMask.NameToLayer("UI");
            rewardRoot.transform.SetParent(card.transform, false);
            SetRect(
                rewardRoot.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.43f),
                new Vector2(0.5f, 0.43f),
                Vector2.zero,
                new Vector2(600f, 480f));

            var rotatingGlow = new GameObject("RotatingGlow", typeof(RectTransform));
            rotatingGlow.layer = LayerMask.NameToLayer("UI");
            rotatingGlow.transform.SetParent(rewardRoot.transform, false);
            SetRect(
                rotatingGlow.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(500f, 500f));
            var sunburst = CreateImage("Sunburst", rotatingGlow.transform, new Color(1f, 1f, 1f, 0.78f));
            sunburst.sprite = sunburstSprite;
            sunburst.type = Image.Type.Simple;
            sunburst.preserveAspect = true;
            SetStretch(sunburst.rectTransform, Vector2.zero, Vector2.zero);

            var previewImageObject = new GameObject(
                "Chip3DPreview",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            previewImageObject.layer = LayerMask.NameToLayer("UI");
            previewImageObject.transform.SetParent(rewardRoot.transform, false);
            var previewImage = previewImageObject.GetComponent<RawImage>();
            previewImage.texture = previewTexture;
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;
            SetRect(
                previewImage.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(330f, 330f));

            var previewStage = new GameObject("RewardPreviewStage");
            previewStage.layer = 31;
            previewStage.transform.SetParent(rewardRoot.transform, false);
            previewStage.transform.position = new Vector3(5000f, 5000f, 5000f);

            var chipPivot = new GameObject("ChipTilt").transform;
            chipPivot.gameObject.layer = 31;
            chipPivot.SetParent(previewStage.transform, false);
            chipPivot.localRotation = Quaternion.Euler(18f, 0f, 0f);

            var chipInstance = (GameObject)PrefabUtility.InstantiatePrefab(chipPrefab, chipPivot);
            chipInstance.name = "UnlockedChip3D";
            chipInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            SetLayerRecursively(chipInstance, 31);
            var chipView = chipInstance.GetComponent<ChipView>();

            var cameraObject = new GameObject("RewardPreviewCamera", typeof(Camera));
            cameraObject.layer = 31;
            cameraObject.transform.SetParent(previewStage.transform, false);
            var previewCamera = cameraObject.GetComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.cullingMask = 1 << 31;
            previewCamera.targetTexture = previewTexture;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = true;
            previewCamera.orthographic = true;
            FramePreviewCamera(previewCamera, chipInstance.GetComponentsInChildren<Renderer>(true));

            var view = root.AddComponent<LevelUpUI>();
            SetReference(view, "_levelText", title);
            SetReference(view, "_unlockRewardRoot", rewardRoot);
            SetReference(view, "_rotatingGlow", rotatingGlow.GetComponent<RectTransform>());
            SetReference(view, "_chipPreviewImage", previewImage);
            SetReference(view, "_chipPreviewCamera", previewCamera);
            SetReference(view, "_unlockChipPivot", chipPivot);
            SetReference(view, "_unlockChipView", chipView);
            SetReference(view, "_confirmIndicator", confirmIndicator);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, LevelUpPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LevelUpUI>();
        }

        private static void PrepareSunburstSprite()
        {
            if (AssetImporter.GetAtPath(SunburstSpritePath) is not TextureImporter importer)
                return;

            var changed =
                importer.textureType != TextureImporterType.Sprite ||
                !importer.alphaIsTransparency ||
                importer.mipmapEnabled;
            if (!changed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static RenderTexture GetOrCreateRewardRenderTexture()
        {
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RewardRenderTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
                {
                    name = "RewardChipPreview",
                    antiAliasing = 4,
                    useMipMap = false,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Bilinear
                };
                AssetDatabase.CreateAsset(texture, RewardRenderTexturePath);
            }

            return texture;
        }

        private static void FramePreviewCamera(Camera camera, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var largestExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);
            camera.orthographicSize = Mathf.Max(0.1f, largestExtent * 1.45f);
            camera.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y,
                bounds.min.z - Mathf.Max(2f, bounds.size.z * 3f));
            camera.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Mathf.Max(10f, bounds.size.z * 8f);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        // private static bool HasLevelUpReward(GameObject prefab)
        // {
        //     var view = prefab != null ? prefab.GetComponent<LevelUpUI>() : null;
        //     if (view == null)
        //         return false;
        //
        //     var serialized = new SerializedObject(view);
        //     return serialized.FindProperty("_unlockRewardRoot").objectReferenceValue != null &&
        //            serialized.FindProperty("_rotatingGlow").objectReferenceValue != null &&
        //            serialized.FindProperty("_chipPreviewCamera").objectReferenceValue != null &&
        //            serialized.FindProperty("_unlockChipView").objectReferenceValue != null &&
        //            serialized.FindProperty("_confirmIndicator").objectReferenceValue != null;
        // }

        private static LoseUI BuildLosePrefab()
        {
            var root = CreateOverlay("LoseUI");
            var card = CreateImage("Card", root.transform, new Color(0.16f, 0.19f, 0.25f, 1f));
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760f, 430f));

            var title = CreateText("Title", card.transform, "NO MORE SLOTS", 62f);
            title.color = new Color(1f, 0.85f, 0.24f);
            title.fontStyle = FontStyles.Bold;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f),
                Vector2.zero, new Vector2(660f, 150f));

            var retryObject = CreateImage("RetryButton", card.transform, new Color(0.05f, 0.78f, 0.4f, 1f));
            SetRect(retryObject.rectTransform, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f),
                Vector2.zero, new Vector2(360f, 120f));
            var retryButton = retryObject.gameObject.AddComponent<Button>();
            retryButton.targetGraphic = retryObject;

            var retryText = CreateText("Label", retryObject.transform, "RETRY", 48f);
            retryText.color = Color.white;
            retryText.fontStyle = FontStyles.Bold;
            SetStretch(retryText.rectTransform, Vector2.zero, Vector2.zero);

            var view = root.AddComponent<LoseUI>();
            SetReference(view, "_titleText", title);
            SetReference(view, "_retryButton", retryButton);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, LosePrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LoseUI>();
        }

        private static GameObject CreateOverlay(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.layer = LayerMask.NameToLayer("UI");
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = root.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.64f);
            image.raycastTarget = true;
            return root;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.layer = LayerMask.NameToLayer("UI");
            child.transform.SetParent(parent, false);
            var image = child.GetComponent<Image>();
            image.color = color;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            child.layer = LayerMask.NameToLayer("UI");
            child.transform.SetParent(parent, false);
            var text = child.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            var fontPath = AssetDatabase.GUIDToAssetPath(FontGuid);
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsRegistered(UIEnum name)
        {
            var definition = AssetDatabase.LoadAssetAtPath<UIDefinitionSO>(DefinitionPath);
            if (definition == null)
                return false;

            foreach (var item in definition.Definitions)
            {
                if (item != null && item.Name == name && item.UI != null)
                    return true;
            }

            return false;
        }

        private static void Register(UIEnum name, UIBase view)
        {
            var definition = AssetDatabase.LoadAssetAtPath<UIDefinitionSO>(DefinitionPath);
            var serialized = new SerializedObject(definition);
            var definitions = serialized.FindProperty("definitions");

            for (var i = 0; i < definitions.arraySize; i++)
            {
                var item = definitions.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("Name").enumValueIndex != (int)name)
                    continue;

                item.FindPropertyRelative("UI").objectReferenceValue = view;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                return;
            }

            definitions.InsertArrayElementAtIndex(definitions.arraySize);
            var added = definitions.GetArrayElementAtIndex(definitions.arraySize - 1);
            added.FindPropertyRelative("Name").enumValueIndex = (int)name;
            added.FindPropertyRelative("UI").objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
#endif
