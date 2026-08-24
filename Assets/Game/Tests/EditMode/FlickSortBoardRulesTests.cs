using NUnit.Framework;

namespace FlickSort.Tests
{
    public sealed class FlickSortBoardRulesTests
    {
        private static readonly int[] UnlockLevels = { 2, 3, 5, 11, 15, 20, 25, 30 };

        [Test]
        public void LevelOne_HasSixAvailableOneRentAndEightLockedSlots()
        {
            var available = 0;
            var rent = 0;
            var locked = 0;

            for (var slotIndex = 0; slotIndex < FlickSortBoardRules.TotalSlotCount; slotIndex++)
            {
                switch (FlickSortBoardRules.GetAccessState(slotIndex, 1))
                {
                    case StackAccessState.Available:
                        available++;
                        break;
                    case StackAccessState.Rent:
                        rent++;
                        break;
                    case StackAccessState.Locked:
                        locked++;
                        break;
                }
            }

            Assert.That(available, Is.EqualTo(6));
            Assert.That(rent, Is.EqualTo(1));
            Assert.That(locked, Is.EqualTo(8));
        }

        [Test]
        public void LockedSlots_UnlockInConfiguredOrderAtRequiredLevels()
        {
            for (var lockedIndex = 0; lockedIndex < UnlockLevels.Length; lockedIndex++)
            {
                var slotIndex = FlickSortBoardRules.FirstLockedSlotIndex + lockedIndex;
                var requiredLevel = UnlockLevels[lockedIndex];

                Assert.That(FlickSortBoardRules.GetUnlockLevel(slotIndex), Is.EqualTo(requiredLevel));
                Assert.That(
                    FlickSortBoardRules.GetAccessState(slotIndex, requiredLevel - 1),
                    Is.EqualTo(StackAccessState.Locked));
                Assert.That(
                    FlickSortBoardRules.GetAccessState(slotIndex, requiredLevel),
                    Is.EqualTo(StackAccessState.Available));
            }
        }

        [Test]
        public void RentSlot_RemainsRentAtEveryProgressionMilestone()
        {
            foreach (var level in UnlockLevels)
            {
                Assert.That(
                    FlickSortBoardRules.GetAccessState(FlickSortBoardRules.RentSlotIndex, level),
                    Is.EqualTo(StackAccessState.Rent));
            }
        }

        [TestCase(1, 7, 2)]
        [TestCase(2, 8, 3)]
        [TestCase(3, 9, 5)]
        [TestCase(9, 10, 11)]
        [TestCase(11, 11, 15)]
        [TestCase(15, 12, 20)]
        [TestCase(20, 13, 25)]
        [TestCase(25, 14, 30)]
        public void NextLockedSlot_ReportsItsUnlockLevel(
            int currentLevel,
            int expectedSlotIndex,
            int expectedUnlockLevel)
        {
            var slotIndex = FlickSortBoardRules.GetNextLockedSlotIndex(currentLevel);

            Assert.That(slotIndex, Is.EqualTo(expectedSlotIndex));
            Assert.That(
                FlickSortBoardRules.GetUnlockLevel(slotIndex),
                Is.EqualTo(expectedUnlockLevel));
        }

        [Test]
        public void LevelThirty_HasNoNextLockedSlot()
        {
            Assert.That(FlickSortBoardRules.GetNextLockedSlotIndex(30), Is.EqualTo(-1));
        }
    }
}
