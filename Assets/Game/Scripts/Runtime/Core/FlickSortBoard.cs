using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FlickSort.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlickSort
{
    public sealed class FlickSortBoard : MonoBehaviour
    {
        [SerializeField] private FlickSortGameConfig config;
        [SerializeField] private GameObject chipPrefab;
        private ChipColorConfigSO _colorConfig;
        [SerializeField] private List<ChipStackView> _stacks = new();
        private readonly Dictionary<ChipStackView, List<ChipView>> _views = new();
        private readonly Stack<ChipView> _pool = new();
        private Camera _camera;
        private ChipStackView _selected;
        private LevelSettings _level;
        private System.Random _random;
        private bool _busy;
        private int _currentLevel;
        private int _mergeProgress;

        public event System.Action<int, int, int> ProgressChanged;
        public event System.Action<int> LevelUp;
        public event System.Action LevelLost;
        public event System.Action DealStarted;
        public event System.Action<Vector3> MergeCompleted;
        public event System.Action InvalidMove;
        public bool IsBusy => _busy;
        public int CurrentLevel => _currentLevel;

        public void Init(ChipColorConfigSO colorConfig)
        {
            _colorConfig = colorConfig;
            _camera = Camera.main;
            _currentLevel = Mathf.Max(1, PlayerPrefs.GetInt("FlickSort.CurrentLevel", 1));
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
            ClearBoard();
            InitializeSceneStacks();
            ProgressChanged?.Invoke(_currentLevel, 0, _level.requiredMerges);
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
                InvalidMove?.Invoke();
                ClearSelection();
                return;
            }

            StartCoroutine(MoveRoutine(_selected, stack));
            ClearSelection();
        }

        private IEnumerator MoveRoutine(ChipStackView source, ChipStackView destination)
        {
            _busy = true;
            var tokens = source.Model.RemoveTopGroup();
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
            }
            yield return sequence.WaitForCompletion();
            yield return ResolveMerges(destination);
            if (_mergeProgress >= _level.requiredMerges)
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
            DealStarted?.Invoke();
            var remaining = Mathf.Min(requestedCount, TotalFreeSlots());
            var sequence = DOTween.Sequence().SetId(this);
            var delay = 0f;
            var safety = 0;

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
                    int randomColorLevel = _random.Next(0, _level.colorCount);
                    var token = new ChipToken((ChipColor)randomColorLevel, randomColorLevel);
                    var slot = stack.Model.Count;
                    stack.Model.TryAdd(token);
                    var view = GetChip(token);
                    view.transform.SetParent(stack.ChipRoot, false);
                    view.transform.localScale = Vector3.one;
                    view.transform.position = stack.GetWorldSlot(slot, config.chipSpacing) + Vector3.up * 5f;
                    _views[stack].Add(view);
                    sequence.Join(view.DealTo(stack.GetWorldSlot(slot, config.chipSpacing), config.dealDuration, delay));
                    delay += config.chipMoveDelay * 0.45f;
                }
                remaining -= amount;
            }

            yield return sequence.WaitForCompletion();
            foreach (var stack in _stacks)
                yield return ResolveMerges(stack);

            if (_mergeProgress >= _level.requiredMerges)
            {
                yield return LevelUpRoutine();
                yield break;
            }

            if (checkLoss && TotalFreeSlots() == 0)
            {
                _busy = true;
                LevelLost?.Invoke();
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

                _mergeProgress++;
                ProgressChanged?.Invoke(_currentLevel, _mergeProgress, _level.requiredMerges);
                MergeCompleted?.Invoke(destination);
            }
        }

        private IEnumerator LevelUpRoutine()
        {
            _busy = true;
            _currentLevel++;
            PlayerPrefs.SetInt("FlickSort.CurrentLevel", _currentLevel);
            PlayerPrefs.Save();
            LevelUp?.Invoke(_currentLevel);
            yield return new WaitForSeconds(1.25f);
            StartLevel(_currentLevel);
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

        private int TotalFreeSlots()
        {
            var total = 0;
            foreach (var stack in _stacks)
                total += stack.Model.FreeSlots;
            return total;
        }
    }
}
