using System;
using FlickSort.UI;
using UnityEngine;

namespace FlickSort.Data
{
    [CreateAssetMenu(fileName = "UIDefinitionSO", menuName = "Scriptable Objects/UIDefinitionSO")]
    public class UIDefinitionSO : ScriptableObject
    {
        [SerializeField] private UIDefinition[] definitions;
        

        public UIDefinition[] Definitions {get => definitions;}
    }
    [Serializable]
    public class UIDefinition
    {
        public UIEnum Name;
        public UIBase UI;
    }
}