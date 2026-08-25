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
                var requiredLevel = UnlockLevels[lockedIndex];
                var levelBeforeUnlock = requiredLevel - 1;
                var slotIndex = FlickSortBoardRules.GetNextLockedSlotIndex(levelBeforeUnlock);

                Assert.That(
                    FlickSortBoardRules.GetUnlockLevel(slotIndex, levelBeforeUnlock),
                    Is.EqualTo(requiredLevel));
                Assert.That(
                    FlickSortBoardRules.GetAccessState(slotIndex, levelBeforeUnlock),
                    Is.EqualTo(StackAccessState.Locked));
            }
        }

        [Test]
        public void RentSlot_StartsAtRowTwoColumnFive_ThenMovesToRowThreeColumnFive()
        {
            Assert.That(FlickSortBoardRules.GetRentSlotIndex(1), Is.EqualTo(9));
            Assert.That(FlickSortBoardRules.GetAccessState(9, 1), Is.EqualTo(StackAccessState.Rent));
            Assert.That(FlickSortBoardRules.GetRentSlotIndex(10), Is.EqualTo(9));
            Assert.That(FlickSortBoardRules.GetRentSlotIndex(11), Is.EqualTo(14));
            Assert.That(FlickSortBoardRules.GetAccessState(9, 11), Is.EqualTo(StackAccessState.Available));
            Assert.That(FlickSortBoardRules.GetAccessState(14, 11), Is.EqualTo(StackAccessState.Rent));
        }

        [TestCase(1, 6, 2)]
        [TestCase(2, 7, 3)]
        [TestCase(3, 8, 5)]
        [TestCase(9, 14, 11)]
        [TestCase(11, 10, 15)]
        [TestCase(15, 11, 20)]
        [TestCase(20, 12, 25)]
        [TestCase(25, 13, 30)]
        public void NextLockedSlot_ReportsItsUnlockLevel(
            int currentLevel,
            int expectedSlotIndex,
            int expectedUnlockLevel)
        {
            var slotIndex = FlickSortBoardRules.GetNextLockedSlotIndex(currentLevel);

            Assert.That(slotIndex, Is.EqualTo(expectedSlotIndex));
            Assert.That(
                FlickSortBoardRules.GetUnlockLevel(slotIndex, currentLevel),
                Is.EqualTo(expectedUnlockLevel));
        }

        [Test]
        public void LevelThirty_HasNoNextLockedSlot()
        {
            Assert.That(FlickSortBoardRules.GetNextLockedSlotIndex(30), Is.EqualTo(-1));
        }
    }
}
