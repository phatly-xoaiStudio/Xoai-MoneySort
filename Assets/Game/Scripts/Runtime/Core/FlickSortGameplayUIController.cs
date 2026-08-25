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
        [Header("Boosters")]
        [SerializeField] private string shuffleSaveKey = "FlickSort.Booster.Shuffle";
        [SerializeField] private string hammerSaveKey = "FlickSort.Booster.Hammer";
        [SerializeField] private string freeMoveSaveKey = "FlickSort.Booster.FreeMove";

        private GameplayUI _gameplayUI;
        private MoneyWallet _moneyWallet;
        private BoosterInventory _boosters;
        private int _rentUseCount;
        private int _currentLevel = FlickSortBoardRules.FirstLevelNumber;

        public int Money => _moneyWallet?.Balance ?? 0;
#if GAME_IS_SAVE
        private bool SaveGameEnabled => board != null;
#else
        private bool SaveGameEnabled =false;
#endif
        
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
            var config = board.Config;
            _boosters = new BoosterInventory(
                LoadBooster(shuffleSaveKey, config.defaultShuffleCount),
                LoadBooster(hammerSaveKey, config.defaultHammerCount),
                LoadBooster(freeMoveSaveKey, config.defaultFreeMoveCount));
            _moneyWallet = new MoneyWallet(initialMoney);
            _moneyWallet.Changed += OnMoneyChanged;
            OnMoneyChanged(_moneyWallet.Balance);
            _gameplayUI.InitializeCheats(
                board.CheatWinLevel,
                board.CheatLoseLevel,
                CheatGoToLevel,
                CheatAddBooster,
                AddMoney);
            UpdateCheatBoosterDisplay();
            UpdateBoosterUI();
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
            FlickSortEventBus.RequestFreeMove += OnFreeMoveRequested;
            FlickSortEventBus.BoosterUsed += OnBoosterUsed;
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
            FlickSortEventBus.RequestFreeMove -= OnFreeMoveRequested;
            FlickSortEventBus.BoosterUsed -= OnBoosterUsed;
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
            _currentLevel = level;
            _gameplayUI?.SetProgress(level, current, required);
            UpdateBoosterUI();
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
        private void OnShuffleRequested()
        {
            if (!CanUseBooster(BoosterType.Shuffle) || !board.TryShuffle())
                return;
            ConsumeBooster(BoosterType.Shuffle);
        }

        private void OnHammerRequested()
        {
            if (CanUseBooster(BoosterType.Hammer))
                board.ActivateHammer();
        }

        private void OnFreeMoveRequested()
        {
            if (CanUseBooster(BoosterType.FreeMove))
                board.ActivateFreeMove();
        }

        private void OnBoosterUsed(BoosterType type) => ConsumeBooster(type);

        private void RetryLevel()
        {
            uiManager.HideUI(UIEnum.LOSE_UI);
            board?.RetryLevel();
        }

        private void CheatGoToLevel(int level)
        {
            uiManager.HideUI(UIEnum.LEVEL_UP_UI);
            uiManager.HideUI(UIEnum.LOSE_UI);
            board.StartLevel(Mathf.Max(1, level));
        }

        private void CheatAddBooster(BoosterType type)
        {
            _boosters.Add(type, 1);
            SaveBoosters();
            UpdateBoosterUI();
            UpdateCheatBoosterDisplay();
        }

        private void UpdateCheatBoosterDisplay() =>
            _gameplayUI?.SetCheatBoosterCounts(
                _boosters.GetCount(BoosterType.Shuffle),
                _boosters.GetCount(BoosterType.Hammer),
                _boosters.GetCount(BoosterType.FreeMove));

        private int LoadBooster(string key, int defaultCount) =>
            SaveGameEnabled ? PlayerPrefs.GetInt(key, defaultCount) : defaultCount;

        private bool CanUseBooster(BoosterType type) =>
            _boosters.GetCount(type) > 0 && _currentLevel >= GetUnlockLevel(type);

        private int GetUnlockLevel(BoosterType type)
        {
            var config = board.Config;
            return type switch
            {
                BoosterType.Shuffle => config.shuffleUnlockLevel,
                BoosterType.Hammer => config.hammerUnlockLevel,
                BoosterType.FreeMove => config.freeMoveUnlockLevel,
                _ => int.MaxValue
            };
        }

        private void ConsumeBooster(BoosterType type)
        {
            if (!_boosters.TryConsume(type))
                return;
            SaveBoosters();
            UpdateBoosterUI();
            UpdateCheatBoosterDisplay();
        }

        private void UpdateBoosterUI()
        {
            if (_gameplayUI == null || _boosters == null)
                return;

            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                var unlockLevel = GetUnlockLevel(type);
                _gameplayUI.SetBoosterState(
                    type,
                    _boosters.GetCount(type),
                    _currentLevel >= unlockLevel,
                    unlockLevel);
            }
        }

        private void SaveBoosters()
        {
            if (!SaveGameEnabled)
                return;
            PlayerPrefs.SetInt(shuffleSaveKey, _boosters.GetCount(BoosterType.Shuffle));
            PlayerPrefs.SetInt(hammerSaveKey, _boosters.GetCount(BoosterType.Hammer));
            PlayerPrefs.SetInt(freeMoveSaveKey, _boosters.GetCount(BoosterType.FreeMove));
            PlayerPrefs.Save();
        }
    }
}
