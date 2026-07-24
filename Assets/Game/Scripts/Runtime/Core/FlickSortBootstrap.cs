using DG.Tweening;
using System;
using System.Collections;
using FlickSort.Data;
using FlickSort.Core;
using FlickSort.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FlickSort
{
    public sealed class FlickSortBootstrap : MonoBehaviour
    {
        [SerializeField] private UIDefinitionSO uiDefinitionSo;
        [SerializeField] private ChipColorConfigSO chipColorConfigSo;
        [SerializeField] private Transform chipSpawner;
        [SerializeField] private Font uiFont;
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip mergeSound;
        [SerializeField] private AudioClip dealSound;
        [SerializeField] private float moveSoundMinInterval = 0.06f;
        [SerializeField, Range(0f, 1f)] private float moveSoundVolume = 0.5f;
        [SerializeField] private Vector2 moveSoundPitchRange = new(0.94f, 1.06f);

        [SerializeField] private FlickSortBoard _board;
        [SerializeField] private UIManager _uiManager;
        private GameplayUI _gameplayUI;
        private AudioSource _audioSource;
        private Material _particleMaterial;
        private float _nextMoveSoundTime;

        private void Awake()
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            SetUpCamera();
            // if (_board == null)
            //     _board = FindFirstObjectByType<FlickSortBoard>(FindObjectsInactive.Include);
            // if (_board == null)
            //     throw new InvalidOperationException("FlickSortBoard is missing from the scene.");
            // _board.gameObject.SetActive(false);
            // BuildUi();
            _uiManager.Init(uiDefinitionSo.Definitions);
            _board.Init(chipColorConfigSo, chipSpawner);
            _gameplayUI = _uiManager.GetUi(UIEnum.GAMEPLAY_UI) as GameplayUI;
            _uiManager.ShowUI(UIEnum.LOADING_UI, new object[]
            {
                (Action<Action>)InitializeGame,
                (Action<Action>)PreloadGame,
                (Action)FinishLoading
            });

        }

        private void OnEnable()
        {
            FlickSortEventBus.RequestDeal += OnDealRequested;
            FlickSortEventBus.RequestRetry += RetryLevel;
        }

        private void OnDisable()
        {
            FlickSortEventBus.RequestDeal -= OnDealRequested;
            FlickSortEventBus.RequestRetry -= RetryLevel;
        }

        private void InitializeGame(Action complete)
        {
            StartCoroutine(InitializeGameRoutine(complete));
        }

        private IEnumerator InitializeGameRoutine(Action complete)
        {
            yield return null;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _board.ProgressChanged += OnProgressChanged;
            _board.LevelUp += OnLevelUp;
            _board.LevelLost += OnLevelLost;
            _board.DealStarted += OnDealStarted;
            _board.ChipMoveLanded += OnChipMoveLanded;
            _board.MergeCompleted += OnMergeCompleted;
            _board.InvalidMove += OnInvalidMove;
            complete?.Invoke();
        }

        private void PreloadGame(Action complete)
        {
            StartCoroutine(PreloadGameRoutine(complete));
        }

        private IEnumerator PreloadGameRoutine(Action complete)
        {
            _board.gameObject.SetActive(true);
            // Keep loading visible while StartLevel creates the tray, stack colliders,
            // pooled chip views and the first deal over several rendered frames.
            yield return null;
            yield return null;
            var timeoutAt = Time.realtimeSinceStartup + 5f;
            while (_board != null && _board.IsBusy && Time.realtimeSinceStartup < timeoutAt)
                yield return null;
            complete?.Invoke();
        }

        private void FinishLoading()
        {
            _uiManager.HideUI(UIEnum.LOADING_UI);
            _uiManager.ShowUI(UIEnum.GAMEPLAY_UI);
        }

        private void OnDestroy()
        {
            if (_board == null)
                return;
            _board.ProgressChanged -= OnProgressChanged;
            _board.LevelUp -= OnLevelUp;
            _board.LevelLost -= OnLevelLost;
            _board.DealStarted -= OnDealStarted;
            _board.ChipMoveLanded -= OnChipMoveLanded;
            _board.MergeCompleted -= OnMergeCompleted;
            _board.InvalidMove -= OnInvalidMove;
            if (_particleMaterial != null)
                Destroy(_particleMaterial);
        }

        private void SetUpCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.transform.position = new Vector3(0f, 0f, -16f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 7.3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.38f, 0.76f);
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            if (FindFirstObjectByType<Light>() == null)
            {
                var light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("GameplayUI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            _uiManager = canvasObject.AddComponent<UIManager>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var gameplayRoot = new GameObject("GameplayUI", typeof(RectTransform));
            gameplayRoot.transform.SetParent(canvas.transform, false);
            Stretch(gameplayRoot.GetComponent<RectTransform>());
            _gameplayUI = gameplayRoot.AddComponent<GameplayUI>();

            var levelText = MakeText(gameplayRoot.transform, "LevelText", "LEVEL 1", 62, new Vector2(0.5f, 0.94f), new Vector2(420f, 100f));
            var progressBack = MakeImage(gameplayRoot.transform, "ProgressBack", new Color(0.15f, 0.18f, 0.24f, 0.9f), new Vector2(0.5f, 0.885f), new Vector2(720f, 70f));
            var progressFill = MakeImage(progressBack.transform, "Fill", new Color(0.35f, 0.88f, 0.20f), new Vector2(0f, 0.5f), new Vector2(700f, 52f));
            var fillRect = progressFill.rectTransform;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(-350f, 0f);
            var progressText = MakeText(progressBack.transform, "Value", "0 / 1", 34, new Vector2(0.5f, 0.5f), new Vector2(700f, 60f));

            var deal = MakeButton(gameplayRoot.transform, "DealButton", "DEAL", new Vector2(0.5f, 0.095f), new Vector2(500f, 150f), new Color(0.26f, 0.82f, 0.08f));
            // _gameplayUI.Configure(levelText, progressText, progressFill, deal);
            // _uiManager.Register(_gameplayUI);

            var levelUp = BuildPopup<LevelUpUI>(canvas.transform, "LevelUpPopup", "LEVEL UP!", new Color(1f, 0.72f, 0.06f), false);
            var lose = BuildPopup<LoseUI>(canvas.transform, "LosePopup", "NO MORE SLOTS", new Color(0.92f, 0.20f, 0.18f), true);
            // _uiManager.Register(levelUp);
            // _uiManager.Register(lose);

            var loading = BuildLoading(canvas.transform);
            // _uiManager.Register(loading);
            _uiManager.HideAll();
        }

        private void OnProgressChanged(int level, int current, int required)
        {
            _gameplayUI.SetProgress(level, current, required);
        }

        private void OnLevelUp(int nextLevel)
        {
            var popup = _uiManager.ShowUI(UIEnum.LEVEL_UP_UI,$"LEVEL {nextLevel}!");
            popup.transform.localScale = Vector3.zero;
            popup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack)
                .OnComplete(() => DOVirtual.DelayedCall(0.75f, () => _uiManager.HideUI(UIEnum.LEVEL_UP_UI)));
        }

        private void OnLevelLost() => _uiManager.ShowUI(
            UIEnum.LOSE_UI,
            "NO MORE SLOTS",
            (Action)FlickSortEventBus.RaiseRequestRetry);

        private void OnDealRequested() => _board?.Deal();
        private void RetryLevel()
        {
            _uiManager.HideUI(UIEnum.LOSE_UI);
            _board?.RetryLevel();
        }

        private void OnDealStarted() => PlayOneShot(dealSound, 0.6f);

        private void OnInvalidMove() => PlayOneShot(moveSound, 0.35f);

        private void OnChipMoveLanded()
        {
            if (moveSound == null || _audioSource == null)
                return;

            var now = Time.unscaledTime;
            if (now < _nextMoveSoundTime)
                return;

            _audioSource.pitch = UnityEngine.Random.Range(
                moveSoundPitchRange.x,
                moveSoundPitchRange.y);
            _audioSource.PlayOneShot(moveSound, moveSoundVolume);
            Debug.Log($"Playing move sound");
            _nextMoveSoundTime = now + moveSoundMinInterval;
        }

        private void OnMergeCompleted(Vector3 position)
        {
            PlayOneShot(mergeSound, 0.9f);
            Camera.main?.transform.DOShakePosition(0.18f, 0.08f, 8, 45f, false, true);
            SpawnMergeBurst(position + Vector3.up * 0.25f);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.pitch = 1f;
                _audioSource.PlayOneShot(clip, volume);
            }
        }

        private void SpawnMergeBurst(Vector3 position)
        {
            var effect = new GameObject("MergeBurst");
            effect.transform.position = position;
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.05f), Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 18));
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (_particleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                    _particleMaterial = new Material(shader);
            }
            renderer.sharedMaterial = _particleMaterial;
            particles.Play();
            Destroy(effect, 1.2f);
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, string value, int size, Vector2 anchor, Vector2 dimensions)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = dimensions;
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;

            return text;
        }

        private Image MakeImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 dimensions)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = dimensions;
            var image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 dimensions, Color color)
        {
            var image = MakeImage(parent, name, color, anchor, dimensions);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = MakeText(image.transform, "Label", label, 58, new Vector2(0.5f, 0.5f), dimensions);
            return button;
        }

        private T BuildPopup<T>(Transform parent, string name, string title, Color accent, bool hasAction)
            where T : PopupUI
        {
            var root = MakePopup(parent, name, title, accent);
            var view = root.AddComponent<T>();
            var titleText = root.GetComponentInChildren<TextMeshProUGUI>();
            Button actionButton = null;
            if (hasAction)
                actionButton = MakeButton(root.transform, "Retry", "RETRY", new Vector2(0.5f, 0.38f), new Vector2(420f, 130f), new Color(0.25f, 0.75f, 0.12f));
            view.Configure(titleText, actionButton);
            return view;
        }

        private LoadingUI BuildLoading(Transform parent)
        {
            var root = MakeImage(parent, "LoadingUI", new Color(0.035f, 0.05f, 0.09f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f));
            var view = root.gameObject.AddComponent<LoadingUI>();
            var title = MakeText(root.transform, "LoadingText", "LOADING", 64, new Vector2(0.5f, 0.55f), new Vector2(700f, 120f));
            var back = MakeImage(root.transform, "LoadingBarBack", new Color(0.12f, 0.15f, 0.22f), new Vector2(0.5f, 0.46f), new Vector2(720f, 54f));
            var fill = MakeImage(back.transform, "LoadingBarFill", new Color(0.27f, 0.82f, 0.18f), new Vector2(0f, 0.5f), new Vector2(700f, 42f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            fill.rectTransform.anchoredPosition = new Vector2(-350f, 0f);
            // view.Configure(fill, title);
            root.transform.SetAsLastSibling();
            return view;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private GameObject MakePopup(Transform parent, string name, string title, Color accent)
        {
            var blocker = MakeImage(parent, name, new Color(0.02f, 0.03f, 0.06f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f));
            var panel = MakeImage(blocker.transform, "Panel", new Color(0.96f, 0.94f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(800f, 520f));
            var text = MakeText(panel.transform, "Title", title, 86, new Vector2(0.5f, 0.64f), new Vector2(720f, 180f));
            text.color = accent;
            return blocker.gameObject;
        }
    }
}
