using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _00.ChoiHeesu._01.Script.Explosion
{
    [DisallowMultipleComponent]
    public class GrenadeSound : MonoBehaviour
    {
        private const string SfxFolder = "Assets/00.ChoiHeesu/Grenade/SFX";

        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] private float minPitch = 0.96f;
        [SerializeField] private float maxPitch = 1.04f;

        [Header("Pin")]
        [SerializeField] private AudioClip[] pinClips;
        [SerializeField, Range(0f, 1f)] private float pinVolume = 1f;
        [SerializeField] private bool playPinOnFuseStart = true;

        [Header("Throw")]
        [SerializeField] private AudioClip[] throwClips;
        [SerializeField, Range(0f, 1f)] private float throwVolume = 1f;
        [SerializeField] private bool playThrowOnFuseStart = true;

        [Header("Metal Impact")]
        [SerializeField] private AudioClip[] metalImpactClips;
        [SerializeField, Range(0f, 1f)] private float metalImpactVolume = 1f;
        [SerializeField] private float minImpactSpeed = 1.2f;
        [SerializeField] private float impactCooldown = 0.08f;
        [SerializeField] private AnimationCurve impactVolumeBySpeed = AnimationCurve.Linear(0f, 0f, 10f, 1f);

        [Header("Timer")]
        [SerializeField] private AudioClip timerClip;
        [SerializeField, Range(0f, 1f)] private float timerVolume = 0.85f;
        [SerializeField] private int timerRepeatCount = 6;
        [SerializeField] private float timerStartDelay = 0.15f;
        [SerializeField] private float timerEndPadding = 0.08f;
        [SerializeField] private AnimationCurve timerDelayWeightCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.18f));

        [Header("Explosion")]
        [SerializeField] private AudioClip[] explosionClips;
        [SerializeField, Range(0f, 1f)] private float explosionVolume = 1f;
        [SerializeField] private float detachedExplosionLifePadding = 0.25f;

#if UNITY_EDITOR
        [Header("Editor Auto Fill")]
        [SerializeField] private bool autoAssignClipsInEditor = true;
