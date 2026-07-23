using DG.Tweening;
using UnityEngine;

namespace FlickSort
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ChipStackView : MonoBehaviour
    {
        public int Index { get; private set; }
        public ChipStackModel Model { get; private set; }
        public Transform ChipRoot { get; private set; }
        private BoxCollider _slotBounds;

        public void Initialize(int index, ChipStackModel model)
        {
            Index = index;
            Model = model;
            name = $"Stack_{index + 1}";
            _slotBounds ??= GetComponent<BoxCollider>();

            if (ChipRoot == null)
            {
                ChipRoot = transform.Find("Chips");
                if (ChipRoot == null)
                    throw new MissingReferenceException(
                        $"{nameof(ChipStackView)} '{name}' requires an authored Chips child.");
            }
        }

        public Vector3 GetWorldSlot(int index, float spacing)
        {
            var bounds = _slotBounds.bounds;
            return new Vector3(
                bounds.center.x,
                bounds.max.y - spacing * (index + 0.5f),
                ChipRoot.position.z);
        }

        public void SetSelected(bool selected)
        {
            transform.DOScale(selected ? 1.05f : 1f, 0.12f);
        }

        public void InvalidFeedback()
        {
            transform.DOShakePosition(0.2f, 0.08f, 10, 50f, false, true);
        }
    }
}
