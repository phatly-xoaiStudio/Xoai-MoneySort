using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FlickSort.UI
{
    public sealed class LevelUpUI : UIBase, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _titleText;

        private Action _tapAction;

        public override void SetData(params object[] data)
        {
            if (data.Length > 0)
            {
                _titleText.text = data[0] switch
                {
                    int level => $"LEVEL {level}!",
                    string title => title,
                    _ => _titleText.text
                };
            }

            _tapAction = data.Length > 1 ? data[1] as Action : null;
        }

        public override void Hide()
        {
            _tapAction = null;
            base.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var action = _tapAction;
            _tapAction = null;
            action?.Invoke();
        }
    }
}