#endif

        private Coroutine timerCoroutine;
        private int lastPinIndex = -1;
        private int lastThrowIndex = -1;
        private int lastMetalImpactIndex = -1;
        private int lastExplosionIndex = -1;
        private float lastImpactTime = -999f;
        private bool hasPlayedExplosion;

        private void Awake()
        {
            CacheAudioSource();

#if UNITY_EDITOR
            AutoAssignClipsInEditor();
#endif
        }

        private void Reset()
        {
            CacheAudioSource();

#if UNITY_EDITOR
            AutoAssignClipsInEditor();
#endif
        }

        private void OnDisable()
        {
            StopFuseTimer();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
                return;

            PlayMetalImpact(collision.relativeVelocity.magnitude);
        }

        public void PlayFuse(float fuseDuration)
        {
            hasPlayedExplosion = false;
            CacheAudioSource();
            StopFuseTimer();

            if (playPinOnFuseStart)
                PlayPin();

            if (playThrowOnFuseStart)
                PlayThrow();

            if (timerClip != null && timerRepeatCount > 0)
                timerCoroutine = StartCoroutine(TimerRoutine(fuseDuration));
        }

        public void StopFuseTimer()
        {
            if (timerCoroutine == null)
                return;

            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        public void PlayPin()
        {
            CacheAudioSource();
            PlayRandomOneShot(audioSource, pinClips, ref lastPinIndex, pinVolume);
        }

        public void PlayThrow()
        {
            CacheAudioSource();
            PlayRandomOneShot(audioSource, throwClips, ref lastThrowIndex, throwVolume);
        }

        public void PlayMetalImpact(float impactSpeed)
        {
            if (Time.time < lastImpactTime + impactCooldown)
                return;

            if (impactSpeed < minImpactSpeed)
                return;

            CacheAudioSource();

            float speedVolume = impactVolumeBySpeed != null
                ? Mathf.Clamp01(impactVolumeBySpeed.Evaluate(impactSpeed))
                : 1f;

            if (speedVolume <= 0f)
                return;

            lastImpactTime = Time.time;
            PlayRandomOneShot(audioSource, metalImpactClips, ref lastMetalImpactIndex, metalImpactVolume * speedVolume);
        }

        public void PlayExplosion(Vector3 position)
        {
            if (hasPlayedExplosion)
                return;

            hasPlayedExplosion = true;
            StopFuseTimer();

            AudioClip explosionClip = GetRandomClip(explosionClips, ref lastExplosionIndex);
            if (explosionClip == null)
                return;

            PlayDetachedClip(
                explosionClip,
                position,
                explosionVolume,
                spatialBlend,
                GetRandomPitch(),
                detachedExplosionLifePadding);
        }

        private IEnumerator TimerRoutine(float fuseDuration)
        {
            float safeFuseDuration = Mathf.Max(fuseDuration, 0f);
            int safeRepeatCount = Mathf.Max(timerRepeatCount, 0);
            if (safeRepeatCount <= 0)
            {
                timerCoroutine = null;
                yield break;
            }

            float safeStartDelay = Mathf.Clamp(timerStartDelay, 0f, safeFuseDuration);
            float safeEndPadding = Mathf.Clamp(timerEndPadding, 0f, safeFuseDuration);
            float availableDuration = Mathf.Max(safeFuseDuration - safeStartDelay - safeEndPadding, 0f);

            if (safeStartDelay > 0f)
                yield return new WaitForSeconds(safeStartDelay);

            if (timerClip == null)
            {
                timerCoroutine = null;
                yield break;
            }

            if (safeRepeatCount == 1)
            {
                PlayTimerTick();
                timerCoroutine = null;
                yield break;
            }

            float[] intervalWeights = BuildTimerIntervalWeights(safeRepeatCount - 1);
            float weightSum = GetWeightSum(intervalWeights);

            for (int i = 0; i < safeRepeatCount; i++)
            {
                PlayTimerTick();

                if (i >= intervalWeights.Length)
                    continue;

                float waitTime = weightSum > 0f
                    ? availableDuration * (intervalWeights[i] / weightSum)
                    : 0f;

                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);
            }

            timerCoroutine = null;
        }

        private float[] BuildTimerIntervalWeights(int intervalCount)
        {
            intervalCount = Mathf.Max(intervalCount, 0);
            float[] weights = new float[intervalCount];
            if (intervalCount <= 0)
                return weights;

            for (int i = 0; i < intervalCount; i++)
            {
                float normalizedIndex = intervalCount <= 1 ? 1f : i / (intervalCount - 1f);
                float weight = timerDelayWeightCurve != null
                    ? timerDelayWeightCurve.Evaluate(normalizedIndex)
                    : 1f;

                weights[i] = Mathf.Max(weight, 0.0001f);
            }

            return weights;
        }

        private float GetWeightSum(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                sum += Mathf.Max(weights[i], 0f);
            }

            return sum;
        }

        private void PlayTimerTick()
        {
            if (audioSource == null || timerClip == null)
                return;

            audioSource.pitch = GetRandomPitch();
            audioSource.PlayOneShot(timerClip, timerVolume);
        }

        private void CacheAudioSource()
        {
            if (audioSource == null)
                TryGetComponent(out audioSource);

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource(audioSource, spatialBlend);
        }

        private void ConfigureAudioSource(AudioSource targetAudioSource, float targetSpatialBlend)
        {
            if (targetAudioSource == null)
                return;

            targetAudioSource.playOnAwake = false;
            targetAudioSource.loop = false;
            targetAudioSource.spatialBlend = Mathf.Clamp01(targetSpatialBlend);
        }

        private void PlayRandomOneShot(AudioSource targetAudioSource, AudioClip[] clips, ref int lastIndex, float volume)
        {
            if (targetAudioSource == null)
                return;

            AudioClip clip = GetRandomClip(clips, ref lastIndex);
            if (clip == null)
                return;

            targetAudioSource.pitch = GetRandomPitch();
            targetAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
        {
            int validClipCount = GetValidClipCount(clips);
            if (validClipCount <= 0)
                return null;

            for (int i = 0; i < 8; i++)
            {
                int candidateIndex = Random.Range(0, clips.Length);
                AudioClip candidateClip = clips[candidateIndex];

                if (candidateClip == null)
                    continue;

                if (validClipCount > 1 && candidateIndex == lastIndex)
                    continue;

                lastIndex = candidateIndex;
                return candidateClip;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                lastIndex = i;
                return clips[i];
            }

            return null;
        }

        private int GetValidClipCount(AudioClip[] clips)
        {
            if (clips == null)
                return 0;

            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    count++;
            }

            return count;
        }

        private float GetRandomPitch()
        {
            float safeMinPitch = Mathf.Max(Mathf.Min(minPitch, maxPitch), 0.01f);
            float safeMaxPitch = Mathf.Max(Mathf.Max(minPitch, maxPitch), 0.01f);
            return Random.Range(safeMinPitch, safeMaxPitch);
        }

        private void OnValidate()
        {
            spatialBlend = Mathf.Clamp01(spatialBlend);
            minPitch = Mathf.Max(minPitch, 0.01f);
            maxPitch = Mathf.Max(maxPitch, 0.01f);

            if (minPitch > maxPitch)
            {
                float temp = minPitch;
                minPitch = maxPitch;
                maxPitch = temp;
            }

            pinVolume = Mathf.Clamp01(pinVolume);
            throwVolume = Mathf.Clamp01(throwVolume);
            metalImpactVolume = Mathf.Clamp01(metalImpactVolume);
            timerVolume = Mathf.Clamp01(timerVolume);
            explosionVolume = Mathf.Clamp01(explosionVolume);
            minImpactSpeed = Mathf.Max(minImpactSpeed, 0f);
            impactCooldown = Mathf.Max(impactCooldown, 0f);
            timerRepeatCount = Mathf.Max(timerRepeatCount, 0);
            timerStartDelay = Mathf.Max(timerStartDelay, 0f);
            timerEndPadding = Mathf.Max(timerEndPadding, 0f);
            detachedExplosionLifePadding = Mathf.Max(detachedExplosionLifePadding, 0f);
            ConfigureAudioSource(audioSource, spatialBlend);
        }

        private static void PlayDetachedClip(
            AudioClip clip,
            Vector3 position,
            float volume,
            float spatialBlend,
            float pitch,
            float lifePadding)
        {
            if (clip == null)
                return;

            GameObject audioObject = new GameObject("GrenadeExplosionSound");
            audioObject.transform.position = position;

            AudioSource detachedAudioSource = audioObject.AddComponent<AudioSource>();
            detachedAudioSource.playOnAwake = false;
            detachedAudioSource.loop = false;
            detachedAudioSource.clip = clip;
            detachedAudioSource.volume = Mathf.Clamp01(volume);
            detachedAudioSource.spatialBlend = Mathf.Clamp01(spatialBlend);
            detachedAudioSource.pitch = Mathf.Max(pitch, 0.01f);
            detachedAudioSource.Play();

            float lifeTime = clip.length / detachedAudioSource.pitch + Mathf.Max(lifePadding, 0f);
            Destroy(audioObject, lifeTime);
        }

