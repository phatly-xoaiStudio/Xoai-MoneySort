using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class RentSlotOffer
    {
        public float DurationSeconds { get; }
        public int CoinPrice { get; }
        public int CurrentMoney { get; }
        public int FreeUsesRemaining { get; }
        public Action ConfirmFree { get; }
        public Action ConfirmCoin { get; }
        public Action Close { get; }

        public RentSlotOffer(
            float durationSeconds,
            int coinPrice,
            int currentMoney,
            int freeUsesRemaining,
            Action confirmFree,
            Action confirmCoin,
            Action close)
        {
            DurationSeconds = durationSeconds;
            CoinPrice = coinPrice;
            CurrentMoney = currentMoney;
            FreeUsesRemaining = freeUsesRemaining;
            ConfirmFree = confirmFree;
            ConfirmCoin = confirmCoin;
            Close = close;
        }
    }

    public sealed class RentSlotUI : UIBase
    {
        [SerializeField] private TextMeshProUGUI _durationText;
        [SerializeField] private TextMeshProUGUI _coinPriceText;
        [SerializeField] private TextMeshProUGUI _freeText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _coinButton;
        [SerializeField] private Button _freeButton;
        [SerializeField] private Button _closeButton;

        private RentSlotOffer _offer;

        public override void Init(UIManager manager)
        {
            base.Init(manager);
            _coinButton.onClick.AddListener(OnCoinClicked);
            _freeButton.onClick.AddListener(OnFreeClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void SetData(params object[] data)
        {
            _offer = data.Length > 0 ? data[0] as RentSlotOffer : null;
            if (_offer == null)
                return;

            _durationText.text = $"{Mathf.RoundToInt(_offer.DurationSeconds)} SEC";
            _coinPriceText.text = _offer.CoinPrice.ToString("N0");
            _freeText.text = _offer.FreeUsesRemaining > 0
                ? $"FREE ({_offer.FreeUsesRemaining} LEFT)"
                : "FREE";
            var usesFreeRent = _offer.FreeUsesRemaining > 0;
            _freeButton.gameObject.SetActive(usesFreeRent);
            _coinButton.gameObject.SetActive(!usesFreeRent);
            _coinButton.interactable =
                !usesFreeRent && _offer.CurrentMoney >= _offer.CoinPrice;
            _messageText.text = usesFreeRent
                ? "FREE RENT"
                : _coinButton.interactable
                    ? "GET RENT SLOT"
                    : $"NEED {_offer.CoinPrice - _offer.CurrentMoney:N0} MORE COINS";
        }

        public override void Hide()
        {
            _offer = null;
            base.Hide();
        }

        private void OnCoinClicked() => _offer?.ConfirmCoin?.Invoke();
        private void OnFreeClicked() => _offer?.ConfirmFree?.Invoke();
        private void OnCloseClicked() => _offer?.Close?.Invoke();

        private void OnDestroy()
        {
            if (_coinButton != null) _coinButton.onClick.RemoveListener(OnCoinClicked);
            if (_freeButton != null) _freeButton.onClick.RemoveListener(OnFreeClicked);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
