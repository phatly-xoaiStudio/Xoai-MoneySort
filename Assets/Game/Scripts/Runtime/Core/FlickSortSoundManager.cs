using FlickSort.Core;
using UnityEngine;

namespace FlickSort
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FlickSortSoundManager : MonoBehaviour
    {
        [Header("SFX")]
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip mergeSound;
        [SerializeField] private AudioClip dealSound;

        [Header("Move cadence")]
        [SerializeField, Min(0f)] private float moveSoundMinInterval = 0.06f;
        [SerializeField, Range(0f, 1f)] private float moveSoundVolume = 0.5f;
        [SerializeField] private Vector2 moveSoundPitchRange = new(0.94f, 1.06f);

        private AudioSource _sfxSource;
        private float _nextMoveSoundTime;

        private void Awake()
        {
            _sfxSource = GetComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            FlickSortEventBus.DealStarted += PlayDealSound;
            FlickSortEventBus.ChipMoveLanded += PlayMoveSound;
            FlickSortEventBus.InvalidMove += PlayInvalidMoveSound;
            FlickSortEventBus.MergeCompleted += PlayMergeSound;
        }

        private void OnDisable()
        {
            FlickSortEventBus.DealStarted -= PlayDealSound;
            FlickSortEventBus.ChipMoveLanded -= PlayMoveSound;
            FlickSortEventBus.InvalidMove -= PlayInvalidMoveSound;
            FlickSortEventBus.MergeCompleted -= PlayMergeSound;
        }

        private void PlayDealSound() => PlayOneShot(dealSound, 0.6f);

        private void PlayInvalidMoveSound() => PlayOneShot(moveSound, 0.35f);

        private void PlayMergeSound(Vector3 _) => PlayOneShot(mergeSound, 0.9f);

        private void PlayMoveSound()
        {
            if (moveSound == null || _sfxSource == null)
                return;

            var now = Time.unscaledTime;
            if (now < _nextMoveSoundTime)
                return;

            _sfxSource.pitch = Random.Range(moveSoundPitchRange.x, moveSoundPitchRange.y);
            _sfxSource.PlayOneShot(moveSound, moveSoundVolume);
            _nextMoveSoundTime = now + moveSoundMinInterval;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || _sfxSource == null)
                return;

            _sfxSource.pitch = 1f;
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
