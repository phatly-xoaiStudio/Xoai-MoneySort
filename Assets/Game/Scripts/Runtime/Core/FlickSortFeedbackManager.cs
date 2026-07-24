using DG.Tweening;
using FlickSort.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlickSort
{
    public sealed class FlickSortFeedbackManager : MonoBehaviour
    {
        private Material _particleMaterial;

        private void OnEnable()
        {
            FlickSortEventBus.MergeCompleted += OnMergeCompleted;
        }

        private void OnDisable()
        {
            FlickSortEventBus.MergeCompleted -= OnMergeCompleted;
        }

        private void OnDestroy()
        {
            if (_particleMaterial != null)
                Destroy(_particleMaterial);
        }

        private void OnMergeCompleted(Vector3 position)
        {
            Camera.main?.transform.DOShakePosition(0.18f, 0.08f, 8, 45f, false, true);
            SpawnMergeBurst(position + Vector3.up * 0.25f);
        }

        private void SpawnMergeBurst(Vector3 position)
        {
            var effect = new GameObject("MergeBurst");
            effect.transform.position = position;
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.05f),
                Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 18));

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (_particleMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader != null)
                    _particleMaterial = new Material(shader);
            }

            renderer.sharedMaterial = _particleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
            Destroy(effect, 1.2f);
        }
    }
}
