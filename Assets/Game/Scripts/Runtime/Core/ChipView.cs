using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FlickSort
{
    public sealed class ChipView : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private TextMeshPro _label;
        [SerializeField] private TrailRenderer[] _trails;
        [SerializeField] private int[] materialChangeSlots;   
        private Tween _activeTween;

        public ChipToken Token { get; private set; }

        public void Initialize(ChipToken token, Material[] material)
        {
            // _renderers ??= GetComponentsInChildren<Renderer>(true);
            // _trails ??= GetComponentsInChildren<TrailRenderer>(true);
            // if (_label == null)
            //     throw new MissingReferenceException(
            //         $"{nameof(ChipView)} on '{name}' requires an authored TextMeshPro label.");
            SetToken(token, material);
            SetTrail(false);
        }

        public void SetToken(ChipToken token, Material[] material)
        {
            Token = token;
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            foreach (var item in _renderers)
            {
                if (item is TrailRenderer || item is SpriteRenderer)
                    continue;
                var slots = item.sharedMaterials;
                // for (var i = 0; i < slots.Length; i++)
                //     slots[i] = material;    
                for (var i = 0; i < materialChangeSlots.Length; i++)
                {
                    slots[materialChangeSlots[i]] = material[i];
                }
                item.sharedMaterials = slots;
            }

            _label.text = $"{token.Level + 1}";
            // Level is shown by the label; physical chip dimensions stay uniform.
            transform.localScale = Vector3.one;
        }

        public Tween JumpTo(Vector3 destination, float jumpPower, float duration, float delay)
        {
            KillTween();
            SetTrail(true);
            _activeTween = transform.DOJump(destination, jumpPower, 1, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    SetTrail(false);
                    transform.DOPunchScale(Vector3.one * 0.08f, 0.16f, 4, 0.4f);
                });
            return _activeTween;
        }

        public Tween DealTo(Vector3 destination, float duration, float delay)
        {
            KillTween();
            SetTrail(true);
            _activeTween = transform.DOMove(destination, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .OnComplete(() => SetTrail(false));
            return _activeTween;
        }

        public Tween ArcTo(
            Vector3 destination,
            float arcHeight,
            float duration,
            float delay)
        {
            KillTween();
            SetTrail(true);

            var start = transform.position;
            var control = (start + destination) * 0.5f + Vector3.up * arcHeight;
            _activeTween = DOVirtual.Float(0f, 1f, duration, progress =>
                {
                    var inverse = 1f - progress;
                    transform.position =
                        inverse * inverse * start +
                        2f * inverse * progress * control +
                        progress * progress * destination;
                })
                .SetDelay(delay)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    transform.position = destination;
                    SetTrail(false);
                    transform.DOPunchScale(Vector3.one * 0.08f, 0.16f, 4, 0.4f);
                });
            return _activeTween;
        }

        public Tween MergeInto(Vector3 destination, float duration, float delay)
        {
            KillTween();
            _activeTween = DOTween.Sequence()
                .SetDelay(delay)
                .Append(transform.DOMove(destination, duration).SetEase(Ease.InQuad))
                .Join(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            return _activeTween;
        }

        public Tween BreakAway(
            Vector3 direction,
            float distance,
            float jumpPower,
            float duration,
            float delay)
        {
            KillTween();
            SetTrail(true);

            var destination = transform.position + direction.normalized * distance;
            var sequence = DOTween.Sequence().SetDelay(delay);
            sequence.Join(transform
                .DOJump(destination, jumpPower, 1, duration)
                .SetEase(Ease.OutCubic));
            sequence.Join(transform.DORotate(
                new Vector3(360f, 540f, 270f),
                duration,
                RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            sequence.Insert(
                duration * 0.2f,
                transform.DOScale(Vector3.zero, duration * 0.8f).SetEase(Ease.InBack));
            sequence.OnComplete(() => SetTrail(false));
            _activeTween = sequence;
            return _activeTween;
        }

        public void KillTween()
        {
            _activeTween?.Kill();
            _activeTween = null;
            transform.DOKill();
        }

        private void OnDestroy() => KillTween();

        private void SetTrail(bool enabled)
        {
            _trails ??= GetComponentsInChildren<TrailRenderer>(true);
            foreach (var trail in _trails)
            {
                trail.Clear();
                trail.emitting = enabled;
            }
        }

    }
}
