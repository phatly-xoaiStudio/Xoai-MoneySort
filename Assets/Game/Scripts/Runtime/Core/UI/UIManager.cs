using System;
using System.Collections.Generic;
using FlickSort.Data;
using UnityEngine;

namespace FlickSort.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private readonly Dictionary<UIEnum, UIBase> _views = new();


        public void Init(UIDefinition[] definitions)
        {
            _views.Clear();
            foreach (var view in definitions)
            {
                if (view == null || view.UI == null || _views.ContainsKey(view.Name))
                    continue;
                var ui = Instantiate(view.UI, transform);
                ui.Init(this);
                ui.HideImmediate();
                _views.Add(view.Name, ui);
            }
        }
        
        
        // public T Register<T>(T view) where T : UIBase
        // {
        //     _views[typeof(T)] = view;
        //     view.Init(this);
        //     return view;
        // }

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

        public void HideUI( UIEnum uiEnum)
        {
            GetUi(uiEnum)?.Hide();
        }
        public void HideAll()
        {
            foreach (var view in _views.Values) view.Hide();
        }
    }
}
