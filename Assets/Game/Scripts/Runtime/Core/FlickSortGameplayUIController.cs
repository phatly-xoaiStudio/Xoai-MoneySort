using System;
using DG.Tweening;
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
            FlickSortEventBus.RequestRetry += RetryLevel;
            FlickSortEventBus.ProgressChanged += OnProgressChanged;
            FlickSortEventBus.LevelUp += OnLevelUp;
            FlickSortEventBus.LevelLost += OnLevelLost;
        }

        private void OnDisable()
        {
            FlickSortEventBus.RequestDeal -= OnDealRequested;
            FlickSortEventBus.RequestRetry -= RetryLevel;
            FlickSortEventBus.ProgressChanged -= OnProgressChanged;
            FlickSortEventBus.LevelUp -= OnLevelUp;
            FlickSortEventBus.LevelLost -= OnLevelLost;
        }

        private void OnProgressChanged(int level, int current, int required)
        {
            _gameplayUI?.SetProgress(level, current, required);
        }

        private void OnLevelUp(int nextLevel)
        {
            var popup = uiManager.ShowUI(
                UIEnum.LEVEL_UP_UI,
                nextLevel,
                (Action)(() =>
                {
                    uiManager.HideUI(UIEnum.LEVEL_UP_UI);
                    FlickSortEventBus.RaiseLevelUpAcknowledged();
                }));
            popup.transform.localScale = Vector3.zero;
            popup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        private void OnLevelLost()
        {
            uiManager.ShowUI(
                UIEnum.LOSE_UI,
                "NO MORE SLOTS",
                (Action)FlickSortEventBus.RaiseRequestRetry);
        }

        private void OnDealRequested() => board?.Deal();

        private void RetryLevel()
        {
            uiManager.HideUI(UIEnum.LOSE_UI);
            board?.RetryLevel();
        }
    }
}