#if UNITY_EDITOR
        public void AutoAssignClipsInEditor()
        {
            if (!autoAssignClipsInEditor)
                return;

            pinClips = EnsureClips(pinClips,
                "Pin/SFX_PinGrenadev1",
                "Pin/SFX_PinGrenadev2",
                "Pin/SFX_PinGrenadev3",
                "Pin/SFX_PinGrenadev4",
                "Pin/SFX_PinGrenadev5");

            throwClips = EnsureClips(throwClips,
                "Throw/SFX_MagicItemThrowv1");

            metalImpactClips = EnsureClips(metalImpactClips,
                "Metal/metal_small_impact_01",
                "Metal/metal_small_impact_shake_01",
                "Metal/metal_small_impact_shake_02");

            if (timerClip == null)
                timerClip = LoadClip("Timer/SFX_ActivatedBombTimerSinglev1");

            explosionClips = EnsureClips(explosionClips,
                "Explosion/explosion_large_01",
                "Explosion/explosion_large_02",
                "Explosion/explosion_large_03",
                "Explosion/explosion_large_04",
                "Explosion/explosion_large_05",
                "Explosion/explosion_large_06");
        }

        private AudioClip[] EnsureClips(AudioClip[] currentClips, params string[] clipPaths)
        {
            if (HasExactClips(currentClips, clipPaths))
                return currentClips;

            return LoadClips(clipPaths);
        }

        private bool HasExactClips(AudioClip[] clips, string[] clipPaths)
        {
            if (clips == null || clipPaths == null)
                return false;

            if (clips.Length != clipPaths.Length)
                return false;

            for (int i = 0; i < clipPaths.Length; i++)
            {
                if (clips[i] == null)
                    return false;

                if (clips[i].name != GetClipNameFromPath(clipPaths[i]))
                    return false;
            }

            return true;
        }

        private AudioClip[] LoadClips(params string[] clipPaths)
        {
            AudioClip[] clips = new AudioClip[clipPaths.Length];

            for (int i = 0; i < clipPaths.Length; i++)
            {
                clips[i] = LoadClip(clipPaths[i]);
            }

            return clips;
        }

        private AudioClip LoadClip(string clipPath)
        {
            string assetPath = $"{SfxFolder}/{clipPath}.wav";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        private string GetClipNameFromPath(string clipPath)
        {
            int separatorIndex = clipPath.LastIndexOf('/');
            return separatorIndex >= 0 ? clipPath.Substring(separatorIndex + 1) : clipPath;
        }
#endif
    }
}
