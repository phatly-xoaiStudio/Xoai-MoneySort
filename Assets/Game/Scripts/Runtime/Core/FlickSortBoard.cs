using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FlickSort.Core;
using FlickSort.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlickSort
{
    public sealed class FlickSortBoard : MonoBehaviour
    {
        private enum BoardSkillMode
        {
            None,
            Hammer,
            FreeMove
        }

        private static readonly int ChipColorCount =
            System.Enum.GetValues(typeof(ChipColor)).Length;

        [SerializeField] private FlickSortGameConfig config;
        [SerializeField] private GameObject chipPrefab;
        [Header("Responsive Camera")]
        [SerializeField] private bool fitTrayOnNarrowScreens = true;
        [SerializeField, Min(0.1f)] private float referenceAspect = 9f / 16f;
        [SerializeField, Range(0.5f, 1f)] private float minimumTrayScale = 0.7f;
        [SerializeField] private SpriteRenderer responsiveBackground;
        private ChipColorConfigSO _colorConfig;
        [SerializeField] private List<ChipStackView> _stacks = new();
        private readonly Dictionary<ChipStackView, List<ChipView>> _views = new();
        private readonly Stack<ChipView> _pool = new();
        private readonly List<ChipStackView> _availableDealStacks =
            new(FlickSortBoardRules.TotalSlotCount);
        private Camera _camera;
        private Transform _chipSpawner;
        private ChipStackView _selected;
        private LevelSettings _level;
        private System.Random _random;
        private bool _busy;
        private int _currentLevel;
        private int _chipScore;
        private int _maxUnlockedChipLevel;
        private bool _chipUnlockedThisAction;
        private bool _levelUpAcknowledged;
        private BoardSkillMode _activeSkill;
        private float _authoredCameraSize;
        private Vector3 _authoredCameraPosition;
        private Vector2 _authoredTrayViewportPosition;
        private Vector3 _authoredBackgroundScale;
        private Vector2 _authoredBackgroundWorldSize;
        private Vector2Int _lastScreenSize;
        private int _activeRentStackIndex = -1;
        private int _freeRentUsesRemaining;

        public bool IsBusy => _busy;
        public int CurrentLevel => _currentLevel;
        public int MaxUnlockedChipLevel => _maxUnlockedChipLevel;
        public FlickSortGameConfig Config => config;

        public Material GetChipMaterial(int chipLevel)
        {
            var chipColor = (ChipColor)(Mathf.Max(0, chipLevel) % ChipColorCount);
            return _colorConfig != null ? _colorConfig.GetColor(chipColor) : null;
        }

        private void OnEnable()
        {
            FlickSortEventBus.LevelUpAcknowledged += OnLevelUpAcknowledged;
        }

        private void OnDisable()
        {
            FlickSortEventBus.LevelUpAcknowledged -= OnLevelUpAcknowledged;
        }

        public void Init(ChipColorConfigSO colorConfig, Transform chipSpawner)
        {
            _colorConfig = colorConfig;
            _chipSpawner = chipSpawner != null
                ? chipSpawner
                : throw new MissingReferenceException("FlickSortBoard requires a scene Chip Spawner.");
            _camera = Camera.main;
            if (_camera == null)
                throw new MissingReferenceException(
                    "FlickSortBoard requires a camera tagged MainCamera.");
            CacheResponsiveLayout();
            RefreshResponsiveCamera(true);
            _currentLevel = 1;
            _maxUnlockedChipLevel = Mathf.Clamp(
                config.GetLevel(_currentLevel).colorCount - 1,
                0,
                config.maxChipLevel);
            StartLevel(_currentLevel);
        }

        private void Update()
        {
            RefreshResponsiveCamera(false);

            if (_busy || Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
                return;
            
            var ray = _camera.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 100f))
                return;
            
            var stack = hit.collider.GetComponentInParent<ChipStackView>();
            if (stack == null)
                return;

            if (!stack.IsAvailable)
                return;

            if (_activeSkill == BoardSkillMode.Hammer)
            {
                HandleHammerTap(stack);
                return;
            }

            if (_activeSkill == BoardSkillMode.FreeMove)
            {
                HandleFreeMoveTap(stack);
                return;
            }

            HandleStackTap(stack);
        }

        private void CacheResponsiveLayout()
        {
            _authoredCameraSize = _camera.orthographicSize;
            _authoredCameraPosition = _camera.transform.position;
            var viewportPosition = _camera.WorldToViewportPoint(transform.position);
            _authoredTrayViewportPosition = new Vector2(viewportPosition.x, viewportPosition.y);

            if (responsiveBackground == null)
            {
                var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                for (var i = 0; i < spriteRenderers.Length; i++)
                {
                    if (responsiveBackground == null ||
                        spriteRenderers[i].sortingOrder < responsiveBackground.sortingOrder)
                    {
                        responsiveBackground = spriteRenderers[i];
                    }
                }
            }

            if (responsiveBackground == null)
                return;

            _authoredBackgroundScale = responsiveBackground.transform.localScale;
            var bounds = responsiveBackground.bounds;
            _authoredBackgroundWorldSize = new Vector2(bounds.size.x, bounds.size.y);
        }

        private void RefreshResponsiveCamera(bool force)
        {
            if (!fitTrayOnNarrowScreens || Screen.width <= 0 || Screen.height <= 0)
                return;

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && screenSize == _lastScreenSize)
                return;

            _lastScreenSize = screenSize;
            var aspect = (float)screenSize.x / screenSize.y;
            var visibleScale = Mathf.Clamp(
                aspect / Mathf.Max(0.1f, referenceAspect),
                minimumTrayScale,
                1f);
            var cameraSize = _authoredCameraSize / visibleScale;
            _camera.orthographicSize = cameraSize;

            var cameraPosition = _authoredCameraPosition;
            cameraPosition.x = transform.position.x -
                (_authoredTrayViewportPosition.x - 0.5f) * 2f * cameraSize * aspect;
            cameraPosition.y = transform.position.y -
                (_authoredTrayViewportPosition.y - 0.5f) * 2f * cameraSize;
            _camera.transform.position = cameraPosition;

            FitBackgroundToCamera(cameraSize, aspect);
        }

        private void FitBackgroundToCamera(float cameraSize, float aspect)
        {
            if (responsiveBackground == null ||
                _authoredBackgroundWorldSize.x <= 0f ||
                _authoredBackgroundWorldSize.y <= 0f)
            {
                return;
            }

            var visibleHeight = cameraSize * 2f;
            var visibleWidth = visibleHeight * aspect;
            var coverScale = Mathf.Max(
                visibleWidth / _authoredBackgroundWorldSize.x,
                visibleHeight / _authoredBackgroundWorldSize.y,
                1f);
            responsiveBackground.transform.localScale = _authoredBackgroundScale * coverScale;
            var backgroundPosition = responsiveBackground.transform.position;
            backgroundPosition.x = _camera.transform.position.x;
            backgroundPosition.y = _camera.transform.position.y;
            responsiveBackground.transform.position = backgroundPosition;
        }
        public void StartLevel(int levelNumber)
        {
            StopAllCoroutines();
            DOTween.Kill(this);
            _busy = true;
            _currentLevel = Mathf.Max(1, levelNumber);
            _level = config.GetLevel(_currentLevel);
            _random = new System.Random(_level.randomSeed);
            _chipScore = 0;
            _chipUnlockedThisAction = false;
            _activeSkill = BoardSkillMode.None;
            _activeRentStackIndex = -1;
            ClearBoard();
            InitializeSceneStacks();
            RaiseScoreProgressChanged();
            StartCoroutine(DealRoutine(_level.initialChipCount, false, false));
        }

        public void Deal()
        {
            if (!_busy)
            {
                _activeSkill = BoardSkillMode.None;
                StartCoroutine(DealRoutine(
                    _level.dealChipCount > 0
                        ? _level.dealChipCount
                        : config.defaultDealChipCount,
                    true,
                    true));
            }
        }

        public bool TryShuffle()
        {
            if (_busy)
                return false;

            _activeSkill = BoardSkillMode.None;
            StartCoroutine(ShuffleRoutine());
            return true;
        }

        public void ActivateHammer()
        {
            if (_busy)
                return;

            ClearSelection();
            _activeSkill = _activeSkill == BoardSkillMode.Hammer
                ? BoardSkillMode.None
                : BoardSkillMode.Hammer;
        }

        public void ActivateFreeMove()
        {
            if (_busy)
                return;

            ClearSelection();
            _activeSkill = _activeSkill == BoardSkillMode.FreeMove
                ? BoardSkillMode.None
                : BoardSkillMode.FreeMove;
        }

        public void RetryLevel() => StartLevel(_currentLevel);

        public void CheatWinLevel()
        {
            StopAllCoroutines();
            DOTween.Kill(this, true);
            ClearSelection();
            _activeRentStackIndex = -1;
            _busy = false;
            _chipUnlockedThisAction = false;
            StartCoroutine(LevelUpRoutine());
        }

        public void CheatLoseLevel()
        {
            StopAllCoroutines();
            DOTween.Kill(this, true);
            ClearSelection();
            _busy = true;
            FlickSortEventBus.RaiseLevelLost();
        }

        public bool UnlockRentedStack(int stackIndex)
        {
            if (stackIndex < 0 || stackIndex >= _stacks.Count)
                return false;

            var stack = _stacks[stackIndex];
            if (stack == null || !stack.IsRentable)
                return false;

            stack.SetAccessState(StackAccessState.Available);
            CollectAvailableDealStacks();
            return true;
        }

        public bool RentStackForDuration(int stackIndex, float durationSeconds)
        {
            if (_activeRentStackIndex >= 0)
                return false;
            if (!UnlockRentedStack(stackIndex))
                return false;

            _activeRentStackIndex = stackIndex;
            StartCoroutine(RentStackRoutine(stackIndex, Mathf.Max(1f, durationSeconds)));
            return true;
        }

        public void SetFreeRentUsesRemaining(int remaining)
        {
            _freeRentUsesRemaining = Mathf.Max(0, remaining);
            var rentStackIndex = FlickSortBoardRules.GetRentSlotIndex(_currentLevel);
            if (rentStackIndex >= 0 && rentStackIndex < _stacks.Count)
                _stacks[rentStackIndex]?.SetFreeRentUsesRemaining(_freeRentUsesRemaining);
        }

        private IEnumerator RentStackRoutine(int stackIndex, float durationSeconds)
        {
            if (stackIndex < 0 || stackIndex >= _stacks.Count)
                yield break;

            var stack = _stacks[stackIndex];
            if (stack == null)
                yield break;

            stack.SetAccessState(StackAccessState.Rented);
            var expiresAt = Time.realtimeSinceStartup + durationSeconds;
            var displayedSeconds = -1;
            while (Time.realtimeSinceStartup < expiresAt)
            {
                var secondsRemaining = Mathf.CeilToInt(expiresAt - Time.realtimeSinceStartup);
                if (secondsRemaining != displayedSeconds)
                {
                    displayedSeconds = secondsRemaining;
                    stack.SetRentTimeRemaining(secondsRemaining);
                }
                yield return null;
            }

            stack.SetRentTimeRemaining(0f);
            while (_busy)
                yield return null;

            if (_selected == stack)
                ClearSelection();
            stack.SetAccessState(StackAccessState.RentClosing);
            CollectAvailableDealStacks();

            if (stack.Model.Count > 0)
            {
                ChipStackView destination = null;
                while (destination == null && stack.Model.Count > 0)
                {
                    destination = GetRandomEmptyRegularStack(stack);
                    if (destination == null)
                        yield return null;
                }

                if (destination != null)
                {
                    _busy = true;
                    yield return EvacuateRentedStack(stack, destination);
                }
            }

            _activeRentStackIndex = -1;
            ApplyStackAvailability();
            var rentStackIndex = FlickSortBoardRules.GetRentSlotIndex(_currentLevel);
            if (rentStackIndex >= 0 && rentStackIndex < _stacks.Count)
                _stacks[rentStackIndex]?.SetRentTimeRemaining(durationSeconds);
            CollectAvailableDealStacks();
            if (_chipUnlockedThisAction || HasReachedRequiredScore())
                yield return LevelUpRoutine();
        }

        private ChipStackView GetRandomEmptyRegularStack(ChipStackView rentedStack)
        {
            _availableDealStacks.Clear();
            for (var i = 0; i < _stacks.Count; i++)
            {
                var candidate = _stacks[i];
                if (candidate != null &&
                    candidate != rentedStack &&
                    candidate.CanReceiveDeal &&
                    candidate.Model.Count == 0)
                {
                    _availableDealStacks.Add(candidate);
                }
            }

            return _availableDealStacks.Count > 0
                ? _availableDealStacks[_random.Next(_availableDealStacks.Count)]
                : null;
        }

        private IEnumerator EvacuateRentedStack(
            ChipStackView source,
            ChipStackView destination)
        {
            var tokens = new List<ChipToken>(source.Model.Chips);
            var movingViews = _views[source];
            source.Model.Clear();
            destination.Model.AddRange(tokens);
            _views[source] = new List<ChipView>();
            _views[destination].AddRange(movingViews);

            var sequence = DOTween.Sequence().SetId(this);
            for (var i = 0; i < movingViews.Count; i++)
            {
                var view = movingViews[i];
                view.transform.SetParent(destination.ChipRoot, true);
                view.transform.localScale = Vector3.one;
                sequence.Join(view.ArcTo(
                    destination.GetWorldSlot(i, config.chipSpacing),
                    config.jumpPower * 1.5f,
                    config.moveDuration,
                    i * config.chipMoveDelay));
                sequence.InsertCallback(
                    config.moveDuration + i * config.chipMoveDelay,
                    FlickSortEventBus.RaiseChipMoveLanded);
            }

            yield return sequence.WaitForCompletion();
            yield return ResolveMerges(destination);
            _busy = false;
        }

        private void HandleHammerTap(ChipStackView stack)
        {
            if (stack.Model.Count == 0)
            {
                stack.InvalidFeedback();
                FlickSortEventBus.RaiseInvalidMove();
                return;
            }

            _activeSkill = BoardSkillMode.None;
            FlickSortEventBus.RaiseBoosterUsed(BoosterType.Hammer);
            StartCoroutine(HammerRoutine(stack));
        }

        private void HandleFreeMoveTap(ChipStackView stack)
        {
            if (_selected == null)
            {
                if (stack.Model.Count == 0)
                {
                    stack.InvalidFeedback();
                    return;
                }

                _selected = stack;
                _selected.SetSelected(true);
                return;
            }

            if (_selected == stack)
            {
                ClearSelection();
                return;
            }

            if (stack.Model.FreeSlots <= 0)
            {
                stack.InvalidFeedback();
                FlickSortEventBus.RaiseInvalidMove();
                return;
            }

            var source = _selected;
            ClearSelection();
            _activeSkill = BoardSkillMode.None;
            FlickSortEventBus.RaiseBoosterUsed(BoosterType.FreeMove);
            StartCoroutine(MoveRoutine(source, stack));
        }

        private void HandleStackTap(ChipStackView stack)
        {
            if (!stack.IsAvailable)
                return;

            if (_selected == null)
            {
                if (stack.Model.Count == 0)
                {
                    stack.InvalidFeedback();
                    return;
                }
                _selected = stack;
                _selected.SetSelected(true);
                return;
            }

            if (_selected == stack)
            {
                ClearSelection();
                return;
            }

            if (!_selected.Model.CanMoveTopGroupTo(stack.Model))
            {
                stack.InvalidFeedback();
                FlickSortEventBus.RaiseInvalidMove();
                ClearSelection();
                return;
            }

            StartCoroutine(MoveRoutine(_selected, stack));
            ClearSelection();
        }

        private IEnumerator MoveRoutine(ChipStackView source, ChipStackView destination)
        {
            _busy = true;
            var tokens = source.Model.RemoveTopGroup(destination.Model.FreeSlots);
            var sourceViews = _views[source];
            var movingViews = sourceViews.GetRange(sourceViews.Count - tokens.Count, tokens.Count);
            sourceViews.RemoveRange(sourceViews.Count - tokens.Count, tokens.Count);
            var startIndex = destination.Model.Count;
            destination.Model.AddRange(tokens);
            _views[destination].AddRange(movingViews);

            var sequence = DOTween.Sequence().SetId(this);
            for (var i = 0; i < movingViews.Count; i++)
            {
                movingViews[i].transform.SetParent(destination.ChipRoot, true);
                movingViews[i].transform.localScale = Vector3.one;
                var tween = movingViews[i].ArcTo(
                    destination.GetWorldSlot(startIndex + i, config.chipSpacing),
                    config.jumpPower * 1.5f,
                    config.moveDuration,
                    i * config.chipMoveDelay);
                sequence.Join(tween);
                sequence.InsertCallback(
                    config.moveDuration + i * config.chipMoveDelay,
                    FlickSortEventBus.RaiseChipMoveLanded);
            }
            yield return sequence.WaitForCompletion();
            yield return ResolveMerges(destination);
            if (_chipUnlockedThisAction || HasReachedRequiredScore())
            {
                yield return LevelUpRoutine();
                yield break;
            }
            _busy = false;
        }

        private IEnumerator DealRoutine(
            int requestedCount,
            bool checkLoss,
            bool playDealSound)
        {
            _busy = true;
            ClearSelection();
            if (playDealSound)
                FlickSortEventBus.RaiseDealStarted();
            var remaining = Mathf.Min(requestedCount, TotalDealFreeSlots());
            var sequence = DOTween.Sequence().SetId(this);
            var delay = 0f;
            var safety = 0;
            var dealtHighestUnlockedChip = false;

            while (remaining >= config.minimumDealColorGroupSize && safety++ < 1000)
            {
                CollectAvailableDealStacks();
                for (var i = _availableDealStacks.Count - 1; i >= 0; i--)
                {
                    if (_availableDealStacks[i].Model.FreeSlots < config.minimumDealColorGroupSize)
                        _availableDealStacks.RemoveAt(i);
                }
                if (_availableDealStacks.Count == 0)
                    break;

                var range = _level.chipsPerStackRange.y > 0 ? _level.chipsPerStackRange : config.randomChipsPerStack;
                var largestAvailableSpace = 0;
                for (var i = 0; i < _availableDealStacks.Count; i++)
                    largestAvailableSpace = Mathf.Max(largestAvailableSpace, _availableDealStacks[i].Model.FreeSlots);
                var amount = FlickSortBoardRules.GetDealGroupSize(
                    remaining,
                    config.minimumDealColorGroupSize,
                    range.y,
                    largestAvailableSpace,
                    _random);
                if (amount < config.minimumDealColorGroupSize)
                    break;

                for (var i = _availableDealStacks.Count - 1; i >= 0; i--)
                {
                    if (_availableDealStacks[i].Model.FreeSlots < amount)
                        _availableDealStacks.RemoveAt(i);
                }
                var stack = _availableDealStacks[_random.Next(_availableDealStacks.Count)];
                var randomLevel = dealtHighestUnlockedChip
                    ? _random.Next(0, _maxUnlockedChipLevel + 1)
                    : _maxUnlockedChipLevel;
                dealtHighestUnlockedChip = true;
                for (var i = 0; i < amount; i++)
                {
                    var token = new ChipToken(
                        (ChipColor)(randomLevel % ChipColorCount),
                        randomLevel);
                    var slot = stack.Model.Count;
                    stack.Model.TryAdd(token);
                    var view = GetChip(token);
                    view.transform.SetParent(stack.ChipRoot, false);
                    view.transform.localScale = Vector3.one;
                    view.transform.position = _chipSpawner.position;
                    _views[stack].Add(view);
                    sequence.Join(view.JumpTo(
                        stack.GetWorldSlot(slot, config.chipSpacing),
                        config.jumpPower,
                        config.dealDuration,
                        delay));
                    delay += config.chipMoveDelay * 0.45f;
                }
                remaining -= amount;
            }

            yield return sequence.WaitForCompletion();
            foreach (var stack in _stacks)
            {
                if (stack.IsAvailable)
                    yield return ResolveMerges(stack);
            }

            if (_chipUnlockedThisAction || HasReachedRequiredScore())
            {
                yield return LevelUpRoutine();
                yield break;
            }

            if (checkLoss && TotalPlayableFreeSlots() == 0)
            {
                _busy = true;
                FlickSortEventBus.RaiseLevelLost();
                yield break;
            }

            _busy = false;
        }

        private IEnumerator ShuffleRoutine()
        {
            _busy = true;
            ClearSelection();

            var availableStacks = new List<ChipStackView>();
            var targetCounts = new List<int>();
            var chips = new List<ChipToken>();
            var viewsByToken = new Dictionary<ChipToken, Queue<ChipView>>();

            for (var stackIndex = 0; stackIndex < _stacks.Count; stackIndex++)
            {
                var stack = _stacks[stackIndex];
                if (!stack.IsAvailable)
                    continue;

                availableStacks.Add(stack);
                targetCounts.Add(stack.Model.Count);
                for (var chipIndex = 0; chipIndex < stack.Model.Chips.Count; chipIndex++)
                    chips.Add(stack.Model.Chips[chipIndex]);

                var stackViews = _views[stack];
                for (var viewIndex = 0; viewIndex < stackViews.Count; viewIndex++)
                {
                    var view = stackViews[viewIndex];
                    if (!viewsByToken.TryGetValue(view.Token, out var queue))
                    {
                        queue = new Queue<ChipView>();
                        viewsByToken.Add(view.Token, queue);
                    }
                    queue.Enqueue(view);
                }
            }

            if (chips.Count < 2)
            {
                _busy = false;
                yield break;
            }

            FlickSortEventBus.RaiseShuffleStarted();
            var plan = ChipShufflePlanner.Build(
                chips,
                targetCounts,
                config.mergeChipCount,
                _random);

            for (var i = 0; i < availableStacks.Count; i++)
            {
                availableStacks[i].Model.Clear();
                _views[availableStacks[i]].Clear();
            }

            var sequence = DOTween.Sequence().SetId(this);
            for (var stackIndex = 0; stackIndex < availableStacks.Count; stackIndex++)
            {
                var stack = availableStacks[stackIndex];
                var stackPlan = plan[stackIndex];
                for (var chipIndex = 0; chipIndex < stackPlan.Count; chipIndex++)
                {
                    var token = stackPlan[chipIndex];
                    var view = viewsByToken[token].Dequeue();
                    stack.Model.TryAdd(token);
                    _views[stack].Add(view);
                    view.transform.SetParent(stack.ChipRoot, true);
                    view.transform.localScale = Vector3.one;

                    sequence.Join(view.ArcTo(
                        stack.GetWorldSlot(chipIndex, config.chipSpacing),
                        config.jumpPower * 1.5f,
                        config.moveDuration,
                        0f));
                }
            }

            sequence.InsertCallback(
                config.moveDuration,
                FlickSortEventBus.RaiseChipMoveLanded);
            yield return sequence.WaitForCompletion();
            _busy = false;
        }

        private IEnumerator HammerRoutine(ChipStackView stack)
        {
            _busy = true;
            ClearSelection();

            FlickSortEventBus.RaiseHammerStarted();

            var destroyedChipCount = stack.Model.Count;
            var destroyedViews = new List<ChipView>(_views[stack]);
            stack.Model.Clear();
            _views[stack].Clear();

            var sequence = DOTween.Sequence().SetId(this);
            for (var i = 0; i < destroyedViews.Count; i++)
            {
                var angle = (float)(_random.NextDouble() * Mathf.PI * 2f);
                var direction = new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    -0.2f);
                var distanceMultiplier = Mathf.Lerp(
                    0.8f,
                    1.2f,
                    (float)_random.NextDouble());
                sequence.Join(destroyedViews[i].BreakAway(
                    direction,
                    config.hammerFlyDistance * distanceMultiplier,
                    config.jumpPower,
                    config.hammerFlyDuration,
                    i * config.hammerFlyStagger));
            }

            yield return sequence.WaitForCompletion();

            for (var i = 0; i < destroyedViews.Count; i++)
                ReturnChip(destroyedViews[i]);

            AddChipScore(destroyedChipCount);
            if (HasReachedRequiredScore())
            {
                yield return LevelUpRoutine();
                yield break;
            }

            _busy = false;
        }

        private IEnumerator ResolveMerges(ChipStackView stack)
        {
            while (stack.Model.TryMergeTop(config.mergeChipCount, config.maxChipLevel, out var result))
            {
                var views = _views[stack];
                var mergeViews = views.GetRange(views.Count - config.mergeChipCount, config.mergeChipCount);
                views.RemoveRange(views.Count - config.mergeChipCount, config.mergeChipCount);
                var resultView = mergeViews[0];
                var destination = stack.GetWorldSlot(stack.Model.Count - 1, config.chipSpacing);
                var sequence = DOTween.Sequence().SetId(this);
                for (var i = 1; i < mergeViews.Count; i++)
                    sequence.Join(mergeViews[i].MergeInto(destination, config.mergeDuration, (i - 1) * config.chipMoveDelay * 0.5f));
                yield return sequence.WaitForCompletion();

                for (var i = 1; i < mergeViews.Count; i++)
                    ReturnChip(mergeViews[i]);
                resultView.transform.position = destination;
                resultView.transform.localScale = Vector3.one;
                resultView.SetToken(result, _colorConfig.GetColor(result.Color));
                resultView.transform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 6, 0.5f);
                views.Add(resultView);

                if (result.Level > _maxUnlockedChipLevel)
                {
                    _maxUnlockedChipLevel = result.Level;
                    _chipUnlockedThisAction = true;
                }

                AddChipScore(config.mergeChipCount);
                FlickSortEventBus.RaiseMergeCompleted(destination);
            }
        }

        private IEnumerator LevelUpRoutine()
        {
            _busy = true;
            var unlockedChipLevel = _chipUnlockedThisAction
                ? _maxUnlockedChipLevel
                : -1;
            _currentLevel++;
            _level = config.GetLevel(_currentLevel);
            _random = new System.Random(_level.randomSeed);
            ApplyStackAvailability();
            _chipScore = 0;
            _chipUnlockedThisAction = false;
            RaiseScoreProgressChanged();

            _levelUpAcknowledged = false;
            FlickSortEventBus.RaiseLevelUp(_currentLevel, unlockedChipLevel);
            yield return new WaitUntil(() => _levelUpAcknowledged);

            _busy = false;
        }

        private void OnLevelUpAcknowledged() => _levelUpAcknowledged = true;

        private void AddChipScore(int consumedChipCount)
        {
            if (consumedChipCount <= 0)
                return;

            _chipScore += consumedChipCount;
            RaiseScoreProgressChanged();
        }

        private bool HasReachedRequiredScore() =>
            _chipScore >= _level.requiredChipScore;

        private void RaiseScoreProgressChanged()
        {
            FlickSortEventBus.RaiseProgressChanged(
                _currentLevel,
                _chipScore,
                _level.requiredChipScore);
        }

        private void InitializeSceneStacks()
        {
            for (var i = 0; i < _stacks.Count; i++)
            {
                var stack = _stacks[i];
                if (stack == null)
                {
                    Debug.LogError($"Stack reference at index {i} is missing.", this);
                    continue;
                }

                stack.Initialize(i, new ChipStackModel(config.stackCapacity));
                _views.Add(stack, new List<ChipView>());
            }

            ApplyStackAvailability();
        }

        private void ApplyStackAvailability()
        {
            var nextLockedSlotIndex =
                FlickSortBoardRules.GetNextLockedSlotIndex(_currentLevel);
            var desiredRentSlotIndex =
                FlickSortBoardRules.GetRentSlotIndex(_currentLevel);

            for (var i = 0; i < _stacks.Count; i++)
            {
                var stack = _stacks[i];
                if (stack == null)
                    continue;

                if (_activeRentStackIndex >= 0)
                {
                    if (i == _activeRentStackIndex)
                        continue;
                    if (i == desiredRentSlotIndex)
                    {
                        stack.SetAccessState(StackAccessState.Locked);
                        continue;
                    }
                }

                var state = FlickSortBoardRules.GetAccessState(i, _currentLevel);
                var displayedUnlockLevel = i == nextLockedSlotIndex
                    ? FlickSortBoardRules.GetUnlockLevel(i, _currentLevel)
                    : 0;
                stack.SetAccessState(state, displayedUnlockLevel);
                if (state == StackAccessState.Rent)
                    stack.SetFreeRentUsesRemaining(_freeRentUsesRemaining);
            }
        }

        private ChipView GetChip(ChipToken token)
        {
            ChipView view;
            if (_pool.Count > 0)
            {
                view = _pool.Pop();
                view.gameObject.SetActive(true);
            }
            else
            {
                var instance = Instantiate(chipPrefab);
                if (!instance.TryGetComponent(out view))
                {
                    Destroy(instance);
                    throw new MissingComponentException(
                        $"The chip prefab '{chipPrefab.name}' must contain a {nameof(ChipView)} component on its root.");
                }
            }
            view.transform.localScale = Vector3.one;
            view.transform.localRotation = Quaternion.identity;
            view.Initialize(token, _colorConfig.GetColor(token.Color));
            return view;
        }

        private void ReturnChip(ChipView view)
        {
            view.KillTween();
            view.gameObject.SetActive(false);
            view.transform.SetParent(transform, false);
            _pool.Push(view);
        }

        private void ClearBoard()
        {
            ClearSelection();
            foreach (var pair in _views)
            {
                foreach (var chip in pair.Value)
                    if (chip != null) ReturnChip(chip);
            }
            _views.Clear();
        }

        private void ClearSelection()
        {
            if (_selected != null)
                _selected.SetSelected(false);
            _selected = null;
        }

        private void CollectAvailableDealStacks()
        {
            _availableDealStacks.Clear();
            for (var i = 0; i < _stacks.Count; i++)
            {
                var stack = _stacks[i];
                if (stack != null && stack.CanReceiveDeal && stack.Model.FreeSlots > 0)
                    _availableDealStacks.Add(stack);
            }
        }
        private int TotalDealFreeSlots()
        {
            var total = 0;
            foreach (var stack in _stacks)
            {
                if (stack.CanReceiveDeal)
                    total += stack.Model.FreeSlots;
            }
            return total;
        }

        private int TotalPlayableFreeSlots()
        {
            var total = 0;
            foreach (var stack in _stacks)
            {
                if (stack.IsAvailable)
                    total += stack.Model.FreeSlots;
            }
            return total;
        }
    }
}
