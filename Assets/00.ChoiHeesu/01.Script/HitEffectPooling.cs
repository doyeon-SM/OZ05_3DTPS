using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _00.ChoiHeesu._01.Script
{
    [DisallowMultipleComponent]
    public class HitEffectPooling : MonoBehaviour
    {
        private const string DefaultPoolFolderName = "HitEffectPoolFolder";

        [Header("Hit Effect")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectLifeTime = 0.2f;
        [SerializeField] private float hitEffectSurfaceOffset = 0.01f;

        [Header("Pool")]
        [SerializeField] private Transform poolFolder;
        [SerializeField] private string poolFolderName = DefaultPoolFolderName;
        [SerializeField] private int initialEffectCreateCount = 10;
        [SerializeField] private int maxEffectCount = 30;
        [SerializeField] private bool createAdditionalEffectUntilMax = true;
        [SerializeField] private bool logSkipWhenPoolIsFull;

        [Header("Particle Option")]
        [SerializeField] private bool clearParticleOnPlay = true;
        [SerializeField] private bool stopParticleOnReturn = true;

        private readonly Queue<GameObject> inactiveEffects = new Queue<GameObject>();
        private readonly List<GameObject> allEffects = new List<GameObject>();
        private readonly HashSet<GameObject> activeEffects = new HashSet<GameObject>();

        private bool isInitialized;
        private bool hasRegisteredFolderChildren;

        public float HitEffectLifeTime => Mathf.Max(hitEffectLifeTime, 0.01f);
        public float HitEffectSurfaceOffset => Mathf.Max(hitEffectSurfaceOffset, 0f);
        public bool HasHitEffectPrefab => hitEffectPrefab != null;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            hitEffectLifeTime = Mathf.Max(hitEffectLifeTime, 0.01f);
            hitEffectSurfaceOffset = Mathf.Max(hitEffectSurfaceOffset, 0f);
            initialEffectCreateCount = Mathf.Max(initialEffectCreateCount, 0);
            maxEffectCount = Mathf.Max(maxEffectCount, 1);

            if (maxEffectCount < initialEffectCreateCount)
                maxEffectCount = initialEffectCreateCount;

            if (string.IsNullOrWhiteSpace(poolFolderName))
                poolFolderName = DefaultPoolFolderName;
        }

        public void ConfigureIfEmpty(GameObject fallbackHitEffectPrefab, float fallbackHitEffectLifeTime)
        {
            ConfigureIfEmpty(fallbackHitEffectPrefab, fallbackHitEffectLifeTime, hitEffectSurfaceOffset);
        }

        public void ConfigureIfEmpty(GameObject fallbackHitEffectPrefab, float fallbackHitEffectLifeTime, float fallbackHitEffectSurfaceOffset)
        {
            if (hitEffectPrefab != null || fallbackHitEffectPrefab == null)
                return;

            hitEffectPrefab = fallbackHitEffectPrefab;
            hitEffectLifeTime = Mathf.Max(fallbackHitEffectLifeTime, 0.01f);
            hitEffectSurfaceOffset = Mathf.Max(fallbackHitEffectSurfaceOffset, 0f);
            isInitialized = false;
            EnsureInitialized();
        }

        public bool TryPlayHitEffect(Vector3 position, Quaternion rotation)
        {
            if (!EnsureInitialized())
                return false;

            GameObject hitEffect = GetAvailableEffect();
            if (hitEffect == null)
                return false;

            Transform effectTransform = hitEffect.transform;
            effectTransform.SetPositionAndRotation(position, rotation);

            hitEffect.SetActive(true);
            PlayParticles(hitEffect);
            StartCoroutine(ReturnAfterLifeTime(hitEffect, HitEffectLifeTime));
            return true;
        }

        private bool EnsureInitialized()
        {
            EnsurePoolFolder();
            RegisterFolderChildren();

            if (isInitialized)
                return true;

            if (hitEffectPrefab == null)
            {
                isInitialized = allEffects.Count > 0;
                return isInitialized;
            }

            int createCount = Mathf.Min(initialEffectCreateCount, maxEffectCount);
            while (allEffects.Count < createCount)
            {
                GameObject effect = CreateEffect();
                inactiveEffects.Enqueue(effect);
            }

            isInitialized = true;
            return true;
        }

        private void EnsurePoolFolder()
        {
            if (poolFolder != null)
                return;

            Transform foundFolder = transform.Find(poolFolderName);
            if (foundFolder != null)
            {
                poolFolder = foundFolder;
                return;
            }

            GameObject folderObject = new GameObject(poolFolderName);
            poolFolder = folderObject.transform;
            poolFolder.SetParent(transform, false);
        }

        private void RegisterFolderChildren()
        {
            if (hasRegisteredFolderChildren || poolFolder == null)
                return;

            hasRegisteredFolderChildren = true;

            for (int i = 0; i < poolFolder.childCount; i++)
            {
                GameObject child = poolFolder.GetChild(i).gameObject;
                if (child == null || allEffects.Contains(child))
                    continue;

                child.SetActive(false);
                allEffects.Add(child);
                inactiveEffects.Enqueue(child);
            }
        }

        private GameObject GetAvailableEffect()
        {
            while (inactiveEffects.Count > 0)
            {
                GameObject effect = inactiveEffects.Dequeue();
                if (effect == null || effect.activeSelf)
                    continue;

                activeEffects.Add(effect);
                return effect;
            }

            if (createAdditionalEffectUntilMax && hitEffectPrefab != null && allEffects.Count < maxEffectCount)
            {
                GameObject effect = CreateEffect();
                activeEffects.Add(effect);
                return effect;
            }

            if (logSkipWhenPoolIsFull)
                Debug.LogWarning("[HitEffectPooling] 사용 가능한 HitEffect가 없어 이번 이펙트를 스킵합니다.", this);

            return null;
        }

        private GameObject CreateEffect()
        {
            GameObject effect = Instantiate(hitEffectPrefab, poolFolder);
            effect.name = $"{hitEffectPrefab.name}_Pool_{allEffects.Count + 1:00}";
            effect.SetActive(false);
            allEffects.Add(effect);
            return effect;
        }

        private IEnumerator ReturnAfterLifeTime(GameObject hitEffect, float lifeTime)
        {
            yield return new WaitForSeconds(lifeTime);
            ReturnEffect(hitEffect);
        }

        private void ReturnEffect(GameObject hitEffect)
        {
            if (hitEffect == null)
                return;

            if (!activeEffects.Remove(hitEffect))
                return;

            if (stopParticleOnReturn)
                StopParticles(hitEffect);

            hitEffect.SetActive(false);

            if (poolFolder != null)
                hitEffect.transform.SetParent(poolFolder, true);

            inactiveEffects.Enqueue(hitEffect);
        }

        private void PlayParticles(GameObject hitEffect)
        {
            ParticleSystem[] particleSystems = hitEffect.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem == null)
                    continue;

                if (clearParticleOnPlay)
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                particleSystem.Play(true);
            }
        }

        private void StopParticles(GameObject hitEffect)
        {
            ParticleSystem[] particleSystems = hitEffect.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem == null)
                    continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
