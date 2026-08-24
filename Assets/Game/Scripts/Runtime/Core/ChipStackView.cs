using DG.Tweening;
using UnityEngine;

namespace FlickSort
{
    public enum StackAccessState
    {
        Available,
        Rent,
        Locked
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class ChipStackView : MonoBehaviour
    {
        [SerializeField] private GameObject _blockPanel;
        [Header("Access visuals")]
        [SerializeField] private Vector3 _statusLabelLocalPosition = new(0f, 0f, 0.13f);
        [SerializeField, Min(1)] private int _statusLabelFontSize = 48;
        [SerializeField, Min(0.001f)] private float _statusLabelCharacterSize = 0.012f;
        [SerializeField] private Color _lockedPanelColor = new(0.43f, 0.47f, 0.54f);
        [SerializeField] private Color _lockedLabelColor = Color.white;
        [SerializeField] private Color _rentPanelColor = new(1f, 0.67f, 0.08f);
        [SerializeField] private Color _rentLabelColor = new(0.22f, 0.12f, 0.02f);

        public int Index { get; private set; }
        public ChipStackModel Model { get; private set; }
        public Transform ChipRoot { get; private set; }
        public StackAccessState AccessState { get; private set; }
        public bool IsAvailable => AccessState == StackAccessState.Available;
        public bool IsRentable => AccessState == StackAccessState.Rent;
        private BoxCollider _slotBounds;
        private Renderer _blockRenderer;
        private TextMesh _statusLabel;
        private MaterialPropertyBlock _blockProperties;
        private Camera _camera;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const string LockedLabel = "LOCK";
        private const string RentLabel = "RENT";

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

            _blockRenderer = _blockPanel.GetComponent<Renderer>();
            _blockProperties ??= new MaterialPropertyBlock();
            _camera = Camera.main;
            EnsureStatusLabel();
        }

        public void SetAccessState(StackAccessState state)
        {
            AccessState = state;
            _blockPanel.SetActive(state != StackAccessState.Available);
            _statusLabel.gameObject.SetActive(state != StackAccessState.Available);
            _statusLabel.text = state == StackAccessState.Rent ? RentLabel : LockedLabel;
            _statusLabel.color = state == StackAccessState.Rent
                ? _rentLabelColor
                : _lockedLabelColor;

            if (_blockRenderer != null && state != StackAccessState.Available)
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

        public void SetAvailable(bool available) => SetAccessState(
            available ? StackAccessState.Available : StackAccessState.Locked);

        private void EnsureStatusLabel()
        {
            var labelTransform = transform.Find("AccessStatusLabel");
            if (labelTransform == null)
            {
                var labelObject = new GameObject("AccessStatusLabel");
                labelTransform = labelObject.transform;
                labelTransform.SetParent(transform, false);
                labelTransform.localPosition = _statusLabelLocalPosition;
            }

            _statusLabel = labelTransform.GetComponent<TextMesh>();
            if (_statusLabel == null)
                _statusLabel = labelTransform.gameObject.AddComponent<TextMesh>();
            _statusLabel.anchor = TextAnchor.MiddleCenter;
            _statusLabel.alignment = TextAlignment.Center;
            _statusLabel.fontSize = _statusLabelFontSize;
            _statusLabel.characterSize = _statusLabelCharacterSize;
            _statusLabel.fontStyle = FontStyle.Bold;
        }

        private void LateUpdate()
        {
            if (_statusLabel == null || !_statusLabel.gameObject.activeInHierarchy)
                return;

            if (_camera == null)
                _camera = Camera.main;
            if (_camera != null)
                _statusLabel.transform.rotation = _camera.transform.rotation;
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
