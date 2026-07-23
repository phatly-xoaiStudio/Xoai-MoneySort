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

        public static void RaiseRequestDeal() => RequestDeal?.Invoke();
        public static void RaiseRequestRetry() => RequestRetry?.Invoke();
        public static void RaiseRequestNextLevel() => RequestNextLevel?.Invoke();
        public static void RaisePauseChanged(bool isPaused) => PauseChanged?.Invoke(isPaused);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart() => Reset();

        public static void Reset()
        {
            RequestDeal = null;
            RequestRetry = null;
            RequestNextLevel = null;
            PauseChanged = null;
        }
    }
}
