using System;

namespace FlickSort
{
    public sealed class MoneyWallet
    {
        public event Action<int> Changed;

        public int Balance { get; private set; }

        public MoneyWallet(int initialBalance)
        {
            if (initialBalance < 0)
                throw new ArgumentOutOfRangeException(nameof(initialBalance));
            Balance = initialBalance;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Balance = checked(Balance + amount);
            Changed?.Invoke(Balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (Balance < amount)
                return false;

            Balance -= amount;
            Changed?.Invoke(Balance);
            return true;
        }
    }
}
