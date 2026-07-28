using System;
using System.Collections;
using DG.Tweening;
using FlickSort.Data;
using FlickSort.UI;
using UnityEngine;

namespace FlickSort
{
    public sealed class FlickSortBootstrap : MonoBehaviour
    {
        [SerializeField] private UIDefinitionSO uiDefinitionSo;
        [SerializeField] private ChipColorConfigSO chipColorConfigSo;
        [SerializeField] private Transform chipSpawner;
        [SerializeField] private FlickSortBoard board;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private FlickSortGameplayUIController gameplayUIController;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            uiManager.Init(uiDefinitionSo.Definitions);
            gameplayUIController.Init();
        }

        private void Start()
        {
            board.Init(chipColorConfigSo, chipSpawner);

            uiManager.ShowUI(UIEnum.LOADING_UI, new object[]
            {
                (Action<Action>)InitializeGame,
                (Action<Action>)PreloadGame,
                (Action)FinishLoading
            });
        }

        private void InitializeGame(Action complete)
        {
            StartCoroutine(InitializeGameRoutine(complete));
        }

        private static IEnumerator InitializeGameRoutine(Action complete)
        {
            yield return null;
            complete?.Invoke();
        }

        private void PreloadGame(Action complete)
        {
            StartCoroutine(PreloadGameRoutine(complete));
        }

        private IEnumerator PreloadGameRoutine(Action complete)
        {
            board.gameObject.SetActive(true);
            yield return null;
            yield return null;

            var timeoutAt = Time.realtimeSinceStartup + 5f;
            while (board != null && board.IsBusy && Time.realtimeSinceStartup < timeoutAt)
                yield return null;

            complete?.Invoke();
        }

        private void FinishLoading()
        {
            uiManager.HideUI(UIEnum.LOADING_UI);
            uiManager.ShowUI(UIEnum.GAMEPLAY_UI);
        }
    }
}
