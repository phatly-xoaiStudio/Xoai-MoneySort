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
        [SerializeField] private Image _progressFill;
        [SerializeField] private Button _dealButton;

        // public void Configure(TextMeshProUGUI levelText, TextMeshProUGUI progressText, Image progressFill, Button dealButton)
        // {
        //     _levelText = levelText;
        //     _progressText = progressText;
        //     _progressFill = progressFill;
        //     _dealButton = dealButton;
        //     _dealButton.onClick.AddListener(OnDealClicked);
        // }
        public override void Init(UIManager manager)
        {
            base.Init(manager);
            _dealButton.onClick.AddListener(OnDealClicked);
        }

        public void SetProgress(int level, int current, int required)
        {
            _levelText.text = $"LEVEL {level}";
            _progressText.text = $"{current} / {required}";
            _progressFill.rectTransform.localScale = new Vector3(
                required > 0 ? Mathf.Clamp01((float)current / required) : 0f, 1f, 1f);
        }

        private void OnDealClicked() => FlickSortEventBus.RaiseRequestDeal();
        private void OnDestroy()
        {
            if (_dealButton != null) _dealButton.onClick.RemoveListener(OnDealClicked);
        }
    }
}
