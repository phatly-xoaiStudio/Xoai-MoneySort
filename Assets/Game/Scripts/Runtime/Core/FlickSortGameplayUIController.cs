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
        [Header("Money")]
        [SerializeField, Min(0)] private int defaultMoney = 300;
        [SerializeField] private string moneySaveKey = "FlickSort.Resource.Money";
        [Header("Rent Slot")]
        [SerializeField] private string rentUseCountSaveKey = "FlickSort.RentSlot.UseCount";

        private GameplayUI _gameplayUI;
        private MoneyWallet _moneyWallet;
        private int _rentUseCount;

        public int Money => _moneyWallet?.Balance ?? 0;
        private bool SaveGameEnabled => board != null && board.Config.enableSaveGame;

        public void Init()
        {
            _gameplayUI = uiManager.GetUi(UIEnum.GAMEPLAY_UI) as GameplayUI;
            if (_gameplayUI == null)
                throw new MissingReferenceException("Gameplay UI is missing from UIManager definitions.");

            var initialMoney = SaveGameEnabled
                ? PlayerPrefs.GetInt(moneySaveKey, defaultMoney)
                : defaultMoney;
            _rentUseCount = SaveGameEnabled
                ? PlayerPrefs.GetInt(rentUseCountSaveKey, 0)
                : 0;
            _moneyWallet = new MoneyWallet(initialMoney);
            _moneyWallet.Changed += OnMoneyChanged;
            OnMoneyChanged(_moneyWallet.Balance);
        }

        public void AddMoney(int amount) => _moneyWallet?.Add(amount);

        public bool TrySpendMoney(int amount) =>
            _moneyWallet != null && _moneyWallet.TrySpend(amount);

        private void OnMoneyChanged(int balance)
        {
            if (SaveGameEnabled)
            {
                PlayerPrefs.SetInt(moneySaveKey, balance);
                PlayerPrefs.Save();
            }
            _gameplayUI?.SetMoney(balance);
        }

        private void OnEnable()
        {
            FlickSortEventBus.RequestDeal += OnDealRequested;
            FlickSortEventBus.RequestShuffle += OnShuffleRequested;
            FlickSortEventBus.RequestHammer += OnHammerRequested;
            FlickSortEventBus.RequestRetry += RetryLevel;
            FlickSortEventBus.ProgressChanged += OnProgressChanged;
            FlickSortEventBus.LevelUp += OnLevelUp;
            FlickSortEventBus.LevelLost += OnLevelLost;
            FlickSortEventBus.RentSlotRequested += OnRentSlotRequested;
        }

        private void OnDisable()
        {
            FlickSortEventBus.RequestDeal -= OnDealRequested;
            FlickSortEventBus.RequestShuffle -= OnShuffleRequested;
            FlickSortEventBus.RequestHammer -= OnHammerRequested;
            FlickSortEventBus.RequestRetry -= RetryLevel;
            FlickSortEventBus.ProgressChanged -= OnProgressChanged;
            FlickSortEventBus.LevelUp -= OnLevelUp;
            FlickSortEventBus.LevelLost -= OnLevelLost;
            FlickSortEventBus.RentSlotRequested -= OnRentSlotRequested;
        }

        private void OnDestroy()
        {
            if (_moneyWallet != null)
                _moneyWallet.Changed -= OnMoneyChanged;
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

        private void OnRentSlotRequested(int stackIndex)
        {
            var config = board.Config;
            var freeUsesRemaining = Mathf.Max(0, config.rentSlotFreeUseCount - _rentUseCount);
            uiManager.ShowUI(
                UIEnum.RENT_SLOT_UI,
                new RentSlotOffer(
                    config.rentSlotDurationSeconds,
                    config.rentSlotCoinPrice,
                    Money,
                    freeUsesRemaining,
                    () => ConfirmRent(stackIndex),
                    () => ConfirmRent(stackIndex),
                    () => uiManager.HideUI(UIEnum.RENT_SLOT_UI)));
        }

        private void ConfirmRent(int stackIndex)
        {
            var config = board.Config;
            var usesFreeRent = _rentUseCount < config.rentSlotFreeUseCount;
            if (!usesFreeRent && !TrySpendMoney(config.rentSlotCoinPrice))
                return;

            if (!board.RentStackForDuration(stackIndex, config.rentSlotDurationSeconds))
            {
                if (!usesFreeRent)
                    AddMoney(config.rentSlotCoinPrice);
                return;
            }

            if (usesFreeRent)
            {
                _rentUseCount++;
                if (SaveGameEnabled)
                {
                    PlayerPrefs.SetInt(rentUseCountSaveKey, _rentUseCount);
                    PlayerPrefs.Save();
                }
            }
            uiManager.HideUI(UIEnum.RENT_SLOT_UI);
        }

        private void OnDealRequested() => board?.Deal();
        private void OnShuffleRequested() => board?.Shuffle();
        private void OnHammerRequested() => board?.ActivateHammer();

        private void RetryLevel()
        {
            uiManager.HideUI(UIEnum.LOSE_UI);
            board?.RetryLevel();
        }
    }
}
