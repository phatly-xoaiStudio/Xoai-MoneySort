using System;
using System.Collections.Generic;

namespace FlickSort
{
    public static class ChipShufflePlanner
    {
        public static List<List<ChipToken>> BuildReplacement(
            int stackCount,
            int stackCapacity,
            int emptyStackCount,
            int highestLevel,
            int maxLevel,
            int higherValueCountMin,
            int higherValueCountMax,
            Random random)
        {
            if (stackCount < 0)
                throw new ArgumentOutOfRangeException(nameof(stackCount));
            if (stackCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(stackCapacity));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var result = new List<List<ChipToken>>(stackCount);
            for (var i = 0; i < stackCount; i++)
                result.Add(new List<ChipToken>());

            var occupiedCount = Math.Max(0, stackCount - Math.Max(0, emptyStackCount));
            if (occupiedCount == 0)
                return result;

            var stackIndices = new List<int>(stackCount);
            for (var i = 0; i < stackCount; i++)
                stackIndices.Add(i);
            ShuffleList(stackIndices, random);

            var clampedHighestLevel = Math.Max(0, Math.Min(highestLevel, maxLevel));
            for (var i = 0; i < occupiedCount; i++)
            {
                var level = random.Next(clampedHighestLevel + 1);
                var count = random.Next(1, stackCapacity + 1);
                AddSet(result[stackIndices[i]], level, count);
            }

            var higherLevel = Math.Min(maxLevel, clampedHighestLevel + 1);
            var minHigherCount = Math.Max(1, Math.Min(higherValueCountMin, stackCapacity));
            var maxHigherCount = Math.Max(
                minHigherCount,
                Math.Min(higherValueCountMax, stackCapacity));
            var higherCount = random.Next(minHigherCount, maxHigherCount + 1);
            var higherStack = result[stackIndices[random.Next(occupiedCount)]];
            higherStack.Clear();
            AddSet(higherStack, higherLevel, higherCount);

            return result;
        }

        private static void AddSet(List<ChipToken> stack, int level, int count)
        {
            var colorCount = Enum.GetValues(typeof(ChipColor)).Length;
            var token = new ChipToken((ChipColor)(level % colorCount), level);
            for (var i = 0; i < count; i++)
                stack.Add(token);
        }

        private static void ShuffleList<T>(IList<T> list, Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
