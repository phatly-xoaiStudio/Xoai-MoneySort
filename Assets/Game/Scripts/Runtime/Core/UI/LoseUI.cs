using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class LoseUI : UIBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _retryButton;

        private Action _retryAction;

        public override void Init(UIManager manager)
        {
            base.Init(manager);
            _retryButton.onClick.AddListener(OnRetryClicked);
        }

        public override void SetData(params object[] data)
        {
            if (data.Length > 0 && data[0] is string title)
                _titleText.text = title;

            _retryAction = data.Length > 1 ? data[1] as Action : null;
        }

        public override void Hide()
        {
            _retryAction = null;
            base.Hide();
        }

        private void OnRetryClicked() => _retryAction?.Invoke();

        private void OnDestroy()
        {
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(OnRetryClicked);
        }
    }
}
