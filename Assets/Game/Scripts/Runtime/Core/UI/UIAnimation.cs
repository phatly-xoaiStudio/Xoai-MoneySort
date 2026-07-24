using System;
using DG.Tweening;
using UnityEngine;

namespace FlickSort.UI
{
    public abstract class UIAnimation : MonoBehaviour
    {
        [Header("Show")]
        [SerializeField, Min(0f)] protected float showDuration = 0.3f;
        [SerializeField] protected Ease showEase = Ease.OutBack;

        [Header("Hide")]
        [SerializeField, Min(0f)] protected float hideDuration = 0.2f;
        [SerializeField] protected Ease hideEase = Ease.InBack;

        [Header("Time")]
        [SerializeField] private bool useUnscaledTime = true;

        private Tween _activeTween;

        public void PlayShow(Action completed = null)
        {
            Kill();
            SetHiddenState();
            SetInteraction(false);
            _activeTween = CreateShowTween()
                .SetUpdate(useUnscaledTime)
                .OnComplete(() =>
                {
                    _activeTween = null;
                    SetShownState();
                    SetInteraction(true);
                    completed?.Invoke();
                });
        }

        public void PlayHide(Action completed = null)
        {
            Kill();
            SetInteraction(false);
            _activeTween = CreateHideTween()
                .SetUpdate(useUnscaledTime)
                .OnComplete(() =>
                {
                    _activeTween = null;
                    SetHiddenState();
                    completed?.Invoke();
                });
        }

        public void SetHiddenImmediate()
        {
            Kill();
            SetHiddenState();
            SetInteraction(false);
        }

        protected abstract Tween CreateShowTween();
        protected abstract Tween CreateHideTween();
        protected abstract void SetShownState();
        protected abstract void SetHiddenState();
        protected virtual void SetInteraction(bool enabled) { }

        protected void Kill()
        {
            _activeTween?.Kill();
            _activeTween = null;
        }

        protected virtual void OnDestroy() => Kill();
    }
}
