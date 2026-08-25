using DG.Tweening;
using FlickSort.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort
{
    public enum StackAccessState
    {
        Available,
        Rented,
        RentClosing,
        Rent,
        Locked
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class ChipStackView : MonoBehaviour
    {
        [SerializeField] private GameObject _blockPanel;
        [Header("Access visual groups")]
        [SerializeField] private GameObject _lockVisualRoot;
        [SerializeField] private GameObject _rentVisualRoot;
        [Header("Lock visuals")]
        [SerializeField] private TextMeshPro _statusLabel;
        [SerializeField] private Color _lockedPanelColor = new(0.43f, 0.47f, 0.54f);
        [SerializeField] private Color _lockedLabelColor = Color.white;
        [SerializeField] private Color _rentPanelColor = new(1f, 0.67f, 0.08f);
        [SerializeField] private float _statusLabelXWithoutBadge;
        [SerializeField] private float _statusLabelXWithBadge = -0.1f;
        [Header("Next unlock badge")]
        [SerializeField] private GameObject _nextUnlockBadge;
        [SerializeField] private TextMeshPro _nextUnlockLevelLabel;
        [Header("Rent button")]
        [SerializeField] private Button _rentButton;
        [SerializeField] private TextMeshProUGUI _rentDurationLabel;
        [SerializeField] private GameObject _freeRentBadge;
        [SerializeField] private TextMeshProUGUI _freeRentCountLabel;

        public int Index { get; private set; }
        public ChipStackModel Model { get; private set; }
        public Transform ChipRoot { get; private set; }
        public StackAccessState AccessState { get; private set; }
        public bool IsAvailable =>
            AccessState == StackAccessState.Available ||
            AccessState == StackAccessState.Rented ||
            AccessState == StackAccessState.RentClosing;
        public bool IsRentable => AccessState == StackAccessState.Rent;
        public bool CanReceiveDeal => AccessState == StackAccessState.Available;
        private BoxCollider _slotBounds;
        private Renderer _blockRenderer;
        private MaterialPropertyBlock _blockProperties;
        private int _freeRentUsesRemaining;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const string LockedLabel = "LOCK";

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

            if (_blockPanel == null)
                throw new MissingReferenceException(
                    $"{nameof(ChipStackView)} '{name}' requires an authored BlockPanel reference.");
            if (_lockVisualRoot == null || _rentVisualRoot == null)
                throw new MissingReferenceException(
                    $"{nameof(ChipStackView)} '{name}' requires authored lock and rent visual roots.");
            if (_statusLabel == null)
                throw new MissingReferenceException(
                    $"{nameof(ChipStackView)} '{name}' requires an authored status label reference.");
            if (_nextUnlockBadge == null || _nextUnlockLevelLabel == null)
                throw new MissingReferenceException(
                    $"{nameof(ChipStackView)} '{name}' requires an authored next-unlock badge.");
            if (_rentButton == null || _rentDurationLabel == null ||
                _freeRentBadge == null || _freeRentCountLabel == null)
                throw new MissingReferenceException(
                    $"{nameof(ChipStackView)} '{name}' requires authored rent UI references.");

            _rentButton.onClick.RemoveListener(OnRentClicked);
            _rentButton.onClick.AddListener(OnRentClicked);

            _blockRenderer = _blockPanel.GetComponent<Renderer>();
            _blockProperties ??= new MaterialPropertyBlock();
        }

        public void SetAccessState(StackAccessState state, int displayedUnlockLevel = 0)
        {
            AccessState = state;
            var displaysClosedPanel =
                state == StackAccessState.Locked || state == StackAccessState.Rent;
            _blockPanel.SetActive(displaysClosedPanel);
            _lockVisualRoot.SetActive(state == StackAccessState.Locked);
            _rentVisualRoot.SetActive(
                state == StackAccessState.Rent ||
                state == StackAccessState.Rented ||
                state == StackAccessState.RentClosing);
            _rentButton.gameObject.SetActive(state == StackAccessState.Rent);
            RefreshFreeRentBadge();

            var highlightsNextUnlock =
                state == StackAccessState.Locked && displayedUnlockLevel > 0;
            _statusLabel.text = LockedLabel;
            _statusLabel.color = _lockedLabelColor;
            var statusLabelPosition = _statusLabel.transform.localPosition;
            statusLabelPosition.x = highlightsNextUnlock
                ? _statusLabelXWithBadge
                : _statusLabelXWithoutBadge;
            _statusLabel.transform.localPosition = statusLabelPosition;

            _nextUnlockBadge.SetActive(highlightsNextUnlock);
            if (highlightsNextUnlock)
                _nextUnlockLevelLabel.text = displayedUnlockLevel.ToString();

            if (_blockRenderer != null && displaysClosedPanel)
            {
                var color = state == StackAccessState.Rent
                    ? _rentPanelColor
                    : _lockedPanelColor;
                _blockRenderer.GetPropertyBlock(_blockProperties);
                _blockProperties.SetColor(BaseColorId, color);
                _blockProperties.SetColor(ColorId, color);
                _blockRenderer.SetPropertyBlock(_blockProperties);
            }

            if (state != StackAccessState.Available)
                SetSelected(false);
        }

        public void SetRentTimeRemaining(float seconds)
        {
            if (_rentDurationLabel != null)
                _rentDurationLabel.text = $"{Mathf.Max(0, Mathf.CeilToInt(seconds))}S";
        }

        public void SetFreeRentUsesRemaining(int remaining)
        {
            _freeRentUsesRemaining = Mathf.Max(0, remaining);
            RefreshFreeRentBadge();
        }

        private void RefreshFreeRentBadge()
        {
            if (_freeRentBadge == null || _freeRentCountLabel == null)
                return;
            var visible = AccessState == StackAccessState.Rent && _freeRentUsesRemaining > 0;
            _freeRentBadge.SetActive(visible);
            if (visible)
                _freeRentCountLabel.text = _freeRentUsesRemaining.ToString();
        }

        public void SetAvailable(bool available) => SetAccessState(
            available ? StackAccessState.Available : StackAccessState.Locked);

        private void OnRentClicked() => FlickSortEventBus.RaiseRentSlotRequested(Index);

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

        private void OnDestroy()
        {
            if (_rentButton != null)
                _rentButton.onClick.RemoveListener(OnRentClicked);
        }
    }
}
