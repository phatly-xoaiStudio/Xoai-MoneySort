using System;
using System.Collections.Generic;

namespace FlickSort
{
    public static class ChipShufflePlanner
    {
        private sealed class ColorBucket
        {
            public ChipColor Color { get; }
            public List<ChipToken> Tokens { get; }

            public ColorBucket(ChipColor color, List<ChipToken> tokens)
            {
                Color = color;
                Tokens = tokens;
            }
        }

        public static List<List<ChipToken>> Build(
            IReadOnlyList<ChipToken> chips,
            IReadOnlyList<int> targetCounts,
            int mergeChipCount,
            Random random)
        {
            if (chips == null)
                throw new ArgumentNullException(nameof(chips));
            if (targetCounts == null)
                throw new ArgumentNullException(nameof(targetCounts));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var expectedChipCount = 0;
            for (var i = 0; i < targetCounts.Count; i++)
            {
                if (targetCounts[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(targetCounts));
                expectedChipCount += targetCounts[i];
            }

            if (expectedChipCount != chips.Count)
                throw new ArgumentException(
                    "Target stack counts must contain every shuffled chip.",
                    nameof(targetCounts));

            var bucketsByColor = new Dictionary<ChipColor, List<ChipToken>>();
            for (var i = 0; i < chips.Count; i++)
            {
                var chip = chips[i];
                if (!bucketsByColor.TryGetValue(chip.Color, out var bucket))
                {
                    bucket = new List<ChipToken>();
                    bucketsByColor.Add(chip.Color, bucket);
                }
                bucket.Add(chip);
            }

            var buckets = new List<ColorBucket>(bucketsByColor.Count);
            foreach (var pair in bucketsByColor)
            {
                ShuffleList(pair.Value, random);
                buckets.Add(new ColorBucket(pair.Key, pair.Value));
            }

            ShuffleList(buckets, random);
            var result = new List<List<ChipToken>>(targetCounts.Count);
            var maxGroupSize = Math.Max(1, mergeChipCount - 1);

            for (var stackIndex = 0; stackIndex < targetCounts.Count; stackIndex++)
            {
                var targetCount = targetCounts[stackIndex];
                var stack = new List<ChipToken>(targetCount);
                ChipColor? previousColor = null;
                var remaining = targetCount;

                while (remaining > 0)
                {
                    var bucket = SelectBucket(buckets, previousColor, random);
                    if (bucket == null)
                        throw new InvalidOperationException(
                            "Shuffle planner ran out of chips before filling all stacks.");

                    var groupCount = Math.Min(
                        remaining,
                        Math.Min(maxGroupSize, bucket.Tokens.Count));

                    // When possible, reserve one slot for a second color so using
                    // shuffle cannot immediately create a full merge group.
                    if (stack.Count == 0 &&
                        remaining > 1 &&
                        groupCount == remaining &&
                        HasOtherColor(buckets, bucket.Color))
                    {
                        groupCount--;
                    }

                    var sourceStart = bucket.Tokens.Count - groupCount;
                    for (var i = sourceStart; i < bucket.Tokens.Count; i++)
                        stack.Add(bucket.Tokens[i]);
                    bucket.Tokens.RemoveRange(sourceStart, groupCount);

                    remaining -= groupCount;
                    previousColor = bucket.Color;
                }

                result.Add(stack);
            }

            return result;
        }

        private static ColorBucket SelectBucket(
            IReadOnlyList<ColorBucket> buckets,
            ChipColor? previousColor,
            Random random)
        {
            var hasDifferentColor = false;
            for (var i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].Tokens.Count > 0 &&
                    (!previousColor.HasValue || buckets[i].Color != previousColor.Value))
                {
                    hasDifferentColor = true;
                    break;
                }
            }

            ColorBucket selected = null;
            var largestCount = -1;
            var tieCount = 0;
            for (var i = 0; i < buckets.Count; i++)
            {
                var candidate = buckets[i];
                if (candidate.Tokens.Count == 0)
                    continue;
                if (hasDifferentColor &&
                    previousColor.HasValue &&
                    candidate.Color == previousColor.Value)
                {
                    continue;
                }

                if (candidate.Tokens.Count > largestCount)
                {
                    selected = candidate;
                    largestCount = candidate.Tokens.Count;
                    tieCount = 1;
                }
                else if (candidate.Tokens.Count == largestCount &&
                         random.Next(++tieCount) == 0)
                {
                    selected = candidate;
                }
            }

            return selected;
        }

        private static bool HasOtherColor(
            IReadOnlyList<ColorBucket> buckets,
            ChipColor excludedColor)
        {
            for (var i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].Color != excludedColor &&
                    buckets[i].Tokens.Count > 0)
                {
                    return true;
                }
            }
            return false;
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
