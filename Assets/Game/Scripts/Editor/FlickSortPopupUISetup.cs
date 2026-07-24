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
            var root = CreateOverlay("LevelUpUI");
            var card = CreateImage("Card", root.transform, new Color(1f, 0.62f, 0.08f, 1f));
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 280f));

            var title = CreateText("Title", card.transform, "LEVEL 2!", 72f);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;
            SetStretch(title.rectTransform, new Vector2(55f, 40f), new Vector2(-55f, -40f));

            var view = root.AddComponent<LevelUpUI>();
            SetReference(view, "_titleText", title);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, LevelUpPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LevelUpUI>();
        }

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
