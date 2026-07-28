using System;
using System.Collections.Generic;
using FlickSort.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FlickSort.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Responsive Layout")]
        [SerializeField] private Vector2 referenceResolution = new(1080f, 1920f);
        [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;
        [SerializeField] private bool applySafeAreaToGameplay = true;

        private readonly Dictionary<UIEnum, UIBase> _views = new();
        private RectTransform _gameplayRoot;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        public void Init(UIDefinition[] definitions)
        {
            ConfigureCanvasScaler();
            _views.Clear();
            foreach (var view in definitions)
            {
                if (view == null || view.UI == null || _views.ContainsKey(view.Name))
                    continue;

                var ui = Instantiate(view.UI, transform);
                StretchToParent(ui.transform as RectTransform);
                ui.Init(this);
                ui.HideImmediate();
                _views.Add(view.Name, ui);

                if (view.Name == UIEnum.GAMEPLAY_UI)
                    _gameplayRoot = ui.transform as RectTransform;
            }

            RefreshSafeArea(true);
        }

        private void Update()
        {
            if (applySafeAreaToGameplay)
                RefreshSafeArea(false);
        }

        public UIBase GetUi(UIEnum uiEnum) =>
            _views.TryGetValue(uiEnum, out var view) ? view : null;

        public UIBase ShowUI(UIEnum uiEnum, params object[] data)
        {
            var view = GetUi(uiEnum);
            if (view == null)
                throw new InvalidOperationException($"UI {uiEnum} is not registered in UIDefinitionSO.");
            view.SetData(data);
            view.Show();
            return view;
        }

        public void HideUI(UIEnum uiEnum)
        {
            GetUi(uiEnum)?.Hide();
        }

        public void HideAll()
        {
            foreach (var view in _views.Values)
                view.Hide();
        }

        private void ConfigureCanvasScaler()
        {
            if (!TryGetComponent(out CanvasScaler scaler))
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        private void RefreshSafeArea(bool force)
        {
            if (_gameplayRoot == null)
                return;

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = applySafeAreaToGameplay
                ? Screen.safeArea
                : new Rect(Vector2.zero, screenSize);

            if (!force && screenSize == _lastScreenSize && safeArea == _lastSafeArea)
                return;

            _lastScreenSize = screenSize;
            _lastSafeArea = safeArea;

            var width = Mathf.Max(1f, screenSize.x);
            var height = Mathf.Max(1f, screenSize.y);
            _gameplayRoot.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            _gameplayRoot.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            _gameplayRoot.offsetMin = Vector2.zero;
            _gameplayRoot.offsetMax = Vector2.zero;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
