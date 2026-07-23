using System;
using UnityEngine;

namespace FlickSort.Data
{
    [CreateAssetMenu(fileName = "ChipColorConfigSO", menuName = "Scriptable Objects/ChipColorConfigSO")]
    public class ChipColorConfigSO : ScriptableObject
    {
        [SerializeField] private ChipColorData[] colors;
        public ChipColorData[] Colors => colors;
        
        public Material GetColor(ChipColor color) => colors[(int)color].Color;
    }
    [Serializable]
    public struct ChipColorData
    {
        public ChipColor ColorEnum;
        public Material Color;
    }
}