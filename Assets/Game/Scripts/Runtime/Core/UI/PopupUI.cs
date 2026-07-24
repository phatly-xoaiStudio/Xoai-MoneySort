using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public class PopupUI : UIBase
    {
        private TextMeshProUGUI _title;
        private Button _actionButton;
        private Action _action;

        public void Configure(TextMeshProUGUI title, Button actionButton = null)
        {
            _title = title;
            _actionButton = actionButton;
            if (_actionButton != null) _actionButton.onClick.AddListener(InvokeAction);
        }

        public override void SetData(params object[] data)
        {
            if (data.Length > 0 && data[0] is string title) _title.text = title;
            _action = data.Length > 1 ? data[1] as Action : null;
        }

        private void InvokeAction() => _action?.Invoke();
        private void OnDestroy()
        {
            if (_actionButton != null) _actionButton.onClick.RemoveListener(InvokeAction);
        }
    }
}
