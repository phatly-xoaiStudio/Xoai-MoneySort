using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FlickSort
{
    public static class FlickSortBoardRules
    {
        public const int TotalSlotCount = 15;
        public const int InitialAvailableSlotCount = 6;
        public const int RentSlotCount = 1;
        public const int LockedSlotCount = 8;
        public const int FirstLevelNumber = 1;
        public const int RentSlotIndex = InitialAvailableSlotCount;
        public const int FirstLockedSlotIndex = RentSlotIndex + RentSlotCount;

        private static readonly int[] LockedSlotUnlockLevels =
        {
            2,
            3,
            5,
            11,
            15,
            20,
            25,
            30
        };

        public static int GetAvailableSlotCount(int oneBasedLevel)
        {
            var availableCount = InitialAvailableSlotCount;
            for (var index = 0; index < LockedSlotUnlockLevels.Length; index++)
            {
                if (oneBasedLevel >= LockedSlotUnlockLevels[index])
                    availableCount++;
            }
            return availableCount;
        }

        public static StackAccessState GetAccessState(int slotIndex, int oneBasedLevel)
        {
            if (slotIndex < 0 || slotIndex >= TotalSlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            if (slotIndex < InitialAvailableSlotCount)
                return StackAccessState.Available;
            if (slotIndex == RentSlotIndex)
                return StackAccessState.Rent;

            var lockedSlotIndex = slotIndex - FirstLockedSlotIndex;
            return oneBasedLevel >= LockedSlotUnlockLevels[lockedSlotIndex]
                ? StackAccessState.Available
                : StackAccessState.Locked;
        }

        public static int GetUnlockLevel(int slotIndex)
        {
            if (slotIndex < FirstLockedSlotIndex || slotIndex >= TotalSlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            return LockedSlotUnlockLevels[slotIndex - FirstLockedSlotIndex];
        }

        public static int GetNextLockedSlotIndex(int oneBasedLevel)
        {
            for (var lockedIndex = 0; lockedIndex < LockedSlotUnlockLevels.Length; lockedIndex++)
            {
                if (oneBasedLevel < LockedSlotUnlockLevels[lockedIndex])
                    return FirstLockedSlotIndex + lockedIndex;
            }

            return -1;
        }
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Game Config", fileName = "FlickSortGameConfig")]
    public sealed class FlickSortGameConfig : ScriptableObject
    {
        [Header("Save Game")]
        [Tooltip("Load and persist player progress. When disabled, every game starts from default values.")]
        public bool enableSaveGame = true;

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

        [Header("Rent Slot")]
        [Min(1f)] public float rentSlotDurationSeconds = 60f;
        [Min(0)] public int rentSlotFreeUseCount = 2;
        [Min(0)] public int rentSlotCoinPrice = 250;

        [Header("Levels")]
        public List<LevelSettings> levels = new();

        public LevelSettings GetLevel(int oneBasedLevel)
        {
            if (levels.Count == 0)
                return LevelSettings.Default(oneBasedLevel);

            var index = Mathf.Clamp(oneBasedLevel - 1, 0, levels.Count - 1);
            var source = levels[index];
            var extra = Mathf.Max(0, oneBasedLevel - levels.Count);
            source.levelNumber = oneBasedLevel;
            source.openedStackCount = FlickSortBoardRules.GetAvailableSlotCount(oneBasedLevel);
            if (extra > 0)
            {
                source.requiredChipScore += extra * mergeChipCount * 2;
                source.dealChipCount += extra / 2;
            }
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
        [Range(0, FlickSortBoardRules.TotalSlotCount)] public int openedStackCount;
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
            openedStackCount = FlickSortBoardRules.GetAvailableSlotCount(level),
            colorCount = Mathf.Clamp(3 + (level - 1) / 4, 3, 5),
            initialChipCount = 24 + level * 2,
            dealChipCount = 8 + level,
            requiredChipScore = 50 + level * 20,
            chipsPerStackRange = new Vector2Int(1, 3),
            randomSeed = 1200 + level * 97
        };
    }
}
