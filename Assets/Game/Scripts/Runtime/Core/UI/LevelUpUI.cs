using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class LevelUpUI : UIBase, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [Header("Unlocked chip reward")]
        // [SerializeField] private GameObject _unlockRewardRoot;
        [SerializeField] private RectTransform _rotatingGlow;
        // [SerializeField] private RawImage _chipPreviewImage;
        // [SerializeField] private Camera _chipPreviewCamera;
        // [SerializeField] private Transform _unlockChipPivot;
        // [SerializeField] private ChipView _unlockChipView;
        [SerializeField, Min(0.1f)] private float _glowRotationDuration = 5f;
        [SerializeField, Min(0.05f)] private float _chipPopDuration = 0.35f;
        [SerializeField, Range(0f, 0.5f)] private float _chipPulseStrength = 0.12f;
        [SerializeField, Min(0.1f)] private float _chipRotationDuration = 2.5f;
        [Header("ComfirmIndicator")]
        [SerializeField] private TextMeshProUGUI _confirmIndicator;
        [SerializeField, Min(0.05f)] private float _fadeDuration = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _minimumFadeAlpha = 0.18f;

        private Action _tapAction;
        private Sequence _rewardSequence;
        private Vector3 _chipBaseScale = Vector3.one;

        public override void SetData(params object[] data)
        {
            if (data.Length > 0)
            {
                _levelText.text = data[0] switch
                {
                    int level => $"{level}",
                    string title => title,
                    _ => _levelText.text
                };
            }

            var unlockedChipLevel = data.Length > 1 && data[1] is int value
                ? value
                : -1;
            var chipMaterial = data.Length > 2 ? data[2] as Material : null;
            _tapAction = data.Length > 3 ? data[3] as Action : null;
            SetUnlockReward(unlockedChipLevel, chipMaterial);
        }

        public override void Show()
        {
            base.Show();
            PlayConfirmAnimation();
        }

        public override void Hide()
        {
            StopConfirmAnimation();
            StopRewardAnimation();
            _tapAction = null;
            base.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var action = _tapAction;
            _tapAction = null;
            action?.Invoke();
        }

        private void SetUnlockReward(int unlockedChipLevel, Material chipMaterial)
        {
            // var showReward =
            //     unlockedChipLevel >= 0 &&
            //     chipMaterial != null &&
            //     _unlockRewardRoot != null &&
            //     _unlockChipView != null;
            // if (_unlockRewardRoot != null)
            //     _unlockRewardRoot.SetActive(showReward);
            // if (_chipPreviewCamera != null)
            //     _chipPreviewCamera.enabled = showReward;
            // if (!showReward)
            // {
            //     StopRewardAnimation();
            //     return;
            // }

            var colorCount = System.Enum.GetValues(typeof(ChipColor)).Length;
            var token = new ChipToken(
                (ChipColor)(unlockedChipLevel % colorCount),
                unlockedChipLevel);
            // _unlockChipView.Initialize(token, new[] { chipMaterial });
            FramePreviewCamera();

            PlayRewardAnimation();
        }

        private void FramePreviewCamera()
        {
            // if (_chipPreviewCamera == null || _unlockChipView == null)
            //     return;
            //
            // var renderers = _unlockChipView.GetComponentsInChildren<Renderer>(true);
            // if (renderers.Length == 0)
            //     return;
            //
            // var bounds = renderers[0].bounds;
            // for (var i = 1; i < renderers.Length; i++)
            //     bounds.Encapsulate(renderers[i].bounds);
            //
            // var largestExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);
            // _chipPreviewCamera.orthographicSize = Mathf.Max(0.01f, largestExtent * 1.45f);
            // _chipPreviewCamera.transform.SetPositionAndRotation(
            //     new Vector3(
            //         bounds.center.x,
            //         bounds.center.y,
            //         bounds.min.z - Mathf.Max(0.1f, bounds.size.z * 3f)),
            //     Quaternion.identity);
            // _chipPreviewCamera.nearClipPlane = 0.001f;
            // _chipPreviewCamera.farClipPlane = Mathf.Max(1f, bounds.size.z * 8f);
        }

        private void PlayRewardAnimation()
        {
            StopRewardAnimation();

            if (_rotatingGlow != null)
            {
                _rotatingGlow.localRotation = Quaternion.identity;
                _rotatingGlow
                    .DORotate(new Vector3(0f, 0f, -360f), _glowRotationDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1);
            }

            // if (_unlockChipPivot == null)
            //     return;
            //
            // _chipBaseScale = _unlockChipPivot.localScale;
            // _unlockChipPivot.localScale = Vector3.zero;
            // _unlockChipView.transform.localRotation = Quaternion.identity;
            // _unlockChipView.transform
                // .DOLocalRotate(
                //     new Vector3(0f, -360f, 0f),
                //     _chipRotationDuration,
                //     RotateMode.FastBeyond360)
                // .SetEase(Ease.Linear)
                // .SetLoops(-1, LoopType.Restart);
            // _rewardSequence = DOTween.Sequence()
            //     .Append(_unlockChipPivot.DOScale(_chipBaseScale, _chipPopDuration).SetEase(Ease.OutBack))
            //     .Append(_unlockChipPivot.DOPunchScale(
            //         _chipBaseScale * _chipPulseStrength,
            //         0.8f,
            //         4,
            //         0.4f))
            //     .SetLoops(-1, LoopType.Restart)
            //     .OnKill(() =>
            //     {
            //         // if (_unlockChipPivot != null)
            //         //     _unlockChipPivot.localScale = _chipBaseScale;
            //     });
        }

        private void PlayConfirmAnimation()
        {
            if (_confirmIndicator == null)
                return;

            StopConfirmAnimation();
            _confirmIndicator.alpha = 1f;
            DOTween.To(
                    () => _confirmIndicator.alpha,
                    value => _confirmIndicator.alpha = value,
                    _minimumFadeAlpha,
                    Mathf.Max(0.05f, _fadeDuration))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(_confirmIndicator);
        }

        private void StopConfirmAnimation()
        {
            if (_confirmIndicator == null)
                return;

            DOTween.Kill(_confirmIndicator);
            _confirmIndicator.alpha = 1f;
        }

        private void StopRewardAnimation()
        {
            _rewardSequence?.Kill();
            _rewardSequence = null;
            _rotatingGlow?.DOKill();
            // _unlockChipPivot?.DOKill();
            // _unlockChipView?.transform.DOKill();
        }

        private void OnDestroy()
        {
            StopConfirmAnimation();
            StopRewardAnimation();
        }
    }
}
