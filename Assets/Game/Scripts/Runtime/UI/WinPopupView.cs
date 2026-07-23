using System;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    [DisallowMultipleComponent]
    public sealed class WinPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button nextButton;

        public event Action NextRequested;

        private void Awake()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(NotifyNext);
        }

        private void OnDestroy()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(NotifyNext);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void NotifyNext() => NextRequested?.Invoke();
    }
}
