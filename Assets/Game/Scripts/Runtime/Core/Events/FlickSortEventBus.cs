using System;
using UnityEngine;

namespace FlickSort.Core
{
    /// <summary>Global requests shared by UI and the active game session.</summary>
    public static class FlickSortEventBus
    {
        public static event Action RequestDeal;
        public static event Action RequestRetry;
        public static event Action RequestNextLevel;
        public static event Action<bool> PauseChanged;
        public static event Action<int, int, int> ProgressChanged;
        public static event Action<int> LevelUp;
        public static event Action LevelUpAcknowledged;
        public static event Action LevelLost;
        public static event Action DealStarted;
        public static event Action ChipMoveLanded;
        public static event Action<Vector3> MergeCompleted;
        public static event Action InvalidMove;

        public static void RaiseRequestDeal() => RequestDeal?.Invoke();
        public static void RaiseRequestRetry() => RequestRetry?.Invoke();
        public static void RaiseRequestNextLevel() => RequestNextLevel?.Invoke();
        public static void RaisePauseChanged(bool isPaused) => PauseChanged?.Invoke(isPaused);
        public static void RaiseProgressChanged(int level, int current, int required) =>
            ProgressChanged?.Invoke(level, current, required);
        public static void RaiseLevelUp(int nextLevel) => LevelUp?.Invoke(nextLevel);
        public static void RaiseLevelUpAcknowledged() => LevelUpAcknowledged?.Invoke();
        public static void RaiseLevelLost() => LevelLost?.Invoke();
        public static void RaiseDealStarted() => DealStarted?.Invoke();
        public static void RaiseChipMoveLanded() => ChipMoveLanded?.Invoke();
        public static void RaiseMergeCompleted(Vector3 position) => MergeCompleted?.Invoke(position);
        public static void RaiseInvalidMove() => InvalidMove?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart() => Reset();

        public static void Reset()
        {
            RequestDeal = null;
            RequestRetry = null;
            RequestNextLevel = null;
            PauseChanged = null;
            ProgressChanged = null;
            LevelUp = null;
            LevelUpAcknowledged = null;
            LevelLost = null;
            DealStarted = null;
            ChipMoveLanded = null;
            MergeCompleted = null;
            InvalidMove = null;
        }
    }
}
