using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class GameplayCheatUI : MonoBehaviour
    {
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _winButton;
        [SerializeField] private Button _loseButton;
        [SerializeField] private Button _goToLevelButton;
        [SerializeField] private TMP_InputField _levelInput;
        [SerializeField] private Button _addShuffleButton;
        [SerializeField] private Button _addHammerButton;
        [SerializeField] private Button _addFreeMoveButton;
        [SerializeField] private Button _addCoinsButton;
        [SerializeField] private TMP_InputField _coinInput;
        [SerializeField] private TextMeshProUGUI _statusText;

        private Action _win;
        private Action _lose;
        private Action<int> _goToLevel;
        private Action<BoosterType> _addBooster;
        private Action<int> _addCoins;
        private bool _initialized;

        public void Initialize(
            Action win,
            Action lose,
            Action<int> goToLevel,
            Action<BoosterType> addBooster,
            Action<int> addCoins)
        {
#if GAME_IS_CHEAT
            gameObject.SetActive(true);            
#else
            gameObject.SetActive(false);           
#endif
            _win = win;
            _lose = lose;
            _goToLevel = goToLevel;
            _addBooster = addBooster;
            _addCoins = addCoins;

            if (_initialized)
                return;

            ValidateReferences();
            _toggleButton.onClick.AddListener(Toggle);
            _winButton.onClick.AddListener(OnWinClicked);
            _loseButton.onClick.AddListener(OnLoseClicked);
            _goToLevelButton.onClick.AddListener(OnGoToLevelClicked);
            _addShuffleButton.onClick.AddListener(OnAddShuffleClicked);
            _addHammerButton.onClick.AddListener(OnAddHammerClicked);
            _addFreeMoveButton.onClick.AddListener(OnAddFreeMoveClicked);
            _addCoinsButton.onClick.AddListener(OnAddCoinsClicked);
            _contentRoot.SetActive(false);
            _initialized = true;
        }

        public void SetBoosterCounts(int shuffle, int hammer, int freeMove)
        {
            _statusText.text = $"S:{shuffle}  H:{hammer}  F:{freeMove}";
        }

        private void Toggle() => _contentRoot.SetActive(!_contentRoot.activeSelf);
        private void OnWinClicked() => _win?.Invoke();
        private void OnLoseClicked() => _lose?.Invoke();
        private void OnAddShuffleClicked() => _addBooster?.Invoke(BoosterType.Shuffle);
        private void OnAddHammerClicked() => _addBooster?.Invoke(BoosterType.Hammer);
        private void OnAddFreeMoveClicked() => _addBooster?.Invoke(BoosterType.FreeMove);

        private void OnGoToLevelClicked()
        {
            if (TryReadPositive(_levelInput, out var level))
                _goToLevel?.Invoke(level);
        }

        private void OnAddCoinsClicked()
        {
            if (TryReadPositive(_coinInput, out var amount))
                _addCoins?.Invoke(amount);
        }

        private static bool TryReadPositive(TMP_InputField input, out int value) =>
            int.TryParse(input.text, out value) && value > 0;

        private void ValidateReferences()
        {
            if (_contentRoot == null || _toggleButton == null || _winButton == null ||
                _loseButton == null || _goToLevelButton == null || _levelInput == null ||
                _addShuffleButton == null || _addHammerButton == null ||
                _addFreeMoveButton == null || _addCoinsButton == null ||
                _coinInput == null || _statusText == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(GameplayCheatUI)} requires all authored UI references.");
            }
        }

        private void OnDestroy()
        {
            if (!_initialized)
                return;

            _toggleButton.onClick.RemoveListener(Toggle);
            _winButton.onClick.RemoveListener(OnWinClicked);
            _loseButton.onClick.RemoveListener(OnLoseClicked);
            _goToLevelButton.onClick.RemoveListener(OnGoToLevelClicked);
            _addShuffleButton.onClick.RemoveListener(OnAddShuffleClicked);
            _addHammerButton.onClick.RemoveListener(OnAddHammerClicked);
            _addFreeMoveButton.onClick.RemoveListener(OnAddFreeMoveClicked);
            _addCoinsButton.onClick.RemoveListener(OnAddCoinsClicked);
        }
    }
}
