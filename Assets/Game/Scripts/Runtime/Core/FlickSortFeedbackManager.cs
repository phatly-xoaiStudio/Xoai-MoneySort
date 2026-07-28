using DG.Tweening;
using FlickSort.Core;
using UnityEngine;

namespace FlickSort
{
    public sealed class FlickSortFeedbackManager : MonoBehaviour
    {
        [Header("Merge VFX")]
        [SerializeField] private ParticleSystem mergeBurstPrefab;

        private ParticleSystem _mergeBurstInstance;
        private Transform _cameraTransform;

        private void Awake()
        {
            var mainCamera = Camera.main;
            _cameraTransform = mainCamera != null ? mainCamera.transform : null;
            EnsureMergeBurstInstance();
        }

        private void OnEnable()
        {
            FlickSortEventBus.MergeCompleted += OnMergeCompleted;
        }

        private void OnDisable()
        {
            FlickSortEventBus.MergeCompleted -= OnMergeCompleted;
            if (_mergeBurstInstance != null)
                _mergeBurstInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnMergeCompleted(Vector3 position)
        {
            _cameraTransform?.DOShakePosition(0.18f, 0.08f, 8, 45f, false, true);
            EnsureMergeBurstInstance();
            if (_mergeBurstInstance == null)
                return;

            _mergeBurstInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _mergeBurstInstance.transform.SetPositionAndRotation(
                position + Vector3.up * 0.25f,
                mergeBurstPrefab.transform.rotation);
            _mergeBurstInstance.Play(true);
        }

        private void EnsureMergeBurstInstance()
        {
            if (_mergeBurstInstance != null || mergeBurstPrefab == null)
                return;

            _mergeBurstInstance = Instantiate(mergeBurstPrefab, transform);
            _mergeBurstInstance.name = $"{mergeBurstPrefab.name} (Reusable)";
            _mergeBurstInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
