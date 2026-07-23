using NUnit.Framework;

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
        public void Move_RejectsDestinationWithoutEnoughSlots()
        {
            var source = CreateStack(10, ChipColor.Red, 1, 2);
            var destination = CreateStack(10, ChipColor.Red, 1, 9);

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

        private static ChipStackModel CreateStack(int capacity, ChipColor color, int level, int count)
        {
            var stack = new ChipStackModel(capacity);
            for (var i = 0; i < count; i++)
                stack.TryAdd(new ChipToken(color, level));
            return stack;
        }
    }
}
