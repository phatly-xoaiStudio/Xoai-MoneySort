using TMPro;
using UnityEngine;

namespace FlickSort.UI
{
    public sealed class LevelUpUI : UIBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;

        public override void SetData(params object[] data)
        {
            if (data.Length == 0)
                return;

            _titleText.text = data[0] switch
            {
                int level => $"LEVEL {level}!",
                string title => title,
                _ => _titleText.text
            };
        }
    }
}
