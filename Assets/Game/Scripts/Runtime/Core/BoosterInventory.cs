using System;

namespace FlickSort
{
    public enum BoosterType
    {
        Shuffle,
        Hammer,
        FreeMove
    }

    public sealed class BoosterInventory
    {
        private readonly int[] _counts = new int[3];

        public BoosterInventory(int shuffle, int hammer, int freeMove)
        {
            _counts[(int)BoosterType.Shuffle] = Math.Max(0, shuffle);
            _counts[(int)BoosterType.Hammer] = Math.Max(0, hammer);
            _counts[(int)BoosterType.FreeMove] = Math.Max(0, freeMove);
        }

        public int GetCount(BoosterType type) => _counts[(int)type];

        public void Add(BoosterType type, int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            _counts[(int)type] = checked(_counts[(int)type] + amount);
        }

        public bool TryConsume(BoosterType type)
        {
            var index = (int)type;
            if (_counts[index] <= 0)
                return false;
            _counts[index]--;
            return true;
        }
    }
}
