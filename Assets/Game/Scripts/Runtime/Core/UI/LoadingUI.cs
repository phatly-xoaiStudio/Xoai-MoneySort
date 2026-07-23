using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class LoadingUI : UIBase
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TextMeshProUGUI _loadingText;
        private Action _onFinished;
        private Action<Action> _onPreLoad;
        private Action<Action> _onInitComplete;
        private Coroutine _loadingRoutine;
        private Coroutine _animationRoutine;

        // public void Configure(Image fillImage, TextMeshProUGUI loadingText)
        // {
        //     _fillImage = fillImage;
        //     _loadingText = loadingText;
        // }

        public override void SetData(params object[] data)
        {
            _onInitComplete = data.Length > 0 ? data[0] as Action<Action> : null;
            _onPreLoad = data.Length > 1 ? data[1] as Action<Action> : null;
            _onFinished = data.Length > 2 ? data[2] as Action : null;
        }

        public override void Show()
        {
            base.Show();
            StopRunningCoroutines();
            _fillImage.fillAmount = 0f;
            _loadingRoutine = StartCoroutine(LoadRoutine());
            _animationRoutine = StartCoroutine(AnimateLabel());
        }

        public override void Hide()
        {
            StopRunningCoroutines();
            _fillImage.DOKill();
            _loadingText.DOKill();
            _fillImage.fillAmount = 0f;
            base.Hide();
        }

        private IEnumerator LoadRoutine()
        {
            yield return TweenProgress(0.20f, 0.18f);
            yield return RunPhase(_onInitComplete);
            yield return TweenProgress(0.60f, 0.22f);
            yield return RunPhase(_onPreLoad);
            yield return TweenProgress(1f, 0.25f);
            yield return null;
            _onFinished?.Invoke();
        }

        private IEnumerator RunPhase(Action<Action> phase)
        {
            if (phase == null) yield break;
            var complete = false;
            phase(() => complete = true);
            yield return new WaitUntil(() => complete);
        }

        private IEnumerator TweenProgress(float target, float duration)
        {
            var done = false;
            DOVirtual.Float(_fillImage.fillAmount, target, duration, value => _fillImage.fillAmount = value)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => done = true);
            yield return new WaitUntil(() => done);
        }

        private IEnumerator AnimateLabel()
        {
            var dots = 0;
            while (gameObject.activeSelf)
            {
                _loadingText.text = "LOADING" + new string('.', dots);
                dots = (dots + 1) % 4;
                yield return new WaitForSecondsRealtime(0.22f);
            }
        }

        private void StopRunningCoroutines()
        {
            if (_loadingRoutine != null) StopCoroutine(_loadingRoutine);
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _loadingRoutine = null;
            _animationRoutine = null;
        }

        private void OnDisable() => StopRunningCoroutines();
    }
}
