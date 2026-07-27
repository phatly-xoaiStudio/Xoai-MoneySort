using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FlickSort
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Game Config", fileName = "FlickSortGameConfig")]
    public sealed class FlickSortGameConfig : ScriptableObject
    {
        [Header("Core rules")]
        [Min(2)] public int mergeChipCount = 10;
        [Range(1, 10)] public int maxChipLevel = 10;
        [Min(1)] public int stackCapacity = 10;
        [Min(1)] public int defaultDealChipCount = 10;
        public Vector2Int randomChipsPerStack = new(1, 3);

        [Header("Layout")]
        [Min(0.05f)] public float chipSpacing = 0.11f;
        [Min(0.5f)] public float stackSpacing = 1.45f;

        [Header("DOTween")]
        [Min(0.05f)] public float moveDuration = 0.28f;
        [Min(0.05f)] public float dealDuration = 0.34f;
        [Min(0.05f)] public float mergeDuration = 0.22f;
        [Min(0f)] public float chipMoveDelay = 0.045f;
        [Min(0f)] public float jumpPower = 0.75f;

        [Header("Hammer Skill")]
        [Min(0.05f)] public float hammerFlyDuration = 0.5f;
        [Min(0.1f)] public float hammerFlyDistance = 1.25f;
        [Min(0f)] public float hammerFlyStagger = 0.025f;

        [Header("Levels")]
        public List<LevelSettings> levels = new();

        public LevelSettings GetLevel(int oneBasedLevel)
        {
            if (levels.Count == 0)
                return LevelSettings.Default(oneBasedLevel);

            var index = Mathf.Clamp(oneBasedLevel - 1, 0, levels.Count - 1);
            var source = levels[index];
            if (oneBasedLevel <= levels.Count)
                return source;

            var extra = oneBasedLevel - levels.Count;
            source.levelNumber = oneBasedLevel;
            source.requiredChipScore += extra * mergeChipCount * 2;
            source.dealChipCount += extra / 2;
            return source;
        }

        private void OnValidate()
        {
            randomChipsPerStack.x = Mathf.Max(1, randomChipsPerStack.x);
            randomChipsPerStack.y = Mathf.Max(randomChipsPerStack.x, randomChipsPerStack.y);
            stackCapacity = Mathf.Max(mergeChipCount, stackCapacity);
        }
    }

    [Serializable]
    public struct LevelSettings
    {
        [Min(1)] public int levelNumber;
        [Range(3, 20)] public int openedStackCount;
        [Range(2, 10)] public int colorCount;
        [Min(1)] public int initialChipCount;
        [Min(1)] public int dealChipCount;
        [FormerlySerializedAs("requiredMerges")]
        [Min(1)] public int requiredChipScore;
        public Vector2Int chipsPerStackRange;
        public int randomSeed;

        public static LevelSettings Default(int level) => new()
        {
            levelNumber = level,
            openedStackCount = 20,
            colorCount = Mathf.Clamp(3 + (level - 1) / 4, 3, 5),
            initialChipCount = 24 + level * 2,
            dealChipCount = 8 + level,
            requiredChipScore = 50 + level * 20,
            chipsPerStackRange = new Vector2Int(1, 3),
            randomSeed = 1200 + level * 97
        };
    }
}
