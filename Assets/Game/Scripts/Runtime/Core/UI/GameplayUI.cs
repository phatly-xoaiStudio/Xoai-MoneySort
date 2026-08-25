using DG.Tweening;
using FlickSort.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class GameplayUI : UIBase
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Button _dealButton;
        [SerializeField] private Button _shuffleButton;
        [SerializeField] private Button _hammerButton;
        [SerializeField] private Button _freeMoveButton;
        [Header("Booster State")]
        [SerializeField] private TextMeshProUGUI _shuffleCountText;
        [SerializeField] private TextMeshProUGUI _hammerCountText;
        [SerializeField] private TextMeshProUGUI _freeMoveCountText;
        [SerializeField] private GameObject _shuffleLockRoot;
        [SerializeField] private GameObject _hammerLockRoot;
        [SerializeField] private GameObject _freeMoveLockRoot;
        [SerializeField] private TextMeshProUGUI _shuffleLockText;
        [SerializeField] private TextMeshProUGUI _hammerLockText;
        [SerializeField] private TextMeshProUGUI _freeMoveLockText;
        [SerializeField] private GameplayCheatUI _cheatUI;

        [Header("Progress Star Animation")]
        [SerializeField] private RectTransform _starSpawnPoint;
        [SerializeField] private RectTransform _starTarget;
        [SerializeField] private Image[] _flyingStars;
        [SerializeField, Min(0.05f)] private float _starFlyDuration = 0.5f;
        [SerializeField, Min(0f)] private float _starStagger = 0.06f;
        [SerializeField] private Vector2 _starSpawnSpread = new Vector2(150f, 80f);
        [SerializeField, Min(0.05f)] private float _starAppearDuration = 0.12f;
        [SerializeField, Min(0f)] private float _targetPunchScale = 0.12f;

        private Sequence _progressSequence;
        private float _displayedProgress;
        private int _displayedLevel = -1;
        private int _rawProgress;

        public override void Init(UIManager manager)
        {
            base.Init(manager);
            _dealButton.onClick.AddListener(OnDealClicked);
            _shuffleButton.onClick.AddListener(OnShuffleClicked);
            _hammerButton.onClick.AddListener(OnHammerClicked);
            _freeMoveButton.onClick.AddListener(OnFreeMoveClicked);
            HideFlyingStars();
        }

        public void SetProgress(int level, int current, int required)
        {
            _levelText.text = $"LEVEL {level}";
            var target = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
            var shouldAnimate =
                level == _displayedLevel &&
                current > _rawProgress &&
                isActiveAndEnabled &&
                HasStarAnimation();

            _displayedLevel = level;
            _rawProgress = current;

            if (shouldAnimate)
                AnimateProgress(target);
            else
                SetProgressImmediate(target);
        }

        public void SetMoney(int amount)
        {
            if (_moneyText != null)
                _moneyText.text = Mathf.Max(0, amount).ToString("N0");
        }

        public void InitializeCheats(
            System.Action win,
            System.Action lose,
            System.Action<int> goToLevel,
            System.Action<BoosterType> addBooster,
            System.Action<int> addCoins)
        {
            if (_cheatUI == null)
                throw new MissingReferenceException("Gameplay UI requires an authored cheat panel.");
            _cheatUI.Initialize(win, lose, goToLevel, addBooster, addCoins);
        }

        public void SetCheatBoosterCounts(int shuffle, int hammer, int freeMove) =>
            _cheatUI?.SetBoosterCounts(shuffle, hammer, freeMove);

        private void OnDealClicked() => FlickSortEventBus.RaiseRequestDeal();
        private void OnShuffleClicked() => FlickSortEventBus.RaiseRequestShuffle();
        private void OnHammerClicked() => FlickSortEventBus.RaiseRequestHammer();
        private void OnFreeMoveClicked() => FlickSortEventBus.RaiseRequestFreeMove();

        public void SetBoosterState(
            BoosterType type,
            int count,
            bool unlocked,
            int unlockLevel)
        {
            var button = type switch
            {
                BoosterType.Shuffle => _shuffleButton,
                BoosterType.Hammer => _hammerButton,
                BoosterType.FreeMove => _freeMoveButton,
                _ => null
            };
            var countText = type switch
            {
                BoosterType.Shuffle => _shuffleCountText,
                BoosterType.Hammer => _hammerCountText,
                BoosterType.FreeMove => _freeMoveCountText,
                _ => null
            };
            var lockRoot = type switch
            {
                BoosterType.Shuffle => _shuffleLockRoot,
                BoosterType.Hammer => _hammerLockRoot,
                BoosterType.FreeMove => _freeMoveLockRoot,
                _ => null
            };
            var lockText = type switch
            {
                BoosterType.Shuffle => _shuffleLockText,
                BoosterType.Hammer => _hammerLockText,
                BoosterType.FreeMove => _freeMoveLockText,
                _ => null
            };

            if (button == null || countText == null || lockRoot == null || lockText == null)
                throw new MissingReferenceException($"Authored UI for {type} is missing.");

            countText.text = Mathf.Max(0, count).ToString();
            lockRoot.SetActive(!unlocked);
            lockText.text = $"LV {unlockLevel}";
            button.interactable = unlocked && count > 0;
        }

        private void AnimateProgress(float target)
        {
            KillProgressAnimation();

            var totalDuration = _starFlyDuration + (_flyingStars.Length - 1) * _starStagger;
            _progressSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            for (var i = 0; i < _flyingStars.Length; i++)
            {
                var starIndex = i;
                var star = _flyingStars[i];
                var rect = star.rectTransform;
                var delay = i * _starStagger;
                var offset = GetSpawnOffset(i);

                star.gameObject.SetActive(true);
                star.color = Color.white;
                rect.position = _starSpawnPoint.position;
                rect.anchoredPosition += offset;
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.Euler(0f, 0f, -25f + i * 11f);

                _progressSequence.Insert(
                    delay,
                    rect.DOScale(1f, _starAppearDuration).SetEase(Ease.OutBack));
                _progressSequence.Insert(
                    delay,
                    rect.DOMove(_starTarget.position, _starFlyDuration)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            star.gameObject.SetActive(false);
                            FlickSortEventBus.RaiseProgressStarLanded(starIndex);
                            _starTarget.DOKill();
                            _starTarget.localScale = Vector3.one;
                            _starTarget.DOPunchScale(
                                Vector3.one * _targetPunchScale,
                                0.18f,
                                4,
                                0.5f).SetUpdate(true);
                        }));
                _progressSequence.Insert(
                    delay,
                    rect.DORotate(new Vector3(0f, 0f, 180f), _starFlyDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear));
            }

            _progressSequence.Insert(
                0f,
                DOVirtual.Float(_displayedProgress, target, totalDuration, UpdateProgressDisplay)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            _progressSequence.OnComplete(() =>
            {
                UpdateProgressDisplay(target);
                HideFlyingStars();
                _progressSequence = null;
            });
        }

        private Vector2 GetSpawnOffset(int index)
        {
            if (_flyingStars.Length <= 1)
                return Vector2.zero;

            var normalized = index / (float)(_flyingStars.Length - 1);
            return new Vector2(
                Mathf.Lerp(-_starSpawnSpread.x, _starSpawnSpread.x, normalized),
                Mathf.Sin(index * 1.7f) * _starSpawnSpread.y);
        }

        private void SetProgressImmediate(float value)
        {
            KillProgressAnimation();
            UpdateProgressDisplay(value);
            HideFlyingStars();
        }

        private void UpdateProgressDisplay(float value)
        {
            _displayedProgress = Mathf.Clamp01(value);
            _progressFill.fillAmount = _displayedProgress;
            _progressText.text = $"{Mathf.RoundToInt(_displayedProgress * 100f)}%";
        }

        private bool HasStarAnimation() =>
            _starSpawnPoint != null &&
            _starTarget != null &&
            _flyingStars != null &&
            _flyingStars.Length > 0;

        private void KillProgressAnimation()
        {
            _progressSequence?.Kill();
            _progressSequence = null;
            _starTarget?.DOKill();
            if (_starTarget != null)
                _starTarget.localScale = Vector3.one;
        }

        private void HideFlyingStars()
        {
            if (_flyingStars == null)
                return;

            foreach (var star in _flyingStars)
            {
                if (star == null)
                    continue;

                star.rectTransform.DOKill();
                star.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            KillProgressAnimation();
            HideFlyingStars();
        }

        private void OnDestroy()
        {
            KillProgressAnimation();
            if (_dealButton != null) _dealButton.onClick.RemoveListener(OnDealClicked);
            if (_shuffleButton != null) _shuffleButton.onClick.RemoveListener(OnShuffleClicked);
            if (_hammerButton != null) _hammerButton.onClick.RemoveListener(OnHammerClicked);
            if (_freeMoveButton != null) _freeMoveButton.onClick.RemoveListener(OnFreeMoveClicked);
        }
    }
}
