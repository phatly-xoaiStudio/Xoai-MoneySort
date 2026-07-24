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
        private static readonly int ChipColorCount =
            System.Enum.GetValues(typeof(ChipColor)).Length;

        [SerializeField] private FlickSortGameConfig config;
        [SerializeField] private GameObject chipPrefab;
        private ChipColorConfigSO _colorConfig;
        [SerializeField] private List<ChipStackView> _stacks = new();
        private readonly Dictionary<ChipStackView, List<ChipView>> _views = new();
        private readonly Stack<ChipView> _pool = new();
        private Camera _camera;
        private Transform _chipSpawner;
        private ChipStackView _selected;
        private LevelSettings _level;
        private System.Random _random;
        private bool _busy;
        private int _currentLevel;
        private int _mergeProgress;
        private int _maxUnlockedChipLevel;
        private bool _chipUnlockedThisAction;
        private bool _levelUpAcknowledged;

        public bool IsBusy => _busy;
        public int CurrentLevel => _currentLevel;
        public int MaxUnlockedChipLevel => _maxUnlockedChipLevel;

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
            _currentLevel = 1;
            _maxUnlockedChipLevel = Mathf.Clamp(
                config.GetLevel(_currentLevel).colorCount - 1,
                0,
                config.maxChipLevel);
            StartLevel(_currentLevel);
        }

        private void Update()
        {
            if (_busy || Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
                return;
            
            var ray = _camera.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 100f))
                return;
            
            var stack = hit.collider.GetComponentInParent<ChipStackView>();
            if (stack != null)
                HandleStackTap(stack);
        }

        public void StartLevel(int levelNumber)
        {
            StopAllCoroutines();
            DOTween.Kill(this);
            _busy = true;
            _currentLevel = Mathf.Max(1, levelNumber);
            _level = config.GetLevel(_currentLevel);
            _random = new System.Random(_level.randomSeed);
            _mergeProgress = 0;
            _chipUnlockedThisAction = false;
            ClearBoard();
            InitializeSceneStacks();
            FlickSortEventBus.RaiseProgressChanged(_currentLevel, 0, _level.requiredMerges);
            StartCoroutine(DealRoutine(_level.initialChipCount, false));
        }

        public void Deal()
        {
            if (!_busy)
                StartCoroutine(DealRoutine(_level.dealChipCount > 0 ? _level.dealChipCount : config.defaultDealChipCount, true));
        }

        public void RetryLevel() => StartLevel(_currentLevel);

        private void HandleStackTap(ChipStackView stack)
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
                var tween = movingViews[i].JumpTo(destination.GetWorldSlot(startIndex + i, config.chipSpacing), config.jumpPower, config.moveDuration, i * config.chipMoveDelay);
                sequence.Join(tween);
                sequence.InsertCallback(
                    config.moveDuration + i * config.chipMoveDelay,
                    FlickSortEventBus.RaiseChipMoveLanded);
            }
            yield return sequence.WaitForCompletion();
            yield return ResolveMerges(destination);
            if (_chipUnlockedThisAction || _mergeProgress >= _level.requiredMerges)
            {
                yield return LevelUpRoutine();
                yield break;
            }
            _busy = false;
        }

        private IEnumerator DealRoutine(int requestedCount, bool checkLoss)
        {
            _busy = true;
            ClearSelection();
            FlickSortEventBus.RaiseDealStarted();
            var remaining = Mathf.Min(requestedCount, TotalFreeSlots());
            var sequence = DOTween.Sequence().SetId(this);
            var delay = 0f;
            var safety = 0;
            var dealtHighestUnlockedChip = false;

            while (remaining > 0 && safety++ < 1000)
            {
                var available = _stacks.FindAll(item => item.Model.FreeSlots > 0);
                if (available.Count == 0)
                    break;

                var stack = available[_random.Next(available.Count)];
                var range = _level.chipsPerStackRange.y > 0 ? _level.chipsPerStackRange : config.randomChipsPerStack;
                var amount = Mathf.Min(remaining, Mathf.Min(stack.Model.FreeSlots, _random.Next(range.x, range.y + 1)));
                for (var i = 0; i < amount; i++)
                {
                    var randomLevel = dealtHighestUnlockedChip
                        ? _random.Next(0, _maxUnlockedChipLevel + 1)
                        : _maxUnlockedChipLevel;
                    dealtHighestUnlockedChip = true;
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
                yield return ResolveMerges(stack);

            if (_chipUnlockedThisAction || _mergeProgress >= _level.requiredMerges)
            {
                yield return LevelUpRoutine();
                yield break;
            }

            if (checkLoss && TotalFreeSlots() == 0)
            {
                _busy = true;
                FlickSortEventBus.RaiseLevelLost();
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
                resultView.SetToken(result, new Material[]{ _colorConfig.GetColor(result.Color)});
                resultView.transform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 6, 0.5f);
                views.Add(resultView);

                if (result.Level > _maxUnlockedChipLevel)
                {
                    _maxUnlockedChipLevel = result.Level;
                    _chipUnlockedThisAction = true;
                }

                _mergeProgress++;
                FlickSortEventBus.RaiseProgressChanged(
                    _currentLevel,
                    _mergeProgress,
                    _level.requiredMerges);
                FlickSortEventBus.RaiseMergeCompleted(destination);
            }
        }

        private IEnumerator LevelUpRoutine()
        {
            _busy = true;
            _currentLevel++;
            _level = config.GetLevel(_currentLevel);
            _random = new System.Random(_level.randomSeed);
            _mergeProgress = 0;
            _chipUnlockedThisAction = false;
            FlickSortEventBus.RaiseProgressChanged(
                _currentLevel,
                _mergeProgress,
                _level.requiredMerges);

            _levelUpAcknowledged = false;
            FlickSortEventBus.RaiseLevelUp(_currentLevel);
            yield return new WaitUntil(() => _levelUpAcknowledged);

            _busy = false;
        }

        private void OnLevelUpAcknowledged() => _levelUpAcknowledged = true;

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
            view.Initialize(token, new Material[]{_colorConfig.GetColor(token.Color)});
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

        private int TotalFreeSlots()
        {
            var total = 0;
            foreach (var stack in _stacks)
                total += stack.Model.FreeSlots;
            return total;
        }
    }
}
