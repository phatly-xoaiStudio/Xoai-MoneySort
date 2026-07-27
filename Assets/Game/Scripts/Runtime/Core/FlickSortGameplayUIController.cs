using System;
using FlickSort.Core;
using FlickSort.UI;
using UnityEngine;

namespace FlickSort
{
    public sealed class FlickSortGameplayUIController : MonoBehaviour
    {
        [SerializeField] private FlickSortBoard board;
        [SerializeField] private UIManager uiManager;

        private GameplayUI _gameplayUI;

        public void Init()
        {
            _gameplayUI = uiManager.GetUi(UIEnum.GAMEPLAY_UI) as GameplayUI;
            if (_gameplayUI == null)
                throw new MissingReferenceException("Gameplay UI is missing from UIManager definitions.");
        }

        private void OnEnable()
        {
            FlickSortEventBus.RequestDeal += OnDealRequested;
            FlickSortEventBus.RequestShuffle += OnShuffleRequested;
            FlickSortEventBus.RequestRetry += RetryLevel;
            FlickSortEventBus.ProgressChanged += OnProgressChanged;
            FlickSortEventBus.LevelUp += OnLevelUp;
            FlickSortEventBus.LevelLost += OnLevelLost;
        }

        private void OnDisable()
        {
            FlickSortEventBus.RequestDeal -= OnDealRequested;
            FlickSortEventBus.RequestShuffle -= OnShuffleRequested;
            FlickSortEventBus.RequestRetry -= RetryLevel;
            FlickSortEventBus.ProgressChanged -= OnProgressChanged;
            FlickSortEventBus.LevelUp -= OnLevelUp;
            FlickSortEventBus.LevelLost -= OnLevelLost;
        }

        private void OnProgressChanged(int level, int current, int required)
        {
            _gameplayUI?.SetProgress(level, current, required);
        }

        private void OnLevelUp(int nextLevel, int unlockedChipLevel)
        {
            var unlockedChipMaterial = unlockedChipLevel >= 0
                ? board.GetChipMaterial(unlockedChipLevel)
                : null;
            uiManager.ShowUI(
                UIEnum.LEVEL_UP_UI,
                nextLevel,
                unlockedChipLevel,
                unlockedChipMaterial,
                (Action)(() =>
                {
                    uiManager.HideUI(UIEnum.LEVEL_UP_UI);
                    FlickSortEventBus.RaiseLevelUpAcknowledged();
                }));
        }

        private void OnLevelLost()
        {
            uiManager.ShowUI(
                UIEnum.LOSE_UI,
                "NO MORE SLOTS",
                (Action)FlickSortEventBus.RaiseRequestRetry);
        }

        private void OnDealRequested() => board?.Deal();
        private void OnShuffleRequested() => board?.Shuffle();

        private void RetryLevel()
        {
            uiManager.HideUI(UIEnum.LOSE_UI);
            board?.RetryLevel();
        }
    }
}
