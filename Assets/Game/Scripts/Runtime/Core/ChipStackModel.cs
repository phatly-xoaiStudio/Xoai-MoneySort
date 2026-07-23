using System;
using System.Collections.Generic;
using FlickSort;
using UnityEngine;

namespace FlickSort
{
    public enum ChipColor
    {
        Red,
        Blue,
        Yellow,
        Green,
        Purple
    }

    [Serializable]
    public readonly struct ChipToken : IEquatable<ChipToken>
    {
        public ChipColor Color { get; }
        public int Level { get; }

        public ChipToken(ChipColor color, int level)
        {
            Color = color;
            Level = level;
        }

        public bool Equals(ChipToken other) => Color == other.Color && Level == other.Level;
        public override bool Equals(object obj) => obj is ChipToken other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)Color, Level);
    }

    public sealed class ChipStackModel
    {
        private readonly List<ChipToken> _chips = new();

        public int Capacity { get; }
        public int Count => _chips.Count;
        public int FreeSlots => Capacity - Count;
        public bool IsFull => Count >= Capacity;
        public IReadOnlyList<ChipToken> Chips => _chips;
        public ChipToken Top => _chips[^1];

        public ChipStackModel(int capacity)
        {
            Capacity = Math.Max(1, capacity);
        }

        public void Clear() => _chips.Clear();

        public bool TryAdd(ChipToken token)
        {
            if (IsFull)
                return false;
            _chips.Add(token);
            return true;
        }

        public int GetTopGroupCount()
        {
            if (_chips.Count == 0)
                return 0;

            var token = Top;
            var count = 1;
            for (var i = _chips.Count - 2; i >= 0 && _chips[i].Color == token.Color; i--)
                count++;
            return count;
        }

        public bool CanMoveTopGroupTo(ChipStackModel destination)
        {
            if (destination == null || ReferenceEquals(this, destination) || Count == 0)
                return false;

            var groupCount = GetTopGroupCount();
            if (destination.FreeSlots < groupCount)
                return false;
            if (destination.Count > 0)
                Debug.Log($"top moving color: {Top.Color}, top destination color: {destination.Top.Color}, free slots: {destination.FreeSlots}");
            else
            {
                Debug.Log($"top moving color: {Top.Color}, moving to empty stack with free slots: {destination.FreeSlots}   ");
            }
            return destination.Count == 0 || destination.Top.Color == Top.Color;
        }

        public List<ChipToken> RemoveTopGroup()
        {
            var count = GetTopGroupCount();
            var result = new List<ChipToken>(count);
            if (count == 0)
                return result;

            var start = _chips.Count - count;
            for (var i = start; i < _chips.Count; i++)
                result.Add(_chips[i]);
            _chips.RemoveRange(start, count);
            return result;
        }

        public void AddRange(IReadOnlyList<ChipToken> tokens)
        {
            if (tokens.Count > FreeSlots)
                throw new InvalidOperationException("Not enough free slots in destination stack.");
            for (var i = 0; i < tokens.Count; i++)
                _chips.Add(tokens[i]);
            Debug.Log($"this stack have {Count} chips.");
        }

        public bool TryMergeTop(int mergeCount, int maxLevel, out ChipToken result)
        {
            result = default;
            if (mergeCount < 2 || GetTopMatchingTokenCount() < mergeCount)
                return false;

            var source = Top;
            _chips.RemoveRange(_chips.Count - mergeCount, mergeCount);
            var nextLevel = Math.Min(maxLevel, source.Level + 1);
            var nextColor = nextLevel > source.Level
                ? (ChipColor)(((int)source.Color + 1) % Enum.GetValues(typeof(ChipColor)).Length)
                : source.Color;
            result = new ChipToken(nextColor, nextLevel);
            _chips.Add(result);
            return true;
        }

        private int GetTopMatchingTokenCount()
        {
            if (_chips.Count == 0)
                return 0;

            var token = Top;
            var count = 1;
            for (var i = _chips.Count - 2; i >= 0; i--)
            {
               if(_chips[i].Level==token.Level) count++;
                
            }
            return count;
        }
    }
}

