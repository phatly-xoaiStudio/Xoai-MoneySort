using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlickSort.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonScaleAnimation : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [Header("Target")]
        [SerializeField] private RectTransform target;

        [Header("Press")]
        [SerializeField] private Vector3 pressedScale = new(0.9f, 0.9f, 1f);
        [SerializeField, Min(0f)] private float pressDuration = 0.08f;
        [SerializeField] private Ease pressEase = Ease.OutQuad;

        [Header("Release")]
        [SerializeField, Min(0f)] private float releaseDuration = 0.12f;
        [SerializeField] private Ease releaseEase = Ease.OutBack;

        [Header("Time")]
        [SerializeField] private bool useUnscaledTime = true;

        private Button _button;
        private Tween _activeTween;
        private Vector3 _defaultScale;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (target == null)
                target = transform as RectTransform;
            _defaultScale = target.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.IsInteractable())
                return;

            AnimateTo(Vector3.Scale(_defaultScale, pressedScale), pressDuration, pressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(_defaultScale, releaseDuration, releaseEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(_defaultScale, releaseDuration, releaseEase);
        }

        private void AnimateTo(Vector3 scale, float duration, Ease ease)
        {
            _activeTween?.Kill();
            _activeTween = target
                .DOScale(scale, duration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime)
                .OnComplete(() => _activeTween = null);
        }

        private void OnDisable()
        {
            _activeTween?.Kill();
            _activeTween = null;
            if (target != null)
                target.localScale = _defaultScale;
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
            _activeTween = null;
        }
    }
}
