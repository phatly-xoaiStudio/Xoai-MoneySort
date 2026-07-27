using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace FlickSort.Tests
{
    public sealed class ChipStackModelTests
    {
        [Test]
        public void TopGroup_ContainsOnlyMatchingContiguousTokens()
        {
            var stack = new ChipStackModel(10);
            stack.TryAdd(new ChipToken(ChipColor.Blue, 1));
            stack.TryAdd(new ChipToken(ChipColor.Red, 1));
            stack.TryAdd(new ChipToken(ChipColor.Red, 1));

            Assert.That(stack.GetTopGroupCount(), Is.EqualTo(2));
        }

        [Test]
        public void Move_AllowsEmptyOrMatchingDestination()
        {
            var source = CreateStack(10, ChipColor.Red, 1, 2);
            var empty = new ChipStackModel(10);
            var matching = CreateStack(10, ChipColor.Red, 1, 1);
            var wrong = CreateStack(10, ChipColor.Blue, 1, 1);

            Assert.That(source.CanMoveTopGroupTo(empty), Is.True);
            Assert.That(source.CanMoveTopGroupTo(matching), Is.True);
            Assert.That(source.CanMoveTopGroupTo(wrong), Is.False);
        }

        [Test]
        public void Move_AllowsPartialTransferWhenDestinationHasFewerSlots()
        {
            var source = CreateStack(10, ChipColor.Red, 1, 3);
            var destination = CreateStack(10, ChipColor.Red, 1, 9);

            Assert.That(source.CanMoveTopGroupTo(destination), Is.True);

            var moved = source.RemoveTopGroup(destination.FreeSlots);
            destination.AddRange(moved);

            Assert.That(moved.Count, Is.EqualTo(1));
            Assert.That(source.Count, Is.EqualTo(2));
            Assert.That(destination.Count, Is.EqualTo(10));
        }

        [Test]
        public void Move_RejectsFullDestination()
        {
            var source = CreateStack(10, ChipColor.Red, 1, 2);
            var destination = CreateStack(10, ChipColor.Red, 1, 10);

            Assert.That(source.CanMoveTopGroupTo(destination), Is.False);
        }

        [Test]
        public void Move_AllowsSameColorWithDifferentLevel()
        {
            var mergedChip = CreateStack(10, ChipColor.Blue, 2, 1);
            var sameColor = CreateStack(10, ChipColor.Blue, 1, 1);

            Assert.That(mergedChip.CanMoveTopGroupTo(sameColor), Is.True);
        }

        [Test]
        public void TopGroup_IncludesContiguousSameColorAcrossLevels()
        {
            var stack = new ChipStackModel(10);
            stack.TryAdd(new ChipToken(ChipColor.Red, 1));
            stack.TryAdd(new ChipToken(ChipColor.Blue, 1));
            stack.TryAdd(new ChipToken(ChipColor.Blue, 2));

            Assert.That(stack.GetTopGroupCount(), Is.EqualTo(2));
        }

        [Test]
        public void Merge_ConsumesTenAndCreatesNextLevel()
        {
            var stack = CreateStack(10, ChipColor.Green, 3, 10);

            var merged = stack.TryMergeTop(10, 10, out var result);

            Assert.That(merged, Is.True);
            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(result.Color, Is.EqualTo(ChipColor.Purple));
            Assert.That(result.Level, Is.EqualTo(4));
        }

        [Test]
        public void Merge_LevelTenRemainsLevelTen()
        {
            var stack = CreateStack(10, ChipColor.Purple, 10, 10);

            stack.TryMergeTop(10, 10, out var result);

            Assert.That(result.Level, Is.EqualTo(10));
            Assert.That(result.Color, Is.EqualTo(ChipColor.Purple));
            Assert.That(stack.Count, Is.EqualTo(1));
        }

        [Test]
        public void Merge_LastPaletteColorWrapsToFirstColor()
        {
            var stack = CreateStack(10, ChipColor.Purple, 3, 10);

            stack.TryMergeTop(10, 10, out var result);

            Assert.That(result.Level, Is.EqualTo(4));
            Assert.That(result.Color, Is.EqualTo(ChipColor.Red));
        }

        [Test]
        public void Merge_RejectsMixedLevelsWithSameColor()
        {
            var stack = CreateStack(10, ChipColor.Green, 1, 9);
            stack.TryAdd(new ChipToken(ChipColor.Green, 2));

            var merged = stack.TryMergeTop(10, 10, out _);

            Assert.That(merged, Is.False);
            Assert.That(stack.Count, Is.EqualTo(10));
        }

        [Test]
        public void Shuffle_PreservesStackCountsAndUsesTwoColorsWhenPossible()
        {
            var chips = new List<ChipToken>();
            for (var i = 0; i < 10; i++)
                chips.Add(new ChipToken(ChipColor.Red, 1));
            for (var i = 0; i < 10; i++)
                chips.Add(new ChipToken(ChipColor.Blue, 2));

            var plan = ChipShufflePlanner.Build(
                chips,
                new[] { 10, 10 },
                10,
                new Random(42));

            Assert.That(plan[0].Count, Is.EqualTo(10));
            Assert.That(plan[1].Count, Is.EqualTo(10));
            Assert.That(CountColors(plan[0]), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountColors(plan[1]), Is.GreaterThanOrEqualTo(2));
            Assert.That(LongestColorRun(plan[0]), Is.LessThan(10));
            Assert.That(LongestColorRun(plan[1]), Is.LessThan(10));
        }

        [Test]
        public void Shuffle_WithOneColorNeverLosesChips()
        {
            var chips = new List<ChipToken>();
            for (var i = 0; i < 12; i++)
                chips.Add(new ChipToken(ChipColor.Green, 3));

            var plan = ChipShufflePlanner.Build(
                chips,
                new[] { 6, 6 },
                10,
                new Random(7));

            Assert.That(plan[0].Count, Is.EqualTo(6));
            Assert.That(plan[1].Count, Is.EqualTo(6));
        }

        private static int CountColors(IReadOnlyList<ChipToken> chips)
        {
            var colors = new HashSet<ChipColor>();
            for (var i = 0; i < chips.Count; i++)
                colors.Add(chips[i].Color);
            return colors.Count;
        }

        private static int LongestColorRun(IReadOnlyList<ChipToken> chips)
        {
            var longest = 0;
            var current = 0;
            ChipColor? previous = null;
            for (var i = 0; i < chips.Count; i++)
            {
                current = previous == chips[i].Color ? current + 1 : 1;
                previous = chips[i].Color;
                longest = Math.Max(longest, current);
            }
            return longest;
        }

        private static ChipStackModel CreateStack(int capacity, ChipColor color, int level, int count)
        {
            var stack = new ChipStackModel(capacity);
            for (var i = 0; i < count; i++)
                stack.TryAdd(new ChipToken(color, level));
            return stack;
        }
    }
}
