using System;
using NUnit.Framework;

namespace FlickSort.Tests
{
    public sealed class MoneyWalletTests
    {
        [Test]
        public void Add_IncreasesBalanceAndRaisesChanged()
        {
            var wallet = new MoneyWallet(100);
            var reported = -1;
            wallet.Changed += value => reported = value;

            wallet.Add(50);

            Assert.That(wallet.Balance, Is.EqualTo(150));
            Assert.That(reported, Is.EqualTo(150));
        }

        [Test]
        public void TrySpend_WithEnoughMoney_DeductsBalance()
        {
            var wallet = new MoneyWallet(300);

            var spent = wallet.TrySpend(250);

            Assert.That(spent, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(50));
        }

        [Test]
        public void TrySpend_WithoutEnoughMoney_DoesNotChangeBalance()
        {
            var wallet = new MoneyWallet(100);

            var spent = wallet.TrySpend(250);

            Assert.That(spent, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Mutations_RejectNonPositiveAmounts(int amount)
        {
            var wallet = new MoneyWallet(100);

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Add(amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend(amount));
        }
    }
}
