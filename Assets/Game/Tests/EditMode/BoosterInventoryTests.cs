using NUnit.Framework;

namespace FlickSort.Tests
{
    public sealed class BoosterInventoryTests
    {
        [Test]
        public void Defaults_AreAvailableOnce()
        {
            var inventory = new BoosterInventory(1, 1, 1);

            Assert.That(inventory.TryConsume(BoosterType.Shuffle), Is.True);
            Assert.That(inventory.TryConsume(BoosterType.Shuffle), Is.False);
            Assert.That(inventory.GetCount(BoosterType.Hammer), Is.EqualTo(1));
            Assert.That(inventory.GetCount(BoosterType.FreeMove), Is.EqualTo(1));
        }

        [Test]
        public void Add_IncreasesOnlyRequestedBooster()
        {
            var inventory = new BoosterInventory(0, 0, 0);

            inventory.Add(BoosterType.FreeMove, 1);

            Assert.That(inventory.GetCount(BoosterType.FreeMove), Is.EqualTo(1));
            Assert.That(inventory.GetCount(BoosterType.Shuffle), Is.Zero);
            Assert.That(inventory.GetCount(BoosterType.Hammer), Is.Zero);
        }
    }
}
